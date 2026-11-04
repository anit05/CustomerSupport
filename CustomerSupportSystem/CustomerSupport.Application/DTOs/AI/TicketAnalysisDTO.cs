using System.Text.Json.Serialization;
namespace CustomerSupport.Application.DTOs.AI
{
    // Represents the structured JSON generated for one ticket.
    public sealed class TicketAnalysisDTO
    {
        // A short explanation of the customer-reported problem.
        [JsonPropertyName("problem")]
        public required string Problem { get; init; }

        // Troubleshooting actions that were already recorded.
        // Return an empty list when no action is recorded.
        [JsonPropertyName("actionsTried")]
        public required List<string> ActionsTried { get; init; }

        // A next step explicitly confirmed in the ticket.
        [JsonPropertyName("nextStep")]
        public required string NextStep { get; init; }

        // Sentiment is converted from its JSON text label
        // into our TicketAISentiment enum.
        [JsonPropertyName("sentiment")]
        [JsonConverter(typeof(JsonStringEnumConverter<TicketAISentiment>))]
        public required TicketAISentiment Sentiment { get; init; }

        // Suggested category NAME, not a CategoryId.
        // Null means that there is not enough evidence.
        [JsonPropertyName("suggestedCategory")]
        public required string? SuggestedCategory { get; init; }

        // Suggested priority NAME, not a PriorityId.
        // The Application Service will verify it against SQL Server.
        [JsonPropertyName("suggestedPriority")]
        public required string? SuggestedPriority { get; init; }

        // Required JSON property, but null is an allowed value.
        // A customer's requested deadline is not a confirmed date.
        [JsonPropertyName("confirmedResolutionDate")]
        public required string? ConfirmedResolutionDate { get; init; }

        // True means a staff member should review this suggestion.
        // It does not automatically update or approve anything.
        [JsonPropertyName("requiresHumanReview")]
        public required bool RequiresHumanReview { get; init; }
    }
}