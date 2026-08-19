namespace GeminiWeatherApi.Options;

public sealed class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "GeminiWeatherApi";
    public string Audience { get; set; } = "GeminiWeatherApi.Client";
    public int AccessTokenMinutes { get; set; } = 60;
}
