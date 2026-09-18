using System.Net;
using System.Text.Json;
using EduLoan.Application.Common;
using FluentValidation;

namespace EduLoan.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    // Must match the casing the rest of the API uses (MVC's default is camelCase),
    // otherwise error responses come out PascalCase while success responses are camelCase —
    // the frontend only ever checks lowercase keys, so mismatched casing silently loses
    // the real error message and falls back to a generic one.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.ContentType = "application/json";

            var (statusCode, response) = ex switch
            {
                AuthenticationFailedException authEx => (
                    HttpStatusCode.Unauthorized,
                    ApiResponse<object>.FailureResponse(authEx.Message)),

                NotFoundException notFoundEx => (
                    HttpStatusCode.NotFound,
                    ApiResponse<object>.FailureResponse(notFoundEx.Message)),

                DuplicateNameException dupEx => (
                    HttpStatusCode.Conflict,
                    ApiResponse<object>.FailureResponse(dupEx.Message)),

                InUseException inUseEx => (
                    HttpStatusCode.Conflict,
                    ApiResponse<object>.FailureResponse(inUseEx.Message)),

                ForbiddenException forbiddenEx => (
                    HttpStatusCode.Forbidden,
                    ApiResponse<object>.FailureResponse(forbiddenEx.Message)),

                InvalidFileException invalidFileEx => (
                    HttpStatusCode.BadRequest,
                    ApiResponse<object>.FailureResponse(invalidFileEx.Message)),

                ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    ApiResponse<object>.FailureResponse(
                        "Validation failed.",
                        validationEx.Errors.Select(e => e.ErrorMessage).ToList())),

                _ => (
                    HttpStatusCode.InternalServerError,
                    ApiResponse<object>.FailureResponse("An unexpected error occurred.")),
            };

            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
