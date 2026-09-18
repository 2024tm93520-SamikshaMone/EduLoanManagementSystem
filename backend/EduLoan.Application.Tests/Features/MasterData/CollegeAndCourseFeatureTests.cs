using EduLoan.Application.Common;
using EduLoan.Application.Features.MasterData.Colleges;
using EduLoan.Application.Features.MasterData.Courses;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.MasterData;

public class CollegeFeatureTests
{
    [Fact]
    public async Task CreateCollege_Valid_PersistsAndReturnsDto()
    {
        using var db = TestDbContextFactory.Create();

        var result = await new CreateCollegeCommandHandler(db)
            .Handle(new CreateCollegeCommand("BITS Pilani", "Pilani"), default);

        Assert.Equal("BITS Pilani", result.Name);
        Assert.Equal("Pilani", result.City);
        Assert.Single(db.Colleges);
    }

    [Fact]
    public async Task CreateCollege_DuplicateName_ThrowsDuplicateNameException()
    {
        using var db = TestDbContextFactory.Create();
        db.Colleges.Add(new College { Name = "BITS Pilani", City = "Pilani" });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicateNameException>(() =>
            new CreateCollegeCommandHandler(db).Handle(new CreateCollegeCommand("BITS Pilani", "Pilani"), default));
    }

    [Fact]
    public async Task UpdateCollege_UnknownId_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateCollegeCommandHandler(db).Handle(new UpdateCollegeCommand(999, "X", "Y", true), default));
    }

    [Fact]
    public async Task DeleteCollege_Existing_RemovesIt()
    {
        using var db = TestDbContextFactory.Create();
        var college = new College { Name = "IIT Bombay", City = "Mumbai" };
        db.Colleges.Add(college);
        await db.SaveChangesAsync();

        await new DeleteCollegeCommandHandler(db).Handle(new DeleteCollegeCommand(college.Id), default);

        Assert.Empty(db.Colleges);
    }

    [Theory]
    [InlineData("", "Pilani", false)]
    [InlineData("BITS", "", false)]
    [InlineData("BITS", "Pilani", true)]
    public void CreateCollegeCommandValidator_RequiresNameAndCity(string name, string city, bool expectedValid)
    {
        var result = new CreateCollegeCommandValidator().Validate(new CreateCollegeCommand(name, city));
        Assert.Equal(expectedValid, result.IsValid);
    }
}

public class CourseFeatureTests
{
    [Fact]
    public async Task CreateCourse_Valid_PersistsAndReturnsDto()
    {
        using var db = TestDbContextFactory.Create();

        var result = await new CreateCourseCommandHandler(db)
            .Handle(new CreateCourseCommand("M.Tech Software Engineering", "PG", 24), default);

        Assert.Equal("PG", result.Level);
        Assert.Equal(24, result.StandardDurationMonths);
        Assert.Single(db.Courses);
    }

    [Fact]
    public async Task CreateCourse_DuplicateName_ThrowsDuplicateNameException()
    {
        using var db = TestDbContextFactory.Create();
        db.Courses.Add(new Course { Name = "MBA", Level = "PG", StandardDurationMonths = 24 });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DuplicateNameException>(() =>
            new CreateCourseCommandHandler(db).Handle(new CreateCourseCommand("MBA", "PG", 24), default));
    }

    [Fact]
    public async Task DeleteCourse_Existing_RemovesIt()
    {
        using var db = TestDbContextFactory.Create();
        var course = new Course { Name = "B.Tech CSE", Level = "UG", StandardDurationMonths = 48 };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        await new DeleteCourseCommandHandler(db).Handle(new DeleteCourseCommand(course.Id), default);

        Assert.Empty(db.Courses);
    }

    [Theory]
    [InlineData("MBA", "PG", 24, true)]
    [InlineData("MBA", "Postgraduate", 24, false)]  // invalid level not in allowed list
    [InlineData("MBA", "PG", 0, false)]              // duration must be > 0
    [InlineData("MBA", "PG", 200, false)]            // duration too long (> 120 months)
    [InlineData("", "PG", 24, false)]                // name required
    public void CreateCourseCommandValidator_VariousInputs(string name, string level, int duration, bool expectedValid)
    {
        var result = new CreateCourseCommandValidator().Validate(new CreateCourseCommand(name, level, duration));
        Assert.Equal(expectedValid, result.IsValid);
    }
}
