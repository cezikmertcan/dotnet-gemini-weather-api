using System.Security.Cryptography;
using System.Text;

namespace GeminiWeatherApi.Services;

public static class CacheKeyFactory
{
    public static string ForWeatherBrief(Guid userId, string city, string question, string model)
    {
        var canonical = string.Join(
            "|",
            userId.ToString("N"),
            city.Trim().ToLowerInvariant(),
            question.Trim(),
            model.Trim().ToLowerInvariant());

        var digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        return $"weather-brief:{userId:N}:{digest}";
    }
}
