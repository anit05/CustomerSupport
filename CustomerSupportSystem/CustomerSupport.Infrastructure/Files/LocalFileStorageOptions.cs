namespace CustomerSupport.Infrastructure.Files
{
    // Contains settings used only by the Local file-system implementation.
    public sealed class LocalFileStorageOptions
    {
        // Absolute root directory where Ticket files will be stored.
        // Example: C:\...\CustomerSupport.API\Storage\TicketAttachments
        public string RootPath { get; set; } = string.Empty;
    }
}