using CustomerSupport.API.Services;
using CustomerSupport.Application.Interfaces.CurrentUser;

namespace CustomerSupport.API.Extensions
{
  public static class AuthorizationExtensions
  {
    // Registers the services required for Authorization and Current User access.
    // Because this is an extension method for IServiceCollection,
    // it can be called from Program.cs like this:
    // builder.Services.AddApiAuthorization();
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
      // Register ASP.NET Core Authorization services.
      // This enables authorization features such as:
      // [Authorize]
      // [Authorize(Roles = "Administrator")]
      // [AllowAnonymous]
      //
      // Authentication determines WHO the User is.
      // Authorization determines WHAT the User is allowed to do.
      services.AddAuthorization();

      // Register IHttpContextAccessor.
      // CurrentUserService needs access to the current HttpContext
      // so that it can read: HttpContext.User

      // HttpContext.User contains the ClaimsPrincipal created
      // after JWT Bearer Authentication successfully validates
      // the incoming Access Token.

      // Through those Claims, CurrentUserService can retrieve:
      // - User ID
      // - Full Name
      // - Email
      // - Role
      services.AddHttpContextAccessor();

      // Register CurrentUserService as the implementation of ICurrentUserService.
      // The Application layer depends only on ICurrentUserService.
      // It does not directly depend on:
      // - HttpContext
      // - IHttpContextAccessor
      // - ClaimsPrincipal
      // - ASP.NET Core
      // This keeps the Application layer independent of web-specific infrastructure.
      services.AddScoped<ICurrentUserService, CurrentUserService>();

      // Return IServiceCollection so Program.cs can continue
      // registering additional services using method chaining.
      return services;
    }
  }
}
