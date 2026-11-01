
namespace CustomerSupport.Mvc.Models.Api
{
    // Contains structured analysis and AI response information.
    public sealed class TicketAIAnalysisResult
    {
        public int TicketId
        {
            get; set;
        }

        public TicketAIAnalysis Analysis { get; set; } = new();

        // Validated category and priority IDs from the backend.
        public int? SuggestedCategoryId
        {
            get; set;
        }

        public int? SuggestedPriorityId
        {
            get; set;
        }

        // Information about the AI provider and generated response.
        public string Provider { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string ResponseId { get; set; } = string.Empty;

        // Token usage returned by the backend.
        public int InputTokens
        {
            get; set;
        }

        public int OutputTokens
        {
            get; set;
        }

        public int TotalTokens
        {
            get; set;
        }
    }
}
