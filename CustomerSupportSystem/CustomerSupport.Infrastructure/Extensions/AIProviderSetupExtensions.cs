using CustomerSupport.Application.Configurations;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Infrastructure.Configurations;
using CustomerSupport.Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;

namespace CustomerSupport.Infrastructure.Extensions
{
  // Configures OpenAI and Ollama as interchangeable AI providers.
  public static class AIProviderSetupExtensions
  {
    // Registers AI settings and provider services with Dependency Injection.
    public static IServiceCollection AddAIProviderSetup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
      // STEP 1: Read common AI settings from the "AI" section.
      // Validate the configured values when the application starts.
      services.AddOptions<AISettings>()
          .Bind(configuration.GetSection(AISettings.SectionName))

          // Only OpenAI and Ollama are currently supported.
          .Validate(
              x => x.Provider is "OpenAI" or "Ollama",
              "AI:Provider must be OpenAI or Ollama.")

          // Maximum number of tokens the AI can generate.
          .Validate(
              x => x.MaxOutputTokens is >= 200 and <= 4000,
              "AI:MaxOutputTokens must be between 200 and 4000.")

          // Maximum time allowed for an AI request.
          .Validate(
              x => x.TimeoutSeconds is >= 10 and <= 300,
              "AI:TimeoutSeconds must be between 10 and 300.")

          // Detect invalid settings during application startup.
          .ValidateOnStart();

      // STEP 2: Register provider-specific configuration.
      // OpenAISettings contains the API key, model, and other settings.
      services.Configure<OpenAISettings>(
          configuration.GetSection(OpenAISettings.SectionName));

      // OllamaSettings contains the local API URL and model name.
      //services.Configure<OllamaSettings>(
      //    configuration.GetSection("Ollama"));

      // STEP 3: Read the selected provider from appsettings.json.
      // Use OpenAI as the default when no provider is specified.
      string selectedProvider = configuration["AI:Provider"] ?? "OpenAI";

      // STEP 4: Validate settings required by the selected provider.
      if (selectedProvider == "OpenAI")
      {
        // OpenAI requires an API key and a model name.
        if (string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]) ||
            string.IsNullOrWhiteSpace(configuration["OpenAI:Model"]))
        {
          throw new InvalidOperationException("Configure OpenAI:ApiKey and OpenAI:Model.");
        }
      }
      else if (selectedProvider == "Ollama")
      {
        // Read the local Ollama API address and model name.
        string? baseUrl = configuration["Ollama:BaseUrl"];
        string? model = configuration["Ollama:Model"];

        // Check whether BaseUrl is a valid HTTP or HTTPS address.
        bool isValidUrl =
            Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp ||
             uri.Scheme == Uri.UriSchemeHttps);

        // Both a valid API address and model name are required.
        if (!isValidUrl || string.IsNullOrWhiteSpace(model))
        {
          throw new InvalidOperationException("Configure Ollama:BaseUrl and Ollama:Model.");
        }
      }

      // STEP 5: Register the OpenAI implementation of IChatClient.
      // "OpenAI" is the key used to identify this registration.
      services.AddKeyedSingleton<IChatClient>(
          "OpenAI",
          (provider, key) =>
          {
            // Get the OpenAI settings from Dependency Injection.
            OpenAISettings settings = provider
                      .GetRequiredService<IOptions<OpenAISettings>>()
                      .Value;

            // Create the OpenAI SDK client using the configured API key.
            OpenAIClient openAIClient = new OpenAIClient(settings.ApiKey);

            // Convert the OpenAI Responses API client into IChatClient.
            // This allows our application to use Microsoft's common interface.
            // Suppress the SDK's experimental API warning for this adapter.
#pragma warning disable OPENAI001
            IChatClient chatClient = openAIClient
                      .GetResponsesClient()
                      .AsIChatClient(settings.Model);
#pragma warning restore OPENAI001

            // Return the configured OpenAI chat client.
            return chatClient;
          });

      // STEP 6: Register the Ollama implementation of IChatClient.
      // "Ollama" is the key used to identify this registration.
      //services.AddKeyedSingleton<IChatClient>(
      //    "Ollama",
      //    (provider, key) =>
      //    {
      //      // Get the Ollama settings from Dependency Injection.
      //      OllamaSettings settings = provider
      //                .GetRequiredService<IOptions<OllamaSettings>>()
      //                .Value;

      //      // Create an Ollama client using its API URL and model name.
      //      // OllamaSharp provides an implementation of IChatClient.
      //      return new OllamaApiClient(
      //                new Uri(settings.BaseUrl),
      //                settings.Model);
      //    });

      // STEP 7: Register the service that selects the AI provider.
      // AIChatClientProvider retrieves the matching keyed IChatClient
      // based on the AI:Provider setting in appsettings.json.
      services.AddScoped<IAIChatClientProvider, AIChatClientProvider>();

      // Return the updated service collection.
      return services;
    }
  }
}
