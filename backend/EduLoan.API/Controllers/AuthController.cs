using EduLoan.Application.Common;
using EduLoan.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login successful."));
    }

    [HttpPost("forgot-password/request-otp")]
    public async Task<ActionResult<ApiResponse<MessageResponseDto>>> RequestOtp([FromBody] RequestOtpBody request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestPasswordResetOtpCommand(request.Email), ct);
        return Ok(ApiResponse<MessageResponseDto>.SuccessResponse(result, result.Message));
    }

    [HttpPost("forgot-password/verify-otp")]
    public async Task<ActionResult<ApiResponse<VerifyOtpResponseDto>>> VerifyOtp([FromBody] VerifyOtpBody request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyPasswordResetOtpCommand(request.Email, request.Otp), ct);
        return Ok(ApiResponse<VerifyOtpResponseDto>.SuccessResponse(result, "OTP verified."));
    }

    [HttpPost("forgot-password/reset")]
    public async Task<ActionResult<ApiResponse<MessageResponseDto>>> ResetPassword([FromBody] ResetPasswordBody request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ResetPasswordCommand(request.Email, request.ResetToken, request.NewPassword, request.ConfirmPassword), ct);
        return Ok(ApiResponse<MessageResponseDto>.SuccessResponse(result, result.Message));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<MessageResponseDto>>> ChangePassword([FromBody] ChangePasswordBody request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword, request.ConfirmPassword), ct);
        return Ok(ApiResponse<MessageResponseDto>.SuccessResponse(result, result.Message));
    }

    public record RequestOtpBody(string Email);
    public record VerifyOtpBody(string Email, string Otp);
    public record ResetPasswordBody(string Email, string ResetToken, string NewPassword, string ConfirmPassword);
    public record ChangePasswordBody(string CurrentPassword, string NewPassword, string ConfirmPassword);
}
