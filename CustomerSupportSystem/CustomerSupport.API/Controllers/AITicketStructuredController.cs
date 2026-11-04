using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
    [ApiController]
    [Route("api/ai/tickets")]
    [Authorize(Roles = "Administrator,SupportExecutive")]
    public sealed class AITicketStructuredController : ControllerBase
    {
        private readonly ITicketStructuredAnalysisService _service;

        // Inject the Application Service; keep the Controller small.
        public AITicketStructuredController(ITicketStructuredAnalysisService service)
        {
            _service = service;
        }

        // POST /api/ai/tickets/1001/structured-analysis
        [HttpPost("{ticketId:int}/structured-analysis")]
        public async Task<ActionResult<ApiResponse<TicketStructuredAnalysisResponseDTO>>> Analyze(
            int ticketId,
            CancellationToken cancellationToken)
        {
            // Loads the authorized ticket and analyzes it.
            var result = await _service.AnalyzeAsync(ticketId, cancellationToken);

            // Return our familiar strongly typed API.
            return Ok(ApiResponse<
                TicketStructuredAnalysisResponseDTO>.SuccessResponse(
                    result,
                    "Structured ticket analysis generated."));
        }
    }
}