
using CustomerSupport.Mvc.Models.Api;

namespace CustomerSupport.Mvc.Interfaces.Services
{
    // Defines how MVC communicates with the existing AI Web API endpoints.
    public interface ITicketAIApi
    {
        // Analyze an existing ticket.
        Task<ApiResult<TicketAIAnalysisResult>> AnalyzeAsync(
            int ticketId,
            CancellationToken ct = default);

        // Generate a customer-facing reply draft.
        Task<ApiResult<TicketAIDraftResult>> ReplyDraftAsync(
            int ticketId,
            int? knowledgeBaseArticleId,
            CancellationToken ct = default);

        // Generate a staff-only internal note draft.
        Task<ApiResult<TicketAIDraftResult>> InternalNoteDraftAsync(
            int ticketId,
            int? knowledgeBaseArticleId,
            CancellationToken ct = default);

        // Save the reviewed text as a ticket comment.
        Task<ApiResult<TicketComment>> SaveReviewedDraftAsync(
            int ticketId,
            string approvedText,
            bool isInternal,
            CancellationToken ct = default);
    }
}
