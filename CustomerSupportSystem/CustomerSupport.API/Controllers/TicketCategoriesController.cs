using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.Categories;
using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  // All Ticket Category endpoints require an authenticated User.
  [ApiController]
  [Route("api/ticket-categories")]
  [Authorize]
  public sealed class TicketCategoriesController : ControllerBase
  {
    // Application Service that contains Ticket Category business rules.
    private readonly ITicketCategoryService _categoryService;

    // Provides the current authenticated User for audit information.
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TicketCategoriesController> _logger;

    public TicketCategoriesController(
        ITicketCategoryService categoryService,
        ICurrentUserService currentUserService,
        ILogger<TicketCategoriesController> logger)
    {
      _categoryService = categoryService;
      _currentUserService = currentUserService;
      _logger = logger;
    }

    // GET: /api/ticket-categories
    // Any authenticated User can retrieve active Ticket Categories.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketCategoryResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketCategoryResponseDTO>>>> GetActiveCategories()
    {
      _logger.LogInformation("Request received to retrieve active Ticket Categories.");

      var categories = await _categoryService.GetAllAsync(activeOnly: true);

      var response = ApiResponse<IReadOnlyCollection<TicketCategoryResponseDTO>>
              .SuccessResponse(
                  categories,
                  "Active ticket categories retrieved successfully.");

      return Ok(response);
    }

    // GET: /api/ticket-categories/all
    // Only Administrators can retrieve active and inactive Categories.
    [HttpGet("all")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketCategoryResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketCategoryResponseDTO>>>> GetAllCategories()
    {
      var userId = _currentUserService.UserId!.Value;

      _logger.LogInformation("Administrator requested all Ticket Categories. UserId: {UserId}", userId);

      var categories = await _categoryService.GetAllAsync(activeOnly: false);

      var response = ApiResponse<IReadOnlyCollection<TicketCategoryResponseDTO>>
              .SuccessResponse(
                  categories,
                  "Ticket categories retrieved successfully.");

      return Ok(response);
    }

    // GET: /api/ticket-categories/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TicketCategoryResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketCategoryResponseDTO>>> GetById(int id)
    {
      _logger.LogInformation("Request received to retrieve Ticket Category. CategoryId: {CategoryId}", id);

      var category = await _categoryService.GetByIdAsync(id);

      var response = ApiResponse<TicketCategoryResponseDTO>
              .SuccessResponse(
                  category,
                  "Ticket category retrieved successfully.");

      return Ok(response);
    }

    // POST: /api/ticket-categories
    // Only Administrators can create Ticket Categories.
    [HttpPost]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    [ProducesResponseType(typeof(ApiResponse<TicketCategoryResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TicketCategoryResponseDTO>>> Create(CreateTicketCategoryRequestDTO request)
    {
      var userId = _currentUserService.UserId!.Value;

      _logger.LogInformation(
          "Administrator is creating a Ticket Category. UserId: {UserId}, CategoryName: {CategoryName}",
          userId,
          request.Name);

      var category = await _categoryService.CreateAsync(request, userId);

      _logger.LogInformation(
          "Ticket Category creation completed. CategoryId: {CategoryId}, UserId: {UserId}",
          category.Id,
          userId);

      var response = ApiResponse<TicketCategoryResponseDTO>
              .SuccessResponse(
                  category,
                  "Ticket category created successfully.");

      return Ok(response);
    }

    // PUT: /api/ticket-categories/{id}
    // Only Administrators can update Ticket Categories.
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    [ProducesResponseType(typeof(ApiResponse<TicketCategoryResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TicketCategoryResponseDTO>>> Update(
            int id,
            UpdateTicketCategoryRequestDTO request)
    {
      var userId = _currentUserService.UserId!.Value;

      _logger.LogInformation(
          "Administrator is updating a Ticket Category. UserId: {UserId}, CategoryId: {CategoryId}",
          userId,
          id);

      var category = await _categoryService.UpdateAsync(id, request, userId);

      _logger.LogInformation(
          "Ticket Category update completed. CategoryId: {CategoryId}, UserId: {UserId}",
          category.Id,
          userId);

      var response = ApiResponse<TicketCategoryResponseDTO>
              .SuccessResponse(
                  category,
                  "Ticket category updated successfully.");

      return Ok(response);
    }
  }
}

