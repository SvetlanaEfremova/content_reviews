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
            var review = await reviewService.GetReviewById(model.ReviewId);
            if (review != null)
            {
                var comment = new Comment
                {
                    ReviewId = model.ReviewId,
                    Review = review,
                    Text = model.Text,
                    Author = userName,
                };
                review.Comments.Add(comment);
                await SaveCommentInDB(comment);
                reviewService.IndexReviews();
            }
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
