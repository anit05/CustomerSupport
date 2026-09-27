using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Interfaces.Services;
using CustomerSupport.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
    // Handles HTTP requests related to Support Ticket Attachments.
    [ApiController]
    [Route("api/tickets/{ticketId:int}/attachments")]
    [Authorize]
    public sealed class TicketAttachmentsController : ControllerBase
    {
        // Provides the Attachment use cases implemented by the Application layer.
        private readonly ITicketAttachmentService _attachmentService;

        // Used to record important HTTP/API workflow events.
        private readonly ILogger<TicketAttachmentsController> _logger;

        // Dependencies are supplied through ASP.NET Core Dependency Injection.
        public TicketAttachmentsController(
            ITicketAttachmentService attachmentService,
            ILogger<TicketAttachmentsController> logger)
        {
            _attachmentService = attachmentService;
            _logger = logger;
        }

        // Uploads one Attachment to a Support Ticket.
        // Endpoint: POST /api/tickets/{ticketId}/attachments
        // Example: POST /api/tickets/25/attachments
        // Because the request contains a physical file,
        // the client must send the request as multipart/form-data.
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<TicketAttachmentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<TicketAttachmentResponseDTO>>> Upload(int ticketId, [FromForm] IFormFile file)
        {
            // Record useful request metadata.
            _logger.LogInformation(
                "Attachment upload request received. TicketId: {TicketId}, FileSize: {FileSize}",
                ticketId,
                file.Length);

            // IFormFile is an ASP.NET Core type and belongs to the API layer.
            //
            // The Application layer should not depend directly on ASP.NET Core
            // HTTP types such as IFormFile.
            //
            // Therefore, open the uploaded file as a Stream and convert
            // the HTTP-specific IFormFile into our Application model.
            await using var stream = file.OpenReadStream();

            // Convert the API-specific upload into FileUploadModel.
            // FileUploadModel contains only the information required by the Application layer:
            // - Original filename
            // - Content type
            // - File size
            // - File content Stream

            var upload = new FileUploadModel
            {
                // Original filename supplied by the client.
                FileName = file.FileName,

                // MIME type supplied with the uploaded file.
                // Examples:
                // image/png
                // application/pdf
                ContentType = file.ContentType,

                // File size in bytes.
                Length = file.Length,

                // Stream containing the physical file data.
                Content = stream
            };

            // Delegate the actual Attachment workflow to the Application Service.
            // TicketAttachmentService will:
            // - Verify Ticket access
            // - Verify the Ticket is not Closed
            // - Validate file size and extension
            // - Save the physical file
            // - Create Attachment metadata
            // - Add Ticket History
            // - Save database changes
            var attachment = await _attachmentService.UploadAsync(ticketId, upload);

            _logger.LogInformation(
                "Attachment upload completed. TicketId: {TicketId}, AttachmentId: {AttachmentId}",
                ticketId,
                attachment.Id);

            // Wrap the newly created Attachment metadata
            // inside the application's common success-response structure.
            var response = ApiResponse<TicketAttachmentResponseDTO>
                    .SuccessResponse(
                        attachment,
                        "Attachment uploaded successfully.");

            return Ok(response);
        }

        // Returns metadata for all Attachments belonging to a Ticket.
        // Endpoint: GET /api/tickets/{ticketId}/attachments
        // Example: GET /api/tickets/25/attachments

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<TicketAttachmentResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketAttachmentResponseDTO>>>> GetAll(int ticketId)
        {
            // Record the incoming Attachment-list request.
            _logger.LogInformation(
                "Attachment list request received. TicketId: {TicketId}",
                ticketId);

            // Ask the Application Service to:
            // - Verify Ticket access
            // - Retrieve Attachment metadata
            // - Map Attachment Entities to Response DTOs
            var attachments = await _attachmentService.GetAllAsync(ticketId);

            // Wrap the collection inside the common ApiResponse<T> structure.
            var response = ApiResponse<IReadOnlyCollection<TicketAttachmentResponseDTO>>
                    .SuccessResponse(
                        attachments,
                        "Attachments retrieved successfully.");

            // Return HTTP 200 OK with the Attachment metadata collection.
            return Ok(response);
        }

        // Downloads one physical Attachment.
        // Endpoint: GET /api/tickets/{ticketId}/attachments/{attachmentId}/download
        // Example: GET /api/tickets/25/attachments/8/download
        // Unlike the other endpoints, this method does not return ApiResponse<T>
        // for a successful request because the successful response body
        // is the actual file content.
        [HttpGet("{attachmentId:int}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(int ticketId, int attachmentId)
        {
            // Log the requested Ticket and Attachment identifiers.
            _logger.LogInformation(
                "Attachment download request received. TicketId: {TicketId}, AttachmentId: {AttachmentId}",
                ticketId,
                attachmentId);

            // Ask the Application Service to prepare the file for download.
            // Before returning the file, the Service verifies:
            // - Ticket access
            // - Attachment ownership
            // - Physical-file availability

            // FileDownloadResult contains:
            // - File Stream
            // - Content Type
            // - Original File Name
            var file = await _attachmentService.DownloadAsync(ticketId, attachmentId);

            // Return the physical file directly to the HTTP client.
            // file.Content: The readable Stream containing the file.
            // file.ContentType: The MIME type used in the HTTP Content-Type header.
            // file.FileName: The original User-facing filename used by the browser/client
            //      when saving the downloaded file.
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}