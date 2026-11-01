using CustomerSupport.Application.DTOs.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
  public interface IOpenAITicketSummaryService
  {
    // Generates a summary of the supplied ticket comments.
    // CancellationToken allows the API request to be cancelled.
    Task<SummarizeTicketResponseDTO> SummarizeAsync(
        string ticketComments,
        CancellationToken cancellationToken = default);
  }
}

