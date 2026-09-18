using EduLoan.Application.Common;
using EduLoan.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        // Validation failures and AuthenticationFailedException are caught by
        // the global exception middleware and mapped to ApiResponse<T> there.
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login successful."));
    }
}
