using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecallAI.Api.Data;
using RecallAI.Api.Models;

namespace RecallAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly MemoryDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(MemoryDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("database")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            // Test basic connection first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(500, new { Error = "Cannot connect to database" });
            }

            // Use more efficient queries with timeout handling
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;
            
            var result = new
            {
                DatabaseConnection = "Connected",
                Timestamp = DateTimeOffset.UtcNow,
                Tables = new
                {
                    ProfilesCount = await GetTableCountSafely("profiles", cancellationToken),
                    MemoriesCount = await GetTableCountSafely("memories", cancellationToken),
                    MemoryEmbeddingsCount = await GetTableCountSafely("memory_embeddings", cancellationToken),
                    CollectionsCount = await GetTableCountSafely("collections", cancellationToken),
                    MemoryCollectionsCount = await GetTableCountSafely("memory_collections", cancellationToken)
                },
                SampleQueries = await GetSampleQueries(cancellationToken)
            };

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Database test timed out");
            return StatusCode(408, new { Error = "Database operation timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed");
            return StatusCode(500, new {
                Error = "Database connection failed",
                Message = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    private async Task<object> GetTableCountSafely(string tableName, CancellationToken cancellationToken)
    {
        try
        {
            return tableName switch
            {
                "profiles" => await _context.Profiles.CountAsync(cancellationToken),
                "memories" => await _context.Memories.CountAsync(cancellationToken),
                "memory_embeddings" => await _context.MemoryEmbeddings.CountAsync(cancellationToken),
                "collections" => await _context.Collections.CountAsync(cancellationToken),
                "memory_collections" => await _context.MemoryCollections.CountAsync(cancellationToken),
                _ => "Unknown table"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count {TableName}", tableName);
            return $"Error: {ex.Message}";
        }
    }

    private async Task<object> GetSampleQueries(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test basic queries and relationships with proper ordering and optimized queries
            var profiles = await _context.Profiles
                .OrderBy(p => p.CreatedAt)
                .Take(5)
                .Select(p => new { p.Id, p.Email, p.FullName })
                .ToListAsync(cancellationToken);

            var memories = await _context.Memories
                .OrderBy(m => m.CreatedAt)
                .Take(5)
                .Select(m => new {
                    m.Id,
                    m.Title,
                    m.ContentType,
                    ProfileEmail = m.Profile != null ? m.Profile.Email : null
                })
                .ToListAsync(cancellationToken);

            var embeddings = await _context.MemoryEmbeddings
                .OrderBy(e => e.CreatedAt)
                .Take(5)
                .Select(e => new {
                    e.Id,
                    e.ModelName,
                    MemoryTitle = e.Memory != null ? e.Memory.Title : null,
                    HasEmbedding = e.Embedding != null
                })
                .ToListAsync(cancellationToken);

            var collections = await _context.Collections
                .OrderBy(c => c.CreatedAt)
                .Take(5)
                .Select(c => new {
                    c.Id,
                    c.Name,
                    c.Color,
                    ProfileEmail = c.Profile != null ? c.Profile.Email : null
                })
                .ToListAsync(cancellationToken);

            var memoryCollections = await _context.MemoryCollections
                .OrderBy(mc => mc.AddedAt)
                .Take(5)
                .Select(mc => new {
                    mc.MemoryId,
                    mc.CollectionId,
                    MemoryTitle = mc.Memory.Title,
                    CollectionName = mc.Collection.Name,
                    mc.AddedAt
                })
                .ToListAsync(cancellationToken);

            return new
            {
                Profiles = profiles,
                Memories = memories,
                MemoryEmbeddings = embeddings,
                Collections = collections,
                MemoryCollections = memoryCollections
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sample queries failed");
            return new { Error = "Sample queries failed", Message = ex.Message };
        }
    }

    [HttpGet("relationships")]
    public async Task<IActionResult> TestRelationships()
    {
        try
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

            // Test navigation properties with split queries to avoid cartesian explosion
            var profileWithMemories = await _context.Profiles
                .AsSplitQuery()
                .Include(p => p.Memories)
                .Include(p => p.Collections)
                .OrderBy(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var memoryWithEmbeddings = await _context.Memories
                .AsSplitQuery()
                .Include(m => m.MemoryEmbeddings)
                .Include(m => m.Collections)
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var result = new
            {
                ProfileTest = profileWithMemories != null ? new
                {
                    ProfileId = profileWithMemories.Id,
                    Email = profileWithMemories.Email,
                    MemoriesCount = profileWithMemories.Memories.Count,
                    CollectionsCount = profileWithMemories.Collections.Count
                } : null,
                MemoryTest = memoryWithEmbeddings != null ? new
                {
                    MemoryId = memoryWithEmbeddings.Id,
                    Title = memoryWithEmbeddings.Title,
                    EmbeddingsCount = memoryWithEmbeddings.MemoryEmbeddings.Count,
                    CollectionsCount = memoryWithEmbeddings.Collections.Count
                } : null
            };

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Relationship test timed out");
            return StatusCode(408, new { Error = "Relationship test timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Relationship test failed");
            return StatusCode(500, new {
                Error = "Relationship test failed",
                Message = ex.Message
            });
        }
    }

    [HttpGet("schema-validation")]
    public async Task<IActionResult> ValidateSchema()
    {
        try
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;
            
            // Test that we can query the database without errors
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                return BadRequest(new { Error = "Cannot connect to database" });
            }

            // Test each table exists and can be queried with timeout
            var tableTests = new Dictionary<string, bool>();
            
            try { await _context.Profiles.AnyAsync(cancellationToken); tableTests["profiles"] = true; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query profiles table");
                tableTests["profiles"] = false;
            }
            
            try { await _context.Memories.AnyAsync(cancellationToken); tableTests["memories"] = true; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query memories table");
                tableTests["memories"] = false;
            }
            
            try { await _context.MemoryEmbeddings.AnyAsync(cancellationToken); tableTests["memory_embeddings"] = true; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query memory_embeddings table");
                tableTests["memory_embeddings"] = false;
            }
            
            try { await _context.Collections.AnyAsync(cancellationToken); tableTests["collections"] = true; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query collections table");
                tableTests["collections"] = false;
            }
            
            try { await _context.MemoryCollections.AnyAsync(cancellationToken); tableTests["memory_collections"] = true; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query memory_collections table");
                tableTests["memory_collections"] = false;
            }

            return Ok(new
            {
                DatabaseConnected = canConnect,
                TableAccessibility = tableTests,
                AllTablesAccessible = tableTests.Values.All(v => v)
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Schema validation timed out");
            return StatusCode(408, new { Error = "Schema validation timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed");
            return StatusCode(500, new {
                Error = "Schema validation failed",
                Message = ex.Message
            });
        }
    }

    [HttpGet("connection-health")]
    public async Task<IActionResult> TestConnectionHealth()
    {
        try
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Test basic connection
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            var connectionTime = stopwatch.ElapsedMilliseconds;
            
            if (!canConnect)
            {
                return StatusCode(500, new {
                    Error = "Cannot connect to database",
                    ConnectionTime = connectionTime
                });
            }

            stopwatch.Restart();
            
            // Test table accessibility (not data existence)
            bool profilesTableExists = false;
            try
            {
                // This will succeed if the table exists, even if empty
                await _context.Profiles.AnyAsync(cancellationToken);
                profilesTableExists = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Profiles table is not accessible");
                profilesTableExists = false;
            }
            
            var queryTime = stopwatch.ElapsedMilliseconds;

            return Ok(new
            {
                DatabaseConnected = canConnect,
                ConnectionTime = connectionTime,
                QueryTime = queryTime,
                ProfilesTableExists = profilesTableExists,
                Status = queryTime < 1000 ? "Healthy" : queryTime < 5000 ? "Slow" : "Critical"
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new {
                Error = "Connection health check timed out",
                Status = "Timeout"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection health check failed");
            return StatusCode(500, new {
                Error = "Connection health check failed",
                Message = ex.Message,
                Status = "Failed"
            });
        }
    }
}