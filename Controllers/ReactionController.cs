using course_project.Models;
using course_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace course_project.Controllers
{
    public class ReactionController : Controller
    {
        private readonly ReactionService reactionService;

        private readonly ReviewService reviewService;

        public ReactionController(ReactionService reactionService, ReviewService reviewService)
        {
            this.reactionService = reactionService;
            this.reviewService = reviewService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReactionToReview([FromBody] AddReactionViewModel model)
        {
            if (!Guid.TryParse(model.ReviewId, out Guid id)) return Json(new { success = false, message = "Invalid ReviewId" });
            var review = await reviewService.GetReviewById(id);
            if (review == null) return Json(new { success = false, message = "Review not found" });
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await reactionService.AddReactionToReview(model, review, userId);
        }
    }
}
