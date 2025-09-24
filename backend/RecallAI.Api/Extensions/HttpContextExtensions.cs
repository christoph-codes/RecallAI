using System.Security.Claims;

namespace RecallAI.Api.Extensions;

public static class HttpContextExtensions
{
    public static string? GetCurrentUserId(this HttpContext httpContext)
    {
        return httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User?.FindFirst("sub")?.Value;
    }

    public static string? GetCurrentUserEmail(this HttpContext httpContext)
    {
        return httpContext.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? httpContext.User?.FindFirst("email")?.Value;
    }

    public static bool IsAuthenticated(this HttpContext httpContext)
    {
        return httpContext.User?.Identity?.IsAuthenticated == true;
    }

    public static string GetCurrentUserIdOrThrow(this HttpContext httpContext)
    {
        var userId = httpContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    public static Guid GetUserId(this HttpContext httpContext)
    {
        var userIdString = httpContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Guid.Empty;
        }
        return userId;
    }
}