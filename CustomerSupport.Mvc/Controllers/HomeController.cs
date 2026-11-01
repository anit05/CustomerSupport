using CustomerSupport.Mvc.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    public sealed class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true) return Redirect("/login");
            return Redirect(User.IsInRole("Administrator") ? "/admin/dashboard"
                : User.IsInRole("SupportExecutive") ? "/support/dashboard" : "/customer/dashboard");
        }

        [HttpGet("/access-denied")]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View("Error", new ErrorPage(403, "Access denied", "You don't have permission to view this page."));
        }

        [HttpGet("/not-found")]
        public IActionResult NotFoundPage()
        {
            Response.StatusCode = 404;
            return View("Error", new ErrorPage(404, "Page not found", "The page you're looking for doesn't exist."));
        }

        [HttpGet("/server-error")]
        public IActionResult ServerError()
        {
            Response.StatusCode = 500;
            return View("Error", new ErrorPage(500, "Something went wrong", "An unexpected error occurred. Please try again later."));
        }
    }
}
