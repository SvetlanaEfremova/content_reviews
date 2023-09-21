namespace course_project.Models
{
    public class Reaction
    {
        public Guid Id { get; set; }

        public Guid ReviewId { get; set; }

        public Review Review { get; set; } = null!;

        public string UserId { get; set; }

        public ReactionType Type { get; set; }
    }
}
