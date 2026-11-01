
using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    // Receives the final text reviewed by a Support Executive.
    public sealed class SaveReviewedAIDraftForm
    {
        // Require valid reviewed text before saving.
        [Required]
        [StringLength(1900, MinimumLength = 1)]
        public string ApprovedText { get; set; } = string.Empty;

        // true: Internal comment.
        // false: Customer-visible comment.
        public bool IsInternal { get; set; } = true;

        // Required by the MVC Controller for customer-visible comments.
        public bool ConfirmCustomerVisible
        {
            get; set;
        }
    }
}
