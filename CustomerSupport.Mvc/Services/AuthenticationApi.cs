using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;

namespace CustomerSupport.Mvc.Services
{
    public sealed class AuthenticationApi : IAuthenticationApi
    {
        private readonly IApiClient _api;

        public AuthenticationApi(IApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<AuthResult>> LoginAsync(LoginForm request)
        {
            return _api.SendAsync<AuthResult>(HttpMethod.Post, "api/auth/login", new { request.Email, request.Password }, false);
        }

        public Task<ApiResult<AuthResult>> RegisterAsync(RegisterForm request)
        {
            return _api.SendAsync<AuthResult>(HttpMethod.Post, "api/auth/register", new {
                request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Password, request.ConfirmPassword
            }, false);
        }

        public Task<ApiResult<object>> LogoutAsync(string refreshToken)
        {
            return _api.SendAsync<object>(HttpMethod.Post, "api/auth/logout", new { refreshToken }, false);
        }

        public Task<ApiResult<CurrentUser>> ProfileAsync()
        {
            return _api.GetAsync<CurrentUser>("api/account/me");
        }
    }
}
