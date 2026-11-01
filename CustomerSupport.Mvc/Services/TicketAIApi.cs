using CustomerSupport.Mvc.Interfaces.Services;
using CustomerSupport.Mvc.Models.Api;

namespace CustomerSupport.Mvc.Services
{
    // Calls the AI endpoints exposed by CustomerSupport.API.
    public sealed class TicketAIApi : ITicketAIApi
    {
        // Reuse the existing authenticated HTTP API client.
        private readonly IApiClient _api;

        // Receive IApiClient through Dependency Injection.
        public TicketAIApi(IApiClient api)
        {
            _api = api;
        }

        // Generate structured analysis for the selected ticket.
        public Task<ApiResult<TicketAIAnalysisResult>> AnalyzeAsync(
            int ticketId,
            CancellationToken ct = default)
        {
            // The analysis endpoint does not require a request body.
            return _api.SendAsync<TicketAIAnalysisResult>(
                HttpMethod.Post,
                $"api/ai/tickets/{ticketId}/analysis",
                ct: ct);
        }

        // Generate a customer-facing reply draft.
        public Task<ApiResult<TicketAIDraftResult>> ReplyDraftAsync(
            int ticketId,
            int? knowledgeBaseArticleId,
            CancellationToken ct = default)
        {
            // Send the optional Knowledge Base article ID.
            // Internal comments are excluded by the backend.
            return _api.SendAsync<TicketAIDraftResult>(
                HttpMethod.Post,
                $"api/ai/tickets/{ticketId}/reply-draft",
                new
                {
                    knowledgeBaseArticleId
                },
                ct: ct);
        }

        // Generate an internal support note draft.
        public Task<ApiResult<TicketAIDraftResult>> InternalNoteDraftAsync(
            int ticketId,
            int? knowledgeBaseArticleId,
            CancellationToken ct = default)
        {
            // Internal notes may use internal support comments.
            return _api.SendAsync<TicketAIDraftResult>(
                HttpMethod.Post,
                $"api/ai/tickets/{ticketId}/internal-note-draft",
                new
                {
                    knowledgeBaseArticleId
                },
                ct: ct);
        }

        // Save the text reviewed and approved by support staff.
        public Task<ApiResult<TicketComment>> SaveReviewedDraftAsync(
            int ticketId,
            string approvedText,
            bool isInternal,
            CancellationToken ct = default)
        {
            // Saving a reviewed draft is separate from generating it.
            return _api.SendAsync<TicketComment>(
                HttpMethod.Post,
                $"api/ai/tickets/{ticketId}/save-drafts",
                new
                {
                    approvedText,
                    isInternal
                },
                ct: ct);
        }
    }
}
