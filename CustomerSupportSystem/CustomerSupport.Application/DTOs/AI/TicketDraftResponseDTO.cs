namespace CustomerSupport.Application.DTOs.AI
{
  // Text for staff review; this result is not saved to SQL Server.
  public sealed class TicketDraftResponseDTO
  {
    public int TicketId { get; set; }
    public string DraftText { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ResponseId { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
  }
}
