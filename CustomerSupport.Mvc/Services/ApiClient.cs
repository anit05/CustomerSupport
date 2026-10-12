using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Interfaces.Services;

namespace CustomerSupport.Mvc.Services
{
    /// <summary>Server-side API gateway. Refresh tokens are never serialized into Razor HTML.</summary>
    public sealed class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _clients;
        private readonly IHttpContextAccessor _context;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(
            IHttpClientFactory clients,
            IHttpContextAccessor context,
            ILogger<ApiClient> logger)
        {
            _clients = clients;
            _context = context;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        private ISession Session
        {
            get
            {
                return _context.HttpContext?.Session
                    ?? throw new InvalidOperationException("No active MVC HTTP session.");
            }
        }

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            return SendAsync<T>(HttpMethod.Get, path, ct: ct);
        }

        public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path,
            object? payload = null, bool authenticated = true, CancellationToken ct = default)
        {
            try
            {
                using var response = await SendCore(method, path, payload, authenticated, ct);
                var content = await response.Content.ReadAsStringAsync(ct);
                ApiResponse<T>? envelope = null;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<ApiResponse<T>>(content, Json);
                    }
                    catch (JsonException)
                    {
                        // Non-JSON responses produce a safe message below.
                    }
                }
                if (response.IsSuccessStatusCode && envelope?.Success == true)
                    return new(true, envelope.Data, envelope.Message, (int)response.StatusCode);
                var reason = envelope?.Errors.Count > 0 ? string.Join(" ", envelope.Errors) : envelope?.Message;
                return ApiResult<T>.Failure(string.IsNullOrWhiteSpace(reason)
                    ? $"API request failed ({(int)response.StatusCode})." : reason, (int)response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Customer Support API connection error");
                return ApiResult<T>.Failure("Unable to reach the Customer Support API. Check that it is running.", 503);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                return ApiResult<T>.Failure("The Customer Support API timed out.", 504);
            }
        }

        private async Task<HttpResponseMessage> SendCore(HttpMethod method, string path,
            object? payload, bool authenticated, CancellationToken ct)
        {
            if (authenticated) 
                await EnsureFreshAccessTokenAsync(ct);

            var response = await SendOnce(method, path, payload, authenticated, ct);
            if (authenticated && response.StatusCode == HttpStatusCode.Unauthorized
                && !string.IsNullOrWhiteSpace(Session.GetString("Api.Refresh")))
            {
                response.Dispose();
                if (await TryRefreshAsync(ct))
                    return await SendOnce(method, path, payload, authenticated, ct);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
            return response;
        }

        private async Task<HttpResponseMessage> SendOnce(HttpMethod method, string path,
            object? payload, bool authenticated, CancellationToken ct)
        {
            var http = _clients.CreateClient("CustomerSupportApi");
            using var request = new HttpRequestMessage(method, path.TrimStart('/'));
            if (payload is not null) request.Content = JsonContent.Create(payload, options: Json);
            if (authenticated && Session.GetString("Api.Access") is { Length: > 0 } token)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        }

        private async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            var token = Session.GetString("Api.Refresh");
            if (string.IsNullOrWhiteSpace(token)) 
                return false;

            try
            {
                var client = _clients.CreateClient("CustomerSupportApi");
                using var response = await client.PostAsJsonAsync("api/auth/refresh-token", new { refreshToken = token }, Json, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }
                   

                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(Json, ct);
                if (envelope?.Success != true || envelope.Data is null)
                {
                    return false;
                }
                 
                Session.SetString("Api.Access", envelope.Data.AccessToken);
                Session.SetString("Api.Refresh", envelope.Data.RefreshToken);
                Session.SetString("Api.AccessExpiresAt", envelope.Data.AccessTokenExpiresAt.ToUniversalTime().ToString("O"));
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "API token refresh failed");
                return false;
            }
        }

        private async Task EnsureFreshAccessTokenAsync(CancellationToken ct)
        {
            // Refresh before expiration.
            // This also covers upload/download operations,
            // which cannot safely replay a stream after receiving HTTP 401.
            var expiry = Session.GetString("Api.AccessExpiresAt");
            if (DateTime.TryParse(expiry, out var expiresAt) && expiresAt.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(1))
            {
                await TryRefreshAsync(ct);
            }   
        }

        public async Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file, CancellationToken ct = default)
        {
            if (file.Length == 0 || file.Length > 10 * 1024 * 1024)
                return ApiResult<T>.Failure("Select a file up to 10 MB.", 400);
            try
            {
                await EnsureFreshAccessTokenAsync(ct);
                var client = _clients.CreateClient("CustomerSupportApi");
                using var form = new MultipartFormDataContent();
                await using var input = file.OpenReadStream();
                using var content = new StreamContent(input);
                content.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                form.Add(content, "file", Path.GetFileName(file.FileName));
                using var request = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/')) { Content = form };
                if (Session.GetString("Api.Access") is { Length: > 0 } token)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request, ct);
                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json, ct);
                if (response.IsSuccessStatusCode && envelope?.Success == true)
                    return new(true, envelope.Data, envelope.Message, (int)response.StatusCode);
                return ApiResult<T>.Failure(envelope?.Message ?? "Upload failed.", (int)response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Attachment upload API error");
                return ApiResult<T>.Failure("Unable to upload the attachment.", 503);
            }
        }

        public async Task<ApiResult<DownloadedFile>> DownloadAsync(string path, CancellationToken ct = default)
        {
            try
            {
                await EnsureFreshAccessTokenAsync(ct);
                var http = _clients.CreateClient("CustomerSupportApi");
                using var request = new HttpRequestMessage(HttpMethod.Get, path.TrimStart('/'));
                if (Session.GetString("Api.Access") is { Length: > 0 } token)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                    return ApiResult<DownloadedFile>.Failure("Attachment not available or access denied.", (int)response.StatusCode);
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var name = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "attachment";
                return new(true, new DownloadedFile(bytes, type, Path.GetFileName(name)), "Downloaded.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Attachment download API error");
                return ApiResult<DownloadedFile>.Failure("Unable to download the attachment.", 503);
            }
        }
    }
}
