using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Infrastructure.Configurations;
using CustomerSupport.Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace CustomerSupport.Infrastructure.Extensions
{
    public static class OpenAISetupExtensions
    {
        // Extension method to configure OpenAI services
        // and register them with ASP.NET Core Dependency Injection.
        public static IServiceCollection AddOpenAISetup(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register the OpenAISettings class using the Options pattern.
            services.AddOptions<OpenAISettings>()
                // Read the "OpenAI" configuration section.
                .Bind(configuration.GetSection(OpenAISettings.SectionName))

                // The API key must exist.
                .Validate(settings =>
                    !string.IsNullOrWhiteSpace(settings.ApiKey),
                    "OpenAI:ApiKey is missing. Configure User Secrets.")

                // The model name must exist.
                .Validate(settings =>
                    !string.IsNullOrWhiteSpace(settings.Model),
                    "OpenAI:Model is missing in appsettings.json.")

                // Validate the output-token configuration.
                .Validate(settings =>
                    settings.MaxOutputTokens >= 200 &&
                    settings.MaxOutputTokens <= 4000,
                    "OpenAI:MaxOutputTokens must be between 200 and 4000.")

                // Validate the request timeout.
                .Validate(settings =>
                    settings.TimeoutSeconds >= 5 &&
                    settings.TimeoutSeconds <= 120,
                    "OpenAI:TimeoutSeconds must be between 5 and 120.")

                // Detect invalid configuration during startup.
                // ValidateOnStart() ensures that missing configuration
                // values are detected when the application starts.
                .ValidateOnStart();

            // Register OpenAIClient with Dependency Injection.
            // AddSingleton creates one client instance that can be
            // reused throughout the application's lifetime.
            services.AddSingleton<OpenAIClient>(serviceProvider =>
            {
                // Retrieve the configured OpenAISettings.
                // serviceProvider gives access to services registered
                // in the Dependency Injection container.

                // GetRequiredService retrieves IOptions<OpenAISettings>.
                // The Value property returns the OpenAISettings object
                // populated from the application's configuration.
                OpenAISettings settings = serviceProvider
                    .GetRequiredService<IOptions<OpenAISettings>>()
                    .Value;

                // Create the OpenAI client using our API key.
                // The API key is used to authenticate requests sent to the OpenAI API.
                return new OpenAIClient(settings.ApiKey);
            });

            // The OpenAI Responses SDK adapter may show warning OPENAI001.
#pragma warning disable OPENAI001

            // Add this registration inside the existing OpenAI setup method.
            services.AddSingleton<IChatClient>(serviceProvider =>
            {
                // Reuse the OpenAIClient already registered.
                OpenAIClient openAIClient =
                    serviceProvider.GetRequiredService<OpenAIClient>();

                // Reuse the existing model name from OpenAISettings.
                OpenAISettings settings = serviceProvider
                    .GetRequiredService<IOptions<OpenAISettings>>().Value;

                // Adapt the existing Responses API client to IChatClient.
                // Our Application Service will use this common interface.
                return openAIClient.GetResponsesClient()
                    .AsIChatClient(settings.Model);
            });
#pragma warning restore OPENAI001

            // Register our application-specific AI service.
            // Scoped creates a service instance for each
            // ASP.NET Core dependency-injection scope.
            services.AddScoped<IOpenAITicketSummaryService, OpenAITicketSummaryService>();

            services.AddScoped<IPromptExperimentService, PromptExperimentService>();

            // Provider-specific implementation of the text-generation contract.
            services.AddScoped<IOpenAITextGenerationService, OpenAITextGenerationService>();

            // Return IServiceCollection.
            return services;
        }
    }
}
