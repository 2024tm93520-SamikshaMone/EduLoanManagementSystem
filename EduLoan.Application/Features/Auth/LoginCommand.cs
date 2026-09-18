using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
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

        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var userDto = new AuthUserDto(user.Id, user.FullName, user.Email, user.Role);
        return new LoginResponseDto(accessToken, refreshToken, userDto);
    }
}
