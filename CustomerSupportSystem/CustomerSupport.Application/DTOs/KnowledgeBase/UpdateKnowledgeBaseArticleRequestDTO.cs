using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.KnowledgeBase
{
  public sealed class UpdateKnowledgeBaseArticleRequestDTO
  {
    [Required]
    [StringLength(250, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    // Optional Product association.
    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }

    // Controls whether normal Users can see the Article.
    public bool IsPublished { get; set; }

    // Allows an Administrator to activate or deactivate the Article.
    public bool IsActive { get; set; }
  }
}
