using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Completion;
using RecallAI.Api.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace RecallAI.Api.Services;

public class CompletionPipelineService : ICompletionPipelineService
{
    private readonly IOpenAIService _openAIService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IMemoryRepository _memoryRepository;
    private readonly CompletionDefaults _completionConfig;
    private readonly ILogger<CompletionPipelineService> _logger;

    public CompletionPipelineService(
        IOpenAIService openAIService,
        IEmbeddingService embeddingService,
        IMemoryRepository memoryRepository,
        IOptions<CompletionDefaults> completionOptions,
        ILogger<CompletionPipelineService> logger)
    {
        _openAIService = openAIService;
        _embeddingService = embeddingService;
        _memoryRepository = memoryRepository;
        _completionConfig = completionOptions.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> ProcessCompletionAsync(
        CompletionRequest request,
        Guid userId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting completion pipeline for user {UserId}", userId);

        // Apply configuration defaults
        var enableMemorySearch = request.Configuration?.EnableMemorySearch ?? _completionConfig.EnableMemorySearch;
        var maxMemoryResults = request.Configuration?.MaxMemoryResults ?? _completionConfig.MaxMemoryResults;
        var memoryThreshold = request.Configuration?.MemoryThreshold ?? _completionConfig.MemoryThreshold;

        // Step 1: Memory Evaluation (if enabled)
        string? memoryEvaluation = null;
        if (enableMemorySearch)
        {
            _logger.LogDebug("Step 1: Evaluating memory relevance");
            try
            {
                memoryEvaluation = await _openAIService.EvaluateMemoryAsync(
                    request.Message,
                    "Determine if this query would benefit from searching through the user's stored memories and knowledge base."
                );
                _logger.LogDebug("Memory evaluation completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Memory evaluation failed, continuing without memory search");
            }
        }

        // Step 2: Generate HyDE (Hypothetical Document Embeddings)
        string? hydeDocument = null;
        if (enableMemorySearch)
        {
            _logger.LogDebug("Step 2: Generating HyDE document");
            try
            {
                hydeDocument = await _openAIService.GenerateHyDEAsync(request.Message);
                _logger.LogDebug("HyDE document generated");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HyDE generation failed, using original query for search");
            }
        }

        // Step 3: Vector Search
        var relevantMemories = new List<(Models.Memory memory, double similarity)>();
        if (enableMemorySearch)
        {
            _logger.LogDebug("Step 3: Performing vector search");
            try
            {
                var searchText = hydeDocument ?? request.Message;
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchText);
                
                relevantMemories = await _memoryRepository.SearchSimilarAsync(
                    userId,
                    queryEmbedding,
                    maxMemoryResults,
                    memoryThreshold
                );
                
                _logger.LogDebug("Found {Count} relevant memories", relevantMemories.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search failed, continuing without memory context");
            }
        }

        // Step 4: Build context and prompt
        var contextBuilder = new StringBuilder();
        
        if (relevantMemories.Any())
        {
            contextBuilder.AppendLine("Relevant context from your knowledge base:");
            contextBuilder.AppendLine();
            
            foreach (var (memory, similarity) in relevantMemories)
            {
                contextBuilder.AppendLine($"**{memory.Title ?? "Memory"}** (Relevance: {similarity:P1})");
                contextBuilder.AppendLine(memory.Content);
                contextBuilder.AppendLine();
            }
            
            contextBuilder.AppendLine("---");
            contextBuilder.AppendLine();
        }

        if (!string.IsNullOrEmpty(memoryEvaluation))
        {
            contextBuilder.AppendLine($"Memory Analysis: {memoryEvaluation}");
            contextBuilder.AppendLine();
        }

        contextBuilder.AppendLine("User Query:");
        contextBuilder.AppendLine(request.Message);

        var finalPrompt = contextBuilder.ToString();

        // Step 5: Stream LLM Response
        _logger.LogDebug("Step 5: Generating streaming response");
        
        var hasError = false;
        await foreach (var chunk in _openAIService.GenerateStreamingCompletionAsync(
            finalPrompt,
            request.Configuration?.Model,
            request.Configuration?.Temperature,
            request.Configuration?.MaxTokens,
            cancellationToken))
        {
            yield return chunk;
        }

        if (!hasError)
        {
            _logger.LogInformation("Completion pipeline completed successfully for user {UserId}", userId);
        }
    }
}