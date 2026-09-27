namespace CustomerSupport.Application.Models
{
    // Represents an uploaded file inside the Application layer.
    // This keeps ASP.NET Core's IFormFile type out of Application Services.
    public sealed class FileUploadModel
    {
        // Original file name supplied by the client.
        public string FileName { get; set; } = string.Empty;

        // MIME type supplied by the client.
        public string ContentType { get; set; } = string.Empty;

        // File length in bytes.
        public long Length { get; set; }

        // Stream containing the uploaded file content.
        public Stream Content { get; set; } = Stream.Null;
    }
}