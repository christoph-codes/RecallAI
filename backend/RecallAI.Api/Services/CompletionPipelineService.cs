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
    private const double DuplicateSimilarityThreshold = 0.98d;
    private const double MinimumConfidenceToPersist = 0.5d;

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
            _logger.LogInformation("Streaming chunk for user {UserId}: {Chunk}", userId, chunk);
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

        var seenContents = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<MemoryCandidate>();
        for (var index = 0; index < evaluation.Memories.Count; index++)
        {
            var item = evaluation.Memories[index];
            if (item is null)
            {
                continue;
            }

            if (item.ShouldSave != true)
            {
                _logger.LogDebug("Skipping memory candidate {Index} with should_save=false for user {UserId}", index, userId);
                continue;
            }

            var candidate = BuildMemoryCandidate(item, index);
            if (candidate is null)
            {
                _logger.LogDebug("Skipping invalid memory candidate {Index} for user {UserId}", index, userId);
                continue;
            }

            if (!seenContents.Add(candidate.Content))
            {
                _logger.LogDebug("Skipping duplicate memory candidate {Index} for user {UserId}", candidate.Index, userId);
                continue;
            }

            candidates.Add(candidate);
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
                if (candidate.Confidence is double confidence && confidence < MinimumConfidenceToPersist)
                {
                    _logger.LogDebug("Skipping evaluated memory {Index} for user {UserId} due to low confidence {Confidence}", candidate.Index, userId, confidence);
                    continue;
                }

                if (await IsDuplicateMemoryAsync(userId, candidate, embedding))
                {
                    continue;
                }

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

        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var sourceText = item.SourceText?.Trim();
        var content = BuildMemoryContent(summary, sourceText);
        var title = BuildMemoryTitle(summary);
        var metadata = BuildMemoryMetadata(sourceText, item.Confidence);

        return new MemoryCandidate(
            index,
            content,
            content,
            title,
            metadata,
            item.Confidence);
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

    private static Dictionary<string, object> BuildMemoryMetadata(string? sourceText, double? confidence)
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

        if (confidence is double value)
        {
            var clamped = Math.Clamp(value, 0d, 1d);
            metadata["confidence"] = Math.Round(clamped, 3);
        }

        return metadata;
    }

    private async Task<bool> IsDuplicateMemoryAsync(Guid userId, MemoryCandidate candidate, float[] embedding)
    {
        try
        {
            var matches = await _memoryRepository.SearchSimilarAsync(userId, embedding, limit: 1, threshold: DuplicateSimilarityThreshold);
            if (matches.Count == 0)
            {
                return false;
            }

            var (existingMemory, similarity) = matches[0];
            if (similarity >= DuplicateSimilarityThreshold)
            {
                _logger.LogDebug(
                    "Skipping evaluated memory {Index} for user {UserId} because it matches existing memory {ExistingMemoryId} with similarity {Similarity}",
                    candidate.Index,
                    userId,
                    existingMemory.Id,
                    similarity);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Duplicate evaluation check failed for memory {Index} for user {UserId}",
                candidate.Index,
                userId);
        }

        return false;
    }

    private sealed record MemoryCandidate(
        int Index,
        string Content,
        string EmbeddingText,
        string? Title,
        Dictionary<string, object> Metadata,
        double? Confidence);
}
