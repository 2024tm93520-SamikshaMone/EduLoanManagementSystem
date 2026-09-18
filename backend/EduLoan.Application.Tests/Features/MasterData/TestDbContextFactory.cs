using EduLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Tests.Features.MasterData;

internal static class TestDbContextFactory
{
    // A fresh, uniquely-named InMemory database per call keeps tests fully isolated
    // from each other (no shared state, no test-ordering dependencies).
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
