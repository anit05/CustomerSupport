using CustomerSupport.Application.DTOs.FAQs;
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
  // Contains the business logic for Frequently Asked Questions.
  // This Service is responsible for:
  // - Role-based FAQ visibility
  // - Searching and retrieving FAQs
  // - Creating FAQs
  // - Updating FAQs
  // - Deactivating FAQs
  // - Validating optional Product associations
  public sealed class FAQService : IFAQService
  {
    // Used to retrieve and save FAQ records.
    private readonly IFAQRepository _repository;

    // Used to validate the optional Product associated with an FAQ.
    private readonly IProductRepository _productRepository;

    // Provides information about the currently authenticated User, such as UserId and Role.
    private readonly ICurrentUserService _currentUserService;

    // Used to record important FAQ operations.
    private readonly ILogger<FAQService> _logger;

    // Dependencies are supplied through Dependency Injection.
    public FAQService(IFAQRepository repository, IProductRepository productRepository, ICurrentUserService currentUserService, ILogger<FAQService> logger)
    {
      _repository = repository;
      _productRepository = productRepository;
      _currentUserService = currentUserService;
      _logger = logger;
    }

    // Searches FAQs using optional search text, Product filtering, and pagination.
    // Administrators can see both active and inactive FAQs.
    // Normal Users can see only active FAQs.
    public async Task<PagedResult<FAQResponseDTO>> SearchAsync(FAQQueryParameters parameters)
    {
      // Determine whether the current User is an Administrator.
      // The Repository uses this value to decide whether
      // inactive FAQs should also be included in the result.
      var isAdministrator = _currentUserService.Role == RoleType.Administrator;
      _logger.LogInformation("FAQ search started. UserId: {UserId}, IsAdministrator: {IsAdministrator}, PageNumber: {PageNumber}, PageSize: {PageSize}", _currentUserService.UserId, isAdministrator, parameters.PageNumber, parameters.PageSize);

      // Ask the Repository to apply searching, filtering, visibility rules, ordering, and pagination.
      var result = await _repository.SearchAsync(parameters, includeInactive: isAdministrator);

      // Convert the FAQ Entities returned by the Repository into client-facing Response DTOs.
      var items = result.Items.Select(faq => faq.ToResponseDTO()).ToList();

      // Return the mapped FAQs together with the paging information.
      return PagedResult<FAQResponseDTO>.Create(items, result.TotalRecords, result.PageNumber, result.PageSize);
    }

    // Returns one FAQ by its ID.
    // Administrators can retrieve both active and inactive FAQs.
    // Normal Users can retrieve only active FAQs.
    public async Task<FAQResponseDTO> GetByIdAsync(int id)
    {
      _logger.LogInformation("FAQ requested. FAQId: {FAQId}, UserId: {UserId}", id, _currentUserService.UserId);

      // This operation only reads the FAQ, so EF Core tracking is not required.
      var faq = await _repository.GetByIdAsync(id, trackChanges: false);

      // Return NotFound when:
      // - The FAQ does not exist.
      // - A normal User tries to access an inactive FAQ.
      // Using the same NotFound response prevents inactive FAQs from being exposed to normal Users.
      if (faq is null || (_currentUserService.Role != RoleType.Administrator && !faq.IsActive))
      {
        throw new NotFoundException($"FAQ with ID {id} was not found.");
      }

      // Convert the Entity into the client-facing Response DTO.
      return faq.ToResponseDTO();
    }

    // Creates a new FAQ.
    // Only Administrators are allowed to create FAQs.
    public async Task<FAQResponseDTO> CreateAsync(CreateFAQRequestDTO request)
    {
      // Verify that the current User is an Administrator.
      EnsureAdministrator();

      // Get the authenticated Administrator ID.
      // This value is also stored in the audit information.
      var administratorId = _currentUserService.UserId!.Value;
      _logger.LogInformation("FAQ creation started. AdministratorId: {AdministratorId}, ProductId: {ProductId}", administratorId, request.ProductId);

      // Validate the optional Product before creating the FAQ.
      await ValidateProductAsync(request.ProductId);

      // Create the FAQ Entity from the incoming Request DTO.
      var faq = request.ToEntity(administratorId);

      // Add the new FAQ to the DbContext.
      await _repository.AddAsync(faq);

      // Persist the new FAQ in SQL Server.
      await _repository.SaveChangesAsync();

      // Reload the saved FAQ so related Product information
      // is available when creating the response DTO.
      var savedFaq = await _repository.GetByIdAsync(faq.Id, trackChanges: false) ?? throw new InvalidOperationException("The created FAQ could not be retrieved.");

      _logger.LogInformation("FAQ created successfully. FAQId: {FAQId}, AdministratorId: {AdministratorId}", savedFaq.Id, administratorId);

      // Convert the saved Entity into the Response DTO.
      return savedFaq.ToResponseDTO();
    }

    // Updates an existing FAQ.
    // Only Administrators are allowed to update FAQs.
    public async Task<FAQResponseDTO> UpdateAsync(int id, UpdateFAQRequestDTO request)
    {
      // Verify that the current User is an Administrator.
      EnsureAdministrator();

      // Get the authenticated Administrator ID for auditing.
      var administratorId = _currentUserService.UserId!.Value;

      _logger.LogInformation("FAQ update started. FAQId: {FAQId}, AdministratorId: {AdministratorId}", id, administratorId);

      // Retrieve the FAQ with EF Core tracking enabled because its properties will be modified.
      var faq = await _repository.GetByIdAsync(id, trackChanges: true);

      // Stop when the requested FAQ does not exist.
      if (faq is null)
      {
        throw new NotFoundException($"FAQ with ID {id} was not found.");
      }

      // Validate the optional Product before assigning it.
      await ValidateProductAsync(request.ProductId);

      // Update the editable FAQ properties.
      faq.MapFrom(request, administratorId);

      // Persist the tracked changes in SQL Server.
      await _repository.SaveChangesAsync();

      // Reload the updated FAQ so the response contains the latest Product information.
      var updatedFaq = await _repository.GetByIdAsync(id, trackChanges: false) ?? throw new InvalidOperationException("The updated FAQ could not be retrieved.");

      _logger.LogInformation("FAQ updated successfully. FAQId: {FAQId}, AdministratorId: {AdministratorId}", id, administratorId);

      // Convert the updated Entity into the Response DTO.
      return updatedFaq.ToResponseDTO();
    }

    // Performs a soft delete of an FAQ.
    // The FAQ is not physically removed from the database.
    // Instead, IsActive is changed to false.
    public async Task DeactivateAsync(int id)
    {
      // Only Administrators can deactivate FAQs.
      EnsureAdministrator();

      // Get the Administrator ID for audit information.
      var administratorId = _currentUserService.UserId!.Value;

      _logger.LogInformation("FAQ deactivation started. FAQId: {FAQId}, AdministratorId: {AdministratorId}", id, administratorId);

      // Retrieve the FAQ with tracking enabled because
      // IsActive and audit fields will be modified.
      var faq = await _repository.GetByIdAsync(id, trackChanges: true);

      // Stop when the requested FAQ does not exist.
      if (faq is null)
      {
        throw new NotFoundException($"FAQ with ID {id} was not found.");
      }

      // Avoid performing the same deactivation repeatedly.
      if (!faq.IsActive)
      {
        throw new BusinessRuleException("The FAQ is already inactive.");
      }

      // Soft delete the FAQ.
      faq.IsActive = false;

      // Maintain audit information.
      faq.UpdatedAt = DateTime.UtcNow;
      faq.UpdatedBy = administratorId;

      // Persist the changes.
      await _repository.SaveChangesAsync();

      _logger.LogInformation("FAQ deactivated. FAQId: {FAQId}, AdministratorId: {AdministratorId}", id, administratorId);
    }

    // Verifies that the current authenticated User is an Administrator.
    // Keeping this validation in one private method avoids repeating
    // the same Role check inside Create, Update, and Deactivate.
    private void EnsureAdministrator()
    {
      // Reject any User who is not an Administrator.
      if (_currentUserService.Role != RoleType.Administrator)
      {
        throw new BusinessRuleException("Only Administrators can manage FAQs.");
      }
    }

    // Validates the optional Product association.
    // An FAQ can exist without being associated with a Product,
    // therefore a null ProductId is valid.
    private async Task ValidateProductAsync(int? productId)
    {
      // No Product validation is required when
      // the FAQ is not associated with a Product.
      if (!productId.HasValue)
      {
        return;
      }

      // Retrieve the Product as read-only data because
      // this Service only needs to validate it.
      var product = await _productRepository.GetByIdAsync(productId.Value, trackChanges: false);

      // Reject an invalid Product ID.
      if (product is null)
      {
        throw new ApplicationValidationException(new[] { "The selected Product does not exist." });
      }

      // Prevent FAQs from being associated with an inactive Product.
      if (!product.IsActive)
      {
        throw new BusinessRuleException("The selected Product is currently inactive.");
      }
    }
  }
}
