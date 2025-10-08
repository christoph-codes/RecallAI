using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RecallAI.Api.Data;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models;
using Pgvector;
using Npgsql;

namespace RecallAI.Api.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly MemoryDbContext _context;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<MemoryRepository> _logger;

    public MemoryRepository(MemoryDbContext context, NpgsqlDataSource dataSource, ILogger<MemoryRepository> logger)
    {
        _context = context;
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<Memory?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Memories
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
    }

    public async Task<List<Memory>> GetAllByUserAsync(Guid userId, int page, int pageSize)
    {
        _logger.LogInformation("GetAllByUserAsync started for user {UserId} (page {Page}, pageSize {PageSize})", userId, page, pageSize);

        var offset = Math.Max(0, (page - 1) * pageSize);
        var commandTimeout = _context.Database.GetCommandTimeout() ?? 120;

        const string sql = @"\r
            SELECT id, user_id, title, content, content_type, metadata, created_at, updated_at\r
            FROM memories\r
            WHERE user_id = @userId\r
            ORDER BY created_at DESC\r
            OFFSET @offset\r
            LIMIT @limit;\r
        ";

        var results = new List<Memory>(pageSize);

        _logger.LogInformation("Opening PostgreSQL connection for GetAllByUserAsync for user {UserId}", userId);
        await using var connection = await _dataSource.OpenConnectionAsync();
        _logger.LogInformation("Connection established for GetAllByUserAsync for user {UserId}", userId);

        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = commandTimeout
        };

        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@offset", offset);
        command.Parameters.AddWithValue("@limit", pageSize);

        _logger.LogInformation("Executing memory list query for user {UserId}", userId);
        await using var reader = await command.ExecuteReaderAsync();
        _logger.LogInformation("Memory list query completed for user {UserId}", userId);

        while (await reader.ReadAsync())
        {
            var memory = new Memory
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetGuid(reader.GetOrdinal("user_id")),
                Title = reader.IsDBNull(reader.GetOrdinal("title")) ? null : reader.GetString(reader.GetOrdinal("title")),
                Content = reader.GetString(reader.GetOrdinal("content")),
                ContentType = reader.IsDBNull(reader.GetOrdinal("content_type")) ? "text" : reader.GetString(reader.GetOrdinal("content_type")),
                Metadata = reader.IsDBNull(reader.GetOrdinal("metadata")) ? null :
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(reader.GetOrdinal("metadata"))),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
            };

            results.Add(memory);
        }

        _logger.LogInformation("GetAllByUserAsync completed for user {UserId} with {Count} results", userId, results.Count);
        return results;
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await _context.Memories
            .CountAsync(m => m.UserId == userId);
    }

    public async Task<Memory> CreateAsync(Memory memory, float[]? embedding = null, string? modelName = null)
    {
        if (memory is null)
        {
            throw new ArgumentNullException(nameof(memory));
        }

        var now = DateTimeOffset.UtcNow;
        memory.Id = memory.Id == Guid.Empty ? Guid.NewGuid() : memory.Id;
        memory.CreatedAt = now;
        memory.UpdatedAt = now;
        memory.Metadata ??= new Dictionary<string, object>();

        _context.Memories.Add(memory);

        if (embedding is { Length: > 0 })
        {
            var memoryEmbedding = new MemoryEmbedding
            {
                Id = Guid.NewGuid(),
                MemoryId = memory.Id,
                Embedding = new Vector(embedding),
                ModelName = string.IsNullOrWhiteSpace(modelName) ? "text-embedding-3-small" : modelName,
                CreatedAt = now
            };

            memory.MemoryEmbeddings.Add(memoryEmbedding);
            _context.MemoryEmbeddings.Add(memoryEmbedding);
        }

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

        _logger.LogInformation(
            "Executing vector similarity search for user {UserId} with limit {Limit} and threshold {Threshold}. EmbeddingDimensions={Dimensions}",
            userId,
            limit,
            threshold,
            queryEmbedding.Length);

        var stopwatch = Stopwatch.StartNew();

        var sql = @"
            SELECT m.id AS ""Id"", m.user_id AS ""UserId"", m.title AS ""Title"",
                   m.content AS ""Content"", m.content_type AS ""ContentType"",
                   m.metadata AS ""Metadata"", m.created_at AS ""CreatedAt"",
                   m.updated_at AS ""UpdatedAt"",
                   (1 - (me.embedding OPERATOR(vector.<=>) @queryVector)) AS similarity
            FROM memories m
            JOIN memory_embeddings me ON m.id = me.memory_id
            WHERE m.user_id = @userId
              AND (1 - (me.embedding OPERATOR(vector.<=>) @queryVector)) >= @threshold
            ORDER BY me.embedding OPERATOR(vector.<=>) @queryVector
            LIMIT @limit;
";

        var results = new List<(Memory memory, double similarity)>();

        var commandTimeout = _context.Database.GetCommandTimeout() ?? 120;

        _logger.LogInformation("Opening PostgreSQL connection for vector search for user {UserId}", userId);
        await using var connection = await _dataSource.OpenConnectionAsync();
        _logger.LogInformation("Connection established for vector search for user {UserId}", userId);
        await using var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = commandTimeout
        };

        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@queryVector", queryVector);
        command.Parameters.AddWithValue("@threshold", threshold);
        command.Parameters.AddWithValue("@limit", limit);

        _logger.LogInformation("Executing vector similarity query for user {UserId}", userId);
        await using var reader = await command.ExecuteReaderAsync();
        _logger.LogInformation("Vector similarity query completed for user {UserId}", userId);
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

        stopwatch.Stop();
        var topSimilarity = results.Count > 0 ? results.Max(r => r.similarity) : (double?)null;

        _logger.LogInformation(
            "Vector similarity search for user {UserId} completed in {ElapsedMilliseconds} ms with {ResultCount} results. TopSimilarity={TopSimilarity}",
            userId,
            stopwatch.Elapsed.TotalMilliseconds,
            results.Count,
            topSimilarity);

        return results;
    }

    public async Task<List<(Memory memory, double queryScore, double hydeScore)>> HybridSearchAsync(Guid userId, float[] queryEmbedding, float[] hydeEmbedding, int limit, double threshold)
    {
        _logger.LogInformation(
            "Executing hybrid search for user {UserId} with limit {Limit} and threshold {Threshold}. QueryDimensions={QueryDimensions}, HydeDimensions={HydeDimensions}",
            userId,
            limit,
            threshold,
            queryEmbedding.Length,
            hydeEmbedding.Length);

        var stopwatch = Stopwatch.StartNew();
        var combinedResults = new Dictionary<Guid, (Memory memory, double queryScore, double hydeScore)>();

        var queryResults = await SearchSimilarAsync(userId, queryEmbedding, limit, threshold);
        foreach (var (memory, similarity) in queryResults)
        {
            combinedResults[memory.Id] = (memory, similarity, 0d);
        }

        var hydeResults = await SearchSimilarAsync(userId, hydeEmbedding, limit, threshold);
        foreach (var (memory, similarity) in hydeResults)
        {
            if (combinedResults.TryGetValue(memory.Id, out var existing))
            {
                combinedResults[memory.Id] = (existing.memory, existing.queryScore, Math.Max(existing.hydeScore, similarity));
            }
            else
            {
                combinedResults[memory.Id] = (memory, 0d, similarity);
            }
        }

        var finalResults = combinedResults.Values.ToList();

        stopwatch.Stop();

        _logger.LogInformation(
            "Hybrid search for user {UserId} completed in {ElapsedMilliseconds} ms. QueryMatches={QueryMatches}, HydeMatches={HydeMatches}, CombinedResults={CombinedResults}",
            userId,
            stopwatch.Elapsed.TotalMilliseconds,
            queryResults.Count,
            hydeResults.Count,
            finalResults.Count);

        return finalResults;
    }

    public async Task<bool> HasEmbeddingAsync(Guid memoryId)
    {
        return await _context.MemoryEmbeddings
            .AnyAsync(me => me.MemoryId == memoryId && me.Embedding != null);
    }
}

