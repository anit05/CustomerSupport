
using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Mvc.Controllers
{
    [Authorize]
    public sealed class TicketsController : Controller
    {
        private readonly ITicketsApi _tickets;
        private readonly ICatalogApi _catalog;

        public TicketsController(
            ITicketsApi tickets,
            ICatalogApi catalog)
        {
            _tickets = tickets;
            _catalog = catalog;
        }

        // Display tickets belonging to the current Customer.
        [Authorize(Roles = "Customer")]
        [HttpGet("/tickets/my")]
        public async Task<IActionResult> Mine()
        {
            var result = await _tickets.MineAsync();

            if (!result.Ok)
                TempData["Error"] = result.Message;

            return View("Index", new TicketListPage
            {
                IsCustomer = true,
                Title = "My tickets",
                MyTickets = result.Data ?? []
            });
        }

        // Display tickets assigned to the Support Executive.
        [Authorize(Roles = "SupportExecutive")]
        [HttpGet("/support/tickets")]
        public Task<IActionResult> Support(
            [FromQuery] TicketSearch filter)
        {
            return SearchList(filter, "Assigned tickets");
        }

        // Display tickets available to the Administrator.
        [Authorize(Roles = "Administrator")]
        [HttpGet("/admin/tickets")]
        public Task<IActionResult> Admin(
            [FromQuery] TicketSearch filter)
        {
            return SearchList(filter, "Ticket management");
        }

        // Common method for searching and displaying tickets.
        private async Task<IActionResult> SearchList(
            TicketSearch filter,
            string title)
        {
            filter.PageNumber = Math.Max(1, filter.PageNumber);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            var result = await _tickets.SearchAsync(filter);
            if (!result.Ok)
                TempData["Error"] = result.Message;

            var statuses = await _catalog.StatusesAsync();
            var priorities = await _catalog.PrioritiesAsync();

            return View("Index", new TicketListPage
            {
                Title = title,
                Filter = filter,
                Results = result.Data ?? new(),
                Statuses = statuses.Data ?? [],
                Priorities = priorities.Data ?? []
            });
        }

        // Display the form for creating a ticket.
        [Authorize(Roles = "Customer")]
        [HttpGet("/tickets/new")]
        public async Task<IActionResult> Create()
        {
            var model = new TicketCreateForm();
            await PopulateCreateAsync(model);

            return View(model);
        }

        // Create a new support ticket.
        [Authorize(Roles = "Customer")]
        [HttpPost("/tickets/new")]
        public async Task<IActionResult> Create(TicketCreateForm model)
        {
            if (ModelState.IsValid)
            {
                var result = await _tickets.CreateAsync(
                    new TicketCreateRequest
                    {
                        ProductId = model.ProductId,
                        CategoryId = model.CategoryId,
                        PriorityId = model.PriorityId,
                        Subject = model.Subject,
                        Description = model.Description
                    });

                if (result.Ok && result.Data is not null)
                {
                    TempData["Success"] =
                        "Support ticket created successfully.";

                    return RedirectToAction(
                        nameof(Details),
                        new
                        {
                            id = result.Data.Id
                        });
                }

                ModelState.AddModelError(
                    string.Empty,
                    result.Message);
            }

            await PopulateCreateAsync(model);
            return View(model);
        }

        // Load product, category, and priority dropdowns.
        private async Task PopulateCreateAsync(
            TicketCreateForm model)
        {
            model.Products =
                (await _catalog.ProductsAsync()).Data ?? [];

            model.Categories =
                (await _catalog.CategoriesAsync()).Data ?? [];

            model.Priorities =
                (await _catalog.PrioritiesAsync()).Data ?? [];
        }

        // Display Ticket Details and optionally preselect AI suggestions.
        [HttpGet("/tickets/{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            [FromQuery] bool applyAi = false,
            [FromQuery] int? suggestedCategoryId = null,
            [FromQuery] int? suggestedPriorityId = null)
        {
            // STEP 1: Retrieve the ticket from the existing Web API.
            var result = await _tickets.GetAsync(id);

            if (!result.Ok || result.Data is null)
            {
                if (result.StatusCode == 403)
                    return Redirect("/access-denied");

                if (result.StatusCode == 404)
                    return Redirect("/not-found");

                TempData["Error"] = result.Message;
                return Redirect("/");
            }

            // STEP 2: Prepare the Ticket Details view model.
            var model = new TicketDetailsPage
            {
                Ticket = result.Data,
                IsCustomer = User.IsInRole("Customer"),
                IsAdmin = User.IsInRole("Administrator"),
                IsSupport = User.IsInRole("SupportExecutive"),

                // Initially select the values currently stored in SQL Server.
                SelectedCategoryId = result.Data.CategoryId,
                SelectedPriorityId = result.Data.PriorityId
            };

            if (!model.IsCustomer)
            {
                // Load the existing assignment and status information.
                model.Assignments =
                    (await _tickets.AssignmentsAsync(id)).Data ?? [];

                model.Statuses =
                    (await _catalog.StatusesAsync()).Data ?? [];

                // NEW: Load the available Categories and Priorities.
                var categories = await _catalog.CategoriesAsync();
                var priorities = await _catalog.PrioritiesAsync();

                model.Categories = (categories.Data ?? [])
                    .Where(x => x.IsActive)
                    .ToList();

                model.Priorities = (priorities.Data ?? [])
                    .Where(x => x.IsActive)
                    .ToList();

                // STEP 3: Preselect validated AI suggestions when requested.
                // This only changes dropdown selections, not SQL Server.
                if (applyAi && model.Ticket.StatusId != 5)
                {
                    // Accept the suggested Category only if it is active.
                    if (suggestedCategoryId.HasValue &&
                        model.Categories.Any(x =>
                            x.Id == suggestedCategoryId.Value))
                    {
                        model.SelectedCategoryId =
                            suggestedCategoryId.Value;
                    }

                    // Accept the suggested Priority only if it is active.
                    if (suggestedPriorityId.HasValue &&
                        model.Priorities.Any(x =>
                            x.Id == suggestedPriorityId.Value))
                    {
                        model.SelectedPriorityId =
                            suggestedPriorityId.Value;
                    }

                    // Show a message only when preselected values
                    // differ from the ticket's current database values.
                    model.HasPendingSuggestions =
                        model.SelectedCategoryId != model.Ticket.CategoryId ||
                        model.SelectedPriorityId != model.Ticket.PriorityId;
                }
            }

            // Preserve the existing attachment-loading behavior.
            var attachmentResult = await _tickets.AttachmentsAsync(id);

            if (attachmentResult.Ok &&
                attachmentResult.Data is not null)
            {
                model.Ticket.Attachments = attachmentResult.Data;
            }

            return View(model);
        }

        // NEW: Save the Category and Priority approved by support staff.
        [Authorize(Roles = "SupportExecutive,Administrator")]
        [HttpPost("/tickets/classification")]
        public async Task<IActionResult> UpdateClassification(
            TicketClassificationForm form)
        {
            // Reject invalid input before calling the backend.
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Select a valid Category and Priority.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = form.TicketId,
                        applyAi = true,
                        suggestedCategoryId = form.CategoryId,
                        suggestedPriorityId = form.PriorityId
                    });
            }

            // Update the ticket using our existing TicketsApi service.
            var result = await _tickets.UpdateClassificationAsync(
                form.TicketId,
                form.CategoryId,
                form.PriorityId);

            if (result.Ok)
            {
                TempData["Success"] =
                    "Ticket Category and Priority updated successfully.";

                // Reload Ticket Details to show the saved database values.
                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = form.TicketId
                    });
            }

            // Keep the selected values available when saving fails.
            TempData["Error"] = result.Message;

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = form.TicketId,
                    applyAi = true,
                    suggestedCategoryId = form.CategoryId,
                    suggestedPriorityId = form.PriorityId
                });
        }

        // Add a public or internal ticket comment.
        [HttpPost("/tickets/comment")]
        public async Task<IActionResult> Comment(CommentForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Enter a comment before submitting.";
            }
            else if (User.IsInRole("Customer") && form.IsInternal)
            {
                TempData["Error"] =
                    "Customers cannot post internal comments.";
            }
            else
            {
                var result = await _tickets.AddCommentAsync(
                    form.TicketId,
                    form.Content,
                    form.IsInternal);

                TempData[result.Ok ? "Success" : "Error"] =
                    result.Message;
            }

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = form.TicketId
                });
        }

        // Assign a ticket to a Support Executive.
        [Authorize(Roles = "Administrator")]
        [HttpPost("/tickets/assign")]
        public async Task<IActionResult> Assign(AssignmentForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Enter a valid Support Executive ID.";
            }
            else
            {
                var result = await _tickets.AssignAsync(
                    form.TicketId,
                    form.SupportExecutiveId);

                TempData[result.Ok ? "Success" : "Error"] =
                    result.Message;
            }

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = form.TicketId
                });
        }

        // Change the ticket status.
        [Authorize(Roles = "SupportExecutive,Administrator")]
        [HttpPost("/tickets/status")]
        public async Task<IActionResult> Status(StatusForm form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Choose a valid ticket status.";
            }
            else
            {
                var result = await _tickets.ChangeStatusAsync(
                    form.TicketId,
                    form.StatusId,
                    form.Remarks);

                TempData[result.Ok ? "Success" : "Error"] =
                    result.Message;
            }

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = form.TicketId
                });
        }

        // Upload an attachment to an existing ticket.
        [HttpPost("/tickets/upload")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(UploadForm form)
        {
            if (form.File is null || form.File.Length == 0)
            {
                TempData["Error"] = "Select a file to upload.";
            }
            else
            {
                var result = await _tickets.UploadAsync(
                    form.TicketId,
                    form.File);

                TempData[result.Ok ? "Success" : "Error"] =
                    result.Message;
            }

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = form.TicketId
                });
        }

        // Download a ticket attachment.
        [HttpGet("/tickets/{ticketId:int}/attachments/{attachmentId:int}/download")]
        public async Task<IActionResult> Download(
            int ticketId,
            int attachmentId)
        {
            var result = await _tickets.DownloadAsync(
                ticketId,
                attachmentId);

            if (!result.Ok || result.Data is null)
            {
                TempData["Error"] = result.Message;

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = ticketId
                    });
            }

            return File(
                result.Data.Bytes,
                result.Data.ContentType,
                result.Data.FileName);
        }
    }
}
