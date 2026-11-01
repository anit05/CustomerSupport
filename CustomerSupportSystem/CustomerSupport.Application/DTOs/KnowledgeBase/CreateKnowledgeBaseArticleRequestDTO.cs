using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.KnowledgeBase
{
  public sealed class CreateKnowledgeBaseArticleRequestDTO
  {
    // Article title shown in search results and article lists.
    [Required]
    [StringLength(250, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    // Full reusable support content.
    [Required]
    public string Content { get; set; } = string.Empty;

    // Optional Product association.
    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }

    // true makes the Article visible to normal Users immediately.
    // false keeps it as an Administrator-visible draft.
    public bool IsPublished { get; set; }
  }
}