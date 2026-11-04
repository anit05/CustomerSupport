using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

// The Responses API types are experimental in this SDK version.
#pragma warning disable OPENAI001

namespace CustomerSupport.Infrastructure.Services
{
    // Implements both normal text and structured JSON generation.
    public sealed class OpenAITextGenerationService : IOpenAITextGenerationService
    {
        private readonly OpenAIClient _openAIClient;
        private readonly OpenAISettings _settings;
        private readonly ILogger<OpenAITextGenerationService> _logger;

        // Dependencies are created by ASP.NET Core Dependency Injection
        public OpenAITextGenerationService(
            OpenAIClient openAIClient,
            IOptions<OpenAISettings> settings,
            ILogger<OpenAITextGenerationService> logger)
        {
            _openAIClient = openAIClient;
            _settings = settings.Value;
            _logger = logger;
        }

        // Existing Chapter 3.3 features continue to get plain text.
        public Task<AITextResultDTO> GenerateAsync(
            string developerInstructions,
            string userInput,
            CancellationToken cancellationToken = default)
        {
            return SendAsync(
                developerInstructions, userInput,
                jsonSchema: null, cancellationToken: cancellationToken);
        }

        // New feature requests JSON using a supplied schema.
        public Task<AITextResultDTO> GenerateStructuredAsync(
            string developerInstructions,
            string userInput,
            string jsonSchema,
            CancellationToken cancellationToken = default)
        {
            // Reject empty schemas before making a billable API call.
            if (string.IsNullOrWhiteSpace(jsonSchema))
            {
                throw new ApplicationValidationException(["A JSON Schema is required."]);
            }

            return SendAsync(
                developerInstructions, userInput, jsonSchema,
                cancellationToken);
        }

        // Shared implementation for both kinds of AI requests.
        private async Task<AITextResultDTO> SendAsync(
            string developerInstructions,
            string userInput,
            string? jsonSchema,
            CancellationToken cancellationToken)
        {
            // Do not send empty prompts to the provider.
            if (string.IsNullOrWhiteSpace(developerInstructions) ||
                string.IsNullOrWhiteSpace(userInput))
            {
                throw new ApplicationValidationException(["AI instructions and input are required."]);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // These values are read from our existing app settings.
            CreateResponseOptions options = new()
            {
                Model = _settings.Model,
                MaxOutputTokenCount = _settings.MaxOutputTokens,
                StoredOutputEnabled = false
            };


            // Check whether a JSON Schema is provided.
            // If provided, configure Structured Outputs for this request.
            // Otherwise, the request will use the normal text output format.
            if (jsonSchema is not null)
            {
                // Configure how OpenAI should format the generated response.
                options.TextOptions = new ResponseTextOptions
                {
                    // Instruct OpenAI to generate a response
                    // that follows our predefined JSON Schema.
                    TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(

                        // A name used to identify this JSON output format.
                        "ticket_analysis",

                        // Convert the JSON Schema string into BinaryData,
                        // which is the format expected by the OpenAI SDK.
                        BinaryData.FromString(jsonSchema),

                        // Enable strict schema enforcement.
                        // The AI response must follow the supported schema,
                        // including required properties, data types,
                        // allowed values, and restrictions on extra fields.
                        jsonSchemaIsStrict: true)
                };
            }

            // Developer Message: our application's trusted rules.
            options.InputItems.Add(
                ResponseItem.CreateDeveloperMessageItem(developerInstructions));

            // User Message: ticket data, not developer instructions.
            options.InputItems.Add(
                ResponseItem.CreateUserMessageItem(userInput));

            // Stop when the caller cancels or our timeout expires.
            using CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            try
            {
                // Use the OpenAI client already registered in DI.
                ResponsesClient client = _openAIClient.GetResponsesClient();

                // Send the actual Responses API request.
                ResponseResult result = await client
                    .CreateResponseAsync(options, timeout.Token);

                // An incomplete result is not safe to deserialize.
                // Refusals or missing output are rejected below.
                if (result.Status != ResponseStatus.Completed)
                {
                    throw new AIException("OpenAI did not complete the requested feature.");
                }

                // Get the text returned by the model.
                string output = result.GetOutputText();
                if (string.IsNullOrWhiteSpace(output))
                {
                    throw new AIException("OpenAI did not return usable output.");
                }

                // Log technical details, never raw ticket data.
                _logger.LogInformation(
                    "AI response {Id}; total tokens {Tokens}",
                    result.Id, result.Usage?.TotalTokenCount);

                // Preserve the provider response and usage metadata.
                return new AITextResultDTO
                {
                    Text = output.Trim(),
                    Model = result.Model ?? _settings.Model,
                    ResponseId = result.Id ?? string.Empty,
                    InputTokens = result.Usage?.InputTokenCount ?? 0,
                    OutputTokens = result.Usage?.OutputTokenCount ?? 0,
                    TotalTokens = result.Usage?.TotalTokenCount ?? 0
                };
            }
            catch (ClientResultException ex)
            {
                // Example: credentials, quota, or invalid request.
                _logger.LogWarning(
                    "OpenAI request failed; HTTP status {Status}",
                    ex.Status);
                throw new AIException("OpenAI could not complete the request.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // Only convert our configured timeout to AIException.
                throw new AIException("The AI request took too long to complete.");
            }
            catch (HttpRequestException)
            {
                // Network/DNS/provider connectivity problems.
                throw new AIException("Unable to communicate with OpenAI.");
            }
        }
    }
}