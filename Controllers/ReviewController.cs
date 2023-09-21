using course_project.Models;
using course_project.Services;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Messages;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace course_project.Controllers
{
    public class ReviewController : Controller
    {
        private readonly SignInManager<User> signInManager;

        private readonly UserManager<User> userManager;

        private readonly ApplicationDbContext dbcontext;

        private readonly CloudinaryService cloudinaryService;

        private readonly ReviewService reviewService;

        public ReviewController(SignInManager<User> signInManager, UserManager<User> userManager, ApplicationDbContext dbcontext, CloudinaryService cloudinaryService, ReviewService reviewService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.dbcontext = dbcontext;
            this.cloudinaryService = cloudinaryService;
            this.reviewService = reviewService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(ReviewViewModel model) 
        {
            var user = await userManager.GetUserAsync(User);
            var userName = user?.Name;
            var review = new Review
            {
                ContentName = model.ContentName,
                Category = model.Category,
                Text = model.Text,
                ImageUrl = model.ImageUrl ?? "",
                Rating = model.Rating,
                Author = userName,
                Likes = 0,
                Dislikes = 0,
                User = user,
                Date = DateTime.Now,
            };
            if (model.Tags.Count > 0)
                foreach (var tagName in model.Tags)
                {
                    var tag = dbcontext.Tags.FirstOrDefault(t => t.Name == tagName);

                    if (tag == null)
                    {
                        tag = new Tag { Name = tagName };
                        dbcontext.Tags.Add(tag);
                    }

                    review.Tags.Add(tag);
                }
            dbcontext.Reviews.Add(review);
            dbcontext.SaveChanges();
            IndexReviews();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Reviews(string searchQuery)
        {
            var reviewIds = GetSearchQueryResults(searchQuery);
            var reviews = dbcontext.Reviews.Where(r => reviewIds.Contains(r.Id)).Include(r => r.Comments).Include(r => r.Tags).Include(r => r.User).ToList();
            return View(reviews);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserReviews(string sorting = "ContentNameAsc", string category = "All")
        {
            var user = await userManager.GetUserAsync(User);
            var userEmail = user.Email;
            var reviews = dbcontext.Reviews.Include("User").Where(r => r.User.Email == userEmail);
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                reviews = reviews.Include("User").Where(r => r.Category == category);
            }
            reviews = reviewService.SortReviews(reviews,sorting);
            var reviewsList = await reviews.ToListAsync();
            var userReviews = new UserReviewsViewModel { UserReviews = reviewsList, UserName = user.Name, UserId = user.Id };
            return View(userReviews);
        }

        public void IndexReviews()
        {
            var luceneIndexDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuceneIndexes");
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            var indexWriter = new IndexWriter(FSDirectory.Open(luceneIndexDirectory), analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            
            foreach (var review in dbcontext.Reviews.Include(r => r.Tags))
            {
                var document = new Document();
                document.Add(new Field("Id", review.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("ContentName", review.ContentName, Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("Text", review.Text, Field.Store.YES, Field.Index.ANALYZED));
                var tagsString = string.Join(" ", review.Tags.Select(t => t.Name));
                document.Add(new Field("Tags", tagsString, Field.Store.YES, Field.Index.ANALYZED));
                indexWriter.AddDocument(document);
            }
            indexWriter.Close();
            
        }
        public List<Guid> GetSearchQueryResults(string searchQuery)
        {
            var luceneIndexDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuceneIndexes");
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            var indexDirectory = FSDirectory.Open(luceneIndexDirectory);
            var reader = IndexReader.Open(indexDirectory, readOnly: true);
            var searcher = new IndexSearcher(reader);
            var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, new[] { "Id","ContentName", "Text", "Tags" }, analyzer);
            if (string.IsNullOrEmpty(searchQuery))
            {
                return new List<Guid>();
            }
            var query = parser.Parse(searchQuery);
            var hits = searcher.Search(query, null, 10, Sort.RELEVANCE).ScoreDocs;
            var searchResults = hits.Select(hit =>
            {
                var doc = searcher.Doc(hit.Doc);
                var idString = doc.Get("Id");
                if (Guid.TryParse(idString, out Guid id))
                {
                    return dbcontext.Reviews.Find(id);
                }
                else
                {
                    return null;
                }
            }).ToList();

            searcher.Dispose();
            reader.Dispose();
            if (searchResults == null)
            {
                return new List<Guid>();
            }
            return searchResults.Where(r => r != null).Select(r => r.Id).ToList();

        }

        [HttpPost("/upload")]
        public IActionResult Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var url = cloudinaryService.UploadImage(stream);
                Console.WriteLine(url);
                return Ok(url);
            }
            return BadRequest("Error ocurred while downloading image");
        }

        [HttpGet]
        public IActionResult ViewReview(string reviewId)
        {
            if (Guid.TryParse(reviewId, out Guid id))
            {
                var review = dbcontext.Reviews
                    .Include(r => r.Tags)
                    .Include(r => r.Comments)
                    .SingleOrDefault(r => r.Id == id);
                if (review != null)
                    return View(review);
                else return BadRequest("Review not found");
            }
            else return BadRequest("Incorrect review Id");
        }

        [Authorize]
        public async Task<IActionResult> MakeReview(string reviewId)
        {
            if (reviewId == null) 
            {
                return View();
            }
            else if (reviewId != null && Guid.TryParse(reviewId, out Guid id))
            {
                var review = dbcontext.Reviews.Find(id);
                if (review != null)
                    return View(review);
                else return BadRequest("Review not found");
            }
            else return BadRequest("Incorrect review Id");
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteReview(string reviewId)
        {
            if (Guid.TryParse(reviewId, out Guid id))
            {
                var review = await dbcontext.Reviews
                    .Include(r => r.Tags)
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (review != null)
                {
                    foreach (var tag in review.Tags.ToList())
                    {
                        if (tag.Reviews.Count == 1)
                        {
                            dbcontext.Tags.Remove(tag);
                        }
                    }

                    dbcontext.Reviews.Remove(review);
                    await dbcontext.SaveChangesAsync();
                    IndexReviews();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "review not found" });
                }
            }
            else
            {
                return BadRequest("Incorrect review Id");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeReviewData(ChangeReviewViewModel model)
        {
            var review = await dbcontext.Reviews.FindAsync(model.ReviewId);
            if (review != null)
            {
                review.ContentName = model.ContentName;
                review.Category = model.Category;
                review.Text = model.Text;
                review.ImageUrl = model.ImageUrl ?? "";
                review.Rating = model.Rating;
                if (model.Tags.Count > 0)
                    foreach (var tagName in model.Tags)
                    {
                        var tag = dbcontext.Tags.FirstOrDefault(t => t.Name == tagName);
                        if (tag == null)
                        {
                            tag = new Tag { Name = tagName };
                            dbcontext.Tags.Add(tag);
                        }
                        var reviewWithTags = dbcontext.Reviews
                            .Include(r => r.Tags)
                            .FirstOrDefault(r => r.Id == review.Id);
                        var tagsForReview = reviewWithTags?.Tags;
                        if (!tagsForReview.Any(t => t.Id == tag.Id))
                            review.Tags.Add(tag);
                    }
                dbcontext.SaveChanges();
                IndexReviews();
                return RedirectToAction("Index","Home");
            }
            else return BadRequest("Review not found");
            
        }
    }
}
