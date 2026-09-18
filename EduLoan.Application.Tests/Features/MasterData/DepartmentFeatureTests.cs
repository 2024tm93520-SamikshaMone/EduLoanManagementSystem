using EduLoan.Application.Common;
using EduLoan.Application.Features.MasterData.Departments;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.MasterData;

public class DepartmentFeatureTests
{
    [Fact]
    public async Task GetDepartments_ReturnsAllDepartmentsOrderedByName()
    {
        using var db = TestDbContextFactory.Create();
        db.Departments.AddRange(
            new Department { Name = "Finance" },
            new Department { Name = "Administration" });
        await db.SaveChangesAsync();

        var result = await new GetDepartmentsQueryHandler(db).Handle(new GetDepartmentsQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("Administration", result[0].Name); // alphabetical
    }

    [Fact]
    public async Task CreateDepartment_ValidName_PersistsAndReturnsDto()
    {
        using var db = TestDbContextFactory.Create();

        var result = await new CreateDepartmentCommandHandler(db)
            .Handle(new CreateDepartmentCommand("Engineering"), default);

        Assert.Equal("Engineering", result.Name);
        Assert.True(result.IsActive);
        Assert.Single(db.Departments);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_ThrowsDuplicateNameException()
    {
        using var db = TestDbContextFactory.Create();
        db.Departments.Add(new Department { Name = "Engineering" });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicateNameException>(
            () => new CreateDepartmentCommandHandler(db).Handle(new CreateDepartmentCommand("Engineering"), default));
    }

    [Fact]
    public async Task UpdateDepartment_UnknownId_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateDepartmentCommandHandler(db).Handle(new UpdateDepartmentCommand(999, "X", true), default));
    }

    [Fact]
    public async Task UpdateDepartment_RenamedToAnotherExistingDepartmentsName_ThrowsDuplicateNameException()
    {
        using var db = TestDbContextFactory.Create();
        db.Departments.AddRange(
            new Department { Id = 1, Name = "Engineering" },
            new Department { Id = 2, Name = "Finance" });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicateNameException>(() =>
            new UpdateDepartmentCommandHandler(db).Handle(new UpdateDepartmentCommand(2, "Engineering", true), default));
    }

    [Fact]
    public async Task DeleteDepartment_NotReferencedByAnyUser_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var department = new Department { Name = "Administration" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        await new DeleteDepartmentCommandHandler(db).Handle(new DeleteDepartmentCommand(department.Id), default);

        Assert.Empty(db.Departments);
    }

    [Fact]
    public async Task DeleteDepartment_ReferencedByAnEmployee_ThrowsInUseException()
    {
        using var db = TestDbContextFactory.Create();
        var department = new Department { Name = "Engineering" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP9001",
            FullName = "Test Employee",
            Email = "test@acc.com",
            PasswordHash = "x",
            Role = UserRole.Employee,
            DepartmentId = department.Id,
            DateOfJoining = new DateOnly(2024, 1, 1),
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InUseException>(() =>
            new DeleteDepartmentCommandHandler(db).Handle(new DeleteDepartmentCommand(department.Id), default));

        Assert.Single(db.Departments); // not deleted
    }
}

public class DepartmentValidatorTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("Engineering", true)]
    public void CreateDepartmentCommandValidator_NameRequired(string name, bool expectedValid)
    {
        var result = new CreateDepartmentCommandValidator().Validate(new CreateDepartmentCommand(name));
        Assert.Equal(expectedValid, result.IsValid);
    }
}
