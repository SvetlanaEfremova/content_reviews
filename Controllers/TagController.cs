using course_project.Services;
using Microsoft.AspNetCore.Mvc;

namespace course_project.Controllers
{
    public class TagController : Controller
    {
        private readonly TagService tagService;

        public TagController(TagService tagService)
        {
            this.tagService = tagService;
        }

        [HttpGet]
        public IActionResult GetAllTags()
        {
            var allTags = tagService.GetTagNamesList();
            return Json(allTags);
        }
    }
}
