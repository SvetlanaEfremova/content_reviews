using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace course_project.Models
{
    public class Review
    {
        public Guid Id { get; set; }

        public string ContentName { get; set; }

        public string Category { get; set; }

        public List<Tag> Tags { get; } = new();

        [Column(TypeName = "nvarchar(max)")]
        public string Text { get; set; }

        public string ImageUrl { get; set; }

        public int Rating { get; set; }

        public string Author { get; set; }

        public uint Likes { get; set; }

        public uint Dislikes { get; set; }

        public List<Comment> Comments { get; } = new List<Comment>();

        public string UserId { get; set; }

        public User User { get; set; }

        public List<Reaction> Reactions { get; } = new();

        public DateTime Date { get; set; }

    }
}
