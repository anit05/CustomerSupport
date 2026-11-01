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
    public sealed class AITicketMicrosoftAIController : ControllerBase
    {
        private readonly ITicketMicrosoftAIAnalysisService _service;

        // ASP.NET Core injects our Application Service.
        public AITicketMicrosoftAIController(ITicketMicrosoftAIAnalysisService service)
        {
            _service = service;
        }

        // POST /api/ai/tickets/1001/microsoft-ai-analysis
        [HttpPost("{ticketId:int}/microsoft-ai-analysis")]
        public async Task<ActionResult<ApiResponse<TicketStructuredAnalysisResponseDTO>>> Analyze(
            int ticketId,
            CancellationToken cancellationToken)
        {
            // Delegate retrieval, AI call, and validation.
            var result = await _service.AnalyzeAsync(ticketId, cancellationToken);

            // Return the same response format as Chapter 3.4.
            return Ok(ApiResponse<
                TicketStructuredAnalysisResponseDTO>.SuccessResponse(
                    result,
                    "Microsoft.Extensions.AI analysis generated."));
        }
    }
}