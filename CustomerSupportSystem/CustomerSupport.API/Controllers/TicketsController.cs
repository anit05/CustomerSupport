using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Models;
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

        // Existing Action Methods
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

            // Delegate the Ticket creation workflow to TicketService.
            // TicketService is responsible for:
            // - Getting the CustomerId from the authenticated User
            // - Validating Product
            // - Validating optional Ticket Category
            // - Validating Ticket Priority
            // - Generating the Ticket Number
            // - Setting the initial Status to Open
            // - Creating Ticket history
            // - Saving the Ticket to the database
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

            // Delegate the retrieval operation to TicketService.
            // TicketService obtains CustomerId from the authenticated JWT claims
            // and retrieves only Tickets belonging to that Customer.
            // The client does not provide CustomerId.
            // This prevents one Customer from requesting another Customer's Tickets.
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

            // Delegate Ticket retrieval and access validation to TicketService.
            // TicketService determines the current User from JWT claims
            // and applies role-specific access rules.
            //
            // Customer: Can retrieve only their own Ticket.
            // Support Executive: Can retrieve only a Ticket currently assigned to them.
            // Administrator: Can retrieve any Ticket.
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

        // New Action Methods
        // PUT: /api/tickets/{id}/assign
        // Assigns or reassigns a Support Ticket to a Support Executive.
        // Only Administrators are allowed to perform this operation.
        [HttpPut("{id:int}/assign")]
        [Authorize(Roles = nameof(RoleType.Administrator))]
        [ProducesResponseType(typeof(ApiResponse<TicketDetailsResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<TicketDetailsResponseDTO>>> Assign(int id, AssignTicketRequestDTO request)
        {
            _logger.LogInformation(
                "Ticket assignment request received. TicketId: {TicketId}, SupportExecutiveId: {SupportExecutiveId}",
                id,
                request.SupportExecutiveId);

            // Delegate the assignment business logic to TicketService.
            // The Service is responsible for validating:
            // - Whether the Ticket exists
            // - Whether the Ticket can still be assigned
            // - Whether the selected User exists
            // - Whether the selected User is an active Support Executive
            // - Whether this is an assignment or reassignment
            // - Assignment history and Ticket history
            var ticket = await _ticketService.AssignAsync(id, request);

            // Log successful assignment.
            _logger.LogInformation(
                "Ticket assigned successfully. TicketId: {TicketId}, SupportExecutiveId: {SupportExecutiveId}",
                id,
                request.SupportExecutiveId);

            // Wrap the updated Ticket details inside the common ApiResponse<T> structure.
            var response =
                ApiResponse<TicketDetailsResponseDTO>
                    .SuccessResponse(
                        ticket,
                        "Ticket assigned successfully.");

            // Return HTTP 200 OK with the updated Ticket information.
            return Ok(response);
        }


        // PUT: /api/tickets/{id}/status
        // Changes the current Status of a Support Ticket.
        // Only Support Executives and Administrators can perform this operation.
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = nameof(RoleType.SupportExecutive) + "," + nameof(RoleType.Administrator))]
        [ProducesResponseType(typeof(ApiResponse<TicketDetailsResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TicketDetailsResponseDTO>>> ChangeStatus(
                int id,
                ChangeTicketStatusRequestDTO request)
        {
            _logger.LogInformation(
                "Ticket status change request received. TicketId: {TicketId}, NewStatus: {Status}",
                id,
                request.StatusId);

            // Delegate the Status-change workflow to TicketService.
            // The Service validates:
            // - Whether the Ticket exists
            // - Whether the current User can access the Ticket
            // - Whether the requested Status exists and is active
            // - Whether the Status transition is allowed
            // - ResolvedAt and ClosedAt values
            // - Ticket history and audit information
            var ticket = await _ticketService.ChangeStatusAsync(id, request);

            // Log the final Status after a successful update.
            _logger.LogInformation(
                "Ticket status changed successfully. TicketId: {TicketId}, Status: {Status}",
                id,
                ticket.StatusName);

            // Prepare the successful API response.
            var response =
                ApiResponse<TicketDetailsResponseDTO>
                    .SuccessResponse(
                        ticket,
                        "Ticket status changed successfully.");

            // Return HTTP 200 OK with the updated Ticket details.
            return Ok(response);
        }


        // POST: /api/tickets/{id}/comments
        // Adds a Comment to an existing Support Ticket.
        // Customers, Support Executives, and Administrators can use this endpoint,
        // subject to the access rules enforced by TicketService.
        [HttpPost("{id:int}/comments")]
        [ProducesResponseType(typeof(ApiResponse<TicketCommentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TicketCommentResponseDTO>>> AddComment(
                int id,
                AddTicketCommentRequestDTO request)
        {
            _logger.LogInformation(
                "Ticket comment request received. TicketId: {TicketId}, IsInternal: {IsInternal}",
                id,
                request.IsInternal);

            // Delegate Comment creation to TicketService.
            // The Service checks:
            // - Whether the Ticket exists
            // - Whether the Ticket is Closed
            // - Whether the current User can access the Ticket
            // - Whether a Customer is trying to create an internal Comment
            // - Whether a Support Executive is assigned to the Ticket
            var comment = await _ticketService.AddCommentAsync(id, request);

            // Log the generated Comment ID after successful persistence.
            _logger.LogInformation(
                "Ticket comment added successfully. TicketId: {TicketId}, CommentId: {CommentId}",
                id,
                comment.Id);

            // Prepare the successful response.
            var response =
                ApiResponse<TicketCommentResponseDTO>
                    .SuccessResponse(
                        comment,
                        "Comment added successfully.");

            // Return HTTP 200 OK with the updated comment details.
            return Ok(response);
        }


        // GET: /api/tickets/{id}/assignments
        // Returns the assignment history of a Support Ticket.
        // Only Support Executives and Administrators can access this endpoint.
        [HttpGet("{id:int}/assignments")]
        [Authorize(Roles = nameof(RoleType.SupportExecutive) + "," + nameof(RoleType.Administrator))]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketAssignmentResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketAssignmentResponseDTO>>>> GetAssignmentHistory(int id)
        {
            _logger.LogInformation(
                "Ticket assignment history requested. TicketId: {TicketId}",
                id);

            // Ask TicketService to retrieve the assignment history.
            // The Service also applies access rules so that:
            // - Administrator can access any Ticket
            // - Support Executive can access only a Ticket assigned to them
            var assignments = await _ticketService.GetAssignmentHistoryAsync(id);

            // Wrap the assignment records inside the standard API response.
            var response =
                ApiResponse<
                    IReadOnlyCollection<TicketAssignmentResponseDTO>>
                    .SuccessResponse(
                        assignments,
                        "Ticket assignment history retrieved successfully.");

            // Return HTTP 200 OK.
            return Ok(response);
        }


        // GET: /api/tickets
        // Searches Support Tickets using optional filtering, sorting, and pagination.
        // TicketService automatically restricts the result based on the current Role:
        // Customer         -> Own Tickets only
        // SupportExecutive -> Assigned Tickets only
        // Administrator    -> All Tickets
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TicketSummaryResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<PagedResult<TicketSummaryResponseDTO>>>> Search(
                [FromQuery] TicketQueryParameters parameters)
        {
            _logger.LogInformation(
                "Ticket search request received. PageNumber: {PageNumber}, PageSize: {PageSize}, ProductId: {ProductId}, CategoryId: {CategoryId}, PriorityId: {PriorityId}, StatusId: {StatusId}, CustomerId: {CustomerId}, AssignedToUserId: {AssignedToUserId}",
                parameters.PageNumber,
                parameters.PageSize,
                parameters.ProductId,
                parameters.CategoryId,
                parameters.PriorityId,
                parameters.StatusId,
                parameters.CustomerId,
                parameters.AssignedToUserId);

            // Delegate searching to TicketService.
            // The Repository then performs:
            // - Security filtering
            // - Search
            // - Product filtering
            // - Category filtering
            // - Priority filtering
            // - Status filtering
            // - Customer filtering
            // - Assigned User filtering
            // - Sorting
            // - Pagination
            var result = await _ticketService.SearchAsync(parameters);

            // Log useful information about the completed search.
            _logger.LogInformation(
                "Ticket search completed successfully. PageNumber: {PageNumber}, ReturnedRecords: {ReturnedRecords}, TotalRecords: {TotalRecords}",
                result.PageNumber,
                result.Items.Count,
                result.TotalRecords);

            // Wrap the paged Ticket results inside the common API response.
            // The PagedResult contains:
            // - Items
            // - PageNumber
            // - PageSize
            // - TotalRecords
            // - TotalPages
            // - HasPreviousPage
            // - HasNextPage
            var response =
                ApiResponse<
                    PagedResult<TicketSummaryResponseDTO>>
                    .SuccessResponse(
                        result,
                        "Tickets retrieved successfully.");

            // Return HTTP 200 OK with the paged search result.
            return Ok(response);
        }
    }
}