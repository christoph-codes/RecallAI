namespace RecallAI.Api.Interfaces;

public interface IOpenAIService
{
    Task<string> EvaluateMemoryAsync(string memoryContent, string evaluationCriteria);
    Task<string> GenerateHyDEAsync(string query);
    Task<string> GenerateFinalResultAsync(string prompt, string context);
}