using CustomerSupport.Mvc.Interfaces.Services;
using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Mvc.Controllers
{
    // Handles AI-related pages and actions in our MVC application.
    [Authorize(Roles = "Administrator,SupportExecutive")]
    public sealed class TicketAIController : Controller
    {
        private readonly ITicketAIApi _ai;
        private readonly ITicketsApi _tickets;
        private readonly IContentApi _content;

        // Receive the existing API services through Dependency Injection.
        public TicketAIController(
            ITicketAIApi ai,
            ITicketsApi tickets,
            IContentApi content)
        {
            _ai = ai;
            _tickets = tickets;
            _content = content;
        }

        // GET: /tickets/1/ai
        // Display the AI Assistant page for an existing ticket.
        [HttpGet("/tickets/{id:int}/ai")]
        public async Task<IActionResult> Index(int id)
        {
            // Load ticket details and available Knowledge Base articles.
            var result = await LoadPageAsync(id);

            if (!result.Ok || result.Data is null)
            {
                return PageError(result);
            }

            return View(result.Data);
        }

        // POST: /tickets/1/ai/analysis
        // Generate strongly typed analysis without saving anything.
        [HttpPost("/tickets/{id:int}/ai/analysis")]
        public async Task<IActionResult> Analyze(
            int id,
            CancellationToken cancellationToken)
        {
            var page = await LoadPageAsync(id);

            if (!page.Ok || page.Data is null)
            {
                return PageError(page);
            }

            // Request structured analysis from the Web API.
            var result = await _ai.AnalyzeAsync(
                id,
                cancellationToken);

            if (result.Ok && result.Data is not null)
            {
                // Display the eight analysis values.
                page.Data.Analysis = result.Data;
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);
            }

            return View("Index", page.Data);
        }

        // POST: /tickets/1/ai/reply-draft
        // Generate a customer-facing reply draft.
        [HttpPost("/tickets/{id:int}/ai/reply-draft")]
        public Task<IActionResult> ReplyDraft(
            int id,
            TicketAIDraftForm form,
            CancellationToken cancellationToken)
        {
            // Customer replies must not use internal comments.
            return GenerateDraftAsync(
                id,
                form,
                isInternal: false,
                cancellationToken: cancellationToken);
        }

        // POST: /tickets/1/ai/internal-note-draft
        // Generate an internal support note.
        [HttpPost("/tickets/{id:int}/ai/internal-note-draft")]
        public Task<IActionResult> InternalNoteDraft(
            int id,
            TicketAIDraftForm form,
            CancellationToken cancellationToken)
        {
            // Internal notes may use both public and internal comments.
            return GenerateDraftAsync(
                id,
                form,
                isInternal: true,
                cancellationToken: cancellationToken);
        }

        // Shared method for generating either type of AI draft.
        private async Task<IActionResult> GenerateDraftAsync(
            int id,
            TicketAIDraftForm form,
            bool isInternal,
            CancellationToken cancellationToken)
        {
            // Load the ticket and Knowledge Base dropdown data.
            var page = await LoadPageAsync(id);

            if (!page.Ok || page.Data is null)
            {
                return PageError(page);
            }

            // Preserve the selected Knowledge Base article.
            page.Data.KnowledgeBaseArticleId =
                form.KnowledgeBaseArticleId;

            // Return the page when the submitted form is invalid.
            if (!ModelState.IsValid)
            {
                return View("Index", page.Data);
            }

            // Select the appropriate backend AI operation.
            var result = isInternal
                ? await _ai.InternalNoteDraftAsync(
                    id,
                    form.KnowledgeBaseArticleId,
                    cancellationToken)
                : await _ai.ReplyDraftAsync(
                    id,
                    form.KnowledgeBaseArticleId,
                    cancellationToken);

            if (result.Ok && result.Data is not null)
            {
                // Display the draft and select its default visibility.
                page.Data.Draft = result.Data;

                page.Data.DraftTitle = isInternal
                    ? "Internal note draft"
                    : "Customer reply draft";

                page.Data.IsInternal = isInternal;
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);
            }

            return View("Index", page.Data);
        }

        // POST: /tickets/1/ai/save-draft
        // Save the reviewed text as a ticket comment.
        [HttpPost("/tickets/{id:int}/ai/save-draft")]
        public async Task<IActionResult> SaveDraft(
            int id,
            SaveReviewedAIDraftForm form,
            CancellationToken cancellationToken)
        {
            // Require explicit confirmation for customer-visible comments.
            if (!form.IsInternal && !form.ConfirmCustomerVisible)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Confirm that the public comment is accurate " +
                    "and contains no confidential information.");
            }

            if (ModelState.IsValid)
            {
                // Send only the final reviewed text to the Web API.
                var result = await _ai.SaveReviewedDraftAsync(
                    id,
                    form.ApprovedText.Trim(),
                    form.IsInternal,
                    cancellationToken);

                if (result.Ok)
                {
                    // Return to ticket details after successful saving.
                    TempData["Success"] =
                        "Reviewed AI draft saved as a ticket comment.";

                    return RedirectToAction(
                        "Details",
                        "Tickets",
                        new
                        {
                            id
                        });
                }

                ModelState.AddModelError(
                    string.Empty,
                    result.Message);
            }

            // Preserve the edited draft if validation or saving fails.
            var page = await LoadPageAsync(id);

            if (!page.Ok || page.Data is null)
            {
                return PageError(page);
            }

            page.Data.Draft = new TicketAIDraftResult
            {
                TicketId = id,
                DraftText = form.ApprovedText ?? string.Empty
            };

            page.Data.DraftTitle = "Reviewed draft";
            page.Data.IsInternal = form.IsInternal;
            page.Data.ConfirmCustomerVisible =
                form.ConfirmCustomerVisible;

            return View("Index", page.Data);
        }

        // Load ticket details and eligible Knowledge Base articles.
        private async Task<ApiResult<TicketAIPage>> LoadPageAsync(
            int id)
        {
            // Retrieve the ticket through the existing API service.
            var ticket = await _tickets.GetAsync(id);

            if (!ticket.Ok || ticket.Data is null)
            {
                return ApiResult<TicketAIPage>.Failure(
                    ticket.Message,
                    ticket.StatusCode);
            }

            var page = new TicketAIPage
            {
                Ticket = ticket.Data
            };

            // Retrieve Knowledge Base articles for the dropdown.
            var articles = await _content.ArticlesAsync(
                new ContentSearch
                {
                    PageNumber = 1,
                    PageSize = 100
                });

            if (articles.Ok && articles.Data is not null)
            {
                // Show only active, published, product-compatible articles.
                page.Articles = articles.Data.Items
                    .Where(x =>
                        x.IsActive &&
                        x.IsPublished &&
                        (!x.ProductId.HasValue ||
                         x.ProductId == ticket.Data.ProductId))
                    .OrderBy(x => x.Title)
                    .ToList();
            }

            return new ApiResult<TicketAIPage>(
                true,
                page,
                string.Empty);
        }

        // Handle errors returned while loading a ticket.
        private IActionResult PageError(
            ApiResult<TicketAIPage> result)
        {
            if (result.StatusCode == 403)
            {
                return Redirect("/access-denied");
            }

            if (result.StatusCode == 404)
            {
                return Redirect("/not-found");
            }

            TempData["Error"] = result.Message;

            return Redirect(
                User.IsInRole("Administrator")
                    ? "/admin/tickets"
                    : "/support/tickets");
        }
    }
}
