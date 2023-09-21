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
            await reviewService.AddNewReview(model, user);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Reviews(string searchQuery)
        {
            var reviewIds = reviewService.GetSearchQueryResults(searchQuery);
            var reviews = dbcontext.Reviews.Where(r => reviewIds.Contains(r.Id)).Include(r => r.Comments).Include(r => r.Tags).Include(r => r.User).ToList();
            return View(reviews);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserReviews(string sorting = "ContentNameAsc", string category = "All")
        {
            var user = await userManager.GetUserAsync(User);
            var userReviews = await reviewService.GetUserReviews(user.Id, sorting, category);
            var userReviewsModel = new UserReviewsViewModel { UserReviews = userReviews, UserName = user.Name, UserId = user.Id };
            return View(userReviewsModel);
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
        public async Task<IActionResult> ViewReview(string reviewId)
        {
            if (Guid.TryParse(reviewId, out Guid id))
            {
                var review = await reviewService.GetReviewById(id);
                if (review != null)
                    return View(review);
                else return BadRequest("Review not found");
            }
            else return BadRequest("Incorrect review Id");
        }

        [Authorize]
        public async Task<IActionResult> MakeReview(string reviewId)
        {
            if (reviewId == null) return View();
            else if (reviewId != null && Guid.TryParse(reviewId, out Guid id))
            {
                var review = await reviewService.GetReviewById(id);
                if (review != null) return View(review);
                else return BadRequest("Review not found");
            }
            else return BadRequest("Incorrect review Id");
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteReview(string reviewId)
        {
            if (Guid.TryParse(reviewId, out Guid id)) return await reviewService.GetDeleteReviewResult(id);
            else return BadRequest("Incorrect review Id");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeReviewData(ChangeReviewViewModel model)
        {
            var review = await reviewService.GetReviewById(model.ReviewId);
            if (review != null)
            {
                await reviewService.ChangeReviewData(review, model);
                return RedirectToAction("Index","Home");
            }
            else return BadRequest("Review not found");
        }
    }
}
