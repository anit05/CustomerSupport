using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

// Suppress the OPENAI001 warning for this implementation.
#pragma warning disable OPENAI001

namespace CustomerSupport.Infrastructure.Services
{
    // Implements the Prompt Lab feature.
    // This service allows us to experiment with different prompts
    // without modifying our existing Ticket Summarization feature.
    public sealed class PromptExperimentService : IPromptExperimentService
    {
        // OpenAI client registered through Dependency Injection.
        // We reuse the existing client instead of creating a new one for every request.
        private readonly OpenAIClient _client;

        // Contains our configured OpenAI settings, such as:
        // Model, MaxOutputTokens, and TimeoutSeconds.
        private readonly OpenAISettings _settings;

        // Used to record useful diagnostic information.
        private readonly ILogger<PromptExperimentService> _logger;

        // Constructor Injection:
        // ASP.NET Core automatically supplies these dependencies
        // when the service is created through Dependency Injection.
        public PromptExperimentService(
            OpenAIClient client,
            IOptions<OpenAISettings> settings,
            ILogger<PromptExperimentService> logger)
        {
            // Store the shared OpenAI client.
            _client = client;

            // Read the configured OpenAI settings.
            _settings = settings.Value;

            // Store the logger for this service.
            _logger = logger;
        }

        // Executes a prompt experiment using the OpenAI Responses API.
        // request: Contains DeveloperInstructions, TicketComments, and optional ReferenceText.
        // cancellationToken: Allows the operation to be cancelled when the HTTP request
        // is cancelled or the client disconnects.
        // Returns: The AI-generated output and token usage information.
        public async Task<PromptExperimentResponseDTO> TestAsync(
            PromptExperimentRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            // STEP 1: Validate the required input.
            // DeveloperInstructions tells the AI how to perform the task.
            // TicketComments contains the information to process.
            //
            // Both are required for our Prompt Lab experiment.
            // This prevents unnecessary, potentially billable API calls.
            if (string.IsNullOrWhiteSpace(request.TicketComments) ||
                string.IsNullOrWhiteSpace(request.DeveloperInstructions))
            {
                // Throw our existing application validation exception.
                // The centralized exception handler can convert this
                // into the appropriate HTTP error response.
                throw new ApplicationValidationException(["Instructions and ticket comments are required."]);
            }

            // STEP 2: Prepare the User Message.
            // The ticket comments are supplied as data for the AI model.
            // Trim() removes unnecessary whitespace at the beginning
            // and end of the supplied comments.
            //
            // XML-like tags clearly identify where the ticket comments begin and end.
            string userInput =
                "<ticket_comments>\n" +
                request.TicketComments.Trim() +
                "\n</ticket_comments>";

            // STEP 3: Include optional reference information.
            // ReferenceText is used for Context Engineering experiments.
            // It may contain sample Knowledge Base guidance or other
            // information supplied through Postman.
            if (!string.IsNullOrWhiteSpace(request.ReferenceText))
            {
                // Append the reference information to the User Message.
                // We use a separate tag to distinguish reference data
                // from the original ticket comments.
                // The word "unverified" reminds us that this information
                // should not automatically be treated as confirmed facts.
                userInput +=
                    "\n<reference_unverified>\n" +
                    request.ReferenceText.Trim() +
                    "\n</reference_unverified>";
            }

            // STEP 4: Configure the OpenAI Responses API request.
            // CreateResponseOptions contains the settings and messages
            // that will be sent to the AI model.
            CreateResponseOptions options = new()
            {
                // Select the model configured in appsettings.json.
                // Example: gpt-5.4-mini
                Model = _settings.Model,

                // Define the maximum output-token budget.
                MaxOutputTokenCount = _settings.MaxOutputTokens,

                // Request that the response is not stored for
                // later retrieval through the Responses API.
                StoredOutputEnabled = false
            };

            // STEP 5: Add the Developer Message.
            // The Developer Message defines how the AI should behave.
            // It may contain Role, Task, Rules and Constraints,
            // Output Format, and Missing-Information Rule.
            //
            // In our Prompt Lab, the experimenter supplies these
            // instructions through Postman so we can compare prompts.
            //
            // IMPORTANT:
            // A production application should not allow ordinary
            // customers to control developer-level instructions.
            options.InputItems.Add(
                ResponseItem.CreateDeveloperMessageItem(request.DeveloperInstructions));

            // STEP 6: Add the User Message.
            // The User Message contains the ticket comments and
            // optional reference information prepared earlier.
            options.InputItems.Add(
                ResponseItem.CreateUserMessageItem(userInput));

            // STEP 7: Create a cancellation token with a timeout.
            // The linked token combines:
            // 1. The cancellation token received from the caller.
            // 2. Our application-configured timeout.
            // The request can be cancelled when either condition occurs.
            using CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Automatically cancel the operation if it exceeds
            // the configured number of seconds.
            // Example: TimeoutSeconds = 60 means the request
            // is cancelled after approximately 60 seconds.
            timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            try
            {
                // STEP 8: Get the Responses API client.
                // We obtain it from the existing OpenAIClient
                // registered through Dependency Injection.
                ResponsesClient responses = _client.GetResponsesClient();

                // STEP 9: Send the request to OpenAI.
                // The request includes:
                // - Configured model
                // - Maximum output-token budget
                // - Developer Message
                // - User Message
                ResponseResult result =
                    await responses.CreateResponseAsync(
                        options,
                        timeout.Token);

                // STEP 10: Validate the AI response.
                // Status must be Completed, indicating that the
                // response generation finished successfully.
                // GetOutputText() extracts the generated text.
                if (result.Status != ResponseStatus.Completed ||
                    string.IsNullOrWhiteSpace(result.GetOutputText()))
                {
                    // Throw our existing AIException so the
                    // GlobalExceptionHandler can handle the failure.
                    throw new AIException("The AI response was incomplete or empty.");
                }

                // STEP 11: Log safe diagnostic information.
                // Record the provider response ID and token usage.
                // These details help us troubleshoot requests
                // and compare Prompt Engineering experiments.
                _logger.LogInformation(
                    "Prompt lab response {Id}; tokens {Tokens}",
                    result.Id,
                    result.Usage?.TotalTokenCount);

                // STEP 12: Prepare the response DTO.
                // Convert the OpenAI response into our application's
                // own response structure.
                return new PromptExperimentResponseDTO
                {
                    // Extract the AI-generated text.
                    // Trim() removes leading and trailing whitespace.
                    OutputText = result.GetOutputText().Trim(),

                    // Return the model reported by OpenAI.
                    // If unavailable, use our configured model.
                    Model = result.Model ?? _settings.Model,

                    // Unique ID assigned to the response by OpenAI.
                    // Useful for correlating provider requests.
                    ResponseId = result.Id ?? "",

                    // Number of input tokens reported by OpenAI.
                    InputTokens = result.Usage?.InputTokenCount ?? 0,

                    // Number of output tokens reported by OpenAI.
                    OutputTokens = result.Usage?.OutputTokenCount ?? 0,

                    // Total token usage reported by OpenAI.
                    // Generally includes input and output tokens.
                    TotalTokens = result.Usage?.TotalTokenCount ?? 0
                };
            }
            catch (ClientResultException ex)
            {
                // STEP 13: Handle provider/API failures.
                // ClientResultException can occur when OpenAI returns
                // an error response, such as:
                // - Invalid API credentials
                // - Rate-limit exceeded
                // - Insufficient quota
                // - Invalid request
                _logger.LogWarning("Prompt lab OpenAI failure; HTTP {Status}", ex.Status);

                // Return a simplified error through our
                // centralized exception-handling mechanism.
                throw new AIException("The AI provider request failed.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // STEP 14: Handle application timeout.
                throw new AIException("The AI request timed out.");
            }
            catch (HttpRequestException)
            {
                // STEP 15: Handle network or connectivity failures.
                // Examples:
                // - Network connection failure
                // - DNS resolution problem
                // - Unable to reach the provider
                throw new AIException("The AI service could not be reached.");
            }
        }
    }
}