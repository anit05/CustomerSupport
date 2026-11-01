using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
    [ApiController]
    [Route("api/ai/tickets")]
    [Authorize(Roles = "Administrator,SupportExecutive")]
    public sealed class AITicketFeaturesController : ControllerBase
    {
        private readonly ITicketAIFeatureService _service;

        public AITicketFeaturesController(ITicketAIFeatureService service)
        {
            _service = service;
        }

        // POST /api/ai/tickets/1001/generate
        [HttpPost("{ticketId:int}/generate")]
        public async Task<ActionResult<ApiResponse<TicketAIResponseDTO>>> Generate(
                int ticketId,
                [FromBody] GenerateTicketAIRequestDTO request,
                CancellationToken cancellationToken)
        {
            // The Application service reads the ticket from SQL Server,
            // checks access, builds the AI context, and invokes OpenAI.
            TicketAIResponseDTO result = await _service.GenerateAsync(
                ticketId, request, cancellationToken);

            return Ok(ApiResponse<TicketAIResponseDTO>.SuccessResponse(
                result, "AI suggestion generated successfully."));
        }

        // POST /api/ai/tickets/1001/save-reviewed-note
        [HttpPost("{ticketId:int}/save-reviewed-note")]
        public async Task<ActionResult<ApiResponse<TicketCommentResponseDTO>>> SaveReviewedNote(
                int ticketId,
                [FromBody] SaveReviewedAINoteRequestDTO request)
        {
            // Nothing is persisted until an authorized staff member
            // explicitly submits the reviewed note.
            TicketCommentResponseDTO saved =
                await _service.SaveReviewedNoteAsync(ticketId, request);

            return Ok(ApiResponse<TicketCommentResponseDTO>.SuccessResponse(
                saved, "Reviewed internal note saved successfully."));
        }
    }
}