
namespace CustomerSupport.Mvc.Models.Api
{
    // Contains an AI-generated reply or internal note for staff review.
    public sealed class TicketAIDraftResult
    {
        public int TicketId
        {
            get; set;
        }

        // Generated text that can be edited before saving.
        public string DraftText { get; set; } = string.Empty;

        // AI provider information.
        public string Provider { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string ResponseId { get; set; } = string.Empty;

        // Token usage information.
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
