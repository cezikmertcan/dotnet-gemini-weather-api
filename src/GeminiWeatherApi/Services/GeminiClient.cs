using System.Text.Json;
using System.Text.Json.Serialization;
using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Models;
using GeminiWeatherApi.Options;
using Microsoft.Extensions.Options;

namespace GeminiWeatherApi.Services;

public sealed class GeminiClient(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiClient> logger) : IGeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<GeminiAnalysis> AnalyzeAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var endpoint =
            $"v1beta/models/{Uri.EscapeDataString(settings.Model)}:generateContent" +
            $"?key={Uri.EscapeDataString(settings.ApiKey)}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        summary = new { type = "STRING" },
                        recommendations = new
                        {
                            type = "ARRAY",
                            items = new { type = "STRING" }
                        },
                        safetyNotes = new
                        {
                            type = "ARRAY",
                            items = new { type = "STRING" }
                        },
                        confidence = new { type = "STRING" }
                    },
                    required = new[] { "summary", "recommendations", "safetyNotes", "confidence" }
                },
                temperature = 0.2,
                maxOutputTokens = 800
            }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                endpoint,
                payload,
                JsonOptions,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Gemini returned HTTP {StatusCode}; response body intentionally omitted.",
                    (int)response.StatusCode);

                var detail = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    ? "Gemini rate limit reached. Please retry later."
                    : "Gemini could not complete the analysis.";

                throw new ApiException(
                    StatusCodes.Status502BadGateway,
                    "Gemini request failed",
                    detail,
                    "gemini_request_failed");
            }

            var upstream = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(
                responseBody,
                JsonOptions);

            var generatedText = upstream?.Candidates?
                .SelectMany(candidate => candidate.Content?.Parts ?? Enumerable.Empty<GeminiPart>())
                .Select(part => part.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            return GeminiResponseParser.Parse(generatedText ?? string.Empty);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Gemini request could not reach the provider.");
            throw new ApiException(
                StatusCodes.Status502BadGateway,
                "Gemini unavailable",
                "Gemini could not be reached right now.",
                "gemini_provider_unavailable");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Gemini request timed out.");
            throw new ApiException(
                StatusCodes.Status504GatewayTimeout,
                "Gemini timeout",
                "Gemini did not respond in time.",
                "gemini_provider_timeout");
        }
    }

    private sealed class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; init; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; init; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; init; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }
}
