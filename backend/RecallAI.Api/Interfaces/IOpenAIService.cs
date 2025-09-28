namespace RecallAI.Api.Interfaces;

public interface IOpenAIService
{
    Task<string> EvaluateMemoryAsync(string memoryContent, string evaluationCriteria);
    Task<string> GenerateHyDEAsync(string query);
    Task<string> GenerateFinalResultAsync(string prompt, string context);
    IAsyncEnumerable<string> GenerateStreamingCompletionAsync(string prompt, string? model = null, double? temperature = null, int? maxTokens = null, string? systemPrompt = null, CancellationToken cancellationToken = default);
}
