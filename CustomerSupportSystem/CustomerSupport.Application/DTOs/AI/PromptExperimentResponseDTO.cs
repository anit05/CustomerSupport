namespace CustomerSupport.Application.DTOs.AI
{
    public sealed class PromptExperimentResponseDTO
    {
        public string OutputText { get; set; } = "";
        public string Model { get; set; } = "";
        public string ResponseId { get; set; } = "";
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}