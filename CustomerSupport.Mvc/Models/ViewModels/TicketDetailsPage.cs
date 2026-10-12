using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class TicketDetailsPage
    {
        public TicketDetails Ticket { get; set; } = new();
        public List<TicketAssignment> Assignments { get; set; } = [];
        public List<MasterValue> Statuses { get; set; } = [];
        public bool IsAdmin { get; set; }
        public bool IsSupport { get; set; }
        public bool IsCustomer { get; set; }
        public List<int> AllowedTransitions
        {
            get
            {
                return Ticket.StatusId switch
                {
                    2 => [3], // Assigned -> InProgress
                    3 => [4], // InProgress -> Resolved
                    4 => [3, 5], // Resolved -> InProgress or Closed
                    _ => []
                };
            }
        }
    }
}
