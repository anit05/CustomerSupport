using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    // Receives the Category and Priority approved by support staff.
    public sealed class TicketClassificationForm
    {
        [Range(1, int.MaxValue)]
        public int TicketId
        {
            get; set;
        }

        // Category is optional.
        [Range(1, int.MaxValue)]
        public int? CategoryId
        {
            get; set;
        }

        // Priority is required.
        [Range(1, int.MaxValue)]
        public int PriorityId
        {
            get; set;
        }
    }
}
