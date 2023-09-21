using course_project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace course_project.Services
{
    public class ReactionService
    {
        private readonly ApplicationDbContext dbcontext;

        public ReactionService(ApplicationDbContext dbcontext) 
        { 
            this.dbcontext = dbcontext;
        }

        public async Task<IActionResult> AddReactionToReview(AddReactionViewModel model, Review review, string userId)
        {
            var reactionType = model.Action == "like" ? ReactionType.Like : ReactionType.Dislike;
            ProcessUserReaction(review, userId, reactionType);
            await dbcontext.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        private void ProcessUserReaction(Review review, string userId, ReactionType reactionType)
        {
            var existingReaction = review.Reactions.FirstOrDefault(r => r.UserId == userId);
            if (existingReaction != null) HandleExistingReaction(review, existingReaction, reactionType);
            else AddNewReaction(review, userId, reactionType);
        }

        private void HandleExistingReaction(Review review, Reaction existingReaction, ReactionType reactionType)
        {
            if (existingReaction.Type == reactionType) RemoveReaction(review, existingReaction, reactionType);
            else ChangeReactionType(review, existingReaction, reactionType);
        }

        private void RemoveReaction(Review review, Reaction existingReaction, ReactionType reactionType)
        {
            review.Reactions.Remove(existingReaction);
            dbcontext.Reactions.Remove(existingReaction);
            if (reactionType == ReactionType.Like)
            {
                review.Likes--;
                review.User.Likes--;
            }
            else
            {
                review.Dislikes--;
                review.User.Likes++;
            }
        }

        private void ChangeReactionType(Review review, Reaction existingReaction, ReactionType reactionType)
        {
            if (reactionType == ReactionType.Like)
            {
                review.Likes++;
                review.Dislikes--;
                review.User.Likes++;
            }
            else
            {
                review.Likes--;
                review.Dislikes++;
                review.User.Likes--;
            }
            existingReaction.Type = reactionType;
        }

        private void AddNewReaction(Review review, string userId, ReactionType reactionType)
        {
            var reaction = new Reaction
            {
                ReviewId = review.Id,
                UserId = userId,
                Type = reactionType
            };
            if (reactionType == ReactionType.Like)
            {
                review.Likes++;
                review.User.Likes++;
            }
            else
            {
                review.Dislikes++;
                review.User.Likes--;
            }
            review.Reactions.Add(reaction);
            dbcontext.Reactions.Add(reaction);
        }
    }
}
