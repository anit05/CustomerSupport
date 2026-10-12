using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Interfaces.Services
{
    public interface ITicketsApi
    {
        Task<ApiResult<List<TicketSummary>>> MineAsync();

        Task<ApiResult<PagedResult<TicketSummary>>> SearchAsync(TicketSearch filter);

        Task<ApiResult<TicketDetails>> GetAsync(int id);

        Task<ApiResult<TicketDetails>> CreateAsync(TicketCreateRequest request);

        Task<ApiResult<TicketDetails>> AssignAsync(int id, int supportExecutiveId);

        Task<ApiResult<TicketDetails>> ChangeStatusAsync(int id, int status, string? remarks);

        Task<ApiResult<TicketComment>> AddCommentAsync(int id, string content, bool internalOnly);

        Task<ApiResult<List<TicketAssignment>>> AssignmentsAsync(int id);

        Task<ApiResult<List<TicketAttachment>>> AttachmentsAsync(int id);

        Task<ApiResult<TicketAttachment>> UploadAsync(int id, IFormFile file);

        Task<ApiResult<DownloadedFile>> DownloadAsync(int ticketId, int attachmentId);
    }
}
