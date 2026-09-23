using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("api/tickets")]
  [Authorize]
  public sealed class TicketsController : ControllerBase
  {
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger)
    {
      _ticketService = ticketService;
      _logger = logger;
    }

    // POST: /api/tickets
    // Only Customers can create Support Tickets.
    [HttpPost]
    [Authorize(Roles = nameof(RoleType.Customer))]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailsResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TicketDetailsResponseDTO>>> Create(CreateTicketRequestDTO request)
    {
      _logger.LogInformation(
          "Support Ticket creation request received. ProductId: {ProductId}, CategoryId: {CategoryId}, PriorityId: {PriorityId}",
          request.ProductId,
          request.CategoryId,
          request.PriorityId);

      var ticket = await _ticketService.CreateAsync(request);

      _logger.LogInformation(
          "Support Ticket creation completed successfully. TicketId: {TicketId}, TicketNumber: {TicketNumber}",
          ticket.Id,
          ticket.TicketNumber);

      var response = ApiResponse<TicketDetailsResponseDTO>
              .SuccessResponse(
                  ticket,
                  "Support Ticket created successfully.");

      return Ok(response);
    }

    // GET: /api/tickets/my
    // Returns all Tickets owned by the authenticated Customer.
    [HttpGet("my")]
    [Authorize(Roles = nameof(RoleType.Customer))]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketSummaryResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketSummaryResponseDTO>>>> GetMyTickets()
    {
      _logger.LogInformation("Request received to retrieve the current Customer's Tickets.");

      var tickets = await _ticketService.GetMyTicketsAsync();

      _logger.LogInformation(
          "Current Customer's Tickets retrieved successfully. Count: {TicketCount}",
          tickets.Count);

      var response = ApiResponse<IReadOnlyCollection<TicketSummaryResponseDTO>>
              .SuccessResponse(
                  tickets,
                  "Tickets retrieved successfully.");

      return Ok(response);
    }

    // GET: /api/tickets/{id}
    // Customers can retrieve only their own Tickets.
    // Support Executives and Administrators can retrieve Ticket details.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailsResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketDetailsResponseDTO>>> GetById(int id)
    {
      _logger.LogInformation("Request received to retrieve TicketId: {TicketId}", id);

      var ticket = await _ticketService.GetByIdAsync(id);

      _logger.LogInformation(
          "Support Ticket retrieval completed successfully. TicketId: {TicketId}, TicketNumber: {TicketNumber}",
          ticket.Id,
          ticket.TicketNumber);

      var response = ApiResponse<TicketDetailsResponseDTO>
              .SuccessResponse(
                  ticket,
                  "Ticket retrieved successfully.");

      return Ok(response);
    }
  }
}