namespace GeminiWeatherApi.Data.Entities;

public sealed class AiRequestRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string Topic { get; set; } = "weather";
    public string City { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
    public bool CacheHit { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
