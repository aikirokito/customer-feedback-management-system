using CFMS.Application.Common.Exceptions;
using CFMS.Application.Common.Models;
using System.Net;
using System.Text.Json;

namespace CFMS.Api.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Maps application exceptions to appropriate HTTP status codes.
/// All unhandled exceptions return a consistent ApiResponse envelope.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException e       => (HttpStatusCode.NotFound, e.Message, (IEnumerable<string>?)null),
            ValidationException e     => (HttpStatusCode.BadRequest, "Validation failed.", e.Errors),
            UnauthorizedException e   => (HttpStatusCode.Unauthorized, e.Message, null),
            ForbiddenException e      => (HttpStatusCode.Forbidden, e.Message, null),
            ConflictException e       => (HttpStatusCode.Conflict, e.Message, null),
            BusinessRuleException e   => (HttpStatusCode.UnprocessableEntity, e.Message, null),
            NotImplementedException   => (HttpStatusCode.NotImplemented, "This feature is not yet implemented.", null),
            _                         => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}
