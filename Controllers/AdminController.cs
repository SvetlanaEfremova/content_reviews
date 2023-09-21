using course_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using course_project.Models;
using System;

namespace course_project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserService userService;

        private readonly ReviewService reviewService;

        private readonly UserManager<User> userManager;

        private readonly ApplicationDbContext dbcontext;

        public AdminController(UserService userService, ReviewService reviewService, UserManager<User> userManager, ApplicationDbContext dbcontext)
        {
            this.userService = userService;
            this.reviewService = reviewService;
            this.userManager = userManager;
            this.dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> UsersList([FromQuery] string sorting = "UserNameAsc")
        {
            var users = await userService.GetUsersList(sorting);
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> UserReviews(string userId, string sorting = "ContentNameAsc", string category = "All")
        {
            var userReviews = await reviewService.GetUserReviews(userId, sorting, category);
            var reviewAuthor = await dbcontext.Users.FindAsync(userId);
            if (userReviews != null)
            {
                var userReviewsModel = new UserReviewsViewModel { UserReviews = userReviews, UserName = reviewAuthor.Name, UserId = userId };
                return View("~/Views/Review/UserReviews.cshtml", userReviewsModel);
            }
            else return BadRequest("Reviews not found");
        }

        [HttpGet]
        public async Task<IActionResult> MakeUserReview([FromQuery] string userId)
        {
            var review = await reviewService.MakeNewUserReviewByAdmin(userId);
            return View("~/Views/Review/MakeReview.cshtml", review);
        }

        [HttpPost]
        public async Task<IActionResult> Block([FromBody] SelectedUsersModel model)
        {
            if (!userService.CheckSelectedUsers(model)) return new JsonResult(new { success = false, message = "No users selected" });
            var currentUserId = userManager.GetUserId(User);
            return await userService.UpdateSelectedUsersBlockStatus(model, true, currentUserId);
        }

        [HttpPost]
        public async Task<IActionResult> Unblock([FromBody] SelectedUsersModel model)
        {
            if (!userService.CheckSelectedUsers(model)) return new JsonResult(new { success = false, message = "No users selected" });
            var currentUserId = userManager.GetUserId(User);
            return await userService.UpdateSelectedUsersBlockStatus(model, false, currentUserId);
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] SelectedUsersModel model)
        {
            if (!userService.CheckSelectedUsers(model)) return new JsonResult(new { success = false, message = "No users selected" });
            var currentUserId = userManager.GetUserId(User);
            return await userService.DeleteSelectedUsers(model, currentUserId);
        }
    }
}
