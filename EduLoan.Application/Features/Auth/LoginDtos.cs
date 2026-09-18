using EduLoan.Domain.Entities;

namespace EduLoan.Application.Features.Auth;

public record AuthUserDto(Guid Id, string FullName, string Email, UserRole Role);

public record LoginResponseDto(string AccessToken, string RefreshToken, AuthUserDto User);
