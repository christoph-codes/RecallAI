namespace RecallAI.Api.Models.Configuration;

public class CompletionDefaults
{
    public bool EnableMemorySearch { get; set; } = true;
    public int MaxMemoryResults { get; set; } = 5;
    public double MemoryThreshold { get; set; } = 0.7;
    public double DefaultTemperature { get; set; } = 0.7;
    public int DefaultMaxTokens { get; set; } = 1000;
}