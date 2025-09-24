namespace RecallAI.Api.Models.Completion;

public class CompletionRequest
{
    public string Message { get; set; } = string.Empty;
    public CompletionConfiguration? Configuration { get; set; }
}

public class CompletionConfiguration
{
    public string? Model { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool EnableMemorySearch { get; set; } = true;
    public int MaxMemoryResults { get; set; } = 5;
    public double MemoryThreshold { get; set; } = 0.7;
}