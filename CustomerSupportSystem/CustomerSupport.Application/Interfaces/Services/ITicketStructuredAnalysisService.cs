using CustomerSupport.Application.DTOs.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
    // Analyzes one ticket and returns structured AI data.
    public interface ITicketStructuredAnalysisService
    {
        // ticketId identifies the record in SQL Server.
        Task<TicketStructuredAnalysisResponseDTO> AnalyzeAsync(
            int ticketId,
            CancellationToken cancellationToken = default);
    }
}