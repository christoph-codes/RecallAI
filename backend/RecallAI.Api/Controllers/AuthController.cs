using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecallAI.Api.Extensions;
using RecallAI.Api.Models.Auth;

namespace RecallAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get current user profile information from JWT token
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var userId = HttpContext.GetCurrentUserId();
            var userEmail = HttpContext.GetCurrentUserEmail();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ErrorResponse
                {
                    Error = "Invalid token",
                    Message = "User ID not found in token",
                    StatusCode = 401
                });
            }

            // Get user info from token claims
            var userInfo = new UserInfo
            {
                Id = userId,
                Email = userEmail ?? "Unknown",
                FullName = HttpContext.User?.FindFirst("full_name")?.Value,
                CreatedAt = DateTime.TryParse(HttpContext.User?.FindFirst("created_at")?.Value, out var createdAt) 
                    ? createdAt 
                    : DateTime.UtcNow
            };

            _logger.LogDebug("Retrieved user info for {UserId}", userId);

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user info");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal server error",
                Message = "An error occurred while retrieving user information",
                StatusCode = 500
            });
        }
    }

    /// <summary>
    /// Validate current token and return user info
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        try
        {
            var userId = HttpContext.GetCurrentUserId();
            var userEmail = HttpContext.GetCurrentUserEmail();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ErrorResponse
                {
                    Error = "Invalid token",
                    Message = "User ID not found in token",
                    StatusCode = 401
                });
            }

            var response = new
            {
                Valid = true,
                UserId = userId,
                Email = userEmail,
                ExpiresAt = HttpContext.User?.FindFirst("exp")?.Value,
                IssuedAt = HttpContext.User?.FindFirst("iat")?.Value
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal server error",
                Message = "An error occurred while validating the token",
                StatusCode = 500
            });
        }
    }
}