using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RecallAI.Api.Models.Auth;

namespace RecallAI.Api.Middleware;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtValidationMiddleware> _logger;
    private readonly string _jwtSecret;

    public JwtValidationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
        
        _jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET") 
            ?? _configuration["Supabase:JwtSecret"] 
            ?? throw new InvalidOperationException("JWT secret is not configured");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var token = ExtractTokenFromHeader(context.Request);
        
        if (string.IsNullOrEmpty(token))
        {
            // Let the authentication middleware handle missing tokens
            await _next(context);
            return;
        }

        try
        {
            var principal = ValidateToken(token);
            if (principal != null)
            {
                context.User = principal;
                _logger.LogDebug("JWT token validated successfully for user: {UserId}", 
                    principal.FindFirst("sub")?.Value ?? "Unknown");
            }
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token expired");
            await WriteErrorResponse(context, 401, "Token expired", "The provided token has expired");
            return;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("JWT token has invalid signature");
            await WriteErrorResponse(context, 401, "Invalid token", "The provided token has an invalid signature");
            return;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            await WriteErrorResponse(context, 401, "Invalid token", "The provided token is invalid");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT validation");
            await WriteErrorResponse(context, 500, "Authentication error", "An error occurred during authentication");
            return;
        }

        await _next(context);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Decode the base64 JWT secret
        byte[] key;
        try
        {
            key = Convert.FromBase64String(_jwtSecret);
        }
        catch
        {
            // If it's not base64, treat as plain text
            key = Encoding.UTF8.GetBytes(_jwtSecret);
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Supabase doesn't always set issuer consistently
            ValidateAudience = false, // Supabase doesn't always set audience consistently
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        
        // Ensure it's a JWT token
        if (validatedToken is not JwtSecurityToken jwtToken || 
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm");
        }

        return principal;
    }

    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/api/health",
            "/api/test/public",
            "/swagger",
            "/health"
        };

        return publicPaths.Any(publicPath =>
            path.StartsWithSegments(publicPath, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string error, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Error = error,
            Message = message,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(errorResponse, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}