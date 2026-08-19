using System.Text.Json;
using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Models;

namespace GeminiWeatherApi.Services;

public static class GeminiResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static GeminiAnalysis Parse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new ApiException(
                StatusCodes.Status502BadGateway,
                "Empty upstream response",
                "Gemini returned an empty response.",
                "gemini_empty_response");
        }

        var json = responseText.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = json.IndexOf('\n');
            if (firstLineEnd >= 0)
            {
                json = json[(firstLineEnd + 1)..];
            }

            var closingFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0)
            {
                json = json[..closingFence];
            }
        }

        var objectStart = json.IndexOf('{');
        var objectEnd = json.LastIndexOf('}');
        if (objectStart < 0 || objectEnd <= objectStart)
        {
            throw new ApiException(
                StatusCodes.Status502BadGateway,
                "Invalid upstream response",
                "Gemini did not return a JSON object.",
                "gemini_invalid_json");
        }

        var analysis = JsonSerializer.Deserialize<GeminiAnalysis>(
            json[objectStart..(objectEnd + 1)],
            JsonOptions);

        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Summary))
        {
            throw new ApiException(
                StatusCodes.Status502BadGateway,
                "Incomplete upstream response",
                "Gemini returned JSON without a usable summary.",
                "gemini_incomplete_json");
        }

        var recommendations = (analysis.Recommendations ?? Array.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Take(8)
            .ToArray();

        var safetyNotes = (analysis.SafetyNotes ?? Array.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Take(8)
            .ToArray();

        return new GeminiAnalysis(
            analysis.Summary.Trim(),
            recommendations,
            safetyNotes,
            string.IsNullOrWhiteSpace(analysis.Confidence)
                ? "unknown"
                : analysis.Confidence.Trim());
    }
}
