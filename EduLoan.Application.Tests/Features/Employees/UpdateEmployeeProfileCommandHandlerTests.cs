using EduLoan.Application.Common;
using EduLoan.Application.Features.Employees;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using Moq;
using Xunit;

namespace EduLoan.Application.Tests.Features.Employees;

public class UpdateEmployeeProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly UpdateEmployeeProfileCommandHandler _handler;

    public UpdateEmployeeProfileCommandHandlerTests()
    {
        _handler = new UpdateEmployeeProfileCommandHandler(
            _userRepository.Object, _unitOfWork.Object, _dateTimeProvider.Object);
    }

    private static User BuildUser() => new()
    {
        Id = Guid.NewGuid(),
        EmployeeCode = "EMP1001",
        FullName = "Samiksha Mone",
        Email = "samiksha@acc.com",
        Role = UserRole.Employee,
        DateOfJoining = new DateOnly(2022, 1, 10),
        PhoneNumber = "9876543210",
    };

    [Fact]
    public async Task Handle_ValidPhoneNumber_UpdatesAndPersists()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _dateTimeProvider.Setup(d => d.Today).Returns(new DateOnly(2026, 9, 5));

        var result = await _handler.Handle(
            new UpdateEmployeeProfileCommand(user.Id, "9998887770"), CancellationToken.None);

        Assert.Equal("9998887770", result.PhoneNumber);
        Assert.Equal("9998887770", user.PhoneNumber); // entity mutated in place
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownUserId_ThrowsNotFoundException_AndDoesNotSave()
    {
        var missingId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new UpdateEmployeeProfileCommand(missingId, "9998887770"), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class UpdateEmployeeProfileCommandValidatorTests
{
    private readonly UpdateEmployeeProfileCommandValidator _validator = new();

    [Theory]
    [InlineData("", false)]              // empty
    [InlineData("12345", false)]         // too short
    [InlineData("123456789012", false)]  // too long
    [InlineData("98765abcde", false)]    // non-numeric
    [InlineData("9876543210", true)]     // valid 10-digit number
    public void Validate_VariousPhoneNumbers_ReturnsExpectedValidity(string phoneNumber, bool expectedValid)
    {
        var result = _validator.Validate(new UpdateEmployeeProfileCommand(Guid.NewGuid(), phoneNumber));
        Assert.Equal(expectedValid, result.IsValid);
    }
}
