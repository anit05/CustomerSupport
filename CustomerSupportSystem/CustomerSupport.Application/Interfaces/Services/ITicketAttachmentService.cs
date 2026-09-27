using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Application.Models;

namespace CustomerSupport.Application.Interfaces.Services
{
    // Defines the Ticket Attachment use cases exposed by the Application layer.
    //
    // The API Controller depends on this interface instead of directly depending
    // on TicketAttachmentService. This keeps the Controller independent of the
    // concrete business-logic implementation.
    //
    // This interface deals with application-level operations such as:
    // - Uploading an Attachment
    // - Retrieving Attachment metadata
    // - Downloading an Attachment
    //
    // It does not contain any physical file-storage logic.
    // The actual storage technology is hidden behind IFileStorageService.
    public interface ITicketAttachmentService
    {
        // Uploads a new file to a Support Ticket that the current User
        // is authorized to access.
        // ticketId: Identifies the Support Ticket receiving the Attachment.
        // file: Contains the uploaded file information and file Stream.

        // The method:
        // - Validates Ticket access
        // - Validates the file
        // - Saves the physical file
        // - Stores Attachment metadata in SQL Server
        // - Records Ticket audit/history information
        Task<TicketAttachmentResponseDTO> UploadAsync(int ticketId, FileUploadModel file);

        // Returns all Attachment metadata belonging to a Ticket
        // that the current User is authorized to access.
        //
        // This method returns metadata only.
        // It does not return the physical file contents.
        Task<IReadOnlyCollection<TicketAttachmentResponseDTO>> GetAllAsync(int ticketId);

        // Downloads a specific Attachment from an accessible Ticket.
        // ticketId: Identifies the Ticket.
        // attachmentId: Identifies the Attachment belonging to that Ticket.

        // The returned FileDownloadResult contains:
        // - File Stream
        // - Content Type
        // - Original File Name
        //
        // The API Controller can use this information to return the physical file to the client.
        Task<FileDownloadResult> DownloadAsync(int ticketId, int attachmentId);
    }
}