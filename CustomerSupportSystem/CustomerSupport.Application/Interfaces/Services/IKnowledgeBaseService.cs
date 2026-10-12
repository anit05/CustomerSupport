using CustomerSupport.Application.DTOs.KnowledgeBase;
using CustomerSupport.Application.Models;

namespace CustomerSupport.Application.Interfaces.Services
{
  // Defines Knowledge Base use cases.
  public interface IKnowledgeBaseService
  {
    Task<PagedResult<KnowledgeBaseArticleResponseDTO>> SearchAsync(KnowledgeBaseQueryParameters parameters);

    Task<KnowledgeBaseArticleResponseDTO> GetByIdAsync(int id);

    Task<KnowledgeBaseArticleResponseDTO> CreateAsync(CreateKnowledgeBaseArticleRequestDTO request);

    Task<KnowledgeBaseArticleResponseDTO> UpdateAsync(int id, UpdateKnowledgeBaseArticleRequestDTO request);

    // Soft delete: sets IsActive to false.
    Task DeactivateAsync(int id);
  }
}
