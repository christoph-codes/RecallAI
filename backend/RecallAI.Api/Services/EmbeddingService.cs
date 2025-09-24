using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace RecallAI.Api.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly ConcurrentDictionary<string, (float[] embedding, DateTime timestamp)> _cache;
    private readonly int _maxCacheSize;
    private readonly TimeSpan _cacheExpiry;

    public EmbeddingService(HttpClient httpClient, IOptions<OpenAIConfiguration> openAIOptions, IConfiguration configuration, ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _openAIConfig = openAIOptions.Value;
        _logger = logger;
        _cache = new ConcurrentDictionary<string, (float[], DateTime)>();
        
        // Get cache settings from configuration
        _maxCacheSize = configuration.GetValue<int>("EmbeddingCache:MaxSize", 1000);
        _cacheExpiry = TimeSpan.FromHours(configuration.GetValue<int>("EmbeddingCache:ExpiryHours", 1));
        
        // Configure HttpClient for OpenAI
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                    ?? _openAIConfig.ApiKey;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please set OPENAI_API_KEY environment variable or OpenAI:ApiKey in appsettings.json");
        }
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RecallAI/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(_openAIConfig.TimeoutSeconds);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        // Preprocess text
        var processedText = PreprocessText(text);
        
        // Check cache first
        if (_cache.TryGetValue(processedText, out var cached))
        {
            if (DateTime.UtcNow - cached.timestamp < _cacheExpiry)
            {
                _logger.LogDebug("Retrieved embedding from cache for text: {TextLength} characters", processedText.Length);
                return cached.embedding;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(processedText, out _);
            }
        }

        try
        {
            var embedding = await GenerateEmbeddingFromApiAsync(processedText);
            
            // Cache the result
            CacheEmbedding(processedText, embedding);
            
            _logger.LogDebug("Generated embedding for text: {TextLength} characters", processedText.Length);
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text: {TextLength} characters", processedText.Length);
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
        {
            return new List<float[]>();
        }

        var embeddings = new List<float[]>();
        var uncachedTexts = new List<(string text, int index)>();
        
        // Check cache for each text
        for (int i = 0; i < texts.Count; i++)
        {
            var processedText = PreprocessText(texts[i]);
            
            if (_cache.TryGetValue(processedText, out var cached) && 
                DateTime.UtcNow - cached.timestamp < _cacheExpiry)
            {
                embeddings.Add(cached.embedding);
            }
            else
            {
                embeddings.Add(null!); // Placeholder
                uncachedTexts.Add((processedText, i));
            }
        }

        // Generate embeddings for uncached texts
        if (uncachedTexts.Count > 0)
        {
            try
            {
                var newEmbeddings = await GenerateEmbeddingsFromApiAsync(uncachedTexts.Select(x => x.text).ToList());
                
                for (int i = 0; i < uncachedTexts.Count; i++)
                {
                    var (text, index) = uncachedTexts[i];
                    var embedding = newEmbeddings[i];
                    
                    embeddings[index] = embedding;
                    CacheEmbedding(text, embedding);
                }
                
                _logger.LogDebug("Generated {Count} new embeddings, {CachedCount} from cache", 
                    uncachedTexts.Count, texts.Count - uncachedTexts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for {Count} texts", uncachedTexts.Count);
                throw;
            }
        }

        return embeddings;
    }

    private async Task<float[]> GenerateEmbeddingFromApiAsync(string text)
    {
        var model = _openAIConfig.Models.Embeddings;
        
        var requestBody = new
        {
            input = text,
            model = model,
            encoding_format = "float"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);
        
        var embeddingArray = responseData.GetProperty("data")[0].GetProperty("embedding");
        var embedding = new float[embeddingArray.GetArrayLength()];
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = embeddingArray[i].GetSingle();
        }

        return embedding;
    }

    private async Task<List<float[]>> GenerateEmbeddingsFromApiAsync(List<string> texts)
    {
        var model = _openAIConfig.Models.Embeddings;
        
        var requestBody = new
        {
            input = texts,
            model = model,
            encoding_format = "float"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);
        
        var embeddings = new List<float[]>();
        var dataArray = responseData.GetProperty("data");
        
        for (int i = 0; i < dataArray.GetArrayLength(); i++)
        {
            var embeddingArray = dataArray[i].GetProperty("embedding");
            var embedding = new float[embeddingArray.GetArrayLength()];
            
            for (int j = 0; j < embedding.Length; j++)
            {
                embedding[j] = embeddingArray[j].GetSingle();
            }
            
            embeddings.Add(embedding);
        }

        return embeddings;
    }

    private string PreprocessText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Trim whitespace and normalize
        return text.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private void CacheEmbedding(string text, float[] embedding)
    {
        // Implement simple LRU eviction if cache is full
        if (_cache.Count >= _maxCacheSize)
        {
            var oldestKey = _cache
                .OrderBy(kvp => kvp.Value.timestamp)
                .First().Key;
            
            _cache.TryRemove(oldestKey, out _);
        }

        _cache.TryAdd(text, (embedding, DateTime.UtcNow));
    }
}