using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.DTOs.AI
{
    // Result returned to our API client after validation.
    public sealed class TicketStructuredAnalysisResponseDTO
    {
        // Identifies which ticket was analyzed.
        public int TicketId { get; set; }

        // Strongly typed analysis created from the AI JSON.
        public TicketAnalysisDTO Analysis { get; set; } = null!;

        // Actual active Category ID resolved from the database.
        // Null when the AI did not suggest a supported category.
        public int? SuggestedCategoryId { get; set; }

        // Existing domain priority type from master data.
        // Null when no supported priority could be suggested.
        public TicketPriorityType? SuggestedPriorityId { get; set; }

        // OpenAI metadata for troubleshooting and usage monitoring.
        public string Model { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}