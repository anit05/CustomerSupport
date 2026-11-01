using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.FAQs
{
  // Request model used when an Administrator creates an FAQ.
  public sealed class CreateFAQRequestDTO
  {
    // Frequently asked question displayed to Users.
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Question { get; set; } = string.Empty;

    // Answer shown for the Question.
    [Required]
    public string Answer { get; set; } = string.Empty;

    // Optional Product association.
    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }
  }
}
