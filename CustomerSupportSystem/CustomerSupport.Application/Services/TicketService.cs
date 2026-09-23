using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Mappings;
using CustomerSupport.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Services
{
  public sealed class TicketService : ITicketService
  {
    // Repository used for Support Ticket persistence and retrieval.
    private readonly ITicketRepository _ticketRepository;

    // Used to verify that the selected Product exists
    // and is active before creating a Ticket.
    private readonly IProductRepository _productRepository;

    // Used to verify an optional Ticket Category.
    private readonly ITicketCategoryRepository _categoryRepository;

    // Used to verify the selected Ticket Priority.
    private readonly ITicketPriorityRepository _priorityRepository;

    // Provides information about the currently authenticated User
    // such as UserId and Role from the JWT claims.
    private readonly ICurrentUserService _currentUserService;

    // Used for structured logging of important Ticket workflows.
    private readonly ILogger<TicketService> _logger;

    // All dependencies are supplied through Dependency Injection.
    public TicketService(
        ITicketRepository ticketRepository,
        IProductRepository productRepository,
        ITicketCategoryRepository categoryRepository,
        ITicketPriorityRepository priorityRepository,
        ICurrentUserService currentUserService,
        ILogger<TicketService> logger)
    {
      _ticketRepository = ticketRepository;
      _productRepository = productRepository;
      _categoryRepository = categoryRepository;
      _priorityRepository = priorityRepository;
      _currentUserService = currentUserService;
      _logger = logger;
    }

    public async Task<TicketDetailsResponseDTO> CreateAsync(CreateTicketRequestDTO request)
    {
      // Get the Customer ID from the authenticated JWT context.
      // We intentionally do NOT accept CustomerId from the request DTO.
      // Otherwise, a client could potentially create a Ticket
      // on behalf of another Customer by changing the CustomerId.
      var customerId = _currentUserService.UserId!.Value;

      _logger.LogInformation(
          "Support Ticket creation started. " +
          "CustomerId: {CustomerId}, ProductId: {ProductId}, " +
          "CategoryId: {CategoryId}, PriorityId: {PriorityId}",
          customerId,
          request.ProductId,
          request.CategoryId,
          request.PriorityId);

      // Validate Related Reference/Master Data
      // Instead of throwing after the first validation failure,
      // collect all Product, Category, and Priority problems.
      // This allows ApplicationValidationException to return
      // multiple useful validation messages in one response.
      var errors = new List<string>();

      // Validate Product
      // The selected Product must exist and must be active.
      // trackChanges: false is used because this Product
      // is required only for validation. We do not modify it.
      var product = await _productRepository.GetByIdAsync(request.ProductId, trackChanges: false);

      // Product ID does not reference an existing Product.
      if (product is null)
      {
        errors.Add("The selected Product does not exist.");
      }

      // Product exists, but inactive Products should not
      // be available for new Support Tickets.
      else if (!product.IsActive)
      {
        errors.Add("The selected Product is currently inactive.");
      }

      // Validate Ticket Category
      // Category is optional.
      // Therefore, perform validation only when the client supplies a CategoryId.
      if (request.CategoryId.HasValue)
      {
        // This is also a read-only validation lookup,
        // so EF Core tracking is unnecessary.
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, trackChanges: false);

        // Supplied Category does not exist.
        if (category is null)
        {
          errors.Add("The selected Ticket Category does not exist.");
        }

        // Category exists but is not currently available for new Support Tickets.
        else if (!category.IsActive)
        {
          errors.Add("The selected Ticket Category is currently inactive.");
        }
      }

      // Validate Ticket Priority
      // Priority is required and refers to one of our
      // Ticket Priority master records.
      var priority = await _priorityRepository.GetByIdAsync(request.PriorityId);

      // Priority does not exist.
      if (priority is null)
      {
        errors.Add("The selected Ticket Priority does not exist.");
      }

      // Existing but inactive Priority values cannot be used for new Tickets.
      else if (!priority.IsActive)
      {
        errors.Add("The selected Ticket Priority is currently inactive.");
      }

      // If one or more reference-data validations failed,
      // stop the creation process before saving anything.
      if (errors.Count > 0)
      {
        _logger.LogWarning(
            "Support Ticket creation rejected because reference-data " +
            "validation failed. CustomerId: {CustomerId}, " +
            "ValidationErrorCount: {ValidationErrorCount}",
            customerId,
            errors.Count);

        // GlobalExceptionHandler converts this custom exception
        // into HTTP 400 Bad Request and places the collected
        // validation messages inside ApiResponse.Errors.
        throw new ApplicationValidationException(errors);
      }

      // Create the SupportTicket Entity
      // Convert the validated request DTO into the Domain Entity.
      var ticket = request.ToEntity(customerId);

      // Generate TicketNumber on the server.
      // Ticket numbers are system-controlled business identifiers
      // and should not be supplied by the client.
      ticket.TicketNumber = GenerateTicketNumber();

      // Tell EF Core that a new SupportTicket should be inserted.
      // AddAsync() itself does not execute the SQL INSERT.
      await _ticketRepository.AddAsync(ticket);

      // SaveChangesAsync() persists the Ticket to SQL Server.
      // After saving, database-generated values such as Ticket.Id
      // are available on the entity.
      await _ticketRepository.SaveChangesAsync();

      _logger.LogInformation(
          "Support Ticket created successfully. " +
          "TicketId: {TicketId}, TicketNumber: {TicketNumber}, " +
          "CustomerId: {CustomerId}",
          ticket.Id,
          ticket.TicketNumber,
          customerId);

      // Reload it through TicketRepository because ToDetailsDTO()
      // needs related data such as:
      // - Customer
      // - Product
      // - Category
      // - Priority
      // - Status
      // - Comments
      // - History
      var createdTicket = await _ticketRepository.GetByIdAsync(ticket.Id, trackChanges: false);

      // The Ticket has already been saved successfully.
      // Therefore, failure to retrieve it immediately afterward
      // represents an unexpected persistence/data problem,
      // not a normal user validation error.
      if (createdTicket is null)
      {
        // This is intentionally not one of our normal business exceptions.
        throw new InvalidOperationException("The created Support Ticket could not be retrieved.");
      }

      // The caller is a Customer, so internal staff comments
      // must never be included in the response.
      return createdTicket.ToDetailsDTO(includeInternalComments: false);
    }

    public async Task<IReadOnlyCollection<TicketSummaryResponseDTO>> GetMyTicketsAsync()
    {
      // This operation specifically represents:
      // "Get the Tickets belonging to the currently logged-in Customer."
      // Therefore, only a Customer should use this workflow.
      if (_currentUserService.Role != RoleType.Customer)
      {
        _logger.LogWarning(
            "GetMyTickets rejected because the current User is not a Customer. " +
            "UserId: {UserId}, Role: {Role}",
            _currentUserService.UserId,
            _currentUserService.Role);

        // The User may be authenticated, but this particular
        // business operation is valid only for Customers.
        throw new BusinessRuleException("Only Customers can retrieve their own Tickets.");
      }

      // CustomerId comes from the authenticated JWT,
      var customerId = _currentUserService.UserId!.Value;

      _logger.LogInformation(
          "Retrieving Support Tickets for CustomerId: {CustomerId}",
          customerId);

      // Retrieve all the tickets of the customer
      var tickets = await _ticketRepository.GetByCustomerIdAsync(customerId);

      _logger.LogInformation(
          "Retrieved {TicketCount} Support Tickets for CustomerId: {CustomerId}",
          tickets.Count,
          customerId);

      // Convert Domain Entities into lightweight summary DTOs for the listing page
      return tickets
          .Select(ticket => ticket.ToSummaryDTO())
          .ToList();
    }

    public async Task<TicketDetailsResponseDTO> GetByIdAsync(int id)
    {
      // This method can be used by different authenticated Roles:
      // Customer -> Can view only their own Tickets.
      // SupportExecutive -> Can view only Tickets currently assigned to them.
      // Administrator -> Can view any Ticket.
      // The current User information comes from JWT claims.

      // Get the authenticated User's ID.
      var currentUserId = _currentUserService.UserId!.Value;

      // Get the authenticated User's Role.
      var currentRole = _currentUserService.Role!.Value;

      _logger.LogInformation(
          "Retrieving Support Ticket. " +
          "TicketId: {TicketId}, UserId: {UserId}, Role: {Role}",
          id,
          currentUserId,
          currentRole);

      // Retrieve the requested Ticket together with the
      // related entities required for Ticket details.
      var ticket = await _ticketRepository.GetByIdAsync(id, trackChanges: false);

      // Ticket Existence Check
      // If no Ticket exists with the requested ID, throw NotFoundException.
      if (ticket is null)
      {
        _logger.LogWarning("Support Ticket was not found. TicketId: {TicketId}", id);
        throw new NotFoundException($"Support Ticket with ID {id} was not found.");
      }

      // Customer Access Rule
      // A Customer can view only Tickets belonging to them.
      // Example:
      // Logged-in CustomerId = 10
      // Ticket.CustomerId = 10
      //      -> Allowed
      // Ticket.CustomerId = 25
      //      -> Not Allowed
      if (currentRole == RoleType.Customer && ticket.CustomerId != currentUserId)
      {
        _logger.LogWarning(
            "Customer attempted to access a Ticket owned by another Customer. " +
            "TicketId: {TicketId}, UserId: {UserId}",
            id,
            currentUserId);

        throw new NotFoundException($"Support Ticket with ID {id} was not found.");
      }

      // Support Executive Access Rule
      // A Support Executive can view only Tickets currently assigned to them.
      // Example:
      // Logged-in SupportExecutiveId = 20
      // AssignedToUserId = 20
      //      -> Allowed
      // AssignedToUserId = 35
      //      -> Not Allowed

      // Administrators are not restricted by this check.
      if (currentRole == RoleType.SupportExecutive && ticket.AssignedToUserId != currentUserId)
      {
        _logger.LogWarning(
            "Support Executive attempted to access a Ticket not assigned to them. " +
            "TicketId: {TicketId}, UserId: {UserId}",
            id,
            currentUserId);

        throw new NotFoundException($"Support Ticket with ID {id} was not found.");
      }

      // Internal Comment Visibility
      // Internal comments are intended only for support staff.
      // Customer: Public comments only.
      // SupportExecutive: Public + internal comments for assigned Tickets.
      // Administrator: Public + internal comments for all Tickets.
      var includeInternalComments =
          currentRole == RoleType.SupportExecutive ||
          currentRole == RoleType.Administrator;

      _logger.LogInformation(
          "Support Ticket retrieved successfully. " +
          "TicketId: {TicketId}, UserId: {UserId}, " +
          "IncludeInternalComments: {IncludeInternalComments}",
          id,
          currentUserId,
          includeInternalComments);

      // Convert the Domain Entity into TicketDetailsResponseDTO.
      return ticket.ToDetailsDTO(includeInternalComments);
    }

    private static string GenerateTicketNumber()
    {
      // Create the date portion using UTC.
      // Example:
      // September 18, 2026 -> 20260918
      var datePart = DateTime.UtcNow.ToString("yyyyMMdd");

      // Generate a GUID without hyphens, take the first
      // eight hexadecimal characters, and convert them  to uppercase.
      // Example GUID:
      // a4f92c10d87f4bc3a572af74e8441751
      // uniquePart: A4F92C10
      var uniquePart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

      // Final format:
      // TKT-{DATE}-{UNIQUE PART}
      // Example:
      // TKT-20260918-A4F92C10
      return $"TKT-{datePart}-{uniquePart}";
    }
  }
}
