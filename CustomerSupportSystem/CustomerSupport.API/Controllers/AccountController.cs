using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.Auth;
using CustomerSupport.Application.Interfaces.CurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("api/account")]

  // Every endpoint inside this Controller requires an authenticated User by default.
  // If the Access Token is:
  // - Missing
  // - Invalid
  // - Expired
  // ASP.NET Core rejects the request before the Action Method executes.
  [Authorize]
  public sealed class AccountController : ControllerBase
  {
    // Provides access to information about the currently authenticated User
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AccountController> _logger;

    // Dependencies are provided through ASP.NET Core Dependency Injection.
    public AccountController(
        ICurrentUserService currentUserService,
        ILogger<AccountController> logger)
    {
      _currentUserService = currentUserService;
      _logger = logger;
    }

    // GET: /api/account/me
    // Returns information about the currently authenticated User.
    [HttpGet("me")]
    public ActionResult<ApiResponse<CurrentUserResponseDTO>> GetCurrentUser()
    {
      _logger.LogInformation("Current user profile requested.");

      // At this point, the authenticated User context has been created.
      var currentUser = new CurrentUserResponseDTO
      {
        // UserId comes from ClaimTypes.NameIdentifier.
        UserId = _currentUserService.UserId!.Value,

        // FullName comes from ClaimTypes.Name.
        FullName = _currentUserService.FullName!,

        // Email comes from ClaimTypes.Email.
        Email = _currentUserService.Email!,

        // Role comes from ClaimTypes.Role.
        Role = _currentUserService.Role!.Value.ToString()
      };

      _logger.LogInformation("Current user profile retrieved successfully. UserId: {UserId}", currentUser.UserId);

      // Wrap the result inside the common ApiResponse<T>
      // structure used throughout the API.
      var response = ApiResponse<CurrentUserResponseDTO>
              .SuccessResponse(
                  currentUser,
                  "Current user information retrieved successfully.");

      // Return HTTP 200 OK with the current User information.
      return Ok(response);
    }
  }
}
