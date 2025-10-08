using System.Text;
using System.Text.Json;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models;
using RecallAI.Api.Models.Completion;
using RecallAI.Api.Models.Configuration;
using Microsoft.Extensions.Options;

namespace RecallAI.Api.Services;

public class CompletionPipelineService : ICompletionPipelineService
{
    private const int MemoryTitleMaxLength = 80;

    private readonly IOpenAIService _openAIService;
    private readonly IHydeService _hydeService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IMemoryRepository _memoryRepository;
    private readonly CompletionDefaults _completionConfig;
    private readonly ILogger<CompletionPipelineService> _logger;
    private readonly string _embeddingModelName;

    public CompletionPipelineService(
        IOpenAIService openAIService,
        IHydeService hydeService,
        IEmbeddingService embeddingService,
        IMemoryRepository memoryRepository,
        IOptions<CompletionDefaults> completionOptions,
        IOptions<OpenAIConfiguration> openAIOptions,
        ILogger<CompletionPipelineService> logger)
    {
        _openAIService = openAIService;
        _hydeService = hydeService;
        _embeddingService = embeddingService;
        _memoryRepository = memoryRepository;
        _completionConfig = completionOptions.Value;
        _embeddingModelName = openAIOptions.Value.Models.Embeddings;
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
                    OpenAISystemPrompts.MemoryEvaluation
                );
                _logger.LogDebug("Memory evaluation completed");

                if (!string.IsNullOrWhiteSpace(memoryEvaluation))
                {
                    await PersistMemoriesFromEvaluationAsync(memoryEvaluation, userId);
                }
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
                hydeDocument = await _hydeService.GenerateHypotheticalAsync(request.Message);
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
            model: request.Configuration?.Model,
            temperature: request.Configuration?.Temperature,
            maxTokens: request.Configuration?.MaxTokens,
            systemPrompt: OpenAISystemPrompts.FinalResponse,
            cancellationToken: cancellationToken))
        {
            yield return chunk;
        }

        if (!hasError)
        {
            _logger.LogInformation("Completion pipeline completed successfully for user {UserId}", userId);
        }
    }

    private async Task PersistMemoriesFromEvaluationAsync(string evaluationJson, Guid userId)
    {
        MemoryEvaluationResult? evaluation;
        try
        {
            evaluation = JsonSerializer.Deserialize<MemoryEvaluationResult>(evaluationJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse memory evaluation JSON for user {UserId}", userId);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error parsing memory evaluation JSON for user {UserId}", userId);
            return;
        }

        if (evaluation?.Memories == null || evaluation.Memories.Count == 0)
        {
            _logger.LogDebug("Memory evaluation returned no memories to persist for user {UserId}", userId);
            return;
        }

        var candidates = new List<MemoryCandidate>();
        for (var index = 0; index < evaluation.Memories.Count; index++)
        {
            var candidate = BuildMemoryCandidate(evaluation.Memories[index], index);
            if (candidate is not null)
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            _logger.LogDebug("No valid memories found in evaluation for user {UserId}", userId);
            return;
        }

        List<float[]> embeddings;
        try
        {
            var embeddingInputs = candidates.Select(candidate => candidate.EmbeddingText).ToList();
            embeddings = await _embeddingService.GenerateEmbeddingsAsync(embeddingInputs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embeddings for evaluated memories for user {UserId}", userId);
            return;
        }

        if (embeddings.Count != candidates.Count)
        {
            _logger.LogWarning(
                "Embedding count mismatch when processing evaluated memories for user {UserId}: expected {Expected}, received {Actual}",
                userId,
                candidates.Count,
                embeddings.Count);
            return;
        }

        var storedCount = 0;
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            var embedding = embeddings[i];

            try
            {
                var memory = new Memory
                {
                    UserId = userId,
                    Title = candidate.Title,
                    Content = candidate.Content,
                    ContentType = "text",
                    Metadata = candidate.Metadata
                };

                await _memoryRepository.CreateAsync(memory, embedding, _embeddingModelName);
                storedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist evaluated memory {Index} for user {UserId}", candidate.Index, userId);
            }
        }

        if (storedCount > 0)
        {
            _logger.LogInformation("Stored {Count} new memories from evaluation for user {UserId}", storedCount, userId);
        }
    }

    private static MemoryCandidate? BuildMemoryCandidate(MemoryEvaluationItem? item, int index)
    {
        if (item is null)
        {
            return null;
        }

        var summary = item.Summary?.Trim();
        var sourceText = item.SourceText?.Trim();

        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var content = BuildMemoryContent(summary!, sourceText);
        var title = BuildMemoryTitle(summary!);
        var metadata = BuildMemoryMetadata(sourceText);

        return new MemoryCandidate(
            index,
            content,
            content,
            title,
            metadata);
    }

    private static string BuildMemoryContent(string summary, string? sourceText)
    {
        var normalizedSummary = summary.Trim();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return normalizedSummary;
        }

        var normalizedSource = sourceText.Trim();

        if (string.Equals(normalizedSummary, normalizedSource, System.StringComparison.OrdinalIgnoreCase))
        {
            return normalizedSummary;
        }

        return $"{normalizedSummary}\n\nSource: {normalizedSource}";
    }

    private static string? BuildMemoryTitle(string summary)
    {
        var normalizedSummary = summary.Trim();

        if (normalizedSummary.Length == 0)
        {
            return null;
        }

        if (normalizedSummary.Length <= MemoryTitleMaxLength)
        {
            return normalizedSummary;
        }

        return normalizedSummary.Substring(0, MemoryTitleMaxLength).TrimEnd() + "...";
    }

    private static Dictionary<string, object> BuildMemoryMetadata(string? sourceText)
    {
        var metadata = new Dictionary<string, object>
        {
            ["origin"] = "memory_evaluation",
            ["pipeline"] = "completion"
        };

        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            metadata["source_text"] = sourceText.Trim();
        }

        return metadata;
    }

    private sealed record MemoryCandidate(
        int Index,
        string Content,
        string EmbeddingText,
        string? Title,
        Dictionary<string, object> Metadata);
}