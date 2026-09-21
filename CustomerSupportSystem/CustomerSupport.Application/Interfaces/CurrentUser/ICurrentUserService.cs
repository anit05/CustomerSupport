using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Interfaces.CurrentUser
{
  public interface ICurrentUserService
  {
    // Indicates whether ASP.NET Core successfully authenticated the current request.
    // true  -> a valid authenticated User is available.
    // false -> the request is anonymous or authentication failed.
    bool IsAuthenticated { get; }

    // Returns the authenticated User's unique ID.
    // This value is normally read from: ClaimTypes.NameIdentifier
    int? UserId { get; }

    // Returns the authenticated User's full name.
    // This value is normally read from: ClaimTypes.Name
    string? FullName { get; }

    // Returns the authenticated User's email address.
    // This value is normally read from: ClaimTypes.Email
    string? Email { get; }

    // Returns the authenticated User's application Role.
    // This value is normally read from: ClaimTypes.Role
    RoleType? Role { get; }
  }
}
