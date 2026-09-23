using CustomerSupport.Application.DTOs.Tickets;

namespace CustomerSupport.Application.Interfaces.Services
{
  // Defines the Support Ticket use cases exposed by the Application layer.
  public interface ITicketService
  {
    // Create a new Support Ticket for the authenticated Customer.
    Task<TicketDetailsResponseDTO> CreateAsync(CreateTicketRequestDTO request);

    // Return all Tickets owned by the authenticated Customer.
    Task<IReadOnlyCollection<TicketSummaryResponseDTO>> GetMyTicketsAsync();

    // Return Ticket details when the current User is allowed to see the Ticket.
    Task<TicketDetailsResponseDTO> GetByIdAsync(int id);
  }
}
