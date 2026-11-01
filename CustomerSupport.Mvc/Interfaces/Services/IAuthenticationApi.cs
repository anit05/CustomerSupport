using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;

namespace CustomerSupport.Mvc.Interfaces.Services
{
    public interface IAuthenticationApi
    {
        Task<ApiResult<AuthResult>> LoginAsync(LoginForm request);

        Task<ApiResult<AuthResult>> RegisterAsync(RegisterForm request);

        Task<ApiResult<object>> LogoutAsync(string refreshToken);

        Task<ApiResult<CurrentUser>> ProfileAsync();
    }
}
