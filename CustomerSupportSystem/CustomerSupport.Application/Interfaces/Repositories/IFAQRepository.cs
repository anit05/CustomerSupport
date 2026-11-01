using CustomerSupport.Application.DTOs.FAQs;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Application.Interfaces.Repositories
{
  // Defines data access required by FAQ workflows.
  public interface IFAQRepository
  {
    // Search and page FAQs.
    // includeInactive=true allows Administrators to see inactive records.
    Task<PagedResult<FAQ>> SearchAsync(FAQQueryParameters parameters, bool includeInactive);

    // Retrieve one FAQ with optional change tracking.
    Task<FAQ?> GetByIdAsync(int id, bool trackChanges);

    // Add a new FAQ.
    Task AddAsync(FAQ faq);

    // Persist pending changes.
    Task SaveChangesAsync();
  }
}
