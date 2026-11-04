using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.DTOs.AI
{
    public sealed class TicketAIResponseDTO
    {
        public int TicketId { get; set; }
        public string? Feature { get; set; }

        // Generated summary, suggested label, reply, or note.
        public string? OutputText { get; set; }

        // Filled only when a category suggestion was validated
        // against active category master data.
        public int? SuggestedCategoryId { get; set; }

        // Filled only when a priority suggestion was validated
        // against active priority master data.
        public TicketPriorityType? SuggestedPriorityId { get; set; }

        // OpenAI response metadata and actual token usage.
        public string Model { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}