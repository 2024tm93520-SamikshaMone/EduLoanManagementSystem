using EduLoan.Domain.Entities;

namespace EduLoan.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    bool Verify(string password, string passwordHash);
}

public interface ITokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

/// Wraps EF Core's SaveChangesAsync so Application-layer handlers never
/// depend on DbContext directly (keeps CQRS handlers unit-testable without a real DB).
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// Exposes the currently authenticated user's id (from JWT claims), so
/// Application-layer handlers can answer "get MY profile" without touching HttpContext directly.
public interface ICurrentUserService
{
    Guid? UserId { get; }
}
