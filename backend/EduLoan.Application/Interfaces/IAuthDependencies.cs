using EduLoan.Domain.Entities;

namespace EduLoan.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
}

public interface IEmailService
{
    Task SendAsync(string recipientEmail, string subject, string htmlBody, CancellationToken ct = default);
}
