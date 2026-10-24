using EduLoan.API.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EduLoan.Application.Tests.Security;

public sealed class UploadedFileValidatorTests
{
    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 })]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    public void HasAllowedSignature_ReturnsTrueForSupportedFileSignatures(byte[] bytes)
    {
        var file = CreateFile(bytes);
        Assert.True(UploadedFileValidator.HasAllowedSignature(file));
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03 })]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 })]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0x00, 0x00, 0x00 })]
    [InlineData(new byte[] { })]
    public void HasAllowedSignature_ReturnsFalseForUnsupportedOrShortContent(byte[] bytes)
    {
        var file = CreateFile(bytes);
        Assert.False(UploadedFileValidator.HasAllowedSignature(file));
    }

    private static IFormFile CreateFile(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "document.pdf");
    }
}
