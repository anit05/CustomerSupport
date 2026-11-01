using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.KnowledgeBase;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("api/knowledge-base")]
  [Authorize]
  public sealed class KnowledgeBaseController : ControllerBase
  {
    // Provides access to Knowledge Base business operations.
    private readonly IKnowledgeBaseService _service;

    // Used to record important API-level operations.
    private readonly ILogger<KnowledgeBaseController> _logger;

    // Dependencies are provided through ASP.NET Core Dependency Injection.
    public KnowledgeBaseController(IKnowledgeBaseService service, ILogger<KnowledgeBaseController> logger)
    {
      _service = service;
      _logger = logger;
    }

    // Returns a paginated list of Knowledge Base Articles.
    // Endpoint: GET /api/knowledge-base
    // Optional query-string values can be used for:
    // - Searching
    // - Product filtering
    // - Page number
    // - Page size
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<KnowledgeBaseArticleResponseDTO>>>> Search([FromQuery] KnowledgeBaseQueryParameters parameters)
    {
      _logger.LogInformation("Knowledge Base search request received. PageNumber: {PageNumber}, PageSize: {PageSize}", parameters.PageNumber, parameters.PageSize);

      // Delegate searching, filtering, paging, and visibility rules to the Application Service.
      var result = await _service.SearchAsync(parameters);

      // Wrap the paginated result inside the application's
      // common success-response structure.
      var response = ApiResponse<PagedResult<KnowledgeBaseArticleResponseDTO>>
          .SuccessResponse(result, "Knowledge Base Articles retrieved successfully.");

      // Return HTTP 200 OK with the Knowledge Base results.
      return Ok(response);
    }

    // Returns one Knowledge Base Article by its ID.
    // Endpoint: GET /api/knowledge-base/{id}
    // Example: GET /api/knowledge-base/10
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<KnowledgeBaseArticleResponseDTO>>> GetById(int id)
    {
      _logger.LogInformation("Knowledge Base Article request received. ArticleId: {ArticleId}", id);

      // Ask the Application Service to retrieve the Article
      // and apply the required visibility rules.
      var article = await _service.GetByIdAsync(id);

      // Wrap the Article inside the common API response.
      var response = ApiResponse<KnowledgeBaseArticleResponseDTO>
          .SuccessResponse(article, "Knowledge Base Article retrieved successfully.");

      // Return HTTP 200 OK with the Article details.
      return Ok(response);
    }

    // Creates a new Knowledge Base Article.
    // Endpoint: POST /api/knowledge-base
    // Only Administrators can access this endpoint.
    [HttpPost]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    public async Task<ActionResult<ApiResponse<KnowledgeBaseArticleResponseDTO>>> Create(CreateKnowledgeBaseArticleRequestDTO request)
    {
      _logger.LogInformation("Knowledge Base Article creation request received. ProductId: {ProductId}, IsPublished: {IsPublished}", request.ProductId, request.IsPublished);

      // Delegate Product validation, Entity creation,
      // auditing, and persistence to the Application Service.
      var article = await _service.CreateAsync(request);

      // Wrap the newly created Article inside
      // the common success-response structure.
      var response = ApiResponse<KnowledgeBaseArticleResponseDTO>
          .SuccessResponse(article, "Knowledge Base Article created successfully.");

      // Return HTTP 200 OK with the created Article.
      return Ok(response);
    }

    // Updates an existing Knowledge Base Article.
    // Endpoint: PUT /api/knowledge-base/{id}
    // Example: PUT /api/knowledge-base/10
    // Only Administrators can access this endpoint.
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    public async Task<ActionResult<ApiResponse<KnowledgeBaseArticleResponseDTO>>> Update(int id, UpdateKnowledgeBaseArticleRequestDTO request)
    {
      _logger.LogInformation("Knowledge Base Article update request received. ArticleId: {ArticleId}", id);

      // Delegate the complete update workflow to the Service.
      // The Service handles:
      // - Article lookup
      // - Product validation
      // - Property updates
      // - Audit information
      // - Saving changes
      var article = await _service.UpdateAsync(id, request);

      // Wrap the updated Article in the common API response.
      var response = ApiResponse<KnowledgeBaseArticleResponseDTO>
          .SuccessResponse(article, "Knowledge Base Article updated successfully.");

      // Return HTTP 200 OK with the updated Article.
      return Ok(response);
    }

    // Deactivates a Knowledge Base Article.
    // Endpoint: DELETE /api/knowledge-base/{id}
    // This is a soft delete.
    // The database record remains available, but IsActive is changed to false.
    // Only Administrators can access this endpoint.
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    public async Task<ActionResult<ApiResponse<object?>>> Deactivate(int id)
    {
      _logger.LogInformation("Knowledge Base Article deactivation request received. ArticleId: {ArticleId}", id);

      // Delegate the deactivation business rules
      // and database update to the Application Service.
      await _service.DeactivateAsync(id);

      // No Article data needs to be returned after deactivation,
      // so the response contains null data with a success message.
      var response = ApiResponse<object?>
          .SuccessResponse(null, "Knowledge Base Article deactivated successfully.");

      // Return HTTP 200 OK after successful deactivation.
      return Ok(response);
    }
  }
}

