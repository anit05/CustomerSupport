namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class TicketAttachment
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public int UploadedByUserId { get; set; }
        public string UploadedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
