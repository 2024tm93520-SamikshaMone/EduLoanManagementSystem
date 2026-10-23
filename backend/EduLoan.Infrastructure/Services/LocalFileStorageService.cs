
using EduLoan.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EduLoan.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration config)
    {
        var configuredPath =
            config["FileStorage:RootPath"] ?? "UploadedFiles";

        // Resolve relative paths against the current API working directory.
        _rootPath = Path.GetFullPath(configuredPath);

        Directory.CreateDirectory(_rootPath);

        Console.WriteLine($"EduLoan file storage: {_rootPath}");
    }

    public async Task<string> SaveAsync(
        Stream content,
        string originalFileName,
        CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = GetSafePath(storedFileName);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await content.CopyToAsync(fileStream, ct);

        return storedFileName;
    }

    public Task DeleteAsync(
        string storedFileName,
        CancellationToken ct = default)
    {
        var fullPath = GetSafePath(storedFileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Stream OpenRead(string storedFileName)
    {
        var fullPath = GetSafePath(storedFileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "The stored document could not be found.",
                fullPath);
        }

        return new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }

    private string GetSafePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            storedFileName != Path.GetFileName(storedFileName))
        {
            throw new ArgumentException(
                "Invalid stored filename.",
                nameof(storedFileName));
        }

        return Path.Combine(_rootPath, storedFileName);
    }
}
