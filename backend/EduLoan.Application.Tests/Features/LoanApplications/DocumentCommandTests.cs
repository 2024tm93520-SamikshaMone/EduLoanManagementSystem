using EduLoan.Application.Common;
using EduLoan.Application.Features.LoanApplications;
using EduLoan.Application.Interfaces;
using EduLoan.Application.Tests.Features.MasterData;
using EduLoan.Domain.Entities;
using Moq;
using Xunit;

namespace EduLoan.Application.Tests.Features.LoanApplications;

public class DocumentCommandTests
{
    private static LoanApplication BuildDraftApplication(Guid owner) => new()
    {
        Id = Guid.NewGuid(), ApplicationNumber = $"EDL-2026-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
        EmployeeId = owner, CollegeId = 1, CourseId = 1, Specialization = "AI", CourseDurationMonths = 24,
        TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
        EducationPurpose = "x", Status = ApplicationStatus.Draft,
    };

    [Fact]
    public async Task AddDocument_ToDraftApplicationByOwner_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        var application = BuildDraftApplication(owner);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var fileStorage = new Mock<IFileStorageService>();
        var result = await new AddDocumentCommandHandler(db, fileStorage.Object).Handle(
            new AddDocumentCommand(application.Id, owner, "Fee Receipt", "receipt.pdf", "stored-abc123.pdf", "application/pdf", 12345),
            default);

        Assert.Equal("Fee Receipt", result.DocumentType);
        Assert.Equal("receipt.pdf", result.FileName);
        fileStorage.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddDocument_ToSubmittedApplication_ThrowsInUseException_AndCleansUpTheOrphanedFile()
    {
        using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        var application = BuildDraftApplication(owner);
        application.Status = ApplicationStatus.Submitted;
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var fileStorage = new Mock<IFileStorageService>();

        await Assert.ThrowsAsync<InUseException>(() => new AddDocumentCommandHandler(db, fileStorage.Object)
            .Handle(new AddDocumentCommand(application.Id, owner, "Fee Receipt", "receipt.pdf", "stored-xyz.pdf", "application/pdf", 100), default));

        // The controller already saved the file to disk before this command ran —
        // since the command was rejected, that orphaned file must be cleaned up.
        fileStorage.Verify(f => f.DeleteAsync("stored-xyz.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDocument_ByNonOwner_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        var application = BuildDraftApplication(owner);
        var document = new ApplicationDocument
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, DocumentType = "ID Proof",
            FileName = "id.pdf", StoredFileName = "stored-id.pdf",
        };
        db.LoanApplications.Add(application);
        db.ApplicationDocuments.Add(document);
        await db.SaveChangesAsync();

        var fileStorage = new Mock<IFileStorageService>();

        await Assert.ThrowsAsync<ForbiddenException>(() => new RemoveDocumentCommandHandler(db, fileStorage.Object)
            .Handle(new RemoveDocumentCommand(application.Id, document.Id, Guid.NewGuid()), default));

        fileStorage.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveDocument_ValidOwner_DeletesRowAndPhysicalFile()
    {
        using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        var application = BuildDraftApplication(owner);
        var document = new ApplicationDocument
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, DocumentType = "ID Proof",
            FileName = "id.pdf", StoredFileName = "stored-id.pdf",
        };
        db.LoanApplications.Add(application);
        db.ApplicationDocuments.Add(document);
        await db.SaveChangesAsync();

        var fileStorage = new Mock<IFileStorageService>();
        await new RemoveDocumentCommandHandler(db, fileStorage.Object)
            .Handle(new RemoveDocumentCommand(application.Id, document.Id, owner), default);

        Assert.Empty(db.ApplicationDocuments);
        fileStorage.Verify(f => f.DeleteAsync("stored-id.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDocumentForDownload_ByOwner_ReturnsStorageInfo()
    {
        using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        var application = BuildDraftApplication(owner);
        var document = new ApplicationDocument
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, DocumentType = "ID Proof",
            FileName = "id.pdf", StoredFileName = "stored-id.pdf", ContentType = "application/pdf",
        };
        db.LoanApplications.Add(application);
        db.ApplicationDocuments.Add(document);
        await db.SaveChangesAsync();

        var result = await new GetDocumentForDownloadQueryHandler(db)
            .Handle(new GetDocumentForDownloadQuery(application.Id, document.Id, owner), default);

        Assert.Equal("stored-id.pdf", result.StoredFileName);
        Assert.Equal("id.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public async Task GetDocumentForDownload_ByNonOwner_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.Create();
        var owner = Guid.NewGuid();
        var application = BuildDraftApplication(owner);
        var document = new ApplicationDocument
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, DocumentType = "ID Proof",
            FileName = "id.pdf", StoredFileName = "stored-id.pdf",
        };
        db.LoanApplications.Add(application);
        db.ApplicationDocuments.Add(document);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => new GetDocumentForDownloadQueryHandler(db)
            .Handle(new GetDocumentForDownloadQuery(application.Id, document.Id, Guid.NewGuid()), default));
    }
}
