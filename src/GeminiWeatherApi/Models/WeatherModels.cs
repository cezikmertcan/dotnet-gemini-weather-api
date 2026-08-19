using System.ComponentModel.DataAnnotations;

namespace GeminiWeatherApi.Models;

public sealed class WeatherBriefRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string City { get; init; } = string.Empty;

    [StringLength(500)]
    public string Question { get; init; } = "What should I prepare for today?";
}

public sealed record WeatherLocation(
    string Name,
    string Country,
    string Timezone,
    double Latitude,
    double Longitude,
    WeatherSnapshot Current);

public sealed record WeatherSnapshot(
    double TemperatureC,
    double ApparentTemperatureC,
    double RelativeHumidityPercent,
    double PrecipitationMm,
    double RainMm,
    double WindSpeedKmh,
    int WeatherCode,
    string Description,
    bool IsDay);

public sealed record GeminiAnalysis(
    string Summary,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> SafetyNotes,
    string Confidence);

public sealed record WeatherBriefResponse(
    Guid RequestId,
    bool Cached,
    string City,
    string Country,
    string Timezone,
    WeatherSnapshot Current,
    string Summary,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> SafetyNotes,
    string Confidence,
    string Model,
    DateTimeOffset CreatedAtUtc);
