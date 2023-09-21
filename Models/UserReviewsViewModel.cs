namespace course_project.Models
{
    public class UserReviewsViewModel
    {
        public List<Review> UserReviews { get; set; } = new List<Review>();

        public string UserName { get; set; }

        public string UserId { get; set; }
    }
}
