using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Models;
namespace CustomerSupport.Application.Interfaces.Services
{
    // Defines the Support Ticket use cases exposed by the Application layer.
    public interface ITicketService
    {
        // Existing Methods
        // Create a new Support Ticket for the authenticated Customer.
        Task<TicketDetailsResponseDTO> CreateAsync(CreateTicketRequestDTO request);

        // Return all Tickets owned by the authenticated Customer.
        Task<IReadOnlyCollection<TicketSummaryResponseDTO>> GetMyTicketsAsync();

        // Return Ticket details when the current User is allowed to see the Ticket.
        Task<TicketDetailsResponseDTO> GetByIdAsync(int id);

        //New Methods Added
        // Assign a Support Ticket to a Support Executive.
        Task<TicketDetailsResponseDTO> AssignAsync(int ticketId, AssignTicketRequestDTO request);

        // Change the current Status of a Support Ticket.
        Task<TicketDetailsResponseDTO> ChangeStatusAsync(int ticketId, ChangeTicketStatusRequestDTO request);

        // Add a public or internal Comment to a Support Ticket.
        Task<TicketCommentResponseDTO> AddCommentAsync(int ticketId, AddTicketCommentRequestDTO request);

        // Return the complete assignment history of a Support Ticket.
        Task<IReadOnlyCollection<TicketAssignmentResponseDTO>> GetAssignmentHistoryAsync(int ticketId);

        // Search Support Tickets using filtering, sorting, and pagination.
        Task<PagedResult<TicketSummaryResponseDTO>> SearchAsync(TicketQueryParameters parameters);
    }
}