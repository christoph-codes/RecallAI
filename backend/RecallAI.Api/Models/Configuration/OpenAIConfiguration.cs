namespace RecallAI.Api.Models.Configuration;

public class OpenAIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public OpenAIModelsConfiguration Models { get; set; } = new();
}

public class OpenAIModelsConfiguration
{
    public string Embeddings { get; set; } = "text-embedding-3-small";
    public string MemoryEvaluation { get; set; } = "gpt-5-nano";
    public string HyDE { get; set; } = "gpt-5-nano";
    public string FinalResult { get; set; } = "gpt-5";
}