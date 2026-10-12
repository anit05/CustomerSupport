using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.FAQs
{
  // Query-string values supported by GET /api/faqs.
  public sealed class FAQQueryParameters
  {
    // Optional free-text search over Question and Answer.
    [StringLength(200)]
    public string? Search { get; set; }

    // Optional Product filter.
    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
  }
}
