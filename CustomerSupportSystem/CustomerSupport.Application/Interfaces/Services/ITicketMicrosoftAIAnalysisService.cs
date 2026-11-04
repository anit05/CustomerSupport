using CustomerSupport.Application.DTOs.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
    // Describes the new Microsoft.Extensions.AI ticket analysis.
    public interface ITicketMicrosoftAIAnalysisService
    {
        // Analyze one authorized ticket and return typed data.
        Task<TicketStructuredAnalysisResponseDTO> AnalyzeAsync(
            int ticketId,
            CancellationToken cancellationToken = default);
    }
}