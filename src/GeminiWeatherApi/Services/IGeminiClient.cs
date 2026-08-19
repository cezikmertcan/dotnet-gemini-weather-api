using GeminiWeatherApi.Models;

namespace GeminiWeatherApi.Services;

public interface IGeminiClient
{
    Task<GeminiAnalysis> AnalyzeAsync(string prompt, CancellationToken cancellationToken);
}
