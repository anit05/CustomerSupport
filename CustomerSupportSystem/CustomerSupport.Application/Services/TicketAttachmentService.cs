using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Exceptions;
using CustomerSupport.Application.Interfaces.CurrentUser;
using CustomerSupport.Application.Interfaces.Files;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Mappings;
using CustomerSupport.Application.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Services
{
    // Implements the business rules for Support Ticket Attachments.
    // Responsibilities of this Service include:
    // - Checking whether the current User can access the Ticket
    // - Validating uploaded files
    // - Coordinating physical file storage
    // - Creating Attachment metadata
    // - Updating Ticket audit information
    // - Recording Ticket History
    // - Preparing files for download
    public sealed class TicketAttachmentService : ITicketAttachmentService
    {
        // Maximum allowed Attachment size is 10 MB.
        // 10 * 1024 * 1024 converts 10 MB into bytes.
        private const long MaxFileSizeInBytes = 10 * 1024 * 1024;

        // Defines the file extensions currently allowed by the application.
        //
        // File extension validation prevents Users from uploading
        // arbitrary file types that the application does not support.
        //
        // Additional extensions can be added later when business requirements change.
        private static readonly string[] AllowedExtensions =
        [
            ".pdf",
            ".png",
            ".jpg",
            ".jpeg",
            ".txt",
            ".doc",
            ".docx"
        ];

        // Used to retrieve and update Support Tickets.
        private readonly ITicketRepository _ticketRepository;

        // Provides the abstraction for physical file storage.
        // TicketAttachmentService does not know whether the implementation stores the file:
        // - On the local file system
        // - In Azure Blob Storage
        // - Or in another storage provider
        private readonly IFileStorageService _fileStorageService;

        // Provides information about the currently authenticated User,
        // including UserId, Role, and FullName.
        // This information is used for:
        // - Authorization
        // - Audit information
        // - Attachment ownership information
        private readonly ICurrentUserService _currentUserService;

        // Records important Attachment workflow events.
        private readonly ILogger<TicketAttachmentService> _logger;

        // Dependencies are supplied through Dependency Injection.
        public TicketAttachmentService(
            ITicketRepository ticketRepository,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUserService,
            ILogger<TicketAttachmentService> logger)
        {
            _ticketRepository = ticketRepository;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        // Uploads a physical file and associates it with a Support Ticket.
        // The operation performs the following major steps:
        // 1. Identify the current authenticated User.
        // 2. Verify that the User can access the Ticket.
        // 3. Verify that the Ticket is not Closed.
        // 4. Validate the uploaded file.
        // 5. Save the physical file through IFileStorageService.
        // 6. Create TicketAttachment metadata.
        // 7. Add Ticket History and audit information.
        // 8. Save database changes.
        // 9. Remove the physical file if database persistence fails.
        public async Task<TicketAttachmentResponseDTO> UploadAsync(int ticketId, FileUploadModel file)
        {
            // Get the authenticated User ID from the current JWT claims.
            var currentUserId = _currentUserService.UserId!.Value;

            _logger.LogInformation(
                "Attachment upload started. TicketId: {TicketId}, UserId: {UserId}, FileSize: {FileSize}",
                ticketId,
                currentUserId,
                file.Length);

            // Retrieve the Ticket and verify that the current User is allowed to access it.
            // trackChanges: true is required because this operation modifies:
            // - Ticket.Attachments
            // - Ticket.History
            // - Ticket.UpdatedAt
            // - Ticket.UpdatedBy
            var ticket = await GetAccessibleTicketAsync(ticketId, trackChanges: true);

            // A Closed Ticket is considered final in the current workflow.
            // Existing Attachments may still be listed or downloaded,
            // but Users are not allowed to add new Attachments after closure.
            if (ticket.StatusId == TicketStatusType.Closed)
            {
                _logger.LogWarning(
                    "Attachment upload rejected because the Ticket is Closed. TicketId: {TicketId}, UserId: {UserId}",
                    ticketId,
                    currentUserId);

                throw new BusinessRuleException("Attachments cannot be uploaded to a Closed Ticket.");
            }

            // Validate the file before writing anything to physical storage.
            // This avoids creating unnecessary files when:
            // - The uploaded file is empty
            // - The file is larger than 10 MB
            // - The extension is not supported
            ValidateFile(file);

            // Extract the validated file extension.
            // ToLowerInvariant() ensures that extensions such as:
            // .PDF
            // .Pdf
            // .pdf
            // are treated consistently as ".pdf".
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Keep track of the physical file after it has been stored.
            // This variable is intentionally declared before the try block
            // because it is required by the catch block for cleanup.
            StoredFileResult? storedFile = null;

            try
            {
                // Save the actual physical file through the storage abstraction.
                storedFile = await _fileStorageService.SaveAsync(file.Content, extension, ticketId);

                // Create the database metadata describing the physical file.
                // SQL Server stores information such as:
                // - Original filename
                // - Generated stored filename
                // - Content type
                // - File size
                // - Relative storage path
                // - Uploader
                var attachment =
                    new TicketAttachment
                    {
                        // Ticket to which this Attachment belongs.
                        SupportTicketId = ticket.Id,

                        // Authenticated User who uploaded the file.
                        UploadedByUserId = currentUserId,

                        // Preserve the original filename for display/download.
                        OriginalFileName = Path.GetFileName(file.FileName),

                        // Server-generated physical filename returned
                        // by the current storage provider.
                        StoredFileName = storedFile.StoredFileName,

                        // Preserve the incoming content type when available.
                        // application/octet-stream is used as a safe generic
                        // fallback when no content type was supplied.
                        ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                                ? "application/octet-stream"
                                : file.ContentType,

                        // Store file size for display, validation, and future reporting purposes.
                        FileSizeInBytes = file.Length,

                        // Store only the provider-generated relative path.
                        // We avoid storing machine-specific absolute paths inside SQL Server.
                        RelativePath = storedFile.RelativePath,

                        // Maintain normal application audit information.
                        CreatedBy = currentUserId
                    };

                // Add the Attachment through the Ticket navigation collection.
                // Because the Ticket is tracked, EF Core will recognize
                // the new TicketAttachment Entity during SaveChangesAsync().
                ticket.Attachments.Add(attachment);

                // Adding an Attachment is considered a modification to the Support Ticket.
                // Update the Ticket-level audit information.
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.UpdatedBy = currentUserId;

                // Record the upload in TicketHistory.
                ticket.History.Add(new TicketHistory
                {
                    SupportTicketId = ticket.Id,
                    ChangedByUserId = currentUserId,
                    Action = "Attachment Uploaded",
                    Remarks = $"File '{attachment.OriginalFileName}' uploaded.",
                    CreatedBy = currentUserId
                });

                // Persist all database changes in one operation:
                // - TicketAttachment metadata
                // - Ticket UpdatedAt / UpdatedBy
                // - TicketHistory record
                await _ticketRepository.SaveChangesAsync();

                // At this point both the physical file and its
                // database metadata have been saved successfully.
                _logger.LogInformation(
                    "Attachment uploaded successfully. TicketId: {TicketId}, AttachmentId: {AttachmentId}, UserId: {UserId}",
                    ticketId,
                    attachment.Id,
                    currentUserId);

                // Return the Attachment information required by the API client.
                return new TicketAttachmentResponseDTO
                {
                    Id = attachment.Id,
                    OriginalFileName = attachment.OriginalFileName,
                    ContentType = attachment.ContentType,
                    FileSizeInBytes = attachment.FileSizeInBytes,
                    UploadedByUserId = currentUserId,
                    UploadedByUserName = _currentUserService.FullName ?? string.Empty,
                    CreatedAt = attachment.CreatedAt
                };
            }
            catch
            {
                // The physical file is saved before database persistence.
                //
                // Therefore, a database failure could otherwise leave a file
                // on disk that has no matching TicketAttachment database record.
                // Such a file is commonly called an "orphan file".
                if (storedFile is not null)
                {
                    // Remove the physical file to restore consistency between storage and the database.
                    await _fileStorageService.DeleteAsync(storedFile.RelativePath);
                }

                // Do not convert or manually return an error response here.
                // Rethrow the original exception so the application's existing
                // Global Exception Handler can:
                // - Log it appropriately
                // - Select the HTTP status code
                // - Return the standard ApiResponse<T> failure format
                throw;
            }
        }

        // Returns metadata for all Attachments belonging to an accessible Ticket.
        // This operation is read-only.
        // It does not open or return the physical files.
        public async Task<IReadOnlyCollection<TicketAttachmentResponseDTO>> GetAllAsync(int ticketId)
        {
            _logger.LogInformation(
                "Attachment list requested. TicketId: {TicketId}, UserId: {UserId}",
                ticketId,
                _currentUserService.UserId);

            // Retrieve the Ticket 
            // trackChanges: false is appropriate because this operation
            // only reads data and will not update the Ticket.
            var ticket = await GetAccessibleTicketAsync(ticketId, trackChanges: false);

            // Return newest Attachments first.
            // ToResponseDTO() converts Domain Entities into
            // API-friendly response models without exposing the Entity itself.
            var attachments =
                ticket.Attachments
                    .OrderByDescending(attachment => attachment.CreatedAt)
                    .Select(attachment => attachment.ToResponseDTO())
                    .ToList();

            _logger.LogInformation(
                "Attachment list retrieved successfully. TicketId: {TicketId}, Count: {Count}",
                ticketId,
                attachments.Count);

            return attachments;
        }

        // Prepares a specific Attachment for download.
        // Before opening the physical file, this method verifies:
        // - The Ticket exists
        // - The current User can access the Ticket
        // - The Attachment belongs to the Ticket
        // - The physical file still exists in storage
        public async Task<FileDownloadResult> DownloadAsync(int ticketId, int attachmentId)
        {
            _logger.LogInformation(
                "Attachment download requested. TicketId: {TicketId}, AttachmentId: {AttachmentId}, UserId: {UserId}",
                ticketId,
                attachmentId,
                _currentUserService.UserId);

            // Retrieve the Ticket 
            var ticket = await GetAccessibleTicketAsync(ticketId, trackChanges: false);

            // Find the requested Attachment only inside the already authorized Ticket.
            // This also verifies that the Attachment actually belongs
            // to the specified Ticket.
            var attachment = ticket.Attachments.FirstOrDefault(item => item.Id == attachmentId);

            // Attachment does not belong to this Ticket.
            if (attachment is null)
            {
                throw new NotFoundException($"Attachment with ID {attachmentId} was not found for this Ticket.");
            }

            // Ask the configured storage provider to open the physical file.
            var stream = await _fileStorageService.OpenReadAsync(attachment.RelativePath);

            // It is possible for database metadata to exist while
            // the physical file has been manually deleted or lost.
            // Treat this situation as a missing Attachment resource.
            if (stream is null)
            {
                _logger.LogWarning(
                    "Attachment metadata exists but the physical file was not found. TicketId: {TicketId}, AttachmentId: {AttachmentId}",
                    ticketId,
                    attachmentId);

                throw new NotFoundException("The attachment file could not be found in storage.");
            }

            _logger.LogInformation(
                "Attachment download prepared successfully. TicketId: {TicketId}, AttachmentId: {AttachmentId}",
                ticketId,
                attachmentId);

            // Return everything required by the Controller to construct the HTTP file-download response.
            // Content: Physical file Stream.
            // ContentType: Tells the client what type of file is being returned.
            // FileName: Uses the original User-facing filename rather than
            //           the internal GUID-based storage filename.
            return new FileDownloadResult
            {
                Content = stream,
                ContentType = attachment.ContentType,
                FileName = attachment.OriginalFileName
            };
        }

        // Centralizes Ticket access validation for all Attachment operations.
        // Keeping authorization logic here prevents Upload, GetAll, and Download
        // from duplicating the same access checks.

        // Access rules:
        // Customer: Can access only their own Tickets.
        // Support Executive: Can access only Tickets currently assigned to them.
        // Administrator: Can access any Ticket.
        private async Task<SupportTicket> GetAccessibleTicketAsync(int ticketId, bool trackChanges)
        {
            // Read the current authenticated User information.
            var currentUserId = _currentUserService.UserId!.Value;

            var currentRole = _currentUserService.Role!.Value;

            // Retrieve the Ticket.
            // The caller decides whether EF Core change tracking
            // is required for the operation.
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, trackChanges);

            // Stop immediately when the Ticket does not exist.
            if (ticket is null)
            {
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // CUSTOMER ACCESS RULE
            // A Customer can access Attachment information only
            // for Tickets that belong to that Customer.
            if (currentRole == RoleType.Customer && ticket.CustomerId != currentUserId)
            {
                // Intentionally return the same NotFoundException used for a genuinely missing Ticket.
                // We do not tell the Customer:
                // "This Ticket exists, but belongs to somebody else."
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // SUPPORT EXECUTIVE ACCESS RULE
            // A Support Executive can access Attachment information only
            // for Tickets currently assigned to them.
            if (currentRole == RoleType.SupportExecutive && ticket.AssignedToUserId != currentUserId)
            {
                // Again, use NotFoundException
                throw new NotFoundException($"Support Ticket with ID {ticketId} was not found.");
            }

            // ADMINISTRATOR ACCESS RULE
            // No additional restriction is required for Administrators.
            // They are allowed to access Attachments for any Ticket.
            return ticket;
        }

        // Performs Application-level validation before a file is
        // sent to the physical storage provider.
        //
        // Multiple validation errors are collected first and returned
        // together using ApplicationValidationException.
        //
        // This is better than failing after only the first validation problem
        // because the API client can correct all detected issues at once.
        private static void ValidateFile(FileUploadModel file)
        {
            // Collect all file-validation problems.
            var errors = new List<string>();

            // Reject empty files.
            // Length must be greater than zero before the file is written to storage.
            if (file.Length <= 0)
            {
                errors.Add("The uploaded file is empty.");
            }

            // Prevent excessively large uploads.
            // The current application allows a maximum of 10 MB.
            if (file.Length > MaxFileSizeInBytes)
            {
                errors.Add("The maximum allowed file size is 10 MB.");
            }

            // Extract and normalize the file extension.
            // Example report.PDF becomes: .pdf
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Reject files that:
            // - Have no extension
            // - Use an extension that is not in AllowedExtensions
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                errors.Add("Unsupported file type. Allowed extensions are: " + string.Join(", ", AllowedExtensions));
            }

            // Throw one ApplicationValidationException containing all validation messages.
            //
            // The existing Global Exception Handler will convert this
            // exception into the application's standard HTTP 400 response.
            if (errors.Count > 0)
            {
                throw new ApplicationValidationException(errors);
            }
        }
    }
}