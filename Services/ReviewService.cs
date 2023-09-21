using course_project.Models;
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
            if (category != "All")
            {
                reviews = GetReviewsFromCategory(reviews, category);
            }
            reviews = SortReviews(reviews, sorting);
            return reviews.ToList();
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
            else
                return null;
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

        public void ParseSortString(string sortString, out string sortBy, out string direction)
        {
            var match = Regex.Match(sortString, @"^(.+?)(Asc|Desc)$", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                sortBy = match.Groups[1].Value;
                direction = match.Groups[2].Value;
            }
            else
            {
                throw new ArgumentException("Invalid sort string");
            }
        }

        public async Task<Review> GetReviewById(Guid id)
        {
            return await dbcontext.Reviews
                .Include(r => r.Reactions)
                .Include(r => r.User)
                .SingleOrDefaultAsync(r => r.Id == id);
        }

    }
}
