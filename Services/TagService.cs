using course_project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace course_project.Services
{
    public class TagService : Controller
    {
        private readonly ApplicationDbContext dbcontext;

        public TagService(ApplicationDbContext dbcontext)
        {
            this.dbcontext = dbcontext;
        }

        public List<string> GetTagNamesList()
        {
            var allTagNames = new List<string>();
            var tags = dbcontext.Tags.ToList();
            foreach (var tag in tags)
                allTagNames.Add(tag.Name);
            return allTagNames;
        }
    }
}
