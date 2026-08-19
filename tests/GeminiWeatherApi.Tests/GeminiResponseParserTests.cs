using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Services;
using Microsoft.AspNetCore.Http;

namespace GeminiWeatherApi.Tests;

public sealed class GeminiResponseParserTests
{
    [Fact]
    public void Parse_ReturnsStructuredAnalysis()
    {
        const string json = """
            {
              "summary": "Take a light jacket.",
              "recommendations": ["Carry water.", "Use sunscreen."],
              "safetyNotes": ["Check the wind near the coast."],
              "confidence": "high"
            }
            """;

        var result = GeminiResponseParser.Parse(json);

        Assert.Equal("Take a light jacket.", result.Summary);
        Assert.Equal(2, result.Recommendations.Count);
        Assert.Single(result.SafetyNotes);
        Assert.Equal("high", result.Confidence);
    }

    [Fact]
    public void Parse_RemovesMarkdownFenceAndIgnoresExtraText()
    {
        const string fence = "\u0060\u0060\u0060";
        var response = $"{fence}json\n{{\"summary\":\"Clear skies.\",\"recommendations\":[],\"safetyNotes\":[],\"confidence\":\"medium\"}}\n{fence}";

        var result = GeminiResponseParser.Parse(response);

        Assert.Equal("Clear skies.", result.Summary);
        Assert.Equal("medium", result.Confidence);
    }

    [Fact]
    public void Parse_ThrowsForMissingSummary()
    {
        const string json = """{"recommendations":[],"safetyNotes":[],"confidence":"low"}""";

        var exception = Assert.Throws<ApiException>(() => GeminiResponseParser.Parse(json));

        Assert.Equal(StatusCodes.Status502BadGateway, exception.StatusCode);
        Assert.Equal("gemini_incomplete_json", exception.Code);
    }

    [Fact]
    public void CacheKey_ChangesWhenModelChanges()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = CacheKeyFactory.ForWeatherBrief(userId, "Istanbul", "Do I need a jacket?", "gemini-a");
        var second = CacheKeyFactory.ForWeatherBrief(userId, "Istanbul", "Do I need a jacket?", "gemini-b");

        Assert.NotEqual(first, second);
        Assert.StartsWith("weather-brief:", first);
    }
}
