using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Mappings;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;
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

        // Used to verify the selected Ticket Status
        private readonly ITicketStatusRepository _statusRepository;

        // Used to verify the user
        private readonly IUserRepository _userRepository;

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
            ITicketStatusRepository statusRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUserService,
            ILogger<TicketService> logger)
        {
            _ticketRepository = ticketRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _priorityRepository = priorityRepository;
            _statusRepository = statusRepository;
            _userRepository = userRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        // Existing Methods
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

            // Adding the Ticket into the History Table
            ticket.History.Add(new TicketHistory
            {
                ChangedByUserId = customerId,
                Action = "Ticket Created",
                FieldName = nameof(SupportTicket.StatusId),
                OldValue = null,
                NewValue = TicketStatusType.Open.ToString(),
                Remarks = "Support ticket created.",
                CreatedBy = customerId
            });

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


        // New Methods
        // Assign a Support Ticket to a Support Executive.
        // Only an Administrator is allowed to perform this operation.
        public async Task<TicketDetailsResponseDTO> AssignAsync(int ticketId, AssignTicketRequestDTO request)
        {
            _logger.LogInformation(
                "Ticket assignment started. TicketId: {TicketId}, SupportExecutiveId: {SupportExecutiveId}, UserId: {UserId}",
                ticketId,
                request.SupportExecutiveId,
                _currentUserService.UserId);

            // Verify that the current request contains an authenticated User
            // and that the User has the Administrator Role.
            // Ticket assignment is an administrative operation.
            // Customers and Support Executives are not allowed to assign or reassign Tickets.
            if (!_currentUserService.UserId.HasValue || _currentUserService.Role != RoleType.Administrator)
            {
                _logger.LogWarning(
                   "Ticket assignment rejected because the current User is not an Administrator. TicketId: {TicketId}, UserId: {UserId}, Role: {Role}",
                   ticketId,
                   _currentUserService.UserId,
                   _currentUserService.Role);

                throw new BusinessRuleException("Only Administrators can assign Support Tickets.");
            }

            // Get the Administrator's User ID from the authenticated JWT claims.
            // This value will be used for:
            // - Audit information
            // - Assignment history
            // - Ticket history
            var administratorId = _currentUserService.UserId.Value;

            // Retrieve the Ticket with EF Core change tracking enabled.
            // trackChanges: true is required because this method will modify:
            // - AssignedToUserId
            // - TicketAssignments
            // - TicketHistory
            // - UpdatedAt
            // - UpdatedBy
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges: true);

            // If the requested Ticket does not exist, the assignment operation cannot continue.
            if (ticket is null)
            {
                _logger.LogWarning("Ticket assignment failed because the Ticket was not found. TicketId: {TicketId}", ticketId);
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Resolved and Closed Tickets cannot be assigned or reassigned.
            // Once a Ticket has reached one of these states,
            // assignment changes are not allowed by the current workflow.
            if (ticket.StatusId == TicketStatusType.Resolved || ticket.StatusId == TicketStatusType.Closed)
            {
                _logger.LogWarning(
                   "Ticket assignment rejected because of the current Status. TicketId: {TicketId}, Status: {Status}",
                   ticketId,
                   ticket.StatusId);

                throw new BusinessRuleException($"A Ticket with Status '{ticket.StatusId}' cannot be assigned or reassigned.");
            }

            // Retrieve the User selected by the Administrator as the new Support Executive.
            var supportExecutive = await _userRepository.GetByIdAsync(request.SupportExecutiveId);

            // Validate the selected User.
            // The selected User must:
            // - Exist
            // - Be active
            // - Have the SupportExecutive Role
            if (supportExecutive is null)
            {
                throw new NotFoundException($"User with ID {request.SupportExecutiveId} was not found.");
            }

            // The User must be active.
            if (!supportExecutive.IsActive)
            {
                throw new BusinessRuleException("The selected Support Executive is inactive.");
            }

            // Only a User having the SupportExecutive Role
            // can receive a Ticket assignment.
            if (supportExecutive.RoleId != RoleType.SupportExecutive)
            {
                throw new BusinessRuleException("The selected User is not a Support Executive.");
            }

            // Do not create another assignment record when the Ticket
            // is already assigned to the same Support Executive.
            if (ticket.AssignedToUserId == request.SupportExecutiveId)
            {
                throw new ConflictException("The Ticket is already assigned to the selected Support Executive.");
            }

            // Capture one UTC timestamp and use it consistently
            // for all changes performed during this assignment operation.
            var now = DateTime.UtcNow;

            // Preserve the currently assigned Support Executive, if any.
            // null means: This is the Ticket's first assignment.
            // A value means: The Ticket is being reassigned.
            var previousAssignedUserId = ticket.AssignedToUserId;

            // Find the currently active assignment record.
            // An assignment is considered active when UnassignedAt is null.
            var activeAssignment = ticket.Assignments.FirstOrDefault(assignment => !assignment.UnassignedAt.HasValue);

            // If the Ticket already has an active assignment,
            // close that assignment before creating the new one.
            if (activeAssignment is not null)
            {
                // Record when the previous Support Executive stopped being responsible for the Ticket.
                activeAssignment.UnassignedAt = now;

                // Maintain audit information on the old assignment record.
                activeAssignment.UpdatedAt = now;
                activeAssignment.UpdatedBy = administratorId;
            }

            // Create a new TicketAssignment record.
            // This preserves the assignment history instead of simply
            // replacing AssignedToUserId on the Ticket.
            var assignment = new TicketAssignment
            {
                // Ticket being assigned.
                SupportTicketId = ticket.Id,

                // Support Executive receiving the Ticket.
                AssignedToUserId = request.SupportExecutiveId,

                // Administrator performing the assignment.
                AssignedByUserId = administratorId,

                // Date and time when this assignment started.
                AssignedAt = now,

                // Audit User.
                CreatedBy = administratorId
            };

            // Add the new assignment to the Ticket's assignment history.
            ticket.Assignments.Add(assignment);

            // Update the Ticket's current assignee.
            // TicketAssignments preserves historical assignments,
            // while AssignedToUserId represents the current assignment.
            ticket.AssignedToUserId = request.SupportExecutiveId;

            // Maintain Ticket audit information.
            ticket.UpdatedAt = now;
            ticket.UpdatedBy = administratorId;

            // Add a TicketHistory entry so the assignment or reassignment
            // is visible in the Ticket's audit trail.
            ticket.History.Add(new TicketHistory
            {
                SupportTicketId = ticket.Id,

                // Administrator responsible for the change.
                ChangedByUserId = administratorId,

                // Distinguish between first assignment and reassignment.
                Action = previousAssignedUserId.HasValue ? "Ticket Reassigned" : "Ticket Assigned",

                // Record which Ticket field changed.
                FieldName = nameof(SupportTicket.AssignedToUserId),

                // Previous assignee.
                // This will be null for the first assignment.
                OldValue = previousAssignedUserId?.ToString(),

                // New Support Executive.
                NewValue = request.SupportExecutiveId.ToString(),

                // Human-readable explanation of the operation.
                Remarks =
                    previousAssignedUserId.HasValue
                        ? "Ticket reassigned to another Support Executive."
                        : "Ticket assigned to a Support Executive.",

                CreatedBy = administratorId
            });

            // When a newly created Ticket is assigned for the first time,
            // automatically move its Status from Open to Assigned.
            // A reassignment of an already Assigned/InProgress Ticket
            // should not change its current Status.
            if (ticket.StatusId == TicketStatusType.Open)
            {
                // Update the current Ticket Status.
                ticket.StatusId = TicketStatusType.Assigned;

                // Record the automatic Status change in TicketHistory.
                ticket.History.Add(new TicketHistory
                {
                    SupportTicketId = ticket.Id,
                    ChangedByUserId = administratorId,
                    Action = "Status Changed",
                    FieldName = nameof(SupportTicket.StatusId),
                    OldValue = TicketStatusType.Open.ToString(),
                    NewValue = TicketStatusType.Assigned.ToString(),
                    Remarks = "Ticket automatically moved to Assigned status.",
                    CreatedBy = administratorId
                });
            }

            // Persist:
            // - Ticket assignment
            // - Previous assignment closing information
            // - Current AssignedToUserId
            // - Status change, when applicable
            // - Ticket history
            // - Audit information
            await _ticketRepository.SaveChangesAsync();

            _logger.LogInformation(
               "Ticket assignment completed successfully. TicketId: {TicketId}, PreviousSupportExecutiveId: {PreviousSupportExecutiveId}, NewSupportExecutiveId: {NewSupportExecutiveId}, AdministratorId: {AdministratorId}",
               ticketId,
               previousAssignedUserId,
               request.SupportExecutiveId,
               administratorId);

            // Reload the Ticket with its related Entities.
            // This ensures the response contains the latest:
            // - Assigned User
            // - Assignment history
            // - Status
            // - History
            // - Other related information
            var updatedTicket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges: false);

            // The Ticket was already successfully saved.
            // Failure here indicates an unexpected system/data problem,
            // so allow the Global Exception Handler to return HTTP 500.
            if (updatedTicket is null)
            {
                throw new InvalidOperationException("The updated Support Ticket could not be retrieved.");
            }

            // Administrator is a support-staff User, so internal comments may be included.
            return updatedTicket.ToDetailsDTO(includeInternalComments: true);
        }

        // Change the Status of a Support Ticket.
        // Only Support Executives and Administrators can perform this operation.
        public async Task<TicketDetailsResponseDTO> ChangeStatusAsync(int ticketId, ChangeTicketStatusRequestDTO request)
        {
            // Get the authenticated User's ID.
            // This will be used for authorization and audit information.
            var currentUserId = _currentUserService.UserId!.Value;

            // Get the authenticated User's Role.
            var currentRole = _currentUserService.Role!.Value;

            _logger.LogInformation(
                "Ticket Status change started. TicketId: {TicketId}, RequestedStatus: {RequestedStatus}, UserId: {UserId}, Role: {Role}",
                ticketId,
                request.StatusId,
                currentUserId,
                currentRole);

            // Only Support Executives and Administrators are allowed to change Ticket Status.
            // Customers cannot directly control the Ticket workflow.
            if (currentRole != RoleType.SupportExecutive && currentRole != RoleType.Administrator)
            {
                _logger.LogWarning(
                   "Ticket Status change rejected because the User does not have permission. TicketId: {TicketId}, UserId: {UserId}, Role: {Role}",
                   ticketId,
                   currentUserId,
                   currentRole);

                throw new BusinessRuleException("Only Support Executives and Administrators can change Ticket Status.");
            }

            // Retrieve the Ticket with change tracking enabled
            // because its Status and audit information will be modified.
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges: true);

            // Stop when the requested Ticket does not exist.
            if (ticket is null)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Support Executive Access Rule
            // A Support Executive can change the Status only
            // of a Ticket currently assigned to them.
            // Administrators are not restricted by this check.
            if (currentRole == RoleType.SupportExecutive && ticket.AssignedToUserId != currentUserId)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Verify that the requested Status exists.
            var newStatus = await _statusRepository.GetByIdAsync(request.StatusId);

            if (newStatus is null)
            {
                throw new ApplicationValidationException(
                    new[]
                    {
                        "The selected Ticket Status does not exist."
                    });
            }

            // Existing but inactive Status values cannot be selected.
            if (!newStatus.IsActive)
            {
                throw new BusinessRuleException("The selected Ticket Status is currently inactive.");
            }

            // Preserve the existing Status before modifying the Ticket.
            // This is required for:
            // - Validating the transition
            // - Ticket history
            // - Reopen logic
            var currentStatus = ticket.StatusId;

            // Validate the requested workflow transition.
            // The application allows only predefined Status movements.
            // For example:
            // Assigned -> InProgress
            // InProgress -> Resolved
            // Resolved -> InProgress
            // Resolved -> Closed
            if (!IsValidStatusTransition(currentStatus, request.StatusId))
            {
                _logger.LogWarning(
                   "Invalid Ticket Status transition attempted. TicketId: {TicketId}, CurrentStatus: {CurrentStatus}, RequestedStatus: {RequestedStatus}, UserId: {UserId}",
                   ticketId,
                   currentStatus,
                   request.StatusId,
                   currentUserId);

                throw new BusinessRuleException($"Changing Ticket Status from '{currentStatus}' to '{request.StatusId}' is not allowed.");
            }

            // Use one UTC timestamp for all modifications performed by this Status-change operation.
            var now = DateTime.UtcNow;

            // Update the Ticket's current Status.
            ticket.StatusId = request.StatusId;

            // Maintain Ticket audit information.
            ticket.UpdatedAt = now;
            ticket.UpdatedBy = currentUserId;

            // When moving to Resolved,
            // record when the Ticket was resolved.
            if (request.StatusId == TicketStatusType.Resolved)
            {
                ticket.ResolvedAt = now;

                // A resolved Ticket is not closed yet.
                ticket.ClosedAt = null;
            }

            // Reopen Rule
            // Resolved -> InProgress means that the Ticket
            // has been reopened because additional work is required.
            if (currentStatus == TicketStatusType.Resolved && request.StatusId == TicketStatusType.InProgress)
            {
                // Since the Ticket is no longer resolved,
                // clear its previous resolved timestamp.
                ticket.ResolvedAt = null;

                // A reopened Ticket cannot remain closed.
                ticket.ClosedAt = null;
            }

            // When moving to Closed,
            // record when the Ticket was closed.
            if (request.StatusId == TicketStatusType.Closed)
            {
                ticket.ClosedAt = now;
            }

            // Add the Status change to TicketHistory
            // so the complete Ticket lifecycle can be audited.
            ticket.History.Add(new TicketHistory
            {
                SupportTicketId = ticket.Id,

                // User who performed the change.
                ChangedByUserId = currentUserId,

                // Use a more meaningful action name
                // when a resolved Ticket is reopened.
                Action =
                        request.StatusId == TicketStatusType.InProgress &&
                        currentStatus == TicketStatusType.Resolved
                            ? "Ticket Reopened"
                            : "Status Changed",

                // Identify the field that changed.
                FieldName = nameof(SupportTicket.StatusId),

                // Previous Status.
                OldValue = currentStatus.ToString(),

                // New Status.
                NewValue = request.StatusId.ToString(),

                // Optional explanation supplied by the User.
                Remarks = request.Remarks?.Trim(),

                CreatedBy = currentUserId
            });

            // Persist the Status change and TicketHistory record.
            await _ticketRepository.SaveChangesAsync();

            _logger.LogInformation(
               "Ticket Status changed successfully. TicketId: {TicketId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}, ChangedByUserId: {UserId}",
               ticketId,
               currentStatus,
               request.StatusId,
               currentUserId);

            // Reload the Ticket so the response contains
            // the latest Status, timestamps, history, and related information.
            var updatedTicket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges: false);

            if (updatedTicket is null)
            {
                throw new InvalidOperationException("The updated Support Ticket could not be retrieved.");
            }

            // This operation is performed by support staff, so internal comments can be included.
            return updatedTicket.ToDetailsDTO(includeInternalComments: true);
        }


        // Add a Comment to a Support Ticket.
        // Customers, Support Executives, and Administrators can add Comments,
        // subject to their Ticket-access and internal-comment rules.
        public async Task<TicketCommentResponseDTO> AddCommentAsync(int ticketId, AddTicketCommentRequestDTO request)
        {
            // Get current User information from JWT claims.
            var currentUserId = _currentUserService.UserId!.Value;
            var currentRole = _currentUserService.Role!.Value;

            _logger.LogInformation(
              "Ticket Comment creation started. TicketId: {TicketId}, UserId: {UserId}, Role: {Role}, IsInternal: {IsInternal}",
              ticketId,
              currentUserId,
              currentRole,
              request.IsInternal);

            // Retrieve the Ticket with change tracking enabled.
            // We need tracking because a new TicketComment and TicketHistory record will be added.
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges: true);

            // The Comment cannot be added when the Ticket does not exist.
            if (ticket is null)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Closed Tickets are final in the current workflow.
            // Therefore, no additional Comments can be added.
            if (ticket.StatusId == TicketStatusType.Closed)
            {
                _logger.LogWarning(
                    "Comment creation rejected because the Ticket is Closed. TicketId: {TicketId}, UserId: {UserId}",
                    ticketId,
                    currentUserId);

                throw new BusinessRuleException("Comments cannot be added to a Closed Ticket.");
            }

            // Customer Access Rules
            if (currentRole == RoleType.Customer)
            {
                // A Customer can comment only on their own Ticket.
                if (ticket.CustomerId != currentUserId)
                {
                    throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
                }

                // Customers are not allowed to create internal Comments.
                // Internal Comments are intended only for communication
                // between Support Executives and Administrators.
                if (request.IsInternal)
                {
                    _logger.LogWarning(
                      "Internal Comment creation rejected for Customer. TicketId: {TicketId}, CustomerId: {CustomerId}",
                      ticketId,
                      currentUserId);

                    throw new BusinessRuleException("Customers cannot add internal Comments.");
                }
            }

            // Support Executives can comment only on
            // Tickets currently assigned to them.
            if (currentRole == RoleType.SupportExecutive && ticket.AssignedToUserId != currentUserId)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Reject unsupported Roles defensively.
            if (currentRole != RoleType.Customer &&
                currentRole != RoleType.SupportExecutive &&
                currentRole != RoleType.Administrator)
            {
                throw new BusinessRuleException("The current User is not allowed to add Ticket Comments.");
            }

            // Create the TicketComment Entity.
            var comment = new TicketComment
            {
                // Ticket receiving the Comment.
                SupportTicketId = ticket.Id,

                // Authenticated User creating the Comment.
                UserId = currentUserId,

                // Remove unnecessary spaces from the beginning and end of the Comment.
                Content = request.Content.Trim(),

                // Determines whether the Comment is visible only to support staff.
                IsInternal = request.IsInternal,

                // Audit User.
                CreatedBy = currentUserId
            };

            // Add the Comment through the Ticket navigation collection.
            // Because the Ticket is tracked by EF Core,
            // the new Comment will be inserted when SaveChangesAsync() executes.
            ticket.Comments.Add(comment);

            // A new Comment is considered an update to the Ticket.
            ticket.UpdatedAt = DateTime.UtcNow;

            ticket.UpdatedBy = currentUserId;

            // Add an audit-history entry for the Comment.
            ticket.History.Add(new TicketHistory
            {
                SupportTicketId = ticket.Id,
                ChangedByUserId = currentUserId,

                // Distinguish internal staff Comments from normal public Comments.
                Action = request.IsInternal ? "Internal Comment Added" : "Comment Added",

                // Store a readable audit description.
                Remarks = request.IsInternal ? "An internal comment was added." : "A comment was added.",

                CreatedBy = currentUserId
            });

            // Persist both:
            // - TicketComment
            // - TicketHistory
            // together with Ticket audit changes.
            await _ticketRepository.SaveChangesAsync();

            _logger.LogInformation(
              "Ticket Comment added successfully. TicketId: {TicketId}, CommentId: {CommentId}, UserId: {UserId}, IsInternal: {IsInternal}",
              ticketId,
              comment.Id,
              currentUserId,
              comment.IsInternal);

            // Build the response directly from the newly created Comment.
            return new TicketCommentResponseDTO
            {
                // Database-generated Comment ID is available after SaveChangesAsync().
                Id = comment.Id,
                UserId = currentUserId,

                // The current User's FullName comes from JWT claims.
                UserName = _currentUserService.FullName ?? string.Empty,

                Content = comment.Content,
                IsInternal = comment.IsInternal,
                CreatedAt = comment.CreatedAt
            };
        }


        // Return the complete assignment history of a Support Ticket.
        // Only Support Executives and Administrators can access this information.
        public async Task<IReadOnlyCollection<TicketAssignmentResponseDTO>> GetAssignmentHistoryAsync(int ticketId)
        {
            // Get current User information.
            var currentUserId = _currentUserService.UserId!.Value;
            var currentRole = _currentUserService.Role!.Value;

            _logger.LogInformation(
               "Ticket assignment history requested. TicketId: {TicketId}, UserId: {UserId}, Role: {Role}",
               ticketId,
               currentUserId,
               currentRole);

            // Assignment history is internal support information.
            // Therefore:
            // SupportExecutive -> Allowed, subject to assignment ownership
            // Administrator -> Allowed
            // Customer -> Not Allowed
            if (currentRole != RoleType.SupportExecutive && currentRole != RoleType.Administrator)
            {
                _logger.LogWarning(
                   "Ticket assignment history access rejected. TicketId: {TicketId}, UserId: {UserId}, Role: {Role}",
                   ticketId,
                   currentUserId,
                   currentRole);

                throw new BusinessRuleException("Only Support Executives and Administrators can view Ticket assignment history.");
            }

            // Read-only operation, so tracking is not required.
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges: false);

            // Stop when the Ticket does not exist.
            if (ticket is null)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Support Executive Access Rule
            // A Support Executive can view assignment history only
            // for a Ticket currently assigned to them.
            // Administrator can view assignment history for any Ticket.
            if (currentRole == RoleType.SupportExecutive && ticket.AssignedToUserId != currentUserId)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // Return all assignment records with the most recent assignment appearing first.
            var assignments = ticket.Assignments

                // Latest assignments first.
                .OrderByDescending(assignment => assignment.AssignedAt)

                // Convert Domain Entities into API response DTOs.
                .Select(assignment => assignment.ToResponseDTO())

                .ToList();

            _logger.LogInformation(
               "Ticket assignment history retrieved successfully. TicketId: {TicketId}, AssignmentCount: {AssignmentCount}, UserId: {UserId}",
               ticketId,
               assignments.Count,
               currentUserId);

            return assignments;
        }

        // Search Support Tickets using filtering, sorting, and pagination.
        public async Task<PagedResult<TicketSummaryResponseDTO>> SearchAsync(TicketQueryParameters parameters)
        {
            // Get the current authenticated User's ID.
            var currentUserId = _currentUserService.UserId!.Value;

            // Get the current authenticated User's Role.
            var currentRole = _currentUserService.Role!.Value;

            _logger.LogInformation(
               "Ticket search started. UserId: {UserId}, Role: {Role}, PageNumber: {PageNumber}, PageSize: {PageSize}",
               currentUserId,
               currentRole,
               parameters.PageNumber,
               parameters.PageSize);

            // These optional values apply the security scope directly at Repository/SQL query level.
            // null means that particular security restriction will not be applied.
            int? customerScopeId = null;
            int? assignedToScopeId = null;

            // Determine which Tickets the current User is allowed to search.
            switch (currentRole)
            {
                case RoleType.Customer:
                    // A Customer can search only their own Tickets.
                    // The Repository will add a condition similar to:
                    // WHERE CustomerId = currentUserId
                    customerScopeId = currentUserId;
                    break;

                case RoleType.SupportExecutive:
                    // A Support Executive can search only Tickets currently assigned to them.
                    // The Repository will add a condition similar to:
                    // WHERE AssignedToUserId = currentUserId
                    assignedToScopeId = currentUserId;
                    break;

                case RoleType.Administrator:
                    // Administrators can search all Tickets.
                    // Both security-scope values remain null,
                    // so no Customer or assignment restriction is added.
                    break;

                default:
                    _logger.LogWarning(
                       "Ticket search rejected because the current Role is unsupported. UserId: {UserId}, Role: {Role}",
                       currentUserId,
                       currentRole);

                    // Reject unknown or unsupported Roles.
                    throw new BusinessRuleException("The current User Role is not supported for Ticket searching.");
            }

            // Delegate filtering, searching, sorting, pagination to the Repository.
            var result = await _ticketRepository.SearchAsync(parameters, customerScopeId, assignedToScopeId);

            // Convert the Domain Entities returned for the current page
            // into lightweight TicketSummaryResponseDTO objects.
            var tickets = result.Items
                    .Select(ticket => ticket.ToSummaryDTO())
                    .ToList();

            _logger.LogInformation(
               "Ticket search completed successfully. UserId: {UserId}, Role: {Role}, ReturnedRecords: {ReturnedRecords}, TotalRecords: {TotalRecords}",
               currentUserId,
               currentRole,
               tickets.Count,
               result.TotalRecords);

            // Create a new paged response containing:
            // - Converted Ticket DTOs
            // - Total matching records
            // - Current page number
            // - Current page size

            // PagedResult.Create() also calculates:
            // - TotalPages
            // - HasPreviousPage
            // - HasNextPage
            return PagedResult<TicketSummaryResponseDTO>.Create(
                tickets,
                result.TotalRecords,
                result.PageNumber,
                result.PageSize);
        }

        // Checks whether the requested Ticket Status change is allowed.
        // The Ticket workflow currently allows:
        // Assigned   -> InProgress
        // InProgress -> Resolved
        // Resolved   -> InProgress (Reopen)
        // Resolved   -> Closed
        // Every other transition is rejected.
        private static bool IsValidStatusTransition(TicketStatusType currentStatus, TicketStatusType newStatus)
        {
            // Assigned -> InProgress
            // The Support Executive has started working on the Ticket.
            if (currentStatus == TicketStatusType.Assigned && newStatus == TicketStatusType.InProgress)
            {
                return true;
            }

            // InProgress -> Resolved
            // The Support Executive has completed the work.
            if (currentStatus == TicketStatusType.InProgress && newStatus == TicketStatusType.Resolved)
            {
                return true;
            }

            // Resolved -> InProgress
            // The resolved Ticket is reopened because more work is required.
            if (currentStatus == TicketStatusType.Resolved && newStatus == TicketStatusType.InProgress)
            {
                return true;
            }

            // Resolved -> Closed
            // The resolved Ticket is finally closed.
            if (currentStatus == TicketStatusType.Resolved && newStatus == TicketStatusType.Closed)
            {
                return true;
            }

            // Every other Status change is not allowed.
            return false;
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