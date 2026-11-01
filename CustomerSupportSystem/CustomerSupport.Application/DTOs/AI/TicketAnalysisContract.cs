namespace CustomerSupport.Application.DTOs.AI
{
    // This class defines the JSON structure that we expect
    // OpenAI to follow when analyzing a customer support ticket.
    public static class TicketAnalysisContract
    {
        public const string JsonSchema = """
        {
          "type": "object",
          "properties": {
            "problem": { "type": "string" },
            "actionsTried": {
              "type": "array",
              "items": { "type": "string" }
            },
            "nextStep": { "type": "string" },
            "sentiment": {
              "type": "string",
              "enum": ["Positive", "Neutral", "Negative", "Mixed", "Unclear"]
            },
            "suggestedCategory": { "type": ["string", "null"] },
            "suggestedPriority": { "type": ["string", "null"] },
            "confirmedResolutionDate": { "type": ["string", "null"] },
            "requiresHumanReview": { "type": "boolean" }
          },
          "required": [
            "problem", "actionsTried", "nextStep", "sentiment",
            "suggestedCategory", "suggestedPriority",
            "confirmedResolutionDate", "requiresHumanReview"
          ],
          "additionalProperties": false
        }
        """;
    }
}