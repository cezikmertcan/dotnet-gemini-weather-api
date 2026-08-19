namespace GeminiWeatherApi.Models;

public sealed record HistoryItem(
    Guid Id,
    string Topic,
    string City,
    string Model,
    bool CacheHit,
    int DurationMs,
    DateTimeOffset CreatedAtUtc,
    string ResultJson);
