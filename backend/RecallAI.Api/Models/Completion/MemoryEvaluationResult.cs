using System.Text.Json.Serialization;

namespace RecallAI.Api.Models.Completion;

public class MemoryEvaluationResult
{
    [JsonPropertyName("memories")]
    public List<MemoryEvaluationItem> Memories { get; set; } = new();
}

public class MemoryEvaluationItem
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("source_text")]
    public string? SourceText { get; set; }

    [JsonPropertyName("should_save")]
    public bool? ShouldSave { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}
