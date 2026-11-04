namespace CustomerSupport.Application.DTOs.AI
{
    public sealed class AITextResultDTO
    {
        public string Text { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
