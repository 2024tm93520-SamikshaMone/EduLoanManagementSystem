namespace EduLoan.Application.Features.Auth;

public record RequestOtpRequestDto(string Email);
public record VerifyOtpRequestDto(string Email, string Otp);
public record VerifyOtpResponseDto(string ResetToken);
public record ResetPasswordRequestDto(string Email, string ResetToken, string NewPassword, string ConfirmPassword);
public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
public record MessageResponseDto(string Message);
