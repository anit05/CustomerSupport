namespace CustomerSupport.Application.DTOs.AI
{
  public sealed class SummarizeTicketResponseDTO
  {
    // AI-generated summary of the ticket comments.
    public string Summary { get; set; } = string.Empty;

    // Actual model used to generate the response.
    public string Model { get; set; } = string.Empty;

    // Unique identifier returned by OpenAI.
    public string ResponseId { get; set; } = string.Empty;

    // Number of input tokens processed.
    public int InputTokens { get; set; }

    // Number of output tokens generated.
    public int OutputTokens { get; set; }

    // Total reported token usage.
    public int TotalTokens { get; set; }
  }
}
