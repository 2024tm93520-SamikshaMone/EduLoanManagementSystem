using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;

namespace EduLoan.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same exception/message whether the user doesn't exist or the password is wrong —
        // avoids leaking which one it was (basic enumeration-protection).
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationFailedException();
        }

        if (!user.IsActive)
        {
            throw new AuthenticationFailedException("This account has been deactivated.");
        }

        // HR accounts are presented as Admin in the application. The persisted user
        // record is not changed; this is a compatibility mapping for existing accounts.
        var effectiveRole = user.Role == UserRole.HR ? UserRole.Admin : user.Role;
        var tokenUser = effectiveRole == user.Role
            ? user
            : new User { Id = user.Id, FullName = user.FullName, Email = user.Email, Role = effectiveRole };

        var accessToken = _tokenGenerator.GenerateAccessToken(tokenUser);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var userDto = new AuthUserDto(user.Id, user.FullName, user.Email, effectiveRole);
        return new LoginResponseDto(accessToken, refreshToken, userDto);
    }
}
