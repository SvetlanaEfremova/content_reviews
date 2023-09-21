using course_project.Models;
using Microsoft.EntityFrameworkCore;

namespace course_project.Services
{
    public class CommentService
    {
        private readonly ApplicationDbContext dbcontext;

        public CommentService(ApplicationDbContext dbcontext) 
        { 
            this.dbcontext = dbcontext;
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
