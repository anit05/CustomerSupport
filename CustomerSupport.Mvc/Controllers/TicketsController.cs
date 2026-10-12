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

        public TicketsController(ITicketsApi tickets, ICatalogApi catalog)
        {
            _tickets = tickets;
            _catalog = catalog;
        }

        [Authorize(Roles="Customer"), HttpGet("/tickets/my")]
        public async Task<IActionResult> Mine()
        {
            var r = await _tickets.MineAsync();
            if (!r.Ok) TempData["Error"] = r.Message;
            return View("Index", new TicketListPage { IsCustomer=true,Title="My tickets",MyTickets=r.Data ?? [] });
        }

        [Authorize(Roles="SupportExecutive"), HttpGet("/support/tickets")]
        public Task<IActionResult> Support([FromQuery] TicketSearch filter)
        {
            return SearchList(filter,"Assigned tickets");
        }

        [Authorize(Roles="Administrator"), HttpGet("/admin/tickets")]
        public Task<IActionResult> Admin([FromQuery] TicketSearch filter)
        {
            return SearchList(filter,"Ticket management");
        }

        private async Task<IActionResult> SearchList(TicketSearch filter,string title)
        {
            filter.PageNumber = Math.Max(1,filter.PageNumber);
            filter.PageSize = Math.Clamp(filter.PageSize,1,100);
            var result = await _tickets.SearchAsync(filter);
            if (!result.Ok) TempData["Error"] = result.Message;
            var statuses = await _catalog.StatusesAsync();
            var priorities = await _catalog.PrioritiesAsync();
            return View("Index",new TicketListPage {
                Title=title,Filter=filter,Results=result.Data??new(),
                Statuses=statuses.Data??[],Priorities=priorities.Data??[]
            });
        }

        [Authorize(Roles="Customer"), HttpGet("/tickets/new")]
        public async Task<IActionResult> Create()
        {
            var model = new TicketCreateForm();
            await PopulateCreateAsync(model);
            return View(model);
        }

        [Authorize(Roles="Customer"), HttpPost("/tickets/new")]
        public async Task<IActionResult> Create(TicketCreateForm model)
        {
            if (ModelState.IsValid)
            {
                var result = await _tickets.CreateAsync(new TicketCreateRequest {
                    ProductId=model.ProductId, CategoryId=model.CategoryId,PriorityId=model.PriorityId,
                    Subject=model.Subject,Description=model.Description
                });
                if (result.Ok && result.Data is not null)
                {
                    TempData["Success"] = "Support ticket created successfully.";
                    return RedirectToAction(nameof(Details),new {id=result.Data.Id});
                }
                ModelState.AddModelError(string.Empty,result.Message);
            }
            await PopulateCreateAsync(model);
            return View(model);
        }

        private async Task PopulateCreateAsync(TicketCreateForm model)
        {
            model.Products=(await _catalog.ProductsAsync()).Data??[];
            model.Categories=(await _catalog.CategoriesAsync()).Data??[];
            model.Priorities=(await _catalog.PrioritiesAsync()).Data??[];
        }

        [HttpGet("/tickets/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var result=await _tickets.GetAsync(id);
            if (!result.Ok || result.Data is null)
            {
                if (result.StatusCode is 403) return Redirect("/access-denied");
                if (result.StatusCode is 404) return Redirect("/not-found");
                TempData["Error"]=result.Message;
                return Redirect("/");
            }
            var model=new TicketDetailsPage {
                Ticket=result.Data, IsCustomer=User.IsInRole("Customer"),IsAdmin=User.IsInRole("Administrator"),
                IsSupport=User.IsInRole("SupportExecutive")
            };
            if (!model.IsCustomer)
            {
                model.Assignments=(await _tickets.AssignmentsAsync(id)).Data??[];
                model.Statuses=(await _catalog.StatusesAsync()).Data??[];
            }
            // Load attachment listing separately; details also contains an attachment snapshot.
            var attachmentResult=await _tickets.AttachmentsAsync(id);
            if (attachmentResult.Ok && attachmentResult.Data is not null) model.Ticket.Attachments=attachmentResult.Data;
            return View(model);
        }

        [HttpPost("/tickets/comment")]
        public async Task<IActionResult> Comment(CommentForm form)
        {
            if (!ModelState.IsValid) TempData["Error"]="Enter a comment before submitting.";
            else if (User.IsInRole("Customer") && form.IsInternal) TempData["Error"]="Customers cannot post internal comments.";
            else
            {
                var result=await _tickets.AddCommentAsync(form.TicketId,form.Content,form.IsInternal);
                TempData[result.Ok?"Success":"Error"]=result.Message;
            }
            return RedirectToAction(nameof(Details),new{id=form.TicketId});
        }

        [Authorize(Roles="Administrator"), HttpPost("/tickets/assign")]
        public async Task<IActionResult> Assign(AssignmentForm form)
        {
            if (!ModelState.IsValid) TempData["Error"]="Enter a valid Support Executive ID.";
            else
            {
                var result=await _tickets.AssignAsync(form.TicketId,form.SupportExecutiveId);
                TempData[result.Ok?"Success":"Error"]=result.Message;
            }
            return RedirectToAction(nameof(Details),new{id=form.TicketId});
        }

        [Authorize(Roles="SupportExecutive,Administrator"), HttpPost("/tickets/status")]
        public async Task<IActionResult> Status(StatusForm form)
        {
            if (!ModelState.IsValid) TempData["Error"]="Choose a valid ticket status.";
            else
            {
                // Server-side rules remain authoritative in the Web API.
                var result=await _tickets.ChangeStatusAsync(form.TicketId,form.StatusId,form.Remarks);
                TempData[result.Ok?"Success":"Error"]=result.Message;
            }
            return RedirectToAction(nameof(Details),new{id=form.TicketId});
        }

        [HttpPost("/tickets/upload")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(UploadForm form)
        {
            if (form.File is null || form.File.Length == 0) TempData["Error"]="Select a file to upload.";
            else
            {
                var result=await _tickets.UploadAsync(form.TicketId,form.File);
                TempData[result.Ok?"Success":"Error"]=result.Message;
            }
            return RedirectToAction(nameof(Details),new{id=form.TicketId});
        }

        [HttpGet("/tickets/{ticketId:int}/attachments/{attachmentId:int}/download")]
        public async Task<IActionResult> Download(int ticketId,int attachmentId)
        {
            var result=await _tickets.DownloadAsync(ticketId,attachmentId);
            if (!result.Ok || result.Data is null)
            {
                TempData["Error"]=result.Message;
                return RedirectToAction(nameof(Details),new{id=ticketId});
            }
            return File(result.Data.Bytes,result.Data.ContentType,result.Data.FileName);
        }
    }
}
