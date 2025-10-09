using System.Diagnostics;
using System.Text;
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

        const string sql = @"
            SELECT id, user_id, title, content, content_type, metadata, created_at, updated_at
            FROM memories
            WHERE user_id = @userId
            ORDER BY created_at DESC
            OFFSET @offset
            LIMIT @limit;
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
        memory.Metadata ??= new Dictionary<string, object>();
        memory.CreatedAt = now;
        memory.UpdatedAt = now;

        var resolvedModelName = string.IsNullOrWhiteSpace(modelName) ? "text-embedding-3-small" : modelName;
        var hasProvidedId = memory.Id != Guid.Empty;
        var shouldCreateEmbedding = embedding is { Length: > 0 };

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (!hasProvidedId || attempt > 1)
            {
                memory.Id = Guid.NewGuid();
            }

            MemoryEmbedding? memoryEmbedding = null;
            if (shouldCreateEmbedding)
            {
                memory.MemoryEmbeddings.Clear();
                memoryEmbedding = new MemoryEmbedding
                {
                    Id = Guid.NewGuid(),
                    MemoryId = memory.Id,
                    Embedding = new Vector(embedding!),
                    ModelName = resolvedModelName,
                    CreatedAt = now
                };

                memory.MemoryEmbeddings.Add(memoryEmbedding);
            }

            _context.Memories.Add(memory);

            try
            {
                await _context.SaveChangesAsync();
                return memory;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException &&
                                               postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                                               string.Equals(postgresException.ConstraintName, "memories_pkey", StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    ex,
                    "Duplicate primary key '{MemoryId}' detected when creating memory for user {UserId}. Retrying with a new identifier ({Attempt}/{MaxAttempts}).",
                    memory.Id,
                    memory.UserId,
                    attempt,
                    maxAttempts);

                DetachEntity(memory);
                if (memoryEmbedding is not null)
                {
                    DetachEntity(memoryEmbedding);
                }

                hasProvidedId = false;
                if (shouldCreateEmbedding)
                {
                    memory.MemoryEmbeddings.Clear();
                }

                if (attempt == maxAttempts)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("Unable to create memory after retrying primary key conflicts.");
    }

    private void DetachEntity(object entity)
    {
        var entry = _context.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
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
        var normalizedThreshold = Math.Clamp(threshold, 0d, 1d);
        var maxDistance = 1d - normalizedThreshold;

        _logger.LogInformation(
            "Executing vector similarity search for user {UserId} with limit {Limit} and threshold {Threshold}. NormalizedThreshold={NormalizedThreshold}, MaxDistance={MaxDistance}. EmbeddingDimensions={Dimensions}",
            userId,
            limit,
            threshold,
            normalizedThreshold,
            maxDistance,
            queryEmbedding.Length);

        var stopwatch = Stopwatch.StartNew();

        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine(@"            SELECT m.id AS ""Id"", m.user_id AS ""UserId"", m.title AS ""Title"",");
        sqlBuilder.AppendLine(@"                   m.content AS ""Content"", m.content_type AS ""ContentType"",");
        sqlBuilder.AppendLine(@"                   m.metadata AS ""Metadata"", m.created_at AS ""CreatedAt"",");
        sqlBuilder.AppendLine(@"                   m.updated_at AS ""UpdatedAt"",");
        sqlBuilder.AppendLine(@"                   (1 - (me.embedding OPERATOR(vector.<=>) @queryVector)) AS similarity");
        sqlBuilder.AppendLine(@"            FROM memories m");
        sqlBuilder.AppendLine(@"            JOIN memory_embeddings me ON m.id = me.memory_id");
        sqlBuilder.AppendLine(@"            WHERE m.user_id = @userId");
        if (normalizedThreshold > 0d)
        {
            sqlBuilder.AppendLine(@"              AND me.embedding OPERATOR(vector.<=>) @queryVector <= @maxDistance");
        }
        sqlBuilder.AppendLine(@"            ORDER BY me.embedding OPERATOR(vector.<=>) @queryVector");
        sqlBuilder.AppendLine(@"            LIMIT @limit;");
        var sql = sqlBuilder.ToString();

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
        command.Parameters.AddWithValue("@limit", limit);
        if (normalizedThreshold > 0d)
        {
            command.Parameters.AddWithValue("@maxDistance", maxDistance);
        }

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
            if (normalizedThreshold > 0d && similarity < normalizedThreshold)
            {
                continue;
            }
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

