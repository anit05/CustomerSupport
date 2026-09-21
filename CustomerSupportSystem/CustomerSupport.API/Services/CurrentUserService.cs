using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Domain.Enums;
using System.Security.Claims;

namespace CustomerSupport.API.Services
{
  public sealed class CurrentUserService : ICurrentUserService
  {
    // IHttpContextAccessor gives this service access to the HttpContext
    // associated with the current HTTP request.
    // HttpContext contains information such as:
    // - Request
    // - Response
    // - Connection
    // - Authenticated User
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    // Dependencies are provided through ASP.NET Core Dependency Injection.
    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CurrentUserService> logger)
    {
      _httpContextAccessor = httpContextAccessor;
      _logger = logger;
    }

    // HttpContext.User represents the ClaimsPrincipal for the current request.
    // When a valid JWT Access Token is received, the JWT Bearer middleware:
    // 1. Validates the token.
    // 2. Reads the Claims from the token.
    // 3. Creates a ClaimsPrincipal.
    // 4. Assigns it to HttpContext.User.

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    // Indicates whether ASP.NET Core successfully authenticated the current request.
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
      get
      {
        // Read the User ID from the NameIdentifier claim.
        // This claim was added when JwtTokenService created the Access Token:
        var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Claims store values as strings.
        // Convert the User ID claim back into an integer.
        // If the claim is missing or contains an invalid value,
        // return null instead of throwing an exception here.
        return int.TryParse(value, out var userId) ? userId : null;
      }
    }

    // Read the User's full name from ClaimTypes.Name.
    public string? FullName => User?.FindFirstValue(ClaimTypes.Name);

    // Read the authenticated User's email address
    // directly from the Email claim in the JWT.
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public RoleType? Role
    {
      get
      {
        // Read the Role claim stored in the Access Token.
        var value = User?.FindFirstValue(ClaimTypes.Role);

        // JWT claims are strings, but the Application layer
        // should work with our strongly typed RoleType enum.

        // Convert the claim back into RoleType.
        // If the claim is missing or invalid, return null.
        return Enum.TryParse<RoleType>(value, out var role) ? role : null;
      }
    }
  }
}
