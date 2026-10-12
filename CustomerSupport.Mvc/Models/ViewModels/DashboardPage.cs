using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class DashboardPage
    {
        public string Role { get; set; } = string.Empty;
        public List<TicketSummary> RecentTickets { get; set; } = [];
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public bool CanShowTotals { get; set; }
    }
}
