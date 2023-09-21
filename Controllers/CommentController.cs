using course_project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using course_project.Services;

namespace course_project.Controllers
{
    public class CommentController : Controller
    {
        private readonly UserManager<User> userManager;

        private readonly CommentService commentService;

        private readonly IHubContext<CommentsHub> hubContext;

        public CommentController(UserManager<User> userManager, IHubContext<CommentsHub> hubContext, CommentService commentService)
        {
            this.userManager = userManager;
            this.hubContext = hubContext;
            this.commentService = commentService;
        }

        [HttpPost]
        public async Task<IActionResult> Add (CommentViewModel model)
        {
            var user = await userManager.FindByIdAsync(userManager.GetUserId(User));
            if (user == null) return Json(new { success = false });
            await commentService.CreateNewComment(model, user.Name);
            await hubContext.Clients.All.SendAsync("NewComment", new { reviewId = model.ReviewId, author = user.Name, text = model.Text });
            return Json(new { success = true, author = user.Name, text = model.Text });
        }
    }
}
