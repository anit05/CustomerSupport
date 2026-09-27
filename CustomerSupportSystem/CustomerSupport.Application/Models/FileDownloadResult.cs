namespace CustomerSupport.Application.Models
{
    // Contains everything required by the API to return a downloadable file.
    public sealed class FileDownloadResult
    {
        // Stream containing the physical file content.
        public Stream Content { get; set; } = Stream.Null;

        // MIME type returned in the HTTP response.
        public string ContentType { get; set; } = "application/octet-stream";

        // Original file name presented to the client during download.
        public string FileName { get; set; } = string.Empty;
    }
}