using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class TicketListPage
    {
        public string Title { get; set; } = "Tickets";
        public TicketSearch Filter { get; set; } = new();
        public PagedResult<TicketSummary> Results { get; set; } = new();
        public List<TicketSummary> MyTickets { get; set; } = [];
        public bool IsCustomer { get; set; }
        public List<MasterValue> Statuses { get; set; } = [];
        public List<MasterValue> Priorities { get; set; } = [];
    }
}
