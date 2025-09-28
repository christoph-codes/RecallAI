using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecallAI.Api.Services;

internal static class OpenAIResponseHelpers
{
    internal static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static object[] BuildMessages(string? systemPrompt, string userPrompt)
    {
        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new
            {
                role = "system",
                content = new object[]
                {
                    new { type = "input_text", text = systemPrompt }
                }
            });
        }

        messages.Add(new
        {
            role = "user",
            content = new object[]
            {
                new { type = "input_text", text = userPrompt }
            }
        });

        return messages.ToArray();
    }

    public static object[] BuildUserTextInput(string text) => BuildMessages(null, text);

    public static string ExtractTextContent(JsonElement root)
    {
        var builder = new StringBuilder();

        if (TryAppendOutputText(root, builder))
        {
            return builder.ToString();
        }

        if (TryAppendOutput(root, builder))
        {
            return builder.ToString();
        }

        if (TryAppendChoices(root, builder))
        {
            return builder.ToString();
        }

        return string.Empty;
    }

    public static bool TryExtractStreamDelta(JsonElement element, out string delta)
    {
        delta = string.Empty;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty("type", out var typeProperty))
        {
            var typeValue = typeProperty.GetString();

            if (string.Equals(typeValue, "response.output_text.delta", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(typeValue, "response.output.delta", StringComparison.OrdinalIgnoreCase))
            {
                if (element.TryGetProperty("delta", out var deltaElement))
                {
                    delta = ExtractTextFragment(deltaElement);
                    return !string.IsNullOrEmpty(delta);
                }
            }
            else if (string.Equals(typeValue, "response.output_text.done", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else if (string.Equals(typeValue, "response.error", StringComparison.OrdinalIgnoreCase))
            {
                if (element.TryGetProperty("error", out var errorElement))
                {
                    delta = ExtractTextFragment(errorElement);
                }
                return false;
            }
        }

        if (element.TryGetProperty("delta", out var plainDeltaElement))
        {
            delta = ExtractTextFragment(plainDeltaElement);
            if (!string.IsNullOrEmpty(delta))
            {
                return true;
            }
        }

        return TryExtractLegacyDelta(element, out delta);
    }

    private static bool TryAppendOutputText(JsonElement root, StringBuilder builder)
    {
        if (!root.TryGetProperty("output_text", out var outputTextElement))
        {
            return false;
        }

        switch (outputTextElement.ValueKind)
        {
            case JsonValueKind.String:
                AppendIfNotEmpty(builder, outputTextElement.GetString());
                break;
            case JsonValueKind.Array:
                foreach (var item in outputTextElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        AppendIfNotEmpty(builder, item.GetString());
                    }
                }
                break;
        }

        return builder.Length > 0;
    }

    private static bool TryAppendOutput(JsonElement root, StringBuilder builder)
    {
        if (!root.TryGetProperty("output", out var outputElement) || outputElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in outputElement.EnumerateArray())
        {
            if (entry.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var contentItem in contentElement.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                    {
                        AppendIfNotEmpty(builder, textElement.GetString());
                    }
                }
            }
        }

        return builder.Length > 0;
    }

    private static bool TryAppendChoices(JsonElement root, StringBuilder builder)
    {
        if (!root.TryGetProperty("choices", out var choicesElement) || choicesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var choice in choicesElement.EnumerateArray())
        {
            if (choice.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.Object)
            {
                if (messageElement.TryGetProperty("content", out var contentElement))
                {
                    switch (contentElement.ValueKind)
                    {
                        case JsonValueKind.String:
                            AppendIfNotEmpty(builder, contentElement.GetString());
                            break;
                        case JsonValueKind.Array:
                            foreach (var contentItem in contentElement.EnumerateArray())
                            {
                                if (contentItem.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                                {
                                    AppendIfNotEmpty(builder, textElement.GetString());
                                }
                            }
                            break;
                    }
                }
            }
            else if (choice.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
            {
                AppendIfNotEmpty(builder, textElement.GetString());
            }
        }

        return builder.Length > 0;
    }

    private static bool TryExtractLegacyDelta(JsonElement element, out string delta)
    {
        delta = string.Empty;

        if (!element.TryGetProperty("choices", out var choicesElement) || choicesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var choice in choicesElement.EnumerateArray())
        {
            if (choice.TryGetProperty("delta", out var deltaElement) && deltaElement.ValueKind == JsonValueKind.Object)
            {
                if (deltaElement.TryGetProperty("content", out var contentElement))
                {
                    if (contentElement.ValueKind == JsonValueKind.String)
                    {
                        var text = contentElement.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            delta = text;
                            return true;
                        }
                    }
                    else if (contentElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var contentPart in contentElement.EnumerateArray())
                        {
                            if (contentPart.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                            {
                                var text = textElement.GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    delta = text;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private static string ExtractTextFragment(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Array => ExtractFromArray(element),
            JsonValueKind.Object => ExtractFromObject(element),
            _ => string.Empty
        };
    }

    private static string ExtractFromArray(JsonElement arrayElement)
    {
        var builder = new StringBuilder();
        foreach (var item in arrayElement.EnumerateArray())
        {
            var fragment = ExtractTextFragment(item);
            if (!string.IsNullOrEmpty(fragment))
            {
                builder.Append(fragment);
            }
        }

        return builder.ToString();
    }

    private static string ExtractFromObject(JsonElement objectElement)
    {
        if (objectElement.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
        {
            return textElement.GetString() ?? string.Empty;
        }

        if (objectElement.TryGetProperty("content", out var contentElement))
        {
            var contentText = ExtractTextFragment(contentElement);
            if (!string.IsNullOrEmpty(contentText))
            {
                return contentText;
            }
        }

        if (objectElement.TryGetProperty("delta", out var nestedDeltaElement))
        {
            var nested = ExtractTextFragment(nestedDeltaElement);
            if (!string.IsNullOrEmpty(nested))
            {
                return nested;
            }
        }

        var builder = new StringBuilder();
        foreach (var property in objectElement.EnumerateObject())
        {
            var fragment = ExtractTextFragment(property.Value);
            if (!string.IsNullOrEmpty(fragment))
            {
                builder.Append(fragment);
            }
        }

        return builder.ToString();
    }

    private static void AppendIfNotEmpty(StringBuilder builder, string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            builder.Append(text);
        }
    }
}

