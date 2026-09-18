using EduLoan.Application.Common;
using EduLoan.Application.Features.Employees;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using Moq;
using Xunit;

namespace EduLoan.Application.Tests.Features.Employees;

public class GetEmployeeProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly GetEmployeeProfileQueryHandler _handler;

    public GetEmployeeProfileQueryHandlerTests()
    {
        _handler = new GetEmployeeProfileQueryHandler(_userRepository.Object, _dateTimeProvider.Object);
    }

    private static User BuildUser() => new()
    {
        Id = Guid.NewGuid(),
        EmployeeCode = "EMP1001",
        FullName = "Samiksha Mone",
        Email = "samiksha@acc.com",
        Role = UserRole.Employee,
        Department = new Department { Id = 1, Name = "Engineering" },
        Designation = "Software Engineer",
        DateOfJoining = new DateOnly(2022, 1, 10),
        MonthlySalary = 65000,
        PhoneNumber = "9876543210",
        IsActive = true,
    };

    [Fact]
    public async Task Handle_ExistingUser_ReturnsMappedProfileDto()
    {
        var user = BuildUser();
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _dateTimeProvider.Setup(d => d.Today).Returns(new DateOnly(2026, 9, 5));

        var result = await _handler.Handle(new GetEmployeeProfileQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.EmployeeCode, result.EmployeeCode);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal("Engineering", result.DepartmentName);
        Assert.Equal("Employee", result.Role);
    }

    [Fact]
    public async Task Handle_UnknownUserId_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new GetEmployeeProfileQuery(missingId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserWithNoDepartment_ReturnsNullDepartmentName()
    {
        var user = BuildUser();
        user.Department = null;
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _dateTimeProvider.Setup(d => d.Today).Returns(new DateOnly(2026, 9, 5));

        var result = await _handler.Handle(new GetEmployeeProfileQuery(user.Id), CancellationToken.None);

        Assert.Null(result.DepartmentName);
    }

    [Theory]
    [InlineData("2022-01-10", "2026-09-05", 55)]  // day-of-month (5) hasn't reached joining day (10) yet — partial month doesn't count
    [InlineData("2022-01-10", "2026-09-01", 55)]  // day-of-month not yet reached this month
    [InlineData("2026-01-01", "2026-01-01", 0)]   // joined today
    [InlineData("2027-01-01", "2026-09-05", 0)]   // future joining date guarded to zero, not negative
    public void CalculateTenureMonths_VariousDates_ReturnsExpectedMonths(string joining, string today, int expected)
    {
        var result = GetEmployeeProfileQueryHandler.CalculateTenureMonths(
            DateOnly.Parse(joining), DateOnly.Parse(today));

        Assert.Equal(expected, result);
    }
}
