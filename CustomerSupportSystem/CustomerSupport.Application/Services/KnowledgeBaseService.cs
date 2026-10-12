using CustomerSupport.Application.DTOs.KnowledgeBase;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Mappings;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Services
{
  // Contains the business logic for Knowledge Base Articles.
  // This Service is responsible for:
  // - Creating Articles
  // - Updating Articles
  // - Deactivating Articles
  // - Validating optional Product associations
  // - Mapping Entities to Response DTOs
  public sealed class KnowledgeBaseService : IKnowledgeBaseService
  {
    // Used to read and save Knowledge Base Articles.
    private readonly IKnowledgeBaseRepository _repository;

    // Used to validate the optional Product selected for an Article.
    private readonly IProductRepository _productRepository;

    // Provides information about the currently authenticated User, such as UserId and Role.
    private readonly ICurrentUserService _currentUserService;

    // Used to record important Knowledge Base operations.
    private readonly ILogger<KnowledgeBaseService> _logger;

    // Dependencies are supplied through Dependency Injection.
    public KnowledgeBaseService(IKnowledgeBaseRepository repository, IProductRepository productRepository, ICurrentUserService currentUserService, ILogger<KnowledgeBaseService> logger)
    {
      _repository = repository;
      _productRepository = productRepository;
      _currentUserService = currentUserService;
      _logger = logger;
    }

    // Searches Knowledge Base Articles.
    // Administrators can see all Articles.
    // Normal Users can see only active and published Articles.
    public async Task<PagedResult<KnowledgeBaseArticleResponseDTO>> SearchAsync(KnowledgeBaseQueryParameters parameters)
    {
      // Check whether the current User is an Administrator.
      // The Repository uses this value to decide whether
      // draft and inactive Articles should also be returned.
      var isAdministrator = _currentUserService.Role == RoleType.Administrator;

      _logger.LogInformation("Knowledge Base search started. UserId: {UserId}, IsAdministrator: {IsAdministrator}, PageNumber: {PageNumber}, PageSize: {PageSize}", _currentUserService.UserId, isAdministrator, parameters.PageNumber, parameters.PageSize);

      // Ask the Repository to apply searching, filtering, visibility rules, and pagination.
      var result = await _repository.SearchAsync(parameters, includeAll: isAdministrator);

      // Convert the Domain Entities returned by the Repository into client-facing Response DTOs.
      var items = result.Items
          .Select(article => article.ToResponseDTO())
          .ToList();

      _logger.LogInformation("Knowledge Base search completed. ReturnedRecords: {ReturnedRecords}, TotalRecords: {TotalRecords}", items.Count, result.TotalRecords);

      // Return the DTO collection together with paging information.
      return PagedResult<KnowledgeBaseArticleResponseDTO>
          .Create(items, result.TotalRecords, result.PageNumber, result.PageSize);
    }

    // Returns one Knowledge Base Article by ID.
    // Administrators can retrieve any Article.
    // Normal Users can retrieve only active and published Articles.
    public async Task<KnowledgeBaseArticleResponseDTO> GetByIdAsync(int id)
    {
      _logger.LogInformation("Knowledge Base Article requested. ArticleId: {ArticleId}, UserId: {UserId}", id, _currentUserService.UserId);

      // This operation only reads the Article, so EF Core tracking is not required.
      var article = await _repository.GetByIdAsync(id, trackChanges: false);

      // Stop when the requested Article does not exist.
      if (article is null)
      {
        throw new NotFoundException($"Knowledge Base Article with ID {id} was not found.");
      }

      // Normal Users must not be able to view:
      // - Draft Articles
      // - Inactive Articles
      // We return NotFoundException so unavailable Articles are not exposed to normal Users.
      if (_currentUserService.Role != RoleType.Administrator && (!article.IsActive || !article.IsPublished))
      {
        throw new NotFoundException($"Knowledge Base Article with ID {id} was not found.");
      }

      // Convert the Entity into the client-facing Response DTO.
      return article.ToResponseDTO();
    }

    // Creates a new Knowledge Base Article.
    // Only Administrators are allowed to create Articles.
    public async Task<KnowledgeBaseArticleResponseDTO> CreateAsync(CreateKnowledgeBaseArticleRequestDTO request)
    {
      // Apply the Administrator business rule.
      EnsureAdministrator();

      // Get the authenticated Administrator ID.
      // This value is also used for audit information.
      var administratorId = _currentUserService.UserId!.Value;

      _logger.LogInformation("Knowledge Base Article creation started. AdministratorId: {AdministratorId}, ProductId: {ProductId}, IsPublished: {IsPublished}", administratorId, request.ProductId, request.IsPublished);

      // Validate the optional Product before creating the Article.
      await ValidateProductAsync(request.ProductId);

      // Create the Domain Entity from the incoming Request DTO.
      var article = request.ToEntity(administratorId);

      // Add the new Article to the DbContext.
      await _repository.AddAsync(article);

      // Persist the new Article in SQL Server.
      await _repository.SaveChangesAsync();

      // Reload the saved Article so related Product information is available for the response.
      var savedArticle = await _repository.GetByIdAsync(article.Id, trackChanges: false) ?? throw new InvalidOperationException("The created Knowledge Base Article could not be retrieved.");

      _logger.LogInformation("Knowledge Base Article created successfully. ArticleId: {ArticleId}, AdministratorId: {AdministratorId}", savedArticle.Id, administratorId);

      // Convert the saved Entity into a Response DTO.
      return savedArticle.ToResponseDTO();
    }

    // Updates an existing Knowledge Base Article.
    // Only Administrators are allowed to update Articles.
    public async Task<KnowledgeBaseArticleResponseDTO> UpdateAsync(int id, UpdateKnowledgeBaseArticleRequestDTO request)
    {
      // Ensure the current User is allowed to manage Articles.
      EnsureAdministrator();

      // Get the authenticated Administrator ID for auditing.
      var administratorId = _currentUserService.UserId!.Value;
      _logger.LogInformation("Knowledge Base Article update started. ArticleId: {ArticleId}, AdministratorId: {AdministratorId}", id, administratorId);

      // Retrieve the Article with EF Core tracking enabled
      // because its values will be modified and saved.
      var article = await _repository.GetByIdAsync(id, trackChanges: true);

      // Stop when the Article does not exist.
      if (article is null)
      {
        throw new NotFoundException($"Knowledge Base Article with ID {id} was not found.");
      }

      // Validate the optional Product before assigning it.
      await ValidateProductAsync(request.ProductId);

      // Update the Article properties.
      article.MapFrom(request, administratorId);

      // Persist the tracked changes in SQL Server.
      await _repository.SaveChangesAsync();

      // Reload the Article so the response contains the latest Product information.
      var updatedArticle = await _repository.GetByIdAsync(id, trackChanges: false) ?? throw new InvalidOperationException("The updated Knowledge Base Article could not be retrieved.");

      _logger.LogInformation("Knowledge Base Article updated successfully. ArticleId: {ArticleId}, AdministratorId: {AdministratorId}", id, administratorId);

      // Convert the updated Entity into the Response DTO.
      return updatedArticle.ToResponseDTO();
    }

    // Performs a soft delete of a Knowledge Base Article.
    // The record remains in the database, but IsActive is changed to false.
    public async Task DeactivateAsync(int id)
    {
      // Only Administrators can deactivate Articles.
      EnsureAdministrator();

      // Get the Administrator ID for auditing.
      var administratorId = _currentUserService.UserId!.Value;
      _logger.LogInformation("Knowledge Base Article deactivation started. ArticleId: {ArticleId}, AdministratorId: {AdministratorId}", id, administratorId);

      // Tracking is required because IsActive and audit values will be modified.
      var article = await _repository.GetByIdAsync(id, trackChanges: true);

      // Stop when the requested Article does not exist.
      if (article is null)
      {
        throw new NotFoundException($"Knowledge Base Article with ID {id} was not found.");
      }

      // Avoid deactivating an Article that is already inactive.
      if (!article.IsActive)
      {
        throw new BusinessRuleException("The Knowledge Base Article is already inactive.");
      }

      // Soft delete the Article.
      article.IsActive = false;

      // Maintain audit information.
      article.UpdatedAt = DateTime.UtcNow;
      article.UpdatedBy = administratorId;

      // Persist the changes.
      await _repository.SaveChangesAsync();
      _logger.LogInformation("Knowledge Base Article deactivated. ArticleId: {ArticleId}, AdministratorId: {AdministratorId}", id, administratorId);
    }

    // Verifies that the current User is an Administrator.
    // Keeping this check in one private method prevents
    // the same role validation from being repeated in
    // Create, Update, and Deactivate methods.
    private void EnsureAdministrator()
    {
      // Reject any User who is not an Administrator.
      if (_currentUserService.Role != RoleType.Administrator)
      {
        throw new BusinessRuleException("Only Administrators can manage Knowledge Base Articles.");
      }
    }

    // Validates the optional Product association.
    // A Knowledge Base Article can exist without a Product,
    // so null ProductId is valid.
    private async Task ValidateProductAsync(int? productId)
    {
      // No validation is required when the Article is not associated with a Product.
      if (!productId.HasValue)
      {
        return;
      }

      // Retrieve the Product as read-only data.
      var product = await _productRepository.GetByIdAsync(productId.Value, trackChanges: false);

      // Reject an invalid Product ID.
      if (product is null)
      {
        throw new ApplicationValidationException(new[] { "The selected Product does not exist." });
      }

      // Prevent Articles from being associated with an inactive Product.
      if (!product.IsActive)
      {
        throw new BusinessRuleException("The selected Product is currently inactive.");
      }
    }
  }
}
