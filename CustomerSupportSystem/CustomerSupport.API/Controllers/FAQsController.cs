using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.FAQs;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("api/faqs")]
  [Authorize]
  public sealed class FAQsController : ControllerBase
  {
    // Provides access to FAQ business operations.
    private readonly IFAQService _service;

    // Used to record important API-level FAQ operations.
    private readonly ILogger<FAQsController> _logger;

    // Dependencies are supplied through ASP.NET Core Dependency Injection.
    public FAQsController(IFAQService service, ILogger<FAQsController> logger)
    {
      _service = service;
      _logger = logger;
    }

    // Returns a paginated list of FAQs.
    // Endpoint: GET /api/faqs
    // Optional query-string values can be used for:
    // - Searching
    // - Product filtering
    // - Page number
    // - Page size
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FAQResponseDTO>>>> Search([FromQuery] FAQQueryParameters parameters)
    {
      _logger.LogInformation("FAQ search request received. PageNumber: {PageNumber}, PageSize: {PageSize}", parameters.PageNumber, parameters.PageSize);

      // Delegate searching, filtering, pagination,
      // and visibility rules to the Application Service.
      var result = await _service.SearchAsync(parameters);

      // Wrap the paginated result inside the common API response
      // and return HTTP 200 OK.
      var response = ApiResponse<PagedResult<FAQResponseDTO>>
          .SuccessResponse(result, "FAQs retrieved successfully.");

      return Ok(response);
    }

    // Returns one FAQ by its ID.
    // Endpoint: GET /api/faqs/{id}
    // Example: GET /api/faqs/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<FAQResponseDTO>>> GetById(int id)
    {
      _logger.LogInformation("FAQ request received. FAQId: {FAQId}", id);

      // Ask the Application Service to retrieve the FAQ
      // and apply the required visibility rules.
      var faq = await _service.GetByIdAsync(id);

      // Wrap the FAQ inside the common API response
      // and return HTTP 200 OK.
      var response = ApiResponse<FAQResponseDTO>
          .SuccessResponse(faq, "FAQ retrieved successfully.");

      return Ok(response);
    }

    // Creates a new FAQ.
    // Endpoint: POST /api/faqs
    // Only Administrators can access this endpoint.
    [HttpPost]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    public async Task<ActionResult<ApiResponse<FAQResponseDTO>>> Create(CreateFAQRequestDTO request)
    {
      _logger.LogInformation("FAQ creation request received. ProductId: {ProductId}", request.ProductId);

      // Delegate Product validation, Entity creation,
      // audit information, and persistence to FAQService.
      var faq = await _service.CreateAsync(request);

      // Wrap the newly created FAQ inside
      // the application's common success-response structure.
      var response = ApiResponse<FAQResponseDTO>
          .SuccessResponse(faq, "FAQ created successfully.");

      // Return HTTP 200 OK with the created FAQ.
      return Ok(response);
    }

    // Updates an existing FAQ.
    // Endpoint: PUT /api/faqs/{id}
    // Example: PUT /api/faqs/5
    // Only Administrators can access this endpoint.
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    public async Task<ActionResult<ApiResponse<FAQResponseDTO>>> Update(int id, UpdateFAQRequestDTO request)
    {
      _logger.LogInformation("FAQ update request received. FAQId: {FAQId}", id);

      // Delegate the complete update workflow to FAQService.
      // The Service handles:
      // - FAQ lookup
      // - Product validation
      // - Property updates
      // - Audit information
      // - Saving changes
      var faq = await _service.UpdateAsync(id, request);

      // Wrap the updated FAQ inside the common API response
      // and return HTTP 200 OK.
      return Ok(ApiResponse<FAQResponseDTO>.SuccessResponse(faq, "FAQ updated successfully."));
    }

    // Deactivates an existing FAQ.
    // Endpoint: DELETE /api/faqs/{id}
    // This is a soft delete.
    // The FAQ remains in the database, but IsActive is changed to false.
    // Only Administrators can access this endpoint.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    public async Task<ActionResult<ApiResponse<object?>>> Deactivate(int id)
    {
      _logger.LogInformation("FAQ deactivation request received. FAQId: {FAQId}", id);

      // Delegate the deactivation business rules
      // and database update to FAQService.
      await _service.DeactivateAsync(id);

      // No FAQ data needs to be returned after deactivation,
      // so the response contains null data with a success message.
      return Ok(ApiResponse<object?>.SuccessResponse(null, "FAQ deactivated successfully."));
    }
  }
}
