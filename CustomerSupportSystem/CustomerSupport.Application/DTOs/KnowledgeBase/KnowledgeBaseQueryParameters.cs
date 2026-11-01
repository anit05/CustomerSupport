using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.KnowledgeBase
{
  public sealed class KnowledgeBaseQueryParameters
  {
    // Optional free-text search over Title and Content.
    [StringLength(200)]
    public string? Search { get; set; }

    // Optional Product filter.
    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }

    // Standard paging values reused with PagedResult<T>.
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
  }
}
