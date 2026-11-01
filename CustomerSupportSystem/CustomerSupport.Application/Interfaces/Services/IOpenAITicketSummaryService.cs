using CustomerSupport.Application.DTOs.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
    public interface IOpenAITicketSummaryService
    {
        // Generates a summary using ticket comments and optional client-provided context.
        Task<SummarizeTicketResponseDTO> SummarizeAsync(
            SummarizeTicketRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}