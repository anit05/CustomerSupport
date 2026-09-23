using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Application.Interfaces.Repositories
{
  public interface ITicketRepository
  {
    // Retrieve one Ticket. trackChanges controls EF Core change tracking.
    Task<SupportTicket?> GetByIdAsync(int id, bool trackChanges);

    // Add a new Ticket to the persistence context.
    Task AddAsync(SupportTicket ticket);

    // Retrieve all Tickets owned by one Customer.
    Task<IReadOnlyCollection<SupportTicket>> GetByCustomerIdAsync(int customerId);

    // Query Tickets with optional security scopes, filtering, sorting, and paging.
    Task<PagedResult<SupportTicket>> SearchAsync(TicketQueryParameters parameters, int? customerScopeId, int? assignedToScopeId);

    // Persist pending changes.
    Task SaveChangesAsync();
  }
}
