using course_project.Models;
using Microsoft.EntityFrameworkCore;

namespace course_project.Services
{
    public class CommentService
    {
        private readonly ApplicationDbContext dbcontext;

        private readonly ReviewService reviewService;

        public CommentService(ApplicationDbContext dbcontext, ReviewService reviewService) 
        { 
            this.dbcontext = dbcontext;
            this.reviewService = reviewService;
        }

        public async Task CreateNewComment(CommentViewModel model, string userName)
        {
            var comment = new Comment
            {
                ReviewId = model.ReviewId,
                Text = model.Text,
                Author = userName,
            };
            await SaveCommentInDB(comment);
            reviewService.IndexReviews();
        }

        private async Task SaveCommentInDB(Comment comment)
        {
            var review = dbcontext.Reviews.Include(r => r.Comments).FirstOrDefault(r => r.Id == comment.ReviewId);
            review?.Comments.Add(comment);
            await dbcontext.Comments.AddAsync(comment);
            await dbcontext.SaveChangesAsync();
        }
    }
}
