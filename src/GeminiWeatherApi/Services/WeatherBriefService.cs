using System.Diagnostics;
using System.Text.Json;
using GeminiWeatherApi.Data;
using GeminiWeatherApi.Data.Entities;
using GeminiWeatherApi.Models;
using GeminiWeatherApi.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace GeminiWeatherApi.Services;

public sealed class WeatherBriefService(
    AppDbContext db,
    IDistributedCache cache,
    IOpenMeteoClient weatherClient,
    IGeminiClient geminiClient,
    IOptions<GeminiOptions> geminiOptions,
    ILogger<WeatherBriefService> logger) : IWeatherBriefService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<WeatherBriefResponse> CreateBriefAsync(
        Guid userId,
        WeatherBriefRequest request,
        CancellationToken cancellationToken)
    {
        var city = request.City.Trim();
        var question = string.IsNullOrWhiteSpace(request.Question)
            ? "What should I prepare for today?"
            : request.Question.Trim();
        var cacheKey = CacheKeyFactory.ForWeatherBrief(
            userId,
            city,
            question,
            geminiOptions.Value.Model);
        var stopwatch = Stopwatch.StartNew();

        var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedJson))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<CachedWeatherBrief>(
                    cachedJson,
                    JsonOptions);

                if (cached is not null)
                {
                    logger.LogInformation(
                        "Weather brief cache hit for user {UserId} and city {City}.",
                        userId,
                        city);
                    return await SaveRecordAsync(
                        userId,
                        city,
                        question,
                        cached.Location,
                        cached.Analysis,
                        cached.Model,
                        cacheHit: true,
                        stopwatch,
                        cancellationToken);
                }
            }
            catch (JsonException)
            {
                logger.LogWarning(
                    "Ignoring malformed weather cache entry for user {UserId}.",
                    userId);
            }
        }

        var location = await weatherClient.GetWeatherAsync(city, cancellationToken);
        var prompt = PromptBuilder.Build(location, question);
        var analysis = await geminiClient.AnalyzeAsync(prompt, cancellationToken);
        var model = geminiOptions.Value.Model;

        var cacheEntry = new CachedWeatherBrief(
            location,
            analysis,
            model,
            DateTimeOffset.UtcNow);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(cacheEntry, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            },
            cancellationToken);

        return await SaveRecordAsync(
            userId,
            location.Name,
            prompt,
            location,
            analysis,
            model,
            cacheHit: false,
            stopwatch,
            cancellationToken);
    }

    public async Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        return await db.AiRequestRecords
            .AsNoTracking()
            .Where(record => record.UserId == userId)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(safeLimit)
            .Select(record => new HistoryItem(
                record.Id,
                record.Topic,
                record.City,
                record.Model,
                record.CacheHit,
                record.DurationMs,
                record.CreatedAtUtc,
                record.ResultJson))
            .ToListAsync(cancellationToken);
    }

    private async Task<WeatherBriefResponse> SaveRecordAsync(
        Guid userId,
        string city,
        string prompt,
        WeatherLocation location,
        GeminiAnalysis analysis,
        string model,
        bool cacheHit,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var record = new AiRequestRecord
        {
            UserId = userId,
            Topic = "weather",
            City = city,
            Prompt = prompt,
            Model = model,
            ResultJson = JsonSerializer.Serialize(analysis, JsonOptions),
            CacheHit = cacheHit,
            DurationMs = (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds),
            CreatedAtUtc = createdAt
        };

        db.AiRequestRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return new WeatherBriefResponse(
            record.Id,
            cacheHit,
            location.Name,
            location.Country,
            location.Timezone,
            location.Current,
            analysis.Summary,
            analysis.Recommendations,
            analysis.SafetyNotes,
            analysis.Confidence,
            model,
            createdAt);
    }

    private sealed record CachedWeatherBrief(
        WeatherLocation Location,
        GeminiAnalysis Analysis,
        string Model,
        DateTimeOffset CreatedAtUtc);
}
