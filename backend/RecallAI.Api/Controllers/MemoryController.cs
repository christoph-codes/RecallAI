using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RecallAI.Api.Models.Configuration;
using RecallAI.Api.Extensions;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models;
using RecallAI.Api.Models.Dto;
using RecallAI.Api.Models.Search;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace RecallAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MemoryController : ControllerBase
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IHydeService _hydeService;
    private readonly HydeConfiguration _hydeConfig;

    private readonly ILogger<MemoryController> _logger;

    public MemoryController(
        IMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        IHydeService hydeService,
        IOptions<HydeConfiguration> hydeOptions,
        ILogger<MemoryController> logger)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _hydeService = hydeService;
        _hydeConfig = hydeOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<MemoryListResponse>> GetMemories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Validate pagination parameters
        if (page < 1)
            return BadRequest(new { Message = "Page must be greater than 0" });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { Message = "PageSize must be between 1 and 100" });

        try
        {
            var userId = Guid.Parse(HttpContext.GetCurrentUserIdOrThrow());

            var totalCount = await _memoryRepository.GetCountByUserAsync(userId);
            var memories = await _memoryRepository.GetAllByUserAsync(userId, page, pageSize);

            var response = new MemoryListResponse
            {
                Memories = memories.Select(m => new MemoryResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Content = m.Content,
                    ContentType = m.ContentType,
                    Metadata = m.Metadata,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "User authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving memories for user");
            return StatusCode(500, new { Message = "An error occurred while retrieving memories" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MemoryResponse>> GetMemory(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest(new { Message = "Invalid memory ID" });

        try
        {
            var userId = Guid.Parse(HttpContext.GetCurrentUserIdOrThrow());
            var memory = await _memoryRepository.GetByIdAsync(id, userId);

            if (memory == null)
                return NotFound(new { Message = "Memory not found" });

            var response = new MemoryResponse
            {
                Id = memory.Id,
                Title = memory.Title,
                Content = memory.Content,
                ContentType = memory.ContentType,
                Metadata = memory.Metadata,
                CreatedAt = memory.CreatedAt,
                UpdatedAt = memory.UpdatedAt
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "User authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving memory {MemoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while retrieving the memory" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MemoryResponse>> CreateMemory([FromBody] CreateMemoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate ContentType
        var validContentTypes = new[] { "text", "document", "note", "conversation" };
        if (!string.IsNullOrEmpty(request.ContentType) && !validContentTypes.Contains(request.ContentType))
        {
            return BadRequest(new { Message = "ContentType must be one of: text, document, note, conversation" });
        }

        try
        {
            
            var userId = Guid.Parse(HttpContext.GetCurrentUserIdOrThrow());
            var memory = new Memory
            {
                UserId = userId,
                Title = request.Title,
                Content = request.Content,
                ContentType = request.ContentType ?? "text",
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            var createdMemory = await _memoryRepository.CreateAsync(memory);

            var response = new MemoryResponse
            {
                Id = createdMemory.Id,
                Title = createdMemory.Title,
                Content = createdMemory.Content,
                ContentType = createdMemory.ContentType,
                Metadata = createdMemory.Metadata,
                CreatedAt = createdMemory.CreatedAt,
                UpdatedAt = createdMemory.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetMemory),
                new { id = response.Id },
                response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "User authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating memory");
            return StatusCode(500, new { Message = "An error occurred while creating the memory" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MemoryResponse>> UpdateMemory(Guid id, [FromBody] UpdateMemoryRequest request)
    {
        if (id == Guid.Empty)
            return BadRequest(new { Message = "Invalid memory ID" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate ContentType if provided
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            var validContentTypes = new[] { "text", "document", "note", "conversation" };
            if (!validContentTypes.Contains(request.ContentType))
            {
                return BadRequest(new { Message = "ContentType must be one of: text, document, note, conversation" });
            }
        }

        try
        {
            var userId = Guid.Parse(HttpContext.GetCurrentUserIdOrThrow());
            var existingMemory = await _memoryRepository.GetByIdAsync(id, userId);

            if (existingMemory == null)
                return NotFound(new { Message = "Memory not found" });

            // Update only provided fields (partial update)
            if (request.Title != null)
                existingMemory.Title = request.Title;

            if (request.Content != null)
                existingMemory.Content = request.Content;

            if (request.ContentType != null)
                existingMemory.ContentType = request.ContentType;

            if (request.Metadata != null)
                existingMemory.Metadata = request.Metadata;

            var updatedMemory = await _memoryRepository.UpdateAsync(existingMemory);

            var response = new MemoryResponse
            {
                Id = updatedMemory.Id,
                Title = updatedMemory.Title,
                Content = updatedMemory.Content,
                ContentType = updatedMemory.ContentType,
                Metadata = updatedMemory.Metadata,
                CreatedAt = updatedMemory.CreatedAt,
                UpdatedAt = updatedMemory.UpdatedAt
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "User authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating memory {MemoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while updating the memory" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMemory(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest(new { Message = "Invalid memory ID" });

        try
        {
            var userId = Guid.Parse(HttpContext.GetCurrentUserIdOrThrow());
            var deleted = await _memoryRepository.DeleteAsync(id, userId);

            if (!deleted)
                return NotFound(new { Message = "Memory not found" });

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { Message = "User authentication required" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting memory {MemoryId}", id);
            return StatusCode(500, new { Message = "An error occurred while deleting the memory" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchResponse>> SearchMemories(
        [FromQuery, Required] string query,
        [FromQuery] int limit = 10,
        [FromQuery] double threshold = 0.7,
        [FromQuery] bool useHyde = false)
    {
        // Validate query parameters
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { Message = "Query parameter is required" });

        if (query.Length > 500)
            return BadRequest(new { Message = "Query must be 500 characters or less" });

        if (limit < 1 || limit > 50)
            return BadRequest(new { Message = "Limit must be between 1 and 50" });

        if (threshold < 0.0 || threshold > 1.0)
            return BadRequest(new { Message = "Threshold must be between 0.0 and 1.0" });

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var userId = Guid.Parse(HttpContext.GetCurrentUserIdOrThrow());
            var hydeRequested = useHyde && _hydeConfig.Enabled;
            const double QueryWeight = 0.4;
            const double HydeWeight = 0.6;

            var queryEmbeddingTask = _embeddingService.GenerateEmbeddingAsync(query);
            Task<string>? hydeDocumentTask = null;
            if (hydeRequested)
            {
                hydeDocumentTask = _hydeService.GenerateHypotheticalAsync(query);
            }

            float[] queryEmbedding;
            try
            {
                queryEmbedding = await queryEmbeddingTask;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to generate embedding for search query");
                return StatusCode(503, new { Message = "Search service temporarily unavailable" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error generating embedding for search query");
                return StatusCode(500, new { Message = "An error occurred while processing the search query" });
            }

            bool hydeUsed = false;
            string? hypotheticalDocument = null;
            List<SearchResultItem> items;

            if (hydeRequested && hydeDocumentTask != null)
            {
                try
                {
                    hypotheticalDocument = await hydeDocumentTask;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "HyDE is disabled or unavailable. Falling back to query search for user {UserId}", userId);
                    hydeRequested = false;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "HyDE generation failed for query '{Query}'. Falling back to query search.", query);
                    hydeRequested = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error generating HyDE document for query '{Query}'. Falling back to query search.", query);
                    hydeRequested = false;
                }
            }

            if (hydeRequested && !string.IsNullOrWhiteSpace(hypotheticalDocument))
            {
                float[] hydeEmbedding;
                try
                {
                    hydeEmbedding = await _hydeService.GetHydeEmbeddingAsync(query);
                }
                catch (HttpRequestException ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Failed to generate HyDE embedding for query '{Query}'", query);
                    return StatusCode(503, new { Message = "Search service temporarily unavailable" });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Error generating HyDE embedding for query '{Query}'", query);
                    return StatusCode(500, new { Message = "An error occurred while processing the HyDE search" });
                }

                var hybridResults = await _memoryRepository.HybridSearchAsync(userId, queryEmbedding, hydeEmbedding, limit, threshold);

                hydeUsed = true;

                items = hybridResults
                    .Select(result =>
                    {
                        var queryScore = result.queryScore;
                        var hydeScore = result.hydeScore;
                        var hasQueryScore = queryScore > 0;
                        var hasHydeScore = hydeScore > 0;

                        var weightedScore = hasQueryScore && hasHydeScore
                            ? (queryScore * QueryWeight) + (hydeScore * HydeWeight)
                            : (hasHydeScore ? hydeScore : queryScore);

                        var bestInputScore = hasQueryScore && hasHydeScore
                            ? Math.Max(queryScore, hydeScore)
                            : (hasHydeScore ? hydeScore : queryScore);

                        var combinedScore = hasQueryScore && hasHydeScore
                            ? Math.Max(weightedScore, bestInputScore)
                            : weightedScore;

                        var method = hasQueryScore && hasHydeScore
                            ? "combined"
                            : hasHydeScore ? "hyde" : "query";

                        return new SearchResultItem
                        {
                            Id = result.memory.Id,
                            Title = result.memory.Title,
                            Content = result.memory.Content,
                            ContentType = result.memory.ContentType,
                            SimilarityScore = Math.Round(bestInputScore, 4),
                            CombinedScore = Math.Round(combinedScore, 4),
                            SearchMethod = method,
                            CreatedAt = result.memory.CreatedAt,
                            Metadata = result.memory.Metadata
                        };
                    })
                    .OrderByDescending(item => item.CombinedScore)
                    .Take(limit)
                    .ToList();
            }
            else
            {
                var searchResults = await _memoryRepository.SearchSimilarAsync(userId, queryEmbedding, limit, threshold);

                items = searchResults.Select(result => new SearchResultItem
                {
                    Id = result.memory.Id,
                    Title = result.memory.Title,
                    Content = result.memory.Content,
                    ContentType = result.memory.ContentType,
                    SimilarityScore = Math.Round(result.similarity, 4),
                    CombinedScore = Math.Round(result.similarity, 4),
                    SearchMethod = "query",
                    CreatedAt = result.memory.CreatedAt,
                    Metadata = result.memory.Metadata
                }).ToList();
            }

            stopwatch.Stop();

            var response = new SearchResponse
            {
                Query = query,
                ResultCount = items.Count,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Results = items,
                HydeUsed = hydeUsed,
                HypotheticalDocument = hydeUsed ? hypotheticalDocument : null
            };

            _logger.LogInformation("Search completed for user {UserId}: query='{Query}', results={ResultCount}, time={ExecutionTimeMs}ms, hydeUsed={HydeUsed}",
                userId, query, response.ResultCount, response.ExecutionTimeMs, response.HydeUsed);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            stopwatch.Stop();
            return Unauthorized(new { Message = "User authentication required" });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error performing memory search for query: {Query}", query);
            return StatusCode(500, new { Message = "An error occurred while searching memories" });
        }
    }
}



