namespace RecallAI.Api.Interfaces;

public interface IHydeService
{
    Task<string> GenerateHypotheticalAsync(string query);
    Task<float[]> GetHydeEmbeddingAsync(string query);
}
