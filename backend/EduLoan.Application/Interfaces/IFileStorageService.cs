namespace EduLoan.Application.Interfaces;

public interface IFileStorageService
{
    /// Saves the stream to storage and returns a unique stored file name
    /// (never the original name — avoids collisions and path-traversal issues).
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);

    Task DeleteAsync(string storedFileName, CancellationToken ct = default);

    Stream OpenRead(string storedFileName);
}
