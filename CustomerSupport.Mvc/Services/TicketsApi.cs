using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;
using CustomerSupport.Mvc.Interfaces.Services;

namespace CustomerSupport.Mvc.Services
{
    public sealed class TicketsApi : ITicketsApi
    {
        private readonly IApiClient _api;

        public TicketsApi(IApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<List<TicketSummary>>> MineAsync()
        {
            return _api.GetAsync<List<TicketSummary>>("api/tickets/my");
        }

        public Task<ApiResult<PagedResult<TicketSummary>>> SearchAsync(TicketSearch f)
        {
            var values = new Dictionary<string,string?> {
                ["search"] = f.Search, ["statusId"] = f.StatusId?.ToString(), ["priorityId"] = f.PriorityId?.ToString(),
                ["productId"] = f.ProductId?.ToString(), ["categoryId"] = f.CategoryId?.ToString(),
                ["customerId"] = f.CustomerId?.ToString(), ["assignedToUserId"] = f.AssignedToUserId?.ToString(),
                ["pageNumber"] = Math.Max(1,f.PageNumber).ToString(), ["pageSize"] = Math.Clamp(f.PageSize,1,100).ToString(),
                ["sortBy"] = f.SortBy, ["sortDirection"] = f.SortDirection
            };
            var query = string.Join("&", values.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
            return _api.GetAsync<PagedResult<TicketSummary>>("api/tickets?" + query);
        }

        public Task<ApiResult<TicketDetails>> GetAsync(int id)
        {
            return _api.GetAsync<TicketDetails>($"api/tickets/{id}");
        }

        public Task<ApiResult<TicketDetails>> CreateAsync(TicketCreateRequest request)
        {
            return _api.SendAsync<TicketDetails>(HttpMethod.Post, "api/tickets", request);
        }

        public Task<ApiResult<TicketDetails>> AssignAsync(int id, int supportExecutiveId)
        {
            return _api.SendAsync<TicketDetails>(HttpMethod.Put, $"api/tickets/{id}/assign", new { supportExecutiveId });
        }

        public Task<ApiResult<TicketDetails>> ChangeStatusAsync(int id, int status, string? remarks)
        {
            return _api.SendAsync<TicketDetails>(HttpMethod.Put, $"api/tickets/{id}/status", new { statusId = status, remarks });
        }

        public Task<ApiResult<TicketComment>> AddCommentAsync(int id, string content, bool internalOnly)
        {
            return _api.SendAsync<TicketComment>(HttpMethod.Post, $"api/tickets/{id}/comments", new { content, isInternal = internalOnly });
        }

        public Task<ApiResult<List<TicketAssignment>>> AssignmentsAsync(int id)
        {
            return _api.GetAsync<List<TicketAssignment>>($"api/tickets/{id}/assignments");
        }

        public Task<ApiResult<List<TicketAttachment>>> AttachmentsAsync(int id)
        {
            return _api.GetAsync<List<TicketAttachment>>($"api/tickets/{id}/attachments");
        }

        public Task<ApiResult<TicketAttachment>> UploadAsync(int id, IFormFile file)
        {
            return _api.UploadAsync<TicketAttachment>($"api/tickets/{id}/attachments", file);
        }

        public Task<ApiResult<DownloadedFile>> DownloadAsync(int ticketId, int attachmentId)
        {
            return _api.DownloadAsync($"api/tickets/{ticketId}/attachments/{attachmentId}/download");
        }
    }
}
