using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Configuration;

namespace RecallAI.Api.Services;

public class HydeService : IHydeService
{
    private const int CacheLimit = 50;
    private readonly HttpClient _httpClient;
    private readonly HydeConfiguration _hydeConfig;
    private readonly OpenAIConfiguration _openAiConfig;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<HydeService> _logger;
    private readonly Dictionary<string, (string Document, DateTime CachedAt)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();

    public HydeService(
        HttpClient httpClient,
        IOptions<HydeConfiguration> hydeOptions,
        IOptions<OpenAIConfiguration> openAiOptions,
        IEmbeddingService embeddingService,
        ILogger<HydeService> logger)
    {
        _httpClient = httpClient;
        _hydeConfig = hydeOptions.Value;
        _openAiConfig = openAiOptions.Value;
        _embeddingService = embeddingService;
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _openAiConfig.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RecallAI/HydeService");
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<string> GenerateHypotheticalAsync(string query)
    {
        if (!_hydeConfig.Enabled)
        {
            throw new InvalidOperationException("HyDE is disabled in configuration.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        var normalizedQuery = NormalizeQuery(query);

        if (TryGetCachedDocument(normalizedQuery, out var cachedDocument))
        {
            _logger.LogDebug("Returned cached HyDE document for query hash {QueryKey}", normalizedQuery);
            return cachedDocument;
        }

        var prompt = BuildPrompt(query);

        try
        {
            var requestBody = new
            {
                model = _hydeConfig.Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = _hydeConfig.MaxTokens,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HyDE generation failed with status {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var document = responseData
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(document))
            {
                throw new InvalidOperationException("HyDE generation returned an empty document.");
            }

            CacheDocument(normalizedQuery, document);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate HyDE document for query hash {QueryKey}", normalizedQuery);
            throw;
        }
    }

    public async Task<float[]> GetHydeEmbeddingAsync(string query)
    {
        var document = await GenerateHypotheticalAsync(query);
        return await _embeddingService.GenerateEmbeddingAsync(document);
    }

    private string NormalizeQuery(string query)
    {
        return query.Trim();
    }

    private string BuildPrompt(string query)
    {
        return $"Write a detailed answer or document that would respond to this query about personal memories or notes. Be specific and informative. Keep under 500 words.\n\nQuery: {query}\n\nAnswer:";
    }

    private bool TryGetCachedDocument(string key, out string document)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                document = cached.Document;
                return true;
            }
        }

        document = string.Empty;
        return false;
    }

    private void CacheDocument(string key, string document)
    {
        lock (_cacheLock)
        {
            if (_cache.Count >= CacheLimit)
            {
                var oldestKey = _cache
                    .OrderBy(kvp => kvp.Value.CachedAt)
                    .First().Key;
                _cache.Remove(oldestKey);
            }

            _cache[key] = (document, DateTime.UtcNow);
        }
    }
}
