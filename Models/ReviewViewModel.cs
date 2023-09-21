using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace course_project.Models
{
    public class ReviewViewModel
    {
        public string ContentName { get; set; }

        public string Category { get; set; }

        public List<string> Tags { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Text { get; set; }

        public int Rating { get; set; }

        public string ImageUrl { get; set; }

        public ReviewViewModel()
        {
            Tags = new List<string>();
        }

        public string UserId { get; set; }
    }
}
