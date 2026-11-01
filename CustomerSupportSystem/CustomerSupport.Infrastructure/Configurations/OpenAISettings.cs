namespace CustomerSupport.Infrastructure.Configurations
{
  // Configuration used by our OpenAI integration.
  public sealed class OpenAISettings
  {
    public const string SectionName = "OpenAI";

    // Obtained from User Secrets; never hardcode the key.
    public string ApiKey { get; set; } = string.Empty;

    // Name of the model, obtained from appsettings.json.
    public string Model { get; set; } = string.Empty;

    // Maximum output tokens allowed for one request.
    public int MaxOutputTokens { get; set; } = 1600;

    // Maximum time allowed for the complete AI operation,
    // including any retries performed by the SDK.
    public int TimeoutSeconds { get; set; } = 60;
  }
}

