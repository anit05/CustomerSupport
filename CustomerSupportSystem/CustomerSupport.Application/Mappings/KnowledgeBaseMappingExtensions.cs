using CustomerSupport.Application.DTOs.KnowledgeBase;
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Application.Mappings
{
  // Contains reusable mappings for Knowledge Base responses.
  public static class KnowledgeBaseMappingExtensions
  {
    // Convert a Knowledge Base Article Entity into a Response DTO.
    public static KnowledgeBaseArticleResponseDTO ToResponseDTO(this KnowledgeBaseArticle article)
    {
      return new KnowledgeBaseArticleResponseDTO
      {
        Id = article.Id,
        Title = article.Title,
        Content = article.Content,
        ProductId = article.ProductId,
        ProductName = article.Product?.Name,
        IsPublished = article.IsPublished,
        IsActive = article.IsActive,
        CreatedAt = article.CreatedAt,
        UpdatedAt = article.UpdatedAt
      };
    }

    // Convert a Knowledge Base Article DTO to Entity.
    public static KnowledgeBaseArticle ToEntity(this CreateKnowledgeBaseArticleRequestDTO request, int createdBy)
    {
      return new KnowledgeBaseArticle
      {
        Title = request.Title.Trim(),
        Content = request.Content.Trim(),
        ProductId = request.ProductId,
        IsPublished = request.IsPublished,

        // Every newly created Article starts as active.
        IsActive = true,
        CreatedAt = DateTime.UtcNow,

        // Record who created the Article.
        CreatedBy = createdBy
      };
    }

    // Convert a Knowledge Base Article DTO to Entity.
    public static void MapFrom(this KnowledgeBaseArticle article, UpdateKnowledgeBaseArticleRequestDTO request, int updatedBy)
    {
      // Update the editable Article properties.
      article.Title = request.Title.Trim();
      article.Content = request.Content.Trim();
      article.ProductId = request.ProductId;
      article.IsPublished = request.IsPublished;
      article.IsActive = request.IsActive;

      // Maintain audit information for the update.
      article.UpdatedAt = DateTime.UtcNow;
      article.UpdatedBy = updatedBy;
    }
  }
}
