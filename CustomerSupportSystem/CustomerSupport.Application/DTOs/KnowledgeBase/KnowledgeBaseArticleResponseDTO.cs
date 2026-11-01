namespace CustomerSupport.Application.DTOs.KnowledgeBase
{
  public sealed class KnowledgeBaseArticleResponseDTO
  {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
  }
}
