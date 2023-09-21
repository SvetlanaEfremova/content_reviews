using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace course_project.Models
{
    public class Comment
    {
        public Guid Id { get; set; }

        public Guid ReviewId { get; set; }

        public Review Review { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Text { get; set; }

        public string Author { get; set; }
    }
}
