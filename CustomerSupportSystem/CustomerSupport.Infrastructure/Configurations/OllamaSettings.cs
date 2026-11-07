namespace CustomerSupport.Infrastructure.Configurations
{
    // Settings used to call the local Ollama service.
    public sealed class OllamaSettings
    {
        public const string SectionName = "Ollama";

        // Default local address used by Ollama.
        public string BaseUrl { get; set; } = "http://localhost:11434/";

        // Must match a model shown by the "ollama list" command.
        public string Model { get; set; } = "llama3.2:3b";

        // Local inference can take longer on slower computers.
        public int TimeoutSeconds { get; set; } = 180;
    }
}
