using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Interfaces.Services
{
    public interface IApiClient
    {
        Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default);

        Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? payload = null, bool authenticated = true, CancellationToken ct = default);

        Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file, CancellationToken ct = default);

        Task<ApiResult<DownloadedFile>> DownloadAsync(string path, CancellationToken ct = default);
    }
}
