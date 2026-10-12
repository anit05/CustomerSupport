using CustomerSupport.Application.DTOs.KnowledgeBase;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories
{
  // Implements Knowledge Base data-access operations using Entity Framework Core.
  // This Repository is responsible for:
  // - Searching Knowledge Base Articles
  // - Applying visibility filters
  // - Applying Product filters
  // - Applying pagination
  // - Retrieving an Article by ID
  // - Adding new Articles
  // - Saving database changes
  public sealed class KnowledgeBaseRepository : IKnowledgeBaseRepository
  {
    // Represents the Entity Framework Core database context.
    private readonly CustomerSupportDbContext _context;

    // CustomerSupportDbContext is supplied through Dependency Injection.
    public KnowledgeBaseRepository(CustomerSupportDbContext context)
    {
      _context = context;
    }

    // Searches Knowledge Base Articles using optional search text,
    // Product filtering, visibility rules, and pagination.
    public async Task<PagedResult<KnowledgeBaseArticle>> SearchAsync(KnowledgeBaseQueryParameters parameters, bool includeAll)
    {
      // Start building a query against the KnowledgeBaseArticles table.
      // AsNoTracking() is used because this operation only reads data
      // and does not modify the returned Articles.
      // Include() loads the related Product so Product information
      // is available later during DTO mapping.
      IQueryable<KnowledgeBaseArticle> query = _context.KnowledgeBaseArticles
              .AsNoTracking()
              .Include(article => article.Product);

      // When includeAll is false, return only Articles that are both active and published.
      // Normal authenticated Users use this filtered view,
      // while Administrators can request all Articles.
      if (!includeAll)
      {
        query = query.Where(article => article.IsActive && article.IsPublished);
      }

      // Apply optional text searching.
      // If Search is null, empty, or whitespace, no text-search condition is added.
      if (!string.IsNullOrWhiteSpace(parameters.Search))
      {
        // Remove unnecessary spaces from the search text.
        var search = parameters.Search.Trim();

        // Search in both Article Title and Article Content.
        query = query.Where(article => article.Title.Contains(search) || article.Content.Contains(search));
      }

      // Apply optional Product filtering.
      // When ProductId is provided, return only Articles associated with that Product.
      if (parameters.ProductId.HasValue)
      {
        query = query.Where(article => article.ProductId == parameters.ProductId.Value);
      }

      // Count the total number of matching records before pagination is applied.
      // This value is required so the client can know
      // how many total records match the current filters.
      var totalRecords = await query.CountAsync();

      // Apply ordering and pagination.
      // Recently updated Articles appear first.
      // If UpdatedAt is null, CreatedAt is used instead.
      // ThenByDescending(Id) provides a consistent secondary order.
      var items = await query
          .OrderByDescending(article => article.UpdatedAt ?? article.CreatedAt)
          .ThenByDescending(article => article.Id)
          .Skip((parameters.PageNumber - 1) * parameters.PageSize)
          .Take(parameters.PageSize)
          .ToListAsync();

      // Return the requested page together with total-record and paging information.
      return PagedResult<KnowledgeBaseArticle>.Create(items, totalRecords, parameters.PageNumber, parameters.PageSize);
    }

    // Retrieves one Knowledge Base Article by ID.
    // The caller decides whether EF Core change tracking is required.
    public async Task<KnowledgeBaseArticle?> GetByIdAsync(int id, bool trackChanges)
    {
      // Start building the query and include the optional Product.
      // Product is loaded because the response may need ProductName.
      IQueryable<KnowledgeBaseArticle> query =
          _context.KnowledgeBaseArticles
          .Include(article => article.Product);

      // Disable change tracking for read-only operations.
      // trackChanges = false: Used when the Article is only being displayed.
      // trackChanges = true: Used when the Article will be modified and saved.
      if (!trackChanges)
      {
        query = query.AsNoTracking();
      }

      // Execute the query and return the matching Article.
      // If no matching Article exists, null is returned.
      return await query.FirstOrDefaultAsync(article => article.Id == id);
    }

    // Adds a new Knowledge Base Article to the DbContext.
    // This does not immediately insert the row into SQL Server.
    // The INSERT happens when SaveChangesAsync() is called.
    public async Task AddAsync(KnowledgeBaseArticle article)
    {
      await _context.KnowledgeBaseArticles.AddAsync(article);
    }

    // Persists all pending changes in the DbContext to SQL Server.
    // This may execute INSERT, UPDATE, or other pending database operations.
    public async Task SaveChangesAsync()
    {
      await _context.SaveChangesAsync();
    }
  }
}

