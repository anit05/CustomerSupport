namespace CustomerSupport.Application.Models
{
    // Represents storage information returned after a file is saved.
    public sealed class StoredFileResult
    {
        // Generated unique file/blob name used by the storage provider.
        public string StoredFileName { get; set; } = string.Empty;

        // Provider-relative path or key used to locate the file later.
        // For Local storage this is a relative path.
        // For Azure Blob Storage this can later represent a blob name/key.
        public string RelativePath { get; set; } = string.Empty;
    }
}