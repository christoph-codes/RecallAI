using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace RecallAI.Api.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient httpClient, IOptions<OpenAIConfiguration> openAIOptions, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _openAIConfig = openAIOptions.Value;
        _logger = logger;
        
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

    public async Task<string> EvaluateMemoryAsync(string memoryContent, string evaluationCriteria)
    {
        var model = _openAIConfig.Models.MemoryEvaluation;
        
        var prompt = $"Evaluate the following memory content based on the given criteria:\n\nMemory Content:\n{memoryContent}\n\nEvaluation Criteria:\n{evaluationCriteria}\n\nProvide a detailed evaluation:";
        
        return await GenerateCompletionAsync(model, prompt, "memory evaluation");
    }

    public async Task<string> GenerateHyDEAsync(string query)
    {
        var model = _openAIConfig.Models.HyDE;
        
        var prompt = $"Generate a hypothetical document that would answer the following query. The document should be detailed and informative:\n\nQuery: {query}\n\nHypothetical Document:";
        
        return await GenerateCompletionAsync(model, prompt, "HyDE generation");
    }

    public async Task<string> GenerateFinalResultAsync(string prompt, string context)
    {
        var model = _openAIConfig.Models.FinalResult;
        
        var fullPrompt = $"Context:\n{context}\n\nUser Request:\n{prompt}\n\nProvide a comprehensive and accurate response:";
        
        return await GenerateCompletionAsync(model, fullPrompt, "final result generation");
    }

    private async Task<string> GenerateCompletionAsync(string model, string prompt, string operationType)
    {
        try
        {
            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 2000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OpenAI API request failed for {operationType} with status {response.StatusCode}: {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            var messageContent = responseData
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogDebug("Generated {OperationType} using model {Model}", operationType, model);
            
            return messageContent ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {OperationType} using model {Model}", operationType, model);
            throw;
        }
    }
}