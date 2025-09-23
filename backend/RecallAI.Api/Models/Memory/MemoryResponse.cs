namespace RecallAI.Api.Models.Dto;

public class MemoryResponse
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text";
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}