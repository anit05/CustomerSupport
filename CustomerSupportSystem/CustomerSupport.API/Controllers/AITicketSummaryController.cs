using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("api/ai")]
  [Authorize(Roles = "Administrator")]
  public sealed class AITicketSummaryController : ControllerBase
  {
    private readonly IOpenAITicketSummaryService _summaryService;

    public AITicketSummaryController(IOpenAITicketSummaryService summaryService)
    {
      _summaryService = summaryService;
    }

    // POST: api/ai/summarize-ticket
    [HttpPost("summarize-ticket")]
    public async Task<ActionResult<ApiResponse<SummarizeTicketResponseDTO>>> SummarizeTicket(
            [FromBody] SummarizeTicketRequestDTO request,
            CancellationToken cancellationToken)
    {
      // Call the AI service to generate the summary.
      SummarizeTicketResponseDTO result =
          await _summaryService.SummarizeAsync(request.TicketComments, cancellationToken);

      // Return the generated summary in our standard response.
      var response = ApiResponse<SummarizeTicketResponseDTO>.SuccessResponse(
              result,
              "Ticket summarized successfully.");

      return Ok(response);
    }
  }
}
