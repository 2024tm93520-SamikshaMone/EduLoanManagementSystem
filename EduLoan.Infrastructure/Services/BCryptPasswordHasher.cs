using EduLoan.Application.Interfaces;

namespace EduLoan.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
