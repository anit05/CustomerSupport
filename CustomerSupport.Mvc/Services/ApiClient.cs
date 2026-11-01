
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Interfaces.Services;

namespace CustomerSupport.Mvc.Services
{
    // Sends authenticated requests to CustomerSupport.API.
    // API tokens are stored in the MVC server-side session.
    public sealed class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _clients;
        private readonly IHttpContextAccessor _context;
        private readonly ILogger<ApiClient> _logger;

        private static readonly JsonSerializerOptions Json =
            new(JsonSerializerDefaults.Web);

        public ApiClient(
            IHttpClientFactory clients,
            IHttpContextAccessor context,
            ILogger<ApiClient> logger)
        {
            _clients = clients;
            _context = context;
            _logger = logger;
        }

        // Access the current user's MVC session.
        private ISession Session =>
            _context.HttpContext?.Session
            ?? throw new InvalidOperationException(
                "No active MVC HTTP session.");

        // Send an authenticated GET request.
        public Task<ApiResult<T>> GetAsync<T>(
            string path,
            CancellationToken ct = default)
        {
            return SendAsync<T>(
                HttpMethod.Get,
                path,
                ct: ct);
        }

        // Send a request and convert its response into ApiResult<T>.
        public async Task<ApiResult<T>> SendAsync<T>(
            HttpMethod method,
            string path,
            object? payload = null,
            bool authenticated = true,
            CancellationToken ct = default)
        {
            try
            {
                using var response = await SendCore(
                    method,
                    path,
                    payload,
                    authenticated,
                    ct);

                // Notify MVC when an authenticated request still
                // returns HTTP 401 after the refresh attempt.
                if (authenticated &&
                    response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    MarkAuthenticationFailure();
                }

                string content =
                    await response.Content.ReadAsStringAsync(ct);

                ApiResponse<T>? envelope = null;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        envelope =
                            JsonSerializer.Deserialize<ApiResponse<T>>(
                                content,
                                Json);
                    }
                    catch (JsonException)
                    {
                        // Non-JSON responses use a safe error message.
                    }
                }

                if (response.IsSuccessStatusCode &&
                    envelope?.Success == true)
                {
                    return new ApiResult<T>(
                        true,
                        envelope.Data,
                        envelope.Message,
                        (int)response.StatusCode);
                }

                string? reason =
                    envelope?.Errors.Count > 0
                        ? string.Join(" ", envelope.Errors)
                        : envelope?.Message;

                return ApiResult<T>.Failure(
                    string.IsNullOrWhiteSpace(reason)
                        ? $"API request failed ({(int)response.StatusCode})."
                        : reason,
                    (int)response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                // Connection problems must not automatically log users out.
                _logger.LogWarning(
                    ex,
                    "Customer Support API connection error");

                return ApiResult<T>.Failure(
                    "Unable to reach the Customer Support API.",
                    503);
            }
            catch (TaskCanceledException)
                when (!ct.IsCancellationRequested)
            {
                return ApiResult<T>.Failure(
                    "The Customer Support API timed out.",
                    504);
            }
        }

        // Send the request and retry once when token refresh succeeds.
        private async Task<HttpResponseMessage> SendCore(
            HttpMethod method,
            string path,
            object? payload,
            bool authenticated,
            CancellationToken ct)
        {
            if (authenticated)
            {
                // Refresh an expired or missing Access Token if possible.
                await EnsureFreshAccessTokenAsync(ct);
            }

            // Send the request with the current token.
            var response = await SendOnce(
                method,
                path,
                payload,
                authenticated,
                ct);

            // Anonymous calls do not require token refresh.
            if (!authenticated ||
                response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            // The Access Token was rejected.
            response.Dispose();

            // Attempt refresh-token rotation and retry once.
            if (await TryRefreshAsync(ct))
            {
                return await SendOnce(
                    method,
                    path,
                    payload,
                    authenticated,
                    ct);
            }

            // Refresh failed or no Refresh Token exists.
            // SendAsync will notify the global MVC filter.
            return new HttpResponseMessage(
                HttpStatusCode.Unauthorized);
        }

        // Send one HTTP request with the current Access Token.
        private async Task<HttpResponseMessage> SendOnce(
            HttpMethod method,
            string path,
            object? payload,
            bool authenticated,
            CancellationToken ct)
        {
            var http = _clients.CreateClient(
                "CustomerSupportApi");

            using var request = new HttpRequestMessage(
                method,
                path.TrimStart('/'));

            if (payload is not null)
            {
                request.Content = JsonContent.Create(
                    payload,
                    options: Json);
            }

            if (authenticated &&
                Session.GetString("Api.Access") is
                { Length: > 0 } accessToken)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken);
            }

            return await http.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                ct);
        }

        // Use the Refresh Token to request a new token pair.
        private async Task<bool> TryRefreshAsync(
            CancellationToken ct)
        {
            string? refreshToken =
                Session.GetString("Api.Refresh");

            // A missing Refresh Token cannot be recovered automatically.
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            try
            {
                var client = _clients.CreateClient(
                    "CustomerSupportApi");

                // Call the existing backend Refresh Token API.
                using var response = await client.PostAsJsonAsync(
                    "api/auth/refresh-token",
                    new
                    {
                        refreshToken
                    },
                    Json,
                    ct);

                // Server errors are temporary API failures,
                // not proof that the Refresh Token is invalid.
                if ((int)response.StatusCode >= 500)
                {
                    throw new HttpRequestException(
                        "Token refresh service is unavailable.");
                }

                // Invalid, revoked, or expired Refresh Tokens
                // cannot produce another token pair.
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiResponse<AuthResult>>(
                        Json,
                        ct);

                if (envelope?.Success != true ||
                    envelope.Data is null ||
                    string.IsNullOrWhiteSpace(
                        envelope.Data.AccessToken) ||
                    string.IsNullOrWhiteSpace(
                        envelope.Data.RefreshToken))
                {
                    return false;
                }

                // Refresh Token rotation:
                // Store both NEW tokens, replacing the old pair.
                Session.SetString(
                    "Api.Access",
                    envelope.Data.AccessToken);

                Session.SetString(
                    "Api.Refresh",
                    envelope.Data.RefreshToken);

                Session.SetString(
                    "Api.AccessExpiresAt",
                    envelope.Data.AccessTokenExpiresAt
                        .ToUniversalTime()
                        .ToString("O"));

                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unable to reach token refresh service");

                // Let the caller report a temporary API failure.
                // Do not treat a connection problem as a bad login.
                throw;
            }
        }

        // Refresh before the Access Token expires.
        private async Task EnsureFreshAccessTokenAsync(
            CancellationToken ct)
        {
            string? accessToken =
                Session.GetString("Api.Access");

            string? expiry =
                Session.GetString("Api.AccessExpiresAt");

            bool hasValidExpiry =
                DateTime.TryParse(expiry, out DateTime expiresAt);

            bool shouldRefresh =
                string.IsNullOrWhiteSpace(accessToken) ||
                !hasValidExpiry ||
                expiresAt.ToUniversalTime() <=
                    DateTime.UtcNow.AddMinutes(1);

            if (shouldRefresh)
            {
                // Try refreshing when possible.
                // If it fails, SendCore will handle HTTP 401.
                await TryRefreshAsync(ct);
            }
        }

        // Inform the global MVC filter that API authentication failed.
        private void MarkAuthenticationFailure()
        {
            var httpContext = _context.HttpContext;

            if (httpContext is not null)
            {
                httpContext.Items["Api.AuthFailed"] = true;
            }
        }

        // Upload a ticket attachment.
        public async Task<ApiResult<T>> UploadAsync<T>(
            string path,
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file.Length == 0 ||
                file.Length > 10 * 1024 * 1024)
            {
                return ApiResult<T>.Failure(
                    "Select a file up to 10 MB.",
                    400);
            }

            try
            {
                // Refresh before uploading because the stream
                // cannot safely be replayed after an HTTP 401.
                await EnsureFreshAccessTokenAsync(ct);

                var client = _clients.CreateClient(
                    "CustomerSupportApi");

                using var form = new MultipartFormDataContent();
                await using var input = file.OpenReadStream();
                using var content = new StreamContent(input);

                content.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType);

                form.Add(
                    content,
                    "file",
                    Path.GetFileName(file.FileName));

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    path.TrimStart('/'))
                {
                    Content = form
                };

                if (Session.GetString("Api.Access") is
                    { Length: > 0 } accessToken)
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            accessToken);
                }

                using var response =
                    await client.SendAsync(request, ct);

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    MarkAuthenticationFailure();

                    return ApiResult<T>.Failure(
                        "Authentication required.",
                        401);
                }

                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiResponse<T>>(
                        Json,
                        ct);

                if (response.IsSuccessStatusCode &&
                    envelope?.Success == true)
                {
                    return new ApiResult<T>(
                        true,
                        envelope.Data,
                        envelope.Message,
                        (int)response.StatusCode);
                }

                return ApiResult<T>.Failure(
                    envelope?.Message ?? "Upload failed.",
                    (int)response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Attachment upload API error");

                return ApiResult<T>.Failure(
                    "Unable to upload the attachment.",
                    503);
            }
            catch (TaskCanceledException)
                when (!ct.IsCancellationRequested)
            {
                return ApiResult<T>.Failure(
                    "Attachment upload timed out.",
                    504);
            }
        }

        // Download a ticket attachment.
        public async Task<ApiResult<DownloadedFile>> DownloadAsync(
            string path,
            CancellationToken ct = default)
        {
            try
            {
                // Refresh before downloading the attachment.
                await EnsureFreshAccessTokenAsync(ct);

                var http = _clients.CreateClient(
                    "CustomerSupportApi");

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    path.TrimStart('/'));

                if (Session.GetString("Api.Access") is
                    { Length: > 0 } accessToken)
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            accessToken);
                }

                using var response =
                    await http.SendAsync(request, ct);

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    MarkAuthenticationFailure();

                    return ApiResult<DownloadedFile>.Failure(
                        "Authentication required.",
                        401);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<DownloadedFile>.Failure(
                        "Attachment not available or access denied.",
                        (int)response.StatusCode);
                }

                byte[] bytes =
                    await response.Content.ReadAsByteArrayAsync(ct);

                string contentType =
                    response.Content.Headers.ContentType?.MediaType
                    ?? "application/octet-stream";

                string fileName =
                    response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName
                        ?.Trim('"')
                    ?? "attachment";

                return new ApiResult<DownloadedFile>(
                    true,
                    new DownloadedFile(
                        bytes,
                        contentType,
                        Path.GetFileName(fileName)),
                    "Downloaded.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Attachment download API error");

                return ApiResult<DownloadedFile>.Failure(
                    "Unable to download the attachment.",
                    503);
            }
            catch (TaskCanceledException)
                when (!ct.IsCancellationRequested)
            {
                return ApiResult<DownloadedFile>.Failure(
                    "Attachment download timed out.",
                    504);
            }
        }
    }
}
