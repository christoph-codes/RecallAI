namespace RecallAI.Api.Models.Auth;

public class ErrorResponse
{
    public required string Error { get; set; }
    public required string Message { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}