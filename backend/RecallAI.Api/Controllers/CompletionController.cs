using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecallAI.Api.Extensions;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Completion;
using System.Text;

namespace RecallAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompletionController : ControllerBase
{
    private const string CacheControlHeader = "Cache-Control";
    private readonly ICompletionPipelineService _pipelineService;
    private readonly ILogger<CompletionController> _logger;

    public CompletionController(
        ICompletionPipelineService pipelineService,
        ILogger<CompletionController> logger)
    {
        _pipelineService = pipelineService;
        _logger = logger;
    }

    [HttpGet("debug")]
    public IActionResult DebugAuth()
    {
        var userId = HttpContext.GetUserId();
        var userIdString = HttpContext.GetCurrentUserId();
        var isAuthenticated = HttpContext.IsAuthenticated();
        
        var claims = HttpContext.User?.Claims?.Select(c => new { c.Type, c.Value }).ToList();
        
        return Ok(new
        {
            IsAuthenticated = isAuthenticated,
            UserIdString = userIdString,
            UserIdGuid = userId,
            Claims = claims,
            AuthHeader = Request.Headers["Authorization"].FirstOrDefault()
        });
    }

    [HttpPost]
    public async Task<IActionResult> GetCompletion([FromBody] CompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var userId = HttpContext.GetUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("User ID not found in HTTP context");
                return Unauthorized(new { error = "User ID not found" });
            }

            _logger.LogInformation("Processing completion request for user {UserId}", userId);

            // Set response headers for streaming
            Response.Headers[CacheControlHeader] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Headers"] = CacheControlHeader;

            // Start streaming response
            await foreach (var chunk in _pipelineService.ProcessCompletionAsync(request, userId, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bytes = Encoding.UTF8.GetBytes(chunk);
                await Response.Body.WriteAsync(bytes, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            return new EmptyResult();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Completion request was cancelled");
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process completion request");
            
            // Extract user-friendly error message
            string errorMessage = GetUserFriendlyErrorMessage(ex);
            
            // If response hasn't started, return error response
            if (!Response.HasStarted)
            {
                return StatusCode(500, new { error = errorMessage });
            }
            
            // If response has started, write error to stream
            var errorBytes = Encoding.UTF8.GetBytes($"\n\n{errorMessage}");
            await Response.Body.WriteAsync(errorBytes, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
            
            return new EmptyResult();
        }
    }

    [HttpPost("sse")]
    public async Task<IActionResult> GetCompletionSSE([FromBody] CompletionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var userId = HttpContext.GetUserId();
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("User ID not found in HTTP context");
                return Unauthorized(new { error = "User ID not found" });
            }

            _logger.LogInformation("Processing SSE completion request for user {UserId}", userId);

            // Set response headers for Server-Sent Events
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers[CacheControlHeader] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Headers"] = CacheControlHeader;

            // Send initial connection event
            await WriteSSEEvent("connected", "Connection established", cancellationToken);

            // Start streaming response
            await foreach (var chunk in _pipelineService.ProcessCompletionAsync(request, userId, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await WriteSSEEvent("data", chunk, cancellationToken);
            }

            // Send completion event
            await WriteSSEEvent("done", "Completion finished", cancellationToken);

            return new EmptyResult();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "SSE completion request was cancelled");
            await WriteSSEEvent("error", "Request cancelled", CancellationToken.None);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process SSE completion request");
            
            if (!Response.HasStarted)
            {
                return StatusCode(500, new { error = "Failed to process completion request" });
            }
            
            string errorMessage = GetUserFriendlyErrorMessage(ex);
            await WriteSSEEvent("error", errorMessage, CancellationToken.None);
            return new EmptyResult();
        }
    }

    private async Task WriteSSEEvent(string eventType, string data, CancellationToken cancellationToken)
    {
        var sseData = $"event: {eventType}\ndata: {data}\n\n";
        var bytes = Encoding.UTF8.GetBytes(sseData);
        await Response.Body.WriteAsync(bytes, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
    
    private static string GetUserFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message;
        
        // Check if it's an OpenAI API error with our enhanced error format
        if (ex is HttpRequestException && message.Contains("|ORIGINAL:"))
        {
            var parts = message.Split("|ORIGINAL:", 2);
            if (parts.Length > 0)
            {
                return parts[0]; // Return the user-friendly part
            }
        }
        
        // Handle common error patterns
        if (message.Contains("quota") || message.Contains("insufficient_quota"))
        {
            return "💳 **Quota exceeded.** Your OpenAI API quota has been reached. Please check your billing at platform.openai.com.";
        }
        
        if (message.Contains("rate limit") || message.Contains("Too Many Requests"))
        {
            return "⏳ **Rate limit exceeded.** Please wait a moment and try again.";
        }
        
        if (message.Contains("API key") || message.Contains("Unauthorized"))
        {
            return "🔑 **API key issue.** Please check your OpenAI API key configuration.";
        }
        
        if (message.Contains("timeout") || message.Contains("timed out"))
        {
            return "⏰ **Request timeout.** The request took too long. Please try again.";
        }
        
        if (message.Contains("OpenAI"))
        {
            return "🔧 **OpenAI service issue.** Please try again in a moment.";
        }
        
        // Generic fallback
        return "⚠️ **Something went wrong.** Please try again or contact support if the issue persists.";
    }
}