namespace CustomerSupport.Application.DTOs.Tickets
{
    // Represents safe Attachment information returned by the API to the Client.
    // Internal storage values such as StoredFileName and RelativePath
    // are intentionally not exposed to clients.
    public sealed class TicketAttachmentResponseDTO
    {
        // Unique database identifier of the Attachment record.
        public int Id { get; set; }

        // Original file name supplied by the User.
        // This value is used when the file is downloaded.
        public string OriginalFileName { get; set; } = string.Empty;

        // MIME type recorded for the uploaded file.
        public string ContentType { get; set; } = string.Empty;

        // File size recorded at upload time.
        public long FileSizeInBytes { get; set; }

        // User who uploaded the file.
        public int UploadedByUserId { get; set; }

        // Human-readable uploader name for display purposes.
        public string UploadedByUserName { get; set; } = string.Empty;

        // UTC timestamp inherited from BaseEntity when the Attachment was created.
        public DateTime CreatedAt { get; set; }
    }
}