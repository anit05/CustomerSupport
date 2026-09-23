using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories
{
  public sealed class TicketRepository : ITicketRepository
  {
    // DbContext represents the current EF Core database session.
    // It is used to:
    // - Query Support Tickets
    // - Add new Tickets
    // - Track changes
    // - Save INSERT/UPDATE operations to SQL Server
    private readonly CustomerSupportDbContext _context;

    // CustomerSupportDbContext is provided through
    // ASP.NET Core Dependency Injection.
    public TicketRepository(CustomerSupportDbContext context)
    {
      _context = context;
    }

    public async Task<SupportTicket?> GetByIdAsync(int id, bool trackChanges)
    {
      // Start building a query against the SupportTickets table.
      // IQueryable does NOT immediately execute the SQL query.
      // We are only building the query at this point.
      IQueryable<SupportTicket> query = _context.SupportTickets;

      // Disable EF Core change tracking for read-only operations.
      // trackChanges: false
      //     Used when we only want to read Ticket information.
      // trackChanges: true
      //     Used when the Service intends to modify the Ticket
      //     and later call SaveChangesAsync().

      // AsNoTracking() improves performance for read-only queries
      // because EF Core does not need to store entity state information.
      if (!trackChanges)
      {
        query = query.AsNoTracking();
      }

      // Ticket details require several related Entities.
      return await query
          // Load the Customer who created/owns the Ticket.
          .Include(ticket => ticket.Customer)

          // Load Product information used by Ticket details.
          .Include(ticket => ticket.Product)

          // Load Category information used by Ticket details.
          .Include(ticket => ticket.Category)

          // Load the Ticket Priority master record.
          .Include(ticket => ticket.Priority)

          // Load the Ticket Status master record.
          .Include(ticket => ticket.Status)

          // Load the currently assigned Support Executive, if any.
          .Include(ticket => ticket.AssignedToUser)

          // Load all comments belonging to the Ticket.
          .Include(ticket => ticket.Comments)
              // For each TicketComment, also load the User
              // who created that comment.
              .ThenInclude(comment => comment.User)

          // Load Ticket assignment history.
          .Include(ticket => ticket.Assignments)
              // For each assignment, load the Support Executive
              // to whom the Ticket was assigned.
              .ThenInclude(assignment => assignment.AssignedToUser)

          // Load Assignments again so we can navigate through
          // a different related User property.
          .Include(ticket => ticket.Assignments)
              // Load the User who performed the assignment.
              .ThenInclude(assignment => assignment.AssignedByUser)

          // Load Ticket history records such as:
          // - Ticket Created
          // - Assigned/Reassigned
          // - Status Changed
          // - Comment Added
          .Include(ticket => ticket.History)
              // Load the User responsible for each history entry.
              .ThenInclude(history => history.ChangedByUser)

          // Execute the SQL query and return the matching Ticket.
          .FirstOrDefaultAsync(ticket => ticket.Id == id);
    }

    public async Task AddAsync(SupportTicket ticket)
    {
      // Add the new SupportTicket entity to EF Core's DbContext.
      // At this point EF Core marks the entity as Added.
      // The actual INSERT statement is not sent to SQL Server
      // until SaveChangesAsync() is called.
      await _context.SupportTickets.AddAsync(ticket);
    }

    public async Task<IReadOnlyCollection<SupportTicket>> GetByCustomerIdAsync(int customerId)
    {
      // Retrieve all Tickets belonging to one Customer.
      // This is a read-only operation, so AsNoTracking()
      // avoids unnecessary EF Core tracking overhead.
      return await _context.SupportTickets
          .AsNoTracking()

          // Load only the related entities required by
          // TicketSummaryResponseDTO mapping.
          .Include(ticket => ticket.Customer)
          .Include(ticket => ticket.Product)
          .Include(ticket => ticket.Category)
          .Include(ticket => ticket.Priority)
          .Include(ticket => ticket.Status)
          .Include(ticket => ticket.AssignedToUser)

          // Apply Customer ownership directly in SQL.
          // This is important because another Customer's Tickets
          // are never loaded and then filtered in application memory.
          // SQL will contain a condition similar to:
          // WHERE CustomerId = @customerId
          .Where(ticket => ticket.CustomerId == customerId)

          // Display the most recently created Tickets first.
          .OrderByDescending(ticket => ticket.CreatedAt)

          // Execute the SQL query and return the results.
          .ToListAsync();
    }

    public async Task<PagedResult<SupportTicket>> SearchAsync(
        TicketQueryParameters parameters,
        int? customerScopeId,
        int? assignedToScopeId)
    {
      // Start with a read-only Ticket query.
      // IQueryable allows us to dynamically add:
      // - Security restrictions
      // - Search conditions
      // - Filters
      // - Sorting
      // - Paging
      // before SQL Server actually executes the query.

      IQueryable<SupportTicket> query = _context.SupportTickets.AsNoTracking();

      // Apply Security Scope First
      // No Restriction for Admin

      // Customer scope:
      // When customerScopeId contains a value,
      // only Tickets owned by that Customer are allowed.
      //
      // Example:
      // CustomerId = 10
      // The SQL query is restricted to:
      // WHERE CustomerId = 10

      if (customerScopeId.HasValue)
      {
        query = query.Where(ticket => ticket.CustomerId == customerScopeId.Value);
      }

      // Support Executive scope:
      // When assignedToScopeId contains a value,
      // only Tickets currently assigned to that Support Executive can be returned.
      //
      // Example:
      // SupportExecutiveId = 25
      // SQL is restricted to:
      // WHERE AssignedToUserId = 25

      if (assignedToScopeId.HasValue)
      {
        query = query.Where(ticket => ticket.AssignedToUserId == assignedToScopeId.Value);
      }

      // Apply free-text search
      // Apply text searching only when the User has supplied
      // a non-empty Search value.
      if (!string.IsNullOrWhiteSpace(parameters.Search))
      {
        // Remove unnecessary spaces from the beginning
        // and end of the search text.
        var search = parameters.Search.Trim();

        // Search across several useful Ticket fields.
        query = query.Where(ticket =>
            ticket.TicketNumber.Contains(search) ||
            ticket.Subject.Contains(search) ||
            ticket.Description.Contains(search));
      }

      // Apply optional filters one by one.

      // Filter by Product only when ProductId is supplied.
      if (parameters.ProductId.HasValue)
      {
        query = query.Where(ticket => ticket.ProductId == parameters.ProductId.Value);
      }

      // Filter by Ticket Category only when CategoryId is supplied.
      if (parameters.CategoryId.HasValue)
      {
        query = query.Where(ticket => ticket.CategoryId == parameters.CategoryId.Value);
      }

      // Filter by Priority only when PriorityId is supplied.
      if (parameters.PriorityId.HasValue)
      {
        query = query.Where(ticket => ticket.PriorityId == parameters.PriorityId.Value);
      }

      // Filter by current Ticket Status when StatusId is supplied.
      if (parameters.StatusId.HasValue)
      {
        query = query.Where(ticket => ticket.StatusId == parameters.StatusId.Value);
      }


      // Calculate Total Matching Records
      // Count the complete filtered result BEFORE applying Skip() and Take().
      // This gives the client the total number of records matching the search criteria,
      // not just the number present on the current page.
      // TotalRecords is later used to calculate TotalPages.
      var totalRecords = await query.CountAsync();

      // Apply Sorting
      // Sorting must happen before Skip() and Take().
      // Without sorting, pagination can produce
      // inconsistent results between pages.
      query = ApplySorting(query, parameters.SortBy, parameters.SortDirection);

      // Apply Pagination
      // Calculate how many records should be skipped.
      // Example:
      // PageNumber = 3
      // PageSize   = 10
      // Skip = (3 - 1) * 10 = 20
      // SQL Server skips the first 20 matching records
      // and returns the next 10.
      var tickets =
          await query
              // Skip records belonging to previous pages.
              .Skip((parameters.PageNumber - 1) * parameters.PageSize)

              // Retrieve only the requested number of records.
              .Take(parameters.PageSize)

              // Load navigation properties required
              // by TicketSummaryResponseDTO.
              .Include(ticket => ticket.Customer)
              .Include(ticket => ticket.Product)
              .Include(ticket => ticket.Category)
              .Include(ticket => ticket.Priority)
              .Include(ticket => ticket.Status)
              .Include(ticket => ticket.AssignedToUser)

              // Execute the final SQL query.
              .ToListAsync();

      // Package the current page together with pagination metadata.
      // PagedResult.Create() calculates values such as:
      // - TotalPages
      // - HasPreviousPage
      // - HasNextPage
      return PagedResult<SupportTicket>.Create(
          tickets,
          totalRecords,
          parameters.PageNumber,
          parameters.PageSize);
    }

    public async Task SaveChangesAsync()
    {
      // Persist all pending changes tracked by this DbContext.
      // Depending on what the Service has done,
      // EF Core may generate:
      // - INSERT statements
      // - UPDATE statements
      // - DELETE statements
      await _context.SaveChangesAsync();
    }

    private static IQueryable<SupportTicket> ApplySorting(
        IQueryable<SupportTicket> query,
        string? sortBy,
        string? sortDirection)
    {
      // Normalize the requested sort field.
      // Examples:
      // "Subject"     -> "subject"
      // " PRODUCT "   -> "product"
      // "UpdatedAt"   -> "updatedat"
      //
      // Trimming removes accidental spaces and
      // ToLowerInvariant() makes the comparison case-insensitive.
      var field = sortBy?.Trim().ToLowerInvariant();

      // Determine whether the requested direction is descending.
      // Examples:
      // "desc" -> true
      // "DESC" -> true
      // "asc"  -> false
      // Any value other than "desc" will be treated as ascending here.
      var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

      // Apply sorting based on the field requested by the client.
      // Ticket Id is always added as a secondary sort.

      // Example:
      // Two Tickets may have exactly the same CreatedAt value.
      // Sorting by Id as the second condition guarantees that
      // their relative order remains predictable.

      // Stable ordering is especially important when pagination
      // uses Skip() and Take().
      return field switch
      {
        // Sort by Ticket Number
        "ticketnumber" =>
            descending
                ? query
                    .OrderByDescending(ticket => ticket.TicketNumber)
                : query
                    .OrderBy(ticket => ticket.TicketNumber),

        // Sort alphabetically by Ticket Subject
        "subject" =>
            descending
                ? query
                    .OrderByDescending(ticket => ticket.Subject)
                    .ThenByDescending(ticket => ticket.Id)
                : query
                    .OrderBy(ticket => ticket.Subject)
                    .ThenBy(ticket => ticket.Id),

        // Sort by Product Name
        "product" =>
            descending
                ? query
                    .OrderByDescending(ticket => ticket.Product.Name)
                    .ThenByDescending(ticket => ticket.Id)
                : query
                    .OrderBy(ticket => ticket.Product.Name)
                    .ThenBy(ticket => ticket.Id),

        // Sort by Ticket Priority
        "priority" =>
            descending
                ? query
                    .OrderByDescending(ticket => ticket.Priority.SortOrder)
                    .ThenByDescending(ticket => ticket.Id)
                : query
                    .OrderBy(ticket => ticket.Priority.SortOrder)
                    .ThenBy(ticket => ticket.Id),

        // Sort by Ticket Status
        "status" =>
            descending
                ? query
                    .OrderByDescending(
                        ticket => ticket.Status.SortOrder)
                    .ThenByDescending(
                        ticket => ticket.Id)
                : query
                    .OrderBy(
                        ticket => ticket.Status.SortOrder)
                    .ThenBy(
                        ticket => ticket.Id),

        // Sort by the last modification date
        "updatedat" =>
            descending
                ? query
                    .OrderByDescending(ticket => ticket.UpdatedAt)
                    .ThenByDescending(ticket => ticket.Id)
                : query
                    .OrderBy(ticket => ticket.UpdatedAt)
                    .ThenBy(ticket => ticket.Id),

        // Default: Sort by CreatedAt
        _ =>
            descending
                ? query
                    .OrderByDescending(ticket => ticket.CreatedAt)
                    .ThenByDescending(ticket => ticket.Id)
                : query
                    .OrderBy(ticket => ticket.CreatedAt)
                    .ThenBy(ticket => ticket.Id)
      };
    }
  }
}