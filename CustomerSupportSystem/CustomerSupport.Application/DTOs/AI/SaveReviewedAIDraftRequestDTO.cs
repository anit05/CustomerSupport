using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.AI
{
  public sealed class SaveReviewedAIDraftRequestDTO
  {
    [Required]
    [StringLength(1900, MinimumLength = 1)]
    public string ApprovedText { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = true;
  }
}
