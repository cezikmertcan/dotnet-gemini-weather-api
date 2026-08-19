using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Models;

namespace GeminiWeatherApi.Services;

public sealed class OpenMeteoClient(
    HttpClient httpClient,
    ILogger<OpenMeteoClient> logger) : IOpenMeteoClient
{
    public async Task<WeatherLocation> GetWeatherAsync(
        string city,
        CancellationToken cancellationToken)
    {
        try
        {
            var geocodingUrl =
                "https://geocoding-api.open-meteo.com/v1/search" +
                $"?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";

            var geocoding = await httpClient.GetFromJsonAsync<GeocodingResponse>(
                geocodingUrl,
                cancellationToken);
            var location = geocoding?.Results?.FirstOrDefault();

            if (location is null)
            {
                throw new ApiException(
                    StatusCodes.Status404NotFound,
                    "City not found",
                    $"Open-Meteo could not find a location matching '{city}'.",
                    "city_not_found");
            }

            var forecastUrl =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&longitude={location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                "&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day," +
                "precipitation,rain,weather_code,wind_speed_10m&timezone=auto";

            var forecast = await httpClient.GetFromJsonAsync<ForecastResponse>(
                forecastUrl,
                cancellationToken);
            if (forecast?.Current is null)
            {
                throw new ApiException(
                    StatusCodes.Status502BadGateway,
                    "Weather provider unavailable",
                    "Open-Meteo did not return current weather data.",
                    "weather_data_missing");
            }

            var current = forecast.Current;
            var weatherCode = current.WeatherCode ?? -1;
            return new WeatherLocation(
                location.Name ?? city,
                location.Country ?? string.Empty,
                forecast.Timezone ?? location.Timezone ?? "auto",
                location.Latitude,
                location.Longitude,
                new WeatherSnapshot(
                    current.TemperatureC ?? 0,
                    current.ApparentTemperatureC ?? 0,
                    current.RelativeHumidityPercent ?? 0,
                    current.PrecipitationMm ?? 0,
                    current.RainMm ?? 0,
                    current.WindSpeedKmh ?? 0,
                    weatherCode,
                    DescribeWeatherCode(weatherCode),
                    (current.IsDay ?? 1) == 1));
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Open-Meteo request failed for city {City}.", city);
            throw new ApiException(
                StatusCodes.Status502BadGateway,
                "Weather provider unavailable",
                "Open-Meteo could not be reached right now.",
                "weather_provider_unavailable");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Open-Meteo request timed out for city {City}.", city);
            throw new ApiException(
                StatusCodes.Status504GatewayTimeout,
                "Weather provider timeout",
                "Open-Meteo did not respond in time.",
                "weather_provider_timeout");
        }
    }

    private static string DescribeWeatherCode(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        >= 51 and <= 57 => "Drizzle",
        >= 61 and <= 67 => "Rain",
        >= 71 and <= 77 => "Snow",
        >= 80 and <= 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Unknown conditions"
    };

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingLocation>? Results { get; init; }
    }

    private sealed class GeocodingLocation
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; init; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; init; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("timezone")]
        public string? Timezone { get; init; }

        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; init; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double? TemperatureC { get; init; }

        [JsonPropertyName("apparent_temperature")]
        public double? ApparentTemperatureC { get; init; }

        [JsonPropertyName("relative_humidity_2m")]
        public double? RelativeHumidityPercent { get; init; }

        [JsonPropertyName("precipitation")]
        public double? PrecipitationMm { get; init; }

        [JsonPropertyName("rain")]
        public double? RainMm { get; init; }

        [JsonPropertyName("weather_code")]
        public int? WeatherCode { get; init; }

        [JsonPropertyName("wind_speed_10m")]
        public double? WindSpeedKmh { get; init; }

        [JsonPropertyName("is_day")]
        public int? IsDay { get; init; }
    }
}
