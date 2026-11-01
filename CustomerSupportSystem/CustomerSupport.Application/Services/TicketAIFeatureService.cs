using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Prompts;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Services
{
    public sealed class TicketAIFeatureService : ITicketAIFeatureService
    {
        private readonly ITicketService _ticketService;
        private readonly ITicketCategoryService _categoryService;
        private readonly IMasterDataService _masterDataService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IOpenAITextGenerationService _aiTextService;
        private readonly IOpenAITicketSummaryService _summaryService;

        public TicketAIFeatureService(
            ITicketService ticketService,
            ITicketCategoryService categoryService,
            IMasterDataService masterDataService,
            IKnowledgeBaseService knowledgeBaseService,
            IOpenAITextGenerationService aiTextService,
            IOpenAITicketSummaryService summaryService)
        {
            _ticketService = ticketService;
            _categoryService = categoryService;
            _masterDataService = masterDataService;
            _knowledgeBaseService = knowledgeBaseService;
            _aiTextService = aiTextService;
            _summaryService = summaryService;
        }

        public async Task<TicketAIResponseDTO> GenerateAsync(
            int ticketId,
            GenerateTicketAIRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            // STEP 1: Validate the feature BEFORE requesting OpenAI.
            string feature = request.Feature?.Trim().ToLowerInvariant()
                ?? string.Empty;

            if (feature is not ("summary" or "category" or
                "priority" or "sentiment" or "replydraft" or
                "internalnote"))
            {
                throw new ApplicationValidationException(
                    ["Feature must be Summary, Category, Priority, " +
                     "Sentiment, ReplyDraft, or InternalNote."]);
            }

            if (request.KnowledgeBaseArticleId.HasValue &&
                feature is not ("replydraft" or "internalnote"))
            {
                throw new ApplicationValidationException(
                    ["Knowledge Base context is supported only for " +
                     "ReplyDraft and InternalNote."]);
            }

            // STEP 2: Reuse the existing ticket service.
            // It retrieves SQL Server data through the repository
            // and checks whether the current user can view the ticket.
            TicketDetailsResponseDTO ticket =
                await _ticketService.GetByIdAsync(ticketId);

            // STEP 3: Prepare relevant ticket details and comments.
            string ticketText = BuildTicketText(ticket);
            string ticketContext = BuildTicketContext(ticket);

            // STEP 4: Summary reuses the existing Chapter 3.2 service.
            if (feature == "summary")
            {
                SummarizeTicketResponseDTO summary = await
                    _summaryService.SummarizeAsync(
                        new SummarizeTicketRequestDTO
                        {
                            TicketComments = ticketText,
                            ContextInformation = ticketContext,
                            PromptStyle = "ZeroShot"
                        },
                        cancellationToken);

                return new TicketAIResponseDTO
                {
                    TicketId = ticketId,
                    Feature = "Summary",
                    OutputText = summary.Summary,
                    Model = summary.Model,
                    ResponseId = summary.ResponseId,
                    InputTokens = summary.InputTokens,
                    OutputTokens = summary.OutputTokens,
                    TotalTokens = summary.TotalTokens
                };
            }

            // STEP 5: Read active category / priority masters.
            var categories = feature == "category"
                ? await _categoryService.GetAllAsync(activeOnly: true)
                : null;

            var priorities = feature == "priority"
                ? await _masterDataService.GetTicketPrioritiesAsync()
                : null;

            string categoryNames = categories == null
                ? string.Empty
                : string.Join(", ", categories.Select(x => x.Name));

            string priorityNames = priorities == null
                ? string.Empty
                : string.Join(", ", priorities
                    .Where(x => x.IsActive)
                    .Select(x => x.Name));

            // If no permitted labels exist, do not ask the model
            // to invent a valid category or priority.
            if (feature == "category" && string.IsNullOrWhiteSpace(categoryNames))
            {
                throw new ApplicationValidationException(
                    ["No active ticket categories are available."]);
            }

            if (feature == "priority" && string.IsNullOrWhiteSpace(priorityNames))
            {
                throw new ApplicationValidationException(
                    ["No active ticket priorities are available."]);
            }

            // STEP 6: Optionally load a published Knowledge Base article.
            // This also uses the existing service and repository.
            string knowledgeBaseText = string.Empty;
            if (request.KnowledgeBaseArticleId.HasValue)
            {
                var article = await _knowledgeBaseService.GetByIdAsync(
                    request.KnowledgeBaseArticleId.Value);

                if (!article.IsActive || !article.IsPublished)
                {
                    throw new ApplicationValidationException(
                        ["Only active, published articles may be used for AI."]);
                }

                // Do not attach product-specific guidance to a
                // ticket belonging to another product.
                if (article.ProductId.HasValue &&
                    article.ProductId != ticket.ProductId)
                {
                    throw new ApplicationValidationException(
                        ["The Knowledge Base article does not match " +
                         "the ticket's product."]);
                }

                string excerpt = article.Content.Length > 3000
                    ? article.Content[..3000]
                    : article.Content;

                knowledgeBaseText =
                    $"Article: {article.Title}\nGuidance: {excerpt}";
            }

            // STEP 7: Prepare the fixed Developer Message.
            string instructions = TicketAIPromptBuilder.GetInstructions(
                feature, categoryNames, priorityNames);

            // STEP 8: Add selected ticket and Knowledge Base context.
            string userInput =
                "<ticket_details>\n" + ticketContext +
                "\n</ticket_details>\n" +
                "<ticket_comments>\n" + ticketText +
                "\n</ticket_comments>";

            if (!string.IsNullOrWhiteSpace(knowledgeBaseText))
            {
                userInput += "\n<knowledge_base_reference>\n" +
                    knowledgeBaseText + "\n</knowledge_base_reference>";
            }

            // STEP 9: Invoke the OpenAI provider using its interface.
            AITextResultDTO ai = await _aiTextService.GenerateAsync(
                instructions, userInput, cancellationToken);

            // STEP 10: Validate label outputs against existing masters.
            // Free-form labels are validated here; Chapter 3.4
            // introduces Structured Outputs for stricter contracts.
            string resultText = ai.Text.Trim();
            int? categoryId = null;
            TicketPriorityType? priorityId = null;

            if (feature == "category" &&
                !resultText.Equals("Unclear", StringComparison.OrdinalIgnoreCase))
            {
                var category = categories!.FirstOrDefault(x =>
                    x.Name.Equals(resultText, StringComparison.OrdinalIgnoreCase));

                if (category == null)
                    throw new AIException("AI returned an unsupported category.");

                categoryId = category.Id;
                resultText = category.Name;
            }

            if (feature == "priority" &&
                !resultText.Equals("Unclear", StringComparison.OrdinalIgnoreCase))
            {
                var priority = priorities!.FirstOrDefault(x =>
                    x.IsActive && x.Name.Equals(
                        resultText, StringComparison.OrdinalIgnoreCase));

                if (priority == null)
                    throw new AIException("AI returned an unsupported priority.");

                priorityId = priority.Id;
                resultText = priority.Name;
            }

            if (feature == "sentiment")
            {
                string[] labels =
                    ["Positive", "Neutral", "Negative", "Mixed", "Unclear"];

                string? match = labels.FirstOrDefault(x =>
                    x.Equals(resultText, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                    throw new AIException("AI returned an unsupported sentiment.");

                resultText = match;
            }

            // STEP 11: Return the suggestion without changing the ticket.
            return new TicketAIResponseDTO
            {
                TicketId = ticketId,
                Feature = request.Feature,
                OutputText = resultText,
                SuggestedCategoryId = categoryId,
                SuggestedPriorityId = priorityId,
                Model = ai.Model,
                ResponseId = ai.ResponseId,
                InputTokens = ai.InputTokens,
                OutputTokens = ai.OutputTokens,
                TotalTokens = ai.TotalTokens
            };
        }

        // A staff member explicitly approves this text before saving.
        public Task<TicketCommentResponseDTO> SaveReviewedNoteAsync(
            int ticketId,
            SaveReviewedAINoteRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.ApprovedNote))
            {
                throw new ApplicationValidationException(
                    ["Approved note cannot be empty."]);
            }

            // Provenance label helps distinguish an AI-assisted
            // note from a purely manual staff note.
            string content =
                "[AI-assisted; reviewed by support staff]\n" +
                request.ApprovedNote.Trim();

            if (content.Length > 2000)
            {
                throw new ApplicationValidationException(
                    ["Reviewed note is too long to save."]);
            }

            // Reuse our existing authorization, comment persistence,
            // history and audit behavior. It will reject Closed tickets.
            return _ticketService.AddCommentAsync(
                ticketId,
                new AddTicketCommentRequestDTO
                {
                    Content = content,
                    IsInternal = request.IsInternal
                });
        }

        //Build Ticket Context
        private static string BuildTicketContext(TicketDetailsResponseDTO ticket)
        {
            return $"""
                Ticket: {ticket.TicketNumber}
                Subject: {ticket.Subject}
                Product: {ticket.ProductName}
                Current Status: {ticket.StatusName}
                Current Category: {ticket.CategoryName ?? "Not assigned"}
                Current Priority: {ticket.PriorityName}
                Assigned To: {ticket.AssignedToUserName ?? "Unassigned"}
                """;
        }

        // Compose the actual ticket history from database DTOs.
        private static string BuildTicketText(TicketDetailsResponseDTO ticket)
        {
            var lines = new List<string>
            {
                $"Subject: {ticket.Subject}",
                $"Description: {ticket.Description}"
            };

            // Select recent comments, then restore chronological order.
            var recent = ticket.Comments
                .OrderByDescending(x => x.CreatedAt)
                .Take(15)
                .OrderBy(x => x.CreatedAt);

            foreach (var comment in recent)
            {
                // Include the source role/author as provided by
                // the existing ticket detail mapping.
                string text = comment.Content.Length > 600
                    ? comment.Content[..600] + " [excerpt]"
                    : comment.Content;

                lines.Add($"{comment.UserName}: {text}");
            }

            // Preserve input character limits already introduced
            // in our Chapter 3.1 summarization service.
            string combined = string.Join("\n", lines);
            return combined.Length > 12000
                ? combined[..11950] + "\n[Additional text omitted]"
                : combined;
        }
    }
}
