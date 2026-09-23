using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.MasterData;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  // Master-data endpoints require authentication.
  [ApiController]
  [Route("api/master-data")]
  [Authorize]
  public sealed class MasterDataController : ControllerBase
  {
    // Application Service used to retrieve read-only master data.
    private readonly IMasterDataService _masterDataService;
    private readonly ILogger<MasterDataController> _logger;

    public MasterDataController(
        IMasterDataService masterDataService,
        ILogger<MasterDataController> logger)
    {
      _masterDataService = masterDataService;
      _logger = logger;
    }

    // GET: /api/master-data/ticket-priorities
    [HttpGet("ticket-priorities")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketPriorityResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketPriorityResponseDTO>>>> GetTicketPriorities()
    {
      _logger.LogInformation("Request received to retrieve active Ticket Priorities.");

      var priorities = await _masterDataService.GetTicketPrioritiesAsync();

      var response = ApiResponse<IReadOnlyCollection<TicketPriorityResponseDTO>>
              .SuccessResponse(
                  priorities,
                  "Ticket priorities retrieved successfully.");

      return Ok(response);
    }

    // GET: /api/master-data/ticket-statuses
    [HttpGet("ticket-statuses")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketStatusResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketStatusResponseDTO>>>> GetTicketStatuses()
    {
      _logger.LogInformation("Request received to retrieve active Ticket Statuses.");

      var statuses = await _masterDataService.GetTicketStatusesAsync();

      var response = ApiResponse<IReadOnlyCollection<TicketStatusResponseDTO>>
              .SuccessResponse(
                  statuses,
                  "Ticket statuses retrieved successfully.");

      return Ok(response);
    }
  }
}

