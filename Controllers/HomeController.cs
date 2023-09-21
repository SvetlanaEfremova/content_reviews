using course_project.Models;
using course_project.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace course_project.Controllers
{
    public class HomeController : Controller
    {

        private readonly ReviewService reviewService;

        public HomeController(ReviewService reviewService)
        {
            this.reviewService = reviewService;
        }

        public async Task <IActionResult> Index()
        {
            var topAndLatestReviews = new TopAndLatestReviewsViewModel
            {
                TopReviews = await reviewService.GetTopReviews(),
                LatestReviews = await reviewService.GetLatestReviews()
            };
            return View(topAndLatestReviews);
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl, string searchQuery, string reviewId)
        {
            SetCookie(culture);
            if (!string.IsNullOrEmpty(searchQuery)) returnUrl = $"{returnUrl}?searchQuery={searchQuery}";
            if (!string.IsNullOrEmpty(reviewId)) returnUrl = $"{returnUrl}?reviewId={reviewId}";
            return LocalRedirect(returnUrl);
        }

        private void SetCookie(string culture)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}