using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    [Authorize]
    public sealed class AccountController : Controller
    {
        private readonly IAuthenticationApi _api;

        public AccountController(IAuthenticationApi api)
        {
            _api = api;
        }

        [HttpGet("/account")]
        public async Task<IActionResult> Index()
        {
            var result = await _api.ProfileAsync();
            if (!result.Ok || result.Data is null)
            {
                TempData["Error"] = result.Message;
                return Redirect("/");
            }
            return View(result.Data);
        }
    }
}
