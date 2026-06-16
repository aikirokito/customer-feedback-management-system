using CFMS.Application.Common.Models;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Base controller providing shared utilities for all API controllers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    /// <summary>The authenticated user's Id extracted from the JWT claim.</summary>
    protected Guid CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(AppClaimTypes.UserId);
            return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    protected string CurrentUserIp =>
        HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";

    protected IActionResult OkResponse<T>(T data, string? message = null)
        => Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult CreatedResponse<T>(string routeName, object routeValues, T data)
        => CreatedAtRoute(routeName, routeValues, ApiResponse<T>.Ok(data));

    protected IActionResult FailResponse(string message, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse.Fail(message));

    protected IActionResult NoContentResponse()
        => NoContent();
}
