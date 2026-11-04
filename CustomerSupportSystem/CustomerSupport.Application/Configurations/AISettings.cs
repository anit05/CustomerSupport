namespace CustomerSupport.Application.Configurations
{
  // Common settings for whichever AI provider is selected.
  public sealed class AISettings
  {
    public const string SectionName = "AI";
    public string Provider { get; set; } = "OpenAI";
    public int MaxOutputTokens { get; set; } = 1600;
    public int TimeoutSeconds { get; set; } = 180;
  }
}
