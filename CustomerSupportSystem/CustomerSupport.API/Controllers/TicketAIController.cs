using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  // Exposes API endpoints for AI-powered ticket operations.
  [ApiController]

  // Defines the common URL prefix for all endpoints in this controller.
  [Route("api/ai/tickets")]

  // Allow only authenticated Administrators and Support Executives.
  [Authorize(Roles = "Administrator,SupportExecutive")]
  public sealed class TicketAIController : ControllerBase
  {
    // Application service responsible for AI-related ticket operations.
    private readonly ITicketAIService _service;

    // Receive the AI Application Service through Dependency Injection.
    public TicketAIController(ITicketAIService service)
    {
      _service = service;
    }

    // POST: api/ai/tickets/1001/analysis
    // Analyzes an existing ticket and returns structured AI suggestions.
    [HttpPost("{ticketId:int}/analysis")]
    public async Task<ActionResult<ApiResponse<TicketStructuredAnalysisResponseDTO>>> Analyze(
        int ticketId,
        CancellationToken cancellationToken)
    {
      // Pass the Ticket ID to the Application Service for analysis.
      // The cancellation token allows the request to be cancelled.
      var result = await _service.AnalyzeAsync(
          ticketId,
          cancellationToken);

      // Return HTTP 200 with the analysis in our standard API response.
      // The generated suggestions are not saved to the database.
      return Ok(
          ApiResponse<TicketStructuredAnalysisResponseDTO>
              .SuccessResponse(result, "AI ticket analysis generated."));
    }

    // POST: api/ai/tickets/1001/reply-draft
    // Generates a customer-facing reply based on the ticket details.
    [HttpPost("{ticketId:int}/reply-draft")]
    public async Task<ActionResult<ApiResponse<TicketDraftResponseDTO>>> ReplyDraft(
        int ticketId,
        [FromBody] TicketDraftRequestDTO request,
        CancellationToken cancellationToken)
    {
      // Read the optional Knowledge Base article ID from the request body.
      // The Application Service excludes internal ticket comments.
      var result = await _service.GenerateReplyDraftAsync(
          ticketId,
          request,
          cancellationToken);

      // Return the generated reply for review by a Support Executive.
      // This endpoint does not save or send the reply to the customer.
      return Ok(
          ApiResponse<TicketDraftResponseDTO>
              .SuccessResponse(result, "Customer reply draft generated."));
    }

    // POST: api/ai/tickets/1001/internal-note-draft
    // Generates an internal note for support staff.
    [HttpPost("{ticketId:int}/internal-note-draft")]
    public async Task<ActionResult<ApiResponse<TicketDraftResponseDTO>>> InternalNoteDraft(
        int ticketId,
        [FromBody] TicketDraftRequestDTO request,
        CancellationToken cancellationToken)
    {
      // Generate an internal note using the ticket information.
      // The Application Service may include internal support comments.
      var result = await _service.GenerateInternalNoteDraftAsync(
          ticketId,
          request,
          cancellationToken);

      // Return the generated internal note for staff review.
      // The note is not saved automatically.
      return Ok(
          ApiResponse<TicketDraftResponseDTO>
              .SuccessResponse(result, "Internal note draft generated."));
    }

    // POST: api/ai/tickets/1001/save-drafts
    // Saves a reviewed AI draft as an internal or customer-visible comment.
    [HttpPost("{ticketId:int}/save-drafts")]
    public async Task<ActionResult<ApiResponse<TicketCommentResponseDTO>>> SaveDraft(
        int ticketId,
        [FromBody] SaveReviewedAIDraftRequestDTO request)
    {
      // Read the approved text and IsInternal value from the request.
      // true: Save as an internal support comment.
      // false: Save as a customer-visible ticket comment.
      var saved = await _service.SaveReviewedDraftAsync(
          ticketId,
          request);

      // Return HTTP 200 with the saved ticket comment details.
      // The Application Service uses the existing Ticket Service.
      return Ok(
          ApiResponse<TicketCommentResponseDTO>
              .SuccessResponse(saved, "Reviewed ticket comment saved."));
    }
  }
}
