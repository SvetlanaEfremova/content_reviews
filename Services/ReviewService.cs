using course_project.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace course_project.Services
{
    public class ReviewService
    {
        private readonly ApplicationDbContext dbcontext;

        private readonly UserManager<User> userManager;

        public ReviewService(UserManager<User> userManager, ApplicationDbContext dbcontext)
        {
            this.dbcontext = dbcontext;
            this.userManager = userManager;
        }

        public async Task AddNewReview(ReviewViewModel model, User user)
        {
            var review = CreateNewReviewObject(model, user);
            if (model.Tags.Count > 0) await AddTagsToReview(review, model.Tags);
            await dbcontext.Reviews.AddAsync(review);
            await dbcontext.SaveChangesAsync();
            IndexReviews();
        }

        public Review CreateNewReviewObject(ReviewViewModel model, User user)
        {
            return new Review
            {
                ContentName = model.ContentName,
                Category = model.Category,
                Text = model.Text,
                ImageUrl = model.ImageUrl ?? "",
                Rating = model.Rating,
                Author = user?.Name,
                Likes = 0,
                Dislikes = 0,
                User = user,
                Date = DateTime.Now,
            };
        }

        public async Task AddTagsToReview(Review review, List<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                var tag = await dbcontext.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                if (tag == null)
                {
                    tag = new Tag { Name = tagName };
                    await dbcontext.Tags.AddAsync(tag);
                }
                review.Tags.Add(tag);
            }
        }

        public void IndexReviews()
        {
            var luceneIndexDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuceneIndexes");
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            var indexWriter = new IndexWriter(FSDirectory.Open(luceneIndexDirectory), analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            foreach (var review in dbcontext.Reviews.Include(r => r.Tags).Include(r => r.Comments))
            {
                var document = new Document();
                document.Add(new Field("Id", review.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("ContentName", review.ContentName, Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("Text", review.Text, Field.Store.YES, Field.Index.ANALYZED));
                var tagsString = string.Join(" ", review.Tags.Select(t => t.Name));
                document.Add(new Field("Tags", tagsString, Field.Store.YES, Field.Index.ANALYZED));
                var commentsString = string.Join(" ", review.Comments.Select(c => c.Text));
                document.Add(new Field("Comments", commentsString, Field.Store.YES, Field.Index.ANALYZED));
                indexWriter.AddDocument(document);
            }
            indexWriter.Close();

        }

        public async Task<Review> MakeNewUserReviewByAdmin(string userId)
        {
            var reviewAuthor = await dbcontext.Users.FindAsync(userId);
            var review = new Review
            {
                ContentName = "",
                Category = "Video",
                Text = "",
                ImageUrl = "",
                Rating = 1,
                Author = reviewAuthor?.Name ?? "",
                Likes = 0,
                Dislikes = 0,
                User = reviewAuthor ?? null,
                Date = DateTime.Now,
            };
            await dbcontext.Reviews.AddAsync(review);
            await dbcontext.SaveChangesAsync();
            return review;
        }

        public async Task<List<Review>> GetTopReviews()
        {
            var topReviews = await dbcontext.Reviews
                .Include("User")
                .Include(r => r.Tags)
                .OrderByDescending(r => r.Likes - r.Dislikes)
                .Take(5)
                .ToListAsync();
            return topReviews;
        }

        public async Task<List<Review>> GetLatestReviews()
        {
            var latestReviews = await dbcontext.Reviews
                .Include("User")
                .Include(r => r.Tags)
                .OrderByDescending(r => r.Date)
                .Take(5)
                .ToListAsync();
            return latestReviews;
        }

        public async Task<List<Review>?> GetUserReviews(string userId, string sorting = "ContentNameAsc", string category = "All")
        {
            var reviews = await GetAllUserReviews(userId) ?? new List<Review>().AsQueryable();
            if (category != "All") reviews = GetReviewsFromCategory(reviews, category);
            reviews = SortReviews(reviews, sorting);
            return await reviews.ToListAsync();
        }

        public async Task<IQueryable<Review>> GetAllUserReviews(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var userEmail = user.Email;
                var reviews = dbcontext.Reviews.Include("User").Where(r => r.User.Email == userEmail);
                return reviews;
            }
            else return null;
        }

        public IQueryable<Review> GetReviewsFromCategory(IQueryable<Review> reviews, string category)
        {
            return reviews.Where(r => r.Category == category);
        }

        public IQueryable<Review> SortReviews(IQueryable<Review> query, string sorting)
        {
            ParseSortString(sorting, out string sortBy, out string direction);
            switch (sortBy.ToLower())
            {
                case "contentname":
                    query = direction.ToLower() == "asc" ? query.OrderBy(r => r.ContentName) : query.OrderByDescending(r => r.ContentName);
                    break;
                case "rating":
                    query = direction.ToLower() == "asc" ? query.OrderBy(r => r.Rating) : query.OrderByDescending(r => r.Rating);
                    break;
                case "reviewdate":
                    query = direction.ToLower() == "asc" ? query.OrderBy(r => r.Date) : query.OrderByDescending(r => r.Date);
                    break;
                default:
                    throw new ArgumentException("Invalid sort by parameter");
            }
            return query;
        }

        public List<Guid> GetSearchQueryResults(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery)) return new List<Guid>();
            var searcher = SetupSearcher();
            var hits = PerformSearch(searcher, searchQuery);
            var searchResults = ConvertHitsToDomainObjects(hits, searcher);
            CleanUpResources(searcher);
            return searchResults.Where(r => r != null).Select(r => r.Id).ToList();
        }

        private IndexSearcher SetupSearcher()
        {
            var luceneIndexDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuceneIndexes");
            var indexDirectory = FSDirectory.Open(luceneIndexDirectory);
            var reader = IndexReader.Open(indexDirectory, readOnly: true);
            return new IndexSearcher(reader);
        }

        private ScoreDoc[] PerformSearch(IndexSearcher searcher, string searchQuery)
        {
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, new[] { "Id", "ContentName", "Text", "Tags" }, analyzer);
            var query = parser.Parse(searchQuery);
            return searcher.Search(query, null, 10, Sort.RELEVANCE).ScoreDocs;
        }

        private List<Review> ConvertHitsToDomainObjects(ScoreDoc[] hits, IndexSearcher searcher)
        {
            List<Review> searchResults = new List<Review>();
            foreach (var hit in hits)
            {
                if (Guid.TryParse(searcher.Doc(hit.Doc).Get("Id"), out Guid id))
                {
                    var review = dbcontext.Reviews.Find(id);
                    if (review != null) searchResults.Add(review);
                }
            }
            return searchResults;
        }

        private void CleanUpResources(IndexSearcher searcher)
        {
            searcher.Dispose();
            searcher.IndexReader.Dispose();
        }

        public void ParseSortString(string sortString, out string sortBy, out string direction)
        {
            var match = Regex.Match(sortString, @"^(.+?)(Asc|Desc)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                sortBy = match.Groups[1].Value;
                direction = match.Groups[2].Value;
            }
            else throw new ArgumentException("Invalid sort string");
        }

        public async Task<Review> GetReviewById(Guid id)
        {
            return await dbcontext.Reviews
                    .Include(r => r.Tags)
                    .Include(r => r.Comments)
                    .Include(r => r.User)
                    .Include(r => r.Reactions)
                    .SingleOrDefaultAsync(r => r.Id == id);
        }

        public async Task ChangeReviewData(Review review, ChangeReviewViewModel model)
        {
            review.ContentName = model.ContentName;
            review.Category = model.Category;
            review.Text = model.Text;
            review.ImageUrl = model.ImageUrl ?? "";
            review.Rating = model.Rating;
            if (model.Tags.Count > 0) await AddTagsToReview(review, model.Tags);
            await dbcontext.SaveChangesAsync();
            IndexReviews();
        }

        public async Task<IActionResult> GetDeleteReviewResult(Guid id)
        {
            var review = await GetReviewById(id);
            if (review != null)
            {
                await DeleteReview(review);
                return new JsonResult(new { success = true });
            }
            else return new JsonResult(new { success = false, message = "review not found" });
        }

        public async Task DeleteReview (Review review)
        {
            foreach (var tag in review.Tags.ToList())
            {
                if (tag.Reviews.Count == 1) dbcontext.Tags.Remove(tag);
            }
            dbcontext.Reviews.Remove(review);
            await dbcontext.SaveChangesAsync();
            IndexReviews();
        }
    }
}
