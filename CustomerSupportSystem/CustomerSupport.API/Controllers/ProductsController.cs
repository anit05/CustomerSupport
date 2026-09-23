using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.Products;
using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("api/products")]
  [Authorize]
  public sealed class ProductsController : ControllerBase
  {
    // Application Service that contains Product business rules.
    private readonly IProductService _productService;

    // Provides information about the currently authenticated User
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ICurrentUserService currentUserService,
        ILogger<ProductsController> logger)
    {
      _productService = productService;
      _currentUserService = currentUserService;
      _logger = logger;
    }

    // GET: /api/products
    // Any authenticated User can retrieve active Products.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductResponseDTO>>>> GetActiveProducts()
    {
      _logger.LogInformation("Request received to retrieve active Products.");

      // activeOnly = true ensures inactive Products are not returned.
      var products = await _productService.GetAllAsync(activeOnly: true);

      var response = ApiResponse<IReadOnlyCollection<ProductResponseDTO>>
              .SuccessResponse(
                  products,
                  "Active products retrieved successfully.");

      return Ok(response);
    }

    // GET: /api/products/all
    // Only Administrators can retrieve both active and inactive Products.
    [HttpGet("all")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ProductResponseDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductResponseDTO>>>> GetAllProducts()
    {
      var userId = _currentUserService.UserId!.Value;

      _logger.LogInformation("Administrator requested all Products. UserId: {UserId}", userId);

      var products = await _productService.GetAllAsync(activeOnly: false);

      var response = ApiResponse<IReadOnlyCollection<ProductResponseDTO>>
              .SuccessResponse(
                  products,
                  "Products retrieved successfully.");

      return Ok(response);
    }

    // GET: /api/products/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> GetById(int id)
    {
      _logger.LogInformation("Request received to retrieve Product. ProductId: {ProductId}", id);

      var product = await _productService.GetByIdAsync(id);

      var response = ApiResponse<ProductResponseDTO>
              .SuccessResponse(
                  product,
                  "Product retrieved successfully.");

      return Ok(response);
    }

    // POST: /api/products
    // Product creation is restricted to Administrators.
    [HttpPost]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> Create(CreateProductRequestDTO request)
    {
      var userId = _currentUserService.UserId!.Value;

      _logger.LogInformation(
          "Administrator is creating a Product. UserId: {UserId}, ProductName: {ProductName}",
          userId,
          request.Name);

      var product = await _productService.CreateAsync(request, userId);

      _logger.LogInformation(
          "Product creation completed. ProductId: {ProductId}, UserId: {UserId}",
          product.Id,
          userId);

      var response = ApiResponse<ProductResponseDTO>
              .SuccessResponse(
                  product,
                  "Product created successfully.");

      return Ok(response);
    }

    // PUT: /api/products/{id}
    // Product updates are restricted to Administrators.
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(RoleType.Administrator))]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductResponseDTO>>> Update(int id, UpdateProductRequestDTO request)
    {
      var userId = _currentUserService.UserId!.Value;

      _logger.LogInformation(
          "Administrator is updating a Product. UserId: {UserId}, ProductId: {ProductId}",
          userId,
          id);

      var product = await _productService.UpdateAsync(id, request, userId);

      _logger.LogInformation(
          "Product update completed. ProductId: {ProductId}, UserId: {UserId}",
          product.Id,
          userId);

      var response = ApiResponse<ProductResponseDTO>
              .SuccessResponse(
                  product,
                  "Product updated successfully.");

      return Ok(response);
    }
  }
}

