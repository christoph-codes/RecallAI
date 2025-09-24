using Microsoft.EntityFrameworkCore;
using RecallAI.Api.Data;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models;
using Pgvector;

namespace RecallAI.Api.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly MemoryDbContext _context;

    public MemoryRepository(MemoryDbContext context)
    {
        _context = context;
    }

    public async Task<Memory?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Memories
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
    }

    public async Task<List<Memory>> GetAllByUserAsync(Guid userId, int page, int pageSize)
    {
        return await _context.Memories
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await _context.Memories
            .CountAsync(m => m.UserId == userId);
    }

    public async Task<Memory> CreateAsync(Memory memory)
    {
        memory.Id = Guid.NewGuid();
        memory.CreatedAt = DateTimeOffset.UtcNow;
        memory.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Memories.Add(memory);
        await _context.SaveChangesAsync();
        return memory;
    }

    public async Task<Memory> UpdateAsync(Memory memory)
    {
        memory.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Memories.Update(memory);
        await _context.SaveChangesAsync();
        return memory;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var memory = await _context.Memories
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

        if (memory == null)
            return false;

        _context.Memories.Remove(memory);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, Guid userId)
    {
        return await _context.Memories
            .AnyAsync(m => m.Id == id && m.UserId == userId);
    }

    public async Task<List<(Memory memory, double similarity)>> SearchSimilarAsync(Guid userId, float[] queryEmbedding, int limit, double threshold)
    {
        var queryVector = new Vector(queryEmbedding);
        
        // Use raw SQL for vector similarity search with pgvector
        var sql = @"
            SELECT m.""Id"", m.""UserId"", m.""Title"", m.""Content"", m.""ContentType"",
                   m.""Metadata"", m.""CreatedAt"", m.""UpdatedAt"",
                   (1 - (me.""Embedding"" <=> @queryVector)) as similarity
            FROM ""Memories"" m
            INNER JOIN ""MemoryEmbeddings"" me ON m.""Id"" = me.""MemoryId""
            WHERE m.""UserId"" = @userId
                AND (1 - (me.""Embedding"" <=> @queryVector)) >= @threshold
            ORDER BY me.""Embedding"" <=> @queryVector
            LIMIT @limit";

        var results = new List<(Memory memory, double similarity)>();
        
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        
        var userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);
        
        var queryVectorParam = command.CreateParameter();
        queryVectorParam.ParameterName = "@queryVector";
        queryVectorParam.Value = queryVector;
        command.Parameters.Add(queryVectorParam);
        
        var thresholdParam = command.CreateParameter();
        thresholdParam.ParameterName = "@threshold";
        thresholdParam.Value = threshold;
        command.Parameters.Add(thresholdParam);
        
        var limitParam = command.CreateParameter();
        limitParam.ParameterName = "@limit";
        limitParam.Value = limit;
        command.Parameters.Add(limitParam);

        await _context.Database.OpenConnectionAsync();
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var memory = new Memory
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? null : reader.GetString(reader.GetOrdinal("Title")),
                Content = reader.GetString(reader.GetOrdinal("Content")),
                ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                Metadata = reader.IsDBNull(reader.GetOrdinal("Metadata")) ? null :
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(reader.GetOrdinal("Metadata"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
            
            var similarity = reader.GetDouble(reader.GetOrdinal("similarity"));
            results.Add((memory, similarity));
        }
        
        return results;
    }

    public async Task<bool> HasEmbeddingAsync(Guid memoryId)
    {
        return await _context.MemoryEmbeddings
            .AnyAsync(me => me.MemoryId == memoryId && me.Embedding != null);
    }
}