using CustomerSupport.Application.Interfaces.Files;
using CustomerSupport.Application.Models;

namespace CustomerSupport.Infrastructure.Files
{
    // Provides the local-file-system implementation of IFileStorageService.
    // This class belongs to the Infrastructure layer because working with
    // physical disk storage is an infrastructure concern.
    public sealed class LocalFileStorageService : IFileStorageService
    {
        // Contains configuration required for local file storage.
        // Example: RootPath = "Storage/TicketAttachments"
        // Keeping this value in configuration avoids hard-coding
        // physical storage locations inside the Service.
        private readonly LocalFileStorageOptions _options;

        // LocalFileStorageOptions is provided through Dependency Injection.
        // This allows the storage location to be configured through
        // appsettings.json rather than being hard-coded in this class.
        public LocalFileStorageService(LocalFileStorageOptions options)
        {
            _options = options;
        }

        // Saves a physical file on the local file system.
        // Files are organized by Ticket ID.
        // Example:
        // Storage/
        //   TicketAttachments/
        //      25/
        //         7fb642db68de4d8e94ca362291ce74dd.pdf
        //
        // The original User-supplied filename is NOT used as the physical filename.
        // A unique server-generated filename is used instead.
        public async Task<StoredFileResult> SaveAsync(Stream content, string extension, int ticketId)
        {
            // Create a separate folder for each Ticket.
            // If RootPath is: Storage/TicketAttachments
            // and TicketId is: 25
            // the resulting folder becomes:
            //      Storage/TicketAttachments/25
            //
            // Grouping files by Ticket makes the storage structure easier to understand and maintain.
            var ticketFolder = Path.Combine(_options.RootPath, ticketId.ToString());

            // Ensure that the Ticket folder exists before creating the file.
            // Directory.CreateDirectory does not fail when the directory already exists,
            // so it is safe to call every time.
            Directory.CreateDirectory(ticketFolder);

            // Generate a unique physical filename.
            // Example:
            //      7fb642db68de4d8e94ca362291ce74dd.pdf

            // We intentionally do not store the file using the original filename supplied by the User.
            // Reasons:
            // - Prevent filename collisions.
            // - Avoid overwriting another User's file.
            // - Avoid trusting User-controlled path information.
            // - Make the stored filename safe and predictable for the system.
            //
            // The original filename is still stored separately in SQL Server
            // so it can be shown to the User during download.
            var storedFileName = $"{Guid.NewGuid():N}{extension}";

            // Build the complete physical file path.
            // Example:
            // Storage/TicketAttachments/25/7fb642db68de4d8e94ca362291ce74dd.pdf
            var fullPath = Path.Combine(ticketFolder, storedFileName);

            // Create the new physical file.

            // FileMode.CreateNew:
            //      Creates a new file and fails if a file with the same name already exists.

            // FileAccess.Write:
            //      The stream is opened only for writing.

            // FileShare.None:
            //      Other processes cannot read or write the file
            //      while the upload is still being written.
            await using var outputStream =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            // Copy the uploaded file content into the physical file.
            await content.CopyToAsync(outputStream);

            // Store only a relative path in SQL Server.
            // Instead of storing:
            // C:\Projects\CustomerSupport\Storage\TicketAttachments\25\abc.pdf
            // We store:
            // 25/abc.pdf
            //
            // This is important because absolute paths depend on the machine
            // where the application is running.
            //
            // If the application later moves to another server,
            // container, or hosting environment, the database record
            // remains valid because it does not contain a machine-specific path.
            var relativePath =
                Path.Combine(ticketId.ToString(), storedFileName)
                    .Replace('\\', '/');

            // Return only the storage-generated information required by the Application layer.
            // TicketAttachmentService can save these values into the TicketAttachment Entity.
            return new StoredFileResult
            {
                StoredFileName = storedFileName,
                RelativePath = relativePath
            };
        }

        // Opens an existing local file and returns it as a readable Stream.
        // The Application layer passes the RelativePath stored in SQL Server.
        // This method converts that relative path into a safe physical path.
        public Task<Stream?> OpenReadAsync(string relativePath)
        {
            // Path.GetFullPath converts the given path into a normalized absolute path.
            var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relativePath));

            // The database record may exist while the physical file has been manually removed or lost.
            // Returning null allows the Application Service to decide
            // how that missing-file situation should be handled.
            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream?>(null);
            }

            // Open the file only for reading.
            // FileMode.Open: The file must already exist.
            // FileAccess.Read: Prevents accidental modification.
            // FileShare.Read: Allows multiple clients to read the same file at the same time when necessary.
            Stream stream =
                new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

            // Return the open Stream.
            // The API Controller can later return this Stream as the HTTP file-download response.
            return Task.FromResult<Stream?>(stream);
        }

        // Deletes a physical file from local storage.
        // This method is primarily used for cleanup when the file
        // has already been written successfully but a later database
        // operation fails.
        public Task DeleteAsync(string relativePath)
        {
            // Resolve and validate the path before attempting deletion.
            var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relativePath));

            // Delete the file only when it currently exists.
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}