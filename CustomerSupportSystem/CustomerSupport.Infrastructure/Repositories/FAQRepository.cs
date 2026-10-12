using CustomerSupport.Application.DTOs.FAQs;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories
{
  // Implements FAQ data-access operations using Entity Framework Core.
  // This Repository is responsible for:
  // - Searching FAQs
  // - Applying active/inactive visibility rules
  // - Applying Product filters
  // - Applying pagination
  // - Retrieving an FAQ by ID
  // - Adding new FAQs
  // - Saving database changes
  public sealed class FAQRepository : IFAQRepository
  {
    // Represents the Entity Framework Core database context.
    private readonly CustomerSupportDbContext _context;

    // CustomerSupportDbContext is supplied through Dependency Injection.
    public FAQRepository(CustomerSupportDbContext context)
    {
      _context = context;
    }

    // Searches FAQs using optional search text,
    // Product filtering, visibility rules, and pagination.
    public async Task<PagedResult<FAQ>> SearchAsync(FAQQueryParameters parameters, bool includeInactive)
    {
      // Start building a query against the FAQs table.
      // AsNoTracking() is used because this operation
      // only reads data and does not modify the returned FAQs.
      // Include() loads the related Product so Product information
      // is available later during DTO mapping.
      IQueryable<FAQ> query = _context.FAQs.AsNoTracking().Include(faq => faq.Product);

      // Normal Users can see only active FAQs.
      // When includeInactive is true, Administrators can see both active and inactive FAQs.
      if (!includeInactive)
      {
        query = query.Where(faq => faq.IsActive);
      }

      // Apply optional text searching.
      // If Search is null, empty, or whitespace, no search condition is added.
      if (!string.IsNullOrWhiteSpace(parameters.Search))
      {
        // Remove unnecessary spaces from the search text.
        var search = parameters.Search.Trim();

        // Search in both the Question and Answer fields.
        query = query.Where(faq => faq.Question.Contains(search) || faq.Answer.Contains(search));
      }

      // Apply optional Product filtering.
      // When ProductId is supplied, return only FAQs associated with that Product.
      if (parameters.ProductId.HasValue)
      {
        query = query.Where(faq => faq.ProductId == parameters.ProductId.Value);
      }

      // Count all matching records before pagination is applied.
      // This value is required so the client knows
      // the total number of records matching the current filters.
      var totalRecords = await query.CountAsync();

      // Apply ordering and pagination.
      // Recently updated FAQs appear first.
      // If UpdatedAt is null, CreatedAt is used instead.
      // ThenByDescending(Id) provides a consistent secondary order.
      var items = await query.OrderByDescending(faq => faq.UpdatedAt ?? faq.CreatedAt).ThenByDescending(faq => faq.Id).Skip((parameters.PageNumber - 1) * parameters.PageSize).Take(parameters.PageSize).ToListAsync();

      // Return the requested page together with total-record and paging information.
      return PagedResult<FAQ>.Create(items, totalRecords, parameters.PageNumber, parameters.PageSize);
    }

    // Retrieves one FAQ by its ID.
    // The caller decides whether EF Core should track the returned Entity.
    public async Task<FAQ?> GetByIdAsync(int id, bool trackChanges)
    {
      // Start building the query and include the optional Product.
      // Product is loaded because the response may need ProductName.
      IQueryable<FAQ> query = _context.FAQs.Include(faq => faq.Product);

      // Disable change tracking for read-only operations.
      // trackChanges = false: Used when the FAQ is only being displayed.
      // trackChanges = true: Used when the FAQ will be modified and saved.
      if (!trackChanges)
      {
        query = query.AsNoTracking();
      }

      // Execute the query and return the matching FAQ.
      // If no matching FAQ exists, null is returned.
      return await query.FirstOrDefaultAsync(faq => faq.Id == id);
    }

    // Adds a new FAQ to the DbContext.
    // This does not immediately insert the record into SQL Server.
    // The INSERT happens when SaveChangesAsync() is called.
    public async Task AddAsync(FAQ faq)
    {
      await _context.FAQs.AddAsync(faq);
    }

    // Persists all pending changes in the DbContext to SQL Server.
    // This may execute INSERT, UPDATE, or other pending database operations.
    public async Task SaveChangesAsync()
    {
      await _context.SaveChangesAsync();
    }
  }
}

