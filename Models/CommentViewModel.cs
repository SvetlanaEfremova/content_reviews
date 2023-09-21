using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace course_project.Models
{
    public class CommentViewModel
    {
        public Guid ReviewId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Text { get; set; }
    }
}
