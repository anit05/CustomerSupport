using System.Security.Claims;
using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Mvc.Controllers
{
    public sealed class AuthController : Controller
    {
        private readonly IAuthenticationApi _api;

        public AuthController(IAuthenticationApi api)
        {
            _api = api;
        }

        [AllowAnonymous, HttpGet("/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            return User.Identity?.IsAuthenticated == true
                ? Redirect("/")
                : View(new LoginForm { ReturnUrl = returnUrl });
        }

        [AllowAnonymous, HttpPost("/login")]
        public async Task<IActionResult> Login(LoginForm model)
        {
            if (!ModelState.IsValid) return View(model);
            var result = await _api.LoginAsync(model);
            if (!result.Ok || result.Data is null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }
            await StartSessionAsync(result.Data);
            return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/");
        }

        [AllowAnonymous, HttpGet("/register")]
        public IActionResult Register()
        {
            return User.Identity?.IsAuthenticated == true ? Redirect("/") : View(new RegisterForm());
        }

        [AllowAnonymous, HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterForm model)
        {
            if (!ModelState.IsValid) return View(model);
            var result = await _api.RegisterAsync(model);
            if (!result.Ok || result.Data is null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }
            await StartSessionAsync(result.Data);
            return Redirect("/customer/dashboard");
        }

        private async Task StartSessionAsync(AuthResult auth)
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("Api.Access", auth.AccessToken);
            HttpContext.Session.SetString("Api.Refresh", auth.RefreshToken);
            HttpContext.Session.SetString("Api.AccessExpiresAt", auth.AccessTokenExpiresAt.ToUniversalTime().ToString("O"));
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, auth.User.Id.ToString()),
                new(ClaimTypes.Name, auth.User.FullName),
                new(ClaimTypes.Email, auth.User.Email),
                new(ClaimTypes.Role, auth.User.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity), new AuthenticationProperties {
                    IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }

        [Authorize, HttpPost("/logout")]
        public async Task<IActionResult> Logout()
        {
            var refresh = HttpContext.Session.GetString("Api.Refresh");
            if (!string.IsNullOrWhiteSpace(refresh)) await _api.LogoutAsync(refresh);
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }
    }
}
