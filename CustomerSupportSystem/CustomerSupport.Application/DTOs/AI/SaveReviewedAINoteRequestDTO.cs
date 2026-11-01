using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Application.DTOs.AI
{
    public sealed class SaveReviewedAINoteRequestDTO
    {
        // A staff member reviews and edits the proposed note
        // before sending this request.
        [Required]
        [StringLength(1900, MinimumLength = 1)]
        public string ApprovedNote { get; set; } = string.Empty;

        public bool IsInternal { get; set; } = true;
    }
}