namespace RecallAI.Api.Models.Configuration;

public class HydeConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Model { get; set; } = "gpt-5-nano";
    public int MaxTokens { get; set; } = 100;
}
