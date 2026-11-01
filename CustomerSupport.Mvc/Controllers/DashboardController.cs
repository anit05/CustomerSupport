using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Models.ViewModels;
using CustomerSupport.Mvc.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CustomerSupport.Mvc.Controllers
{
    [Authorize]
    public sealed class DashboardController : Controller
    {
        private readonly ITicketsApi _tickets;

        public DashboardController(ITicketsApi tickets)
        {
            _tickets = tickets;
        }

        [Authorize(Roles="Customer"), HttpGet("/customer/dashboard")]
        public async Task<IActionResult> Customer()
        {
            var found = await _tickets.MineAsync();
            if (!found.Ok) TempData["Error"] = found.Message;
            var list = found.Data ?? [];
            return View("Index", new DashboardPage {
                Role = "Customer", RecentTickets = list.OrderByDescending(x=>x.CreatedAt).Take(6).ToList(),
                TotalTickets = list.Count, OpenTickets = list.Count(x=> x.StatusId == 1 || x.StatusId == 2),
                InProgressTickets = list.Count(x=>x.StatusId == 3), ResolvedTickets = list.Count(x=>x.StatusId == 4 || x.StatusId == 5),
                CanShowTotals = found.Ok
            });
        }

        [Authorize(Roles="SupportExecutive"), HttpGet("/support/dashboard")]
        public Task<IActionResult> Support()
        {
            return LoadTeamDashboard("SupportExecutive");
        }

        [Authorize(Roles="Administrator"), HttpGet("/admin/dashboard")]
        public Task<IActionResult> Admin()
        {
            return LoadTeamDashboard("Administrator");
        }

        private async Task<IActionResult> LoadTeamDashboard(string role)
        {
            var recent = await _tickets.SearchAsync(new TicketSearch { PageNumber=1, PageSize=6 });
            if (!recent.Ok) TempData["Error"] = recent.Message;
            // The backend search provides accurate total counts per status.
            var open = await _tickets.SearchAsync(new TicketSearch { StatusId=1,PageSize=1 });
            var assigned = await _tickets.SearchAsync(new TicketSearch { StatusId=2,PageSize=1 });
            var progress = await _tickets.SearchAsync(new TicketSearch { StatusId=3,PageSize=1 });
            var resolved = await _tickets.SearchAsync(new TicketSearch { StatusId=4,PageSize=1 });
            var closed = await _tickets.SearchAsync(new TicketSearch { StatusId=5,PageSize=1 });
            return View("Index", new DashboardPage {
                Role = role, RecentTickets = recent.Data?.Items ?? [],
                TotalTickets = recent.Data?.TotalRecords ?? 0,
                OpenTickets = (open.Data?.TotalRecords??0)+(assigned.Data?.TotalRecords??0),
                InProgressTickets = progress.Data?.TotalRecords??0,
                ResolvedTickets = (resolved.Data?.TotalRecords??0)+(closed.Data?.TotalRecords??0),
                CanShowTotals = recent.Ok && open.Ok && assigned.Ok && progress.Ok && resolved.Ok && closed.Ok
            });
        }
    }
}
