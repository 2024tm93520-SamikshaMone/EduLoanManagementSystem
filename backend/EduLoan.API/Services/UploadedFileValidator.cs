using Microsoft.AspNetCore.Http;

namespace EduLoan.API.Services;

/// <summary>Validates file content independently of the client-supplied name and MIME type.</summary>
public static class UploadedFileValidator
{
    public static bool HasAllowedSignature(IFormFile file)
    {
        if (file is null || file.Length < 4) return false;
        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (read >= 5 && header[..5].SequenceEqual("%PDF-"u8)) return true;
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return read == 8 && header.SequenceEqual(png);
    }
}
