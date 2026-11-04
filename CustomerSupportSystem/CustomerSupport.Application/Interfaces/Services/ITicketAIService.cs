using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.DTOs.Tickets;

namespace CustomerSupport.Application.Interfaces.Services
{
  // Defines the AI operations available for customer support tickets.
  // The implementation uses the configured AI provider.
  public interface ITicketAIService
  {
    // Analyze a ticket and return structured AI results.
    // ticketId identifies the ticket to analyze.
    // cancellationToken allows cancellation of the AI request.
    Task<TicketStructuredAnalysisResponseDTO> AnalyzeAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    // Generate a customer-facing reply draft for the ticket.
    // request contains an optional Knowledge Base article ID.
    // The draft is returned for review and is not saved automatically.
    Task<TicketDraftResponseDTO> GenerateReplyDraftAsync(
        int ticketId,
        TicketDraftRequestDTO request,
        CancellationToken cancellationToken = default);

    // Generate an internal note draft for Support Executives.
    // The draft may use both public and internal ticket comments.
    // The generated note is not saved automatically.
    Task<TicketDraftResponseDTO> GenerateInternalNoteDraftAsync(
        int ticketId,
        TicketDraftRequestDTO request,
        CancellationToken cancellationToken = default);

    // Save a staff-reviewed draft as a ticket comment.
    // IsInternal = true saves an internal support comment.
    // IsInternal = false saves a customer-visible comment.
    // Uses the existing Ticket Service to save the approved text.
    Task<TicketCommentResponseDTO> SaveReviewedDraftAsync(
        int ticketId,
        SaveReviewedAIDraftRequestDTO request);
  }
}
