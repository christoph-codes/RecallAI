using RecallAI.Api.Models;

namespace RecallAI.Api.Interfaces;

public interface IMemoryRepository
{
    Task<Memory?> GetByIdAsync(Guid id, Guid userId);
    Task<List<Memory>> GetAllByUserAsync(Guid userId, int page, int pageSize);
    Task<int> GetCountByUserAsync(Guid userId);
    Task<Memory> CreateAsync(Memory memory);
    Task<Memory> UpdateAsync(Memory memory);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<bool> ExistsAsync(Guid id, Guid userId);
    
    // Vector search methods
    Task<List<(Memory memory, double similarity)>> SearchSimilarAsync(Guid userId, float[] queryEmbedding, int limit, double threshold);
    Task<List<(Memory memory, double queryScore, double hydeScore)>> HybridSearchAsync(Guid userId, float[] queryEmbedding, float[] hydeEmbedding, int limit, double threshold);
    Task<bool> HasEmbeddingAsync(Guid memoryId);
}
