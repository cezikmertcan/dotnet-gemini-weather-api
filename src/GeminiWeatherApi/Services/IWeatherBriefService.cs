using GeminiWeatherApi.Models;

namespace GeminiWeatherApi.Services;

public interface IWeatherBriefService
{
    Task<WeatherBriefResponse> CreateBriefAsync(
        Guid userId,
        WeatherBriefRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken);
}
