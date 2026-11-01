
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustomerSupport.Mvc.Filters
{
    // Keeps MVC authentication consistent with the API token session.
    public sealed class ApiAuthenticationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            // STEP 1: Check whether MVC recognizes the user as logged in.
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                // Load server-side session information.
                await httpContext.Session.LoadAsync(
                    httpContext.RequestAborted);

                string? accessToken =
                    httpContext.Session.GetString("Api.Access");

                string? refreshToken =
                    httpContext.Session.GetString("Api.Refresh");

                // When both tokens are missing, the old MVC login
                // cannot be used to authenticate Web API requests.
                if (string.IsNullOrWhiteSpace(accessToken) &&
                    string.IsNullOrWhiteSpace(refreshToken))
                {
                    context.Result = await SignOutAndRedirectAsync(httpContext);

                    return;
                }
            }

            // STEP 2: Execute the requested MVC controller action.
            var executedContext = await next();

            // STEP 3: Check whether ApiClient reported an
            // unrecoverable HTTP 401 authentication failure.
            if (httpContext.Items.ContainsKey("Api.AuthFailed"))
            {
                // Replace the controller result with a Login redirect.
                executedContext.Result = await SignOutAndRedirectAsync(httpContext);
            }
        }

        // Clear the old authentication state and open the Login page.
        private static async Task<IActionResult> SignOutAndRedirectAsync(HttpContext httpContext)
        {
            // Remove both API tokens and the stored expiry.
            httpContext.Session.Clear();

            // Remove the MVC authentication cookie.
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Preserve the original page when the request was GET.
            // POST actions should return to a safe page after login.
            string returnUrl = HttpMethods.IsGet(httpContext.Request.Method)
                ? $"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}"
                : "/";

            string loginUrl =
                "/login?returnUrl=" +
                Uri.EscapeDataString(returnUrl);

            return new RedirectResult(loginUrl);
        }
    }
}
