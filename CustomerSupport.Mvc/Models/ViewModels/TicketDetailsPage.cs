
using CustomerSupport.Mvc.Models.Api;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    // Contains the information displayed on the Ticket Details page.
    public sealed class TicketDetailsPage
    {
        public TicketDetails Ticket { get; set; } = new();

        public List<TicketAssignment> Assignments { get; set; } = [];

        public List<MasterValue> Statuses { get; set; } = [];
        public List<TicketCategory> Categories { get; set; } = [];
        public List<MasterValue> Priorities { get; set; } = [];
        public int? SelectedCategoryId
        {
            get; set;
        }

        public int SelectedPriorityId
        {
            get; set;
        }

        public bool HasPendingSuggestions
        {
            get; set;
        }

        public bool IsAdmin
        {
            get; set;
        }

        public bool IsSupport
        {
            get; set;
        }

        public bool IsCustomer
        {
            get; set;
        }

        // Existing ticket workflow rules.
        public List<int> AllowedTransitions
        {
            get
            {
                return Ticket.StatusId switch
                {
                    2 => [3],    // Assigned -> InProgress
                    3 => [4],    // InProgress -> Resolved
                    4 => [3, 5], // Resolved -> InProgress or Closed
                    _ => []
                };
            }
        }
    }
}
