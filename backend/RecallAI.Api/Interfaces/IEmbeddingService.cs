namespace RecallAI.Api.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding for a single text string
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <returns>Embedding vector as float array</returns>
    Task<float[]> GenerateEmbeddingAsync(string text);
    
    /// <summary>
    /// Generate embeddings for multiple text strings
    /// </summary>
    /// <param name="texts">List of texts to generate embeddings for</param>
    /// <returns>List of embedding vectors as float arrays</returns>
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);
}