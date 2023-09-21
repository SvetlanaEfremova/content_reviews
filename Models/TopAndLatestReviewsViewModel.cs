namespace course_project.Models
{
    public class TopAndLatestReviewsViewModel
    {
        public List<Review> TopReviews { get; set; } = new List<Review>();

        public List<Review> LatestReviews { get; set; } = new List<Review>();
    }
}
