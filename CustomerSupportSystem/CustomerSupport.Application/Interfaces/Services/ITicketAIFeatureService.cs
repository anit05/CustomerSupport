using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.DTOs.Tickets;

namespace CustomerSupport.Application.Interfaces.Services
{
    public interface ITicketAIFeatureService
    {
        // Reads an authorized ticket from SQL Server
        // and generates the selected AI suggestion.
        Task<TicketAIResponseDTO> GenerateAsync(
            int ticketId,
            GenerateTicketAIRequestDTO request,
            CancellationToken cancellationToken = default);

        // Saves a human-reviewed note using the existing
        // Ticket Service and TicketComment table.
        Task<TicketCommentResponseDTO> SaveReviewedNoteAsync(
            int ticketId,
            SaveReviewedAINoteRequestDTO request);
    }
}