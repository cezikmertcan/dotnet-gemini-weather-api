using GeminiWeatherApi.Models;

namespace GeminiWeatherApi.Services;

public interface IOpenMeteoClient
{
    Task<WeatherLocation> GetWeatherAsync(string city, CancellationToken cancellationToken);
}
