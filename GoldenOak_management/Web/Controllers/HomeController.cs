using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using System.Diagnostics;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
	public class HomeController : Controller
	{
		private readonly IPropertyManagerDashboardService _dashboardService;

		public HomeController(IPropertyManagerDashboardService dashboardService)
		{
			_dashboardService = dashboardService;
		}

		public async Task<IActionResult> Index()
		{
			if (User.IsInRole("ADMIN"))
				return RedirectToAction("Index", "Admin");

			if (User.IsInRole("PROPERTY_MANAGER"))
				return RedirectToAction("Index", "PropertyManager");

			return Forbid();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult SetLanguage(string culture, string? returnUrl = null)
		{
			if (culture is not ("zh-CN" or "en-US"))
				culture = "zh-CN";

			Response.Cookies.Append(
				CookieRequestCultureProvider.DefaultCookieName,
				CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
				new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

			return LocalRedirect(returnUrl ?? Url.Action(nameof(Index)) ?? "/");
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
