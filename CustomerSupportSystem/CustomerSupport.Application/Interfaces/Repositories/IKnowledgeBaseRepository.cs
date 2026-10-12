using CustomerSupport.Application.DTOs.KnowledgeBase;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Application.Interfaces.Repositories
{
  // Defines data-access operations required by Knowledge Base workflows.
  public interface IKnowledgeBaseRepository
  {
    // Search and page Knowledge Base Articles.
    // includeAll=true allows Administrators to see inactive records.
    Task<PagedResult<KnowledgeBaseArticle>> SearchAsync(KnowledgeBaseQueryParameters parameters, bool includeAll);

    // Retrieve one Article with optional EF Core tracking.
    Task<KnowledgeBaseArticle?> GetByIdAsync(int id, bool trackChanges);

    // Add a new Article.
    Task AddAsync(KnowledgeBaseArticle article);

    // Persist pending changes.
    Task SaveChangesAsync();
  }
}
