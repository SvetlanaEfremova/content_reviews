using Microsoft.AspNetCore.Identity;

namespace course_project.Models
{
    public class User : IdentityUser
    {
        public string Name { get; set; }

        public bool IsBlocked { get; set; }

        public List<Review> Reviews { get;} = new List<Review>();

        public DateTime RegistrationDate { get; set; }

        public int Likes { get; set; }
    }
}
