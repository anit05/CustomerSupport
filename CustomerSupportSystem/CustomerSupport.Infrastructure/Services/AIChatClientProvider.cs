using CustomerSupport.Application.Configurations;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CustomerSupport.Infrastructure.Services
{
  // Provides the AI client based on the provider selected in appsettings.json.
  public sealed class AIChatClientProvider : IAIChatClientProvider
  {
    // Used to retrieve registered services from Dependency Injection.
    private readonly IServiceProvider _services;

    // Contains common AI settings, including the selected provider.
    private readonly AISettings _settings;

    // Receive the service provider and AI configuration through DI.
    public AIChatClientProvider(IServiceProvider services, IOptions<AISettings> settings)
    {
      _services = services;

      // Get the AI settings configured in appsettings.json.
      _settings = settings.Value;
    }

    // Return the configured provider name, such as OpenAI or Ollama.
    public string ProviderName => _settings.Provider;

    // Return the IChatClient registered for the selected provider.
    public IChatClient GetClient()
    {
      // Read the provider name from AISettings.
      // Example: "OpenAI" or "Ollama".
      string providerName = _settings.Provider;

      // Retrieve the matching named IChatClient from DI.
      // OpenAI returns the OpenAI-backed client.
      // Ollama returns the Ollama-backed client.
      return _services.GetRequiredKeyedService<IChatClient>(providerName);
    }
  }
}


