using EduLoan.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EduLoan.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration config)
    {
        // Relative to wherever the API process runs from (EduLoan.API/ when using `dotnet run`).
        // Fine for a dissertation-scale local deployment; a production system would use
        // a mounted volume or blob storage instead, behind this same interface.
        _rootPath = config["FileStorage:RootPath"] ?? "UploadedFiles";
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream, ct);

        return storedFileName;
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storedFileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Stream OpenRead(string storedFileName)
    {
        var fullPath = Path.Combine(_rootPath, storedFileName);
        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }
}
