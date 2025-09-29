using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using RecallAI.Api.Interfaces;
using RecallAI.Api.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Net;

namespace RecallAI.Api.Services;

public class OpenAIService : IOpenAIService
{
    private static readonly Uri ResponsesEndpoint = new("https://api.openai.com/v1/responses");

    private readonly HttpClient _httpClient;
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient httpClient, IOptions<OpenAIConfiguration> openAIOptions, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _openAIConfig = openAIOptions.Value;
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                    ?? _openAIConfig.ApiKey;

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Please set OPENAI_API_KEY environment variable or OpenAI:ApiKey in appsettings.json");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RecallAI/1.0");
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_openAIConfig.TimeoutSeconds);
    }

    public async Task<string> EvaluateMemoryAsync(string memoryContent, string evaluationCriteria)
    {
        var model = _openAIConfig.Models.MemoryEvaluation;

        var userPrompt = $"User Input:\n{memoryContent}\n\nEvaluation Criteria:\n{evaluationCriteria}\n\nDecide if this input should be stored as a memory and explain your reasoning.";

        return await GenerateResponseAsync(model, OpenAISystemPrompts.MemoryEvaluation, userPrompt, "memory evaluation");
    }

    public async Task<string> GenerateHyDEAsync(string query)
    {
        var model = _openAIConfig.Models.HyDE;

        var userPrompt = $"Query: {query}\n\nCreate the hypothetical document:";

        return await GenerateResponseAsync(model, OpenAISystemPrompts.HyDE, userPrompt, "HyDE generation");
    }

    public async Task<string> GenerateFinalResultAsync(string prompt, string context)
    {
        var model = _openAIConfig.Models.FinalResult;

        var userPrompt = $"Context:\n{context}\n\nUser Request:\n{prompt}";

        return await GenerateResponseAsync(model, OpenAISystemPrompts.FinalResponse, userPrompt, "final result generation");
    }

    private async Task<string> GenerateResponseAsync(string model, string systemPrompt, string userPrompt, string operationType)
    {
        try
        {
            var requestPayload = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["input"] = OpenAIResponseHelpers.BuildMessages(systemPrompt, userPrompt),
                ["temperature"] = 0.7,
                ["max_output_tokens"] = 2000
            };

            var json = JsonSerializer.Serialize(requestPayload, OpenAIResponseHelpers.RequestSerializerOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(ResponsesEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string userFriendlyMessage = GetUserFriendlyErrorMessage(response.StatusCode, errorContent);
                throw new HttpRequestException($"{userFriendlyMessage}|ORIGINAL:{errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var messageContent = OpenAIResponseHelpers.ExtractTextContent(responseData);

            _logger.LogDebug("Generated {OperationType} using model {Model}", operationType, model);

            return messageContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {OperationType} using model {Model}", operationType, model);
            throw;
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingCompletionAsync(
        string prompt,
        string? model = null,
        double? temperature = null,
        int? maxTokens = null,
        string? systemPrompt = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var selectedModel = model ?? _openAIConfig.Models.FinalResult;
        var selectedTemperature = temperature ?? 0.7;
        var selectedMaxTokens = maxTokens ?? 2000;
        var effectiveSystemPrompt = systemPrompt ?? OpenAISystemPrompts.FinalResponse;

        var requestPayload = new Dictionary<string, object?>
        {
            ["model"] = selectedModel,
            ["input"] = OpenAIResponseHelpers.BuildMessages(effectiveSystemPrompt, prompt),
            ["temperature"] = selectedTemperature,
            ["max_output_tokens"] = selectedMaxTokens,
            ["stream"] = true
        };

        var json = JsonSerializer.Serialize(requestPayload, OpenAIResponseHelpers.RequestSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint)
        {
            Content = content
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send request to OpenAI API using model {Model}", selectedModel);
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            
            // Parse OpenAI error response for better error messages
            string userFriendlyMessage = GetUserFriendlyErrorMessage(response.StatusCode, errorContent);
            
            _logger.LogError("OpenAI API request failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
            response.Dispose();
            yield return $"\n\n❌ **{userFriendlyMessage}**";
            yield break;
        }

        using (response)
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var data = line[5..].Trim();

                if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                JsonElement jsonData;
                try
                {
                    jsonData = JsonSerializer.Deserialize<JsonElement>(data);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response chunk: {Data}", data);
                    continue;
                }

                if (jsonData.ValueKind == JsonValueKind.Object && jsonData.TryGetProperty("type", out var typeProperty))
                {
                    var typeValue = typeProperty.GetString();

                    if (string.Equals(typeValue, "response.error", StringComparison.OrdinalIgnoreCase))
                    {
                        var errorText = ExtractStreamingError(jsonData);
                        _logger.LogError("Received error from streaming response: {Error}", errorText);
                        continue;
                    }

                    if (string.Equals(typeValue, "response.completed", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }

                if (OpenAIResponseHelpers.TryExtractStreamDelta(jsonData, out var delta) && !string.IsNullOrEmpty(delta))
                {
                    yield return delta;
                }
            }
        }

        _logger.LogDebug("Completed streaming completion using model {Model}", selectedModel);
    }
    
    private string GetUserFriendlyErrorMessage(HttpStatusCode statusCode, string errorContent)
    {
        try
        {
            var errorData = JsonSerializer.Deserialize<JsonElement>(errorContent);
            
            if (errorData.TryGetProperty("error", out var errorElement))
            {
                var errorType = errorElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "";
                var errorMessage = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "";
                var errorCode = errorElement.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : "";
                
                return (statusCode, errorType, errorCode) switch
                {
                    (HttpStatusCode.TooManyRequests, _, _) => 
                        "⏳ **Rate limit exceeded.** Please wait a moment and try again.",
                    
                    (HttpStatusCode.Unauthorized, _, _) => 
                        "🔑 **API key issue.** Please check your OpenAI API key configuration.",
                    
                    (HttpStatusCode.PaymentRequired, _, _) or (_, "insufficient_quota", _) => 
                        "💳 **Quota exceeded.** Your OpenAI API quota has been reached. Please check your billing at platform.openai.com.",
                    
                    (HttpStatusCode.BadRequest, "invalid_request_error", _) => 
                        $"❌ **Invalid request:** {errorMessage}",
                    
                    (HttpStatusCode.InternalServerError, _, _) => 
                        "🔧 **OpenAI service issue.** The OpenAI API is experiencing issues. Please try again later.",
                    
                    _ => $"⚠️ **API Error ({statusCode}):** {errorMessage ?? "Unknown error occurred"}"
                };
            }
        }
        catch (JsonException)
        {
            // If we can't parse the error, provide a generic message
        }
        
        return statusCode switch
        {
            HttpStatusCode.TooManyRequests => "⏳ **Rate limit exceeded.** Please wait and try again.",
            HttpStatusCode.Unauthorized => "🔑 **Authentication failed.** Please check your API key.",
            HttpStatusCode.PaymentRequired => "💳 **Quota exceeded.** Please check your OpenAI billing.",
            HttpStatusCode.InternalServerError => "🔧 **Service unavailable.** Please try again later.",
            _ => $"⚠️ **Error ({statusCode}).** Please try again or contact support."
        };
    }
}