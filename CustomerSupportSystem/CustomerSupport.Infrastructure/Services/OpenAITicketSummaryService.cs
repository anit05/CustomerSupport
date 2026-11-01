using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Infrastructure.Configurations;
using CustomerSupport.Infrastructure.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

// Suppress the SDK experimental warning for Responses API.
#pragma warning disable OPENAI001

namespace CustomerSupport.Infrastructure.Services
{
    public sealed class OpenAITicketSummaryService : IOpenAITicketSummaryService
    {
        private const int MaxTicketCharacters = 12000;
        private const int MaxContextCharacters = 4000;

        private readonly OpenAIClient _openAIClient;
        private readonly OpenAISettings _settings;
        private readonly ILogger<OpenAITicketSummaryService> _logger;

        // Dependencies are provided through Dependency Injection.
        public OpenAITicketSummaryService(
            OpenAIClient openAIClient,
            IOptions<OpenAISettings> settings,
            ILogger<OpenAITicketSummaryService> logger)
        {
            _openAIClient = openAIClient;
            _settings = settings.Value;
            _logger = logger;
        }

        // Generates a summary using the selected prompt style.
        public async Task<SummarizeTicketResponseDTO> SummarizeAsync(
            SummarizeTicketRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            // Step 1: Validate ticket comments.
            if (string.IsNullOrWhiteSpace(request.TicketComments))
            {
                throw new ApplicationValidationException(
                    ["Ticket comments cannot be empty."]);
            }

            if (request.TicketComments.Length > MaxTicketCharacters)
            {
                throw new ApplicationValidationException(
                    ["Ticket comments cannot exceed 12000 characters."]);
            }

            // Step 2: Validate optional client context.
            if (request.ContextInformation != null &&
                request.ContextInformation.Length > MaxContextCharacters)
            {
                throw new ApplicationValidationException(
                    ["Context information cannot exceed 4000 characters."]);
            }

            // Stop immediately if the caller has cancelled.
            cancellationToken.ThrowIfCancellationRequested();

            // Step 3: Prepare the input data.
            string cleanComments = request.TicketComments.Trim();

            // Step 4: Select the developer instructions.
            //
            // The Prompt Builder chooses predefined instructions
            // based on the selected prompt style.
            //
            // An unsupported style throws ApplicationValidationException.
            string developerInstructions =
                TicketSummaryPromptBuilder.GetDeveloperInstructions(request.PromptStyle);

            // Step 5: Prepare the user input.
            //
            // This combines the ticket comments with optional
            // client-provided context.
            string userInput =
                TicketSummaryPromptBuilder.BuildUserInput(
                    cleanComments,
                    request.ContextInformation);

            // Step 6: Get the OpenAI Responses API client.
            ResponsesClient responsesClient =
                _openAIClient.GetResponsesClient();

            // Step 7: Configure the OpenAI request.
            CreateResponseOptions options = new CreateResponseOptions()
            {
                // Read the model name from appsettings.json.
                Model = _settings.Model,

                // Maximum permitted output tokens.
                MaxOutputTokenCount = _settings.MaxOutputTokens
            };

            // Step 8: Add the developer instructions.
            options.InputItems.Add(
                ResponseItem.CreateDeveloperMessageItem(
                    developerInstructions));

            // Step 9: Add ticket comments and context.
            options.InputItems.Add(
                ResponseItem.CreateUserMessageItem(userInput));

            // Step 10: Configure cancellation and timeout.
            using CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeout.CancelAfter(
                TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            try
            {
                // Step 11: Send the actual request to OpenAI.
                ResponseResult response = await responsesClient
                    .CreateResponseAsync(
                        options,
                        timeout.Token);

                // Step 12: Verify that generation completed.
                if (response.Status != ResponseStatus.Completed)
                {
                    _logger.LogWarning(
                        "OpenAI returned an incomplete response. " +
                        "ResponseId: {ResponseId}, Status: {Status}",
                        response.Id,
                        response.Status);

                    throw new AIException(
                        "OpenAI did not complete the ticket summary.");
                }

                // Step 13: Extract the generated summary.
                string summary = response.GetOutputText();

                // Validate that the response contains usable text.
                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.LogWarning(
                        "OpenAI returned an empty summary. " +
                        "ResponseId: {ResponseId}",
                        response.Id);

                    throw new AIException(
                        "OpenAI returned an empty ticket summary.");
                }

                // Step 14: Log safe technical information.
                //
                // Never log the API key, ticket comments,
                // client context, or generated summary.
                _logger.LogInformation(
                    "AI ticket summary generated. " +
                    "PromptStyle: {PromptStyle}, " +
                    "ResponseId: {ResponseId}, " +
                    "Model: {Model}, " +
                    "InputTokens: {InputTokens}, " +
                    "OutputTokens: {OutputTokens}, " +
                    "TotalTokens: {TotalTokens}",
                    request.PromptStyle,
                    response.Id,
                    response.Model,
                    response.Usage?.InputTokenCount,
                    response.Usage?.OutputTokenCount,
                    response.Usage?.TotalTokenCount);

                // Step 15: Convert the result into our Application DTO.
                return new SummarizeTicketResponseDTO()
                {
                    Summary = summary.Trim(),

                    Model = response.Model ?? _settings.Model,

                    ResponseId = response.Id ?? string.Empty,

                    InputTokens =
                        response.Usage?.InputTokenCount ?? 0,

                    OutputTokens =
                        response.Usage?.OutputTokenCount ?? 0,

                    TotalTokens =
                        response.Usage?.TotalTokenCount ?? 0
                };
            }
            catch (ClientResultException ex)
            {
                // Step 16: Handle OpenAI API errors.
                //
                // Examples:
                // - Invalid API key
                // - Rate limits
                // - Insufficient quota
                // - Model access restrictions

                _logger.LogWarning(
                    "OpenAI API request failed. HTTP Status: {Status}",
                    ex.Status);

                // Our Global Exception Handler will handle AIException.
                throw new AIException(
                    "OpenAI could not generate the ticket summary.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // Step 17: Handle our configured timeout.
                //
                // Caller-initiated cancellations are not converted
                // into AIException.
                _logger.LogWarning(
                    "OpenAI ticket summarization timed out. " +
                    "Configured timeout: {TimeoutSeconds} seconds.",
                    _settings.TimeoutSeconds);

                throw new AIException(
                    "The AI request took too long to complete.");
            }
            catch (HttpRequestException)
            {
                // Step 18: Handle network-related failures.
                _logger.LogWarning(
                    "A network error occurred while contacting OpenAI.");

                throw new AIException(
                    "Unable to communicate with the OpenAI service.");
            }
        }
    }
}