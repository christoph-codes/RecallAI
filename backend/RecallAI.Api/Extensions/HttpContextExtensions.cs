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
        if (string.IsNullOrEmpty(userIdString))
        {
            // Log for debugging
            var loggerFactory = httpContext.RequestServices.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("HttpContextExtensions");
            logger?.LogWarning("No user ID found in token claims. Available claims: {Claims}", 
                string.Join(", ", httpContext.User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? new string[0]));
            return Guid.Empty;
        }

        // Try to parse as GUID - Supabase user IDs should be valid UUIDs
        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }

        // If parsing fails, log the issue
        var loggerFactory2 = httpContext.RequestServices.GetService<ILoggerFactory>();
        var logger2 = loggerFactory2?.CreateLogger("HttpContextExtensions");
        logger2?.LogError("Failed to parse user ID '{UserId}' as GUID", userIdString);
        return Guid.Empty;
    }
}