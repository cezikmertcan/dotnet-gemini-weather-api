using System.Globalization;
using GeminiWeatherApi.Models;

namespace GeminiWeatherApi.Services;

public static class PromptBuilder
{
    public static string Build(WeatherLocation location, string question)
    {
        var current = location.Current;
        return string.Join(Environment.NewLine, new[]
        {
            "You are a concise weather assistant. Answer the user's question using only the live weather data below.",
            "Do not invent forecasts, measurements, alerts, or local facts that are not present in the data.",
            "Return only one JSON object with this exact shape:",
            "{",
            "  \"summary\": \"short answer\",",
            "  \"recommendations\": [\"practical suggestion\"],",
            "  \"safetyNotes\": [\"safety note or an empty array\"],",
            "  \"confidence\": \"high, medium, or low\"",
            "}",
            $"User question: {question}",
            $"Location: {location.Name}, {location.Country}",
            $"Timezone: {location.Timezone}",
            $"Temperature C: {current.TemperatureC.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"Feels like C: {current.ApparentTemperatureC.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"Relative humidity percent: {current.RelativeHumidityPercent.ToString("0", CultureInfo.InvariantCulture)}",
            $"Precipitation mm: {current.PrecipitationMm.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"Rain mm: {current.RainMm.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"Wind speed km/h: {current.WindSpeedKmh.ToString("0.0", CultureInfo.InvariantCulture)}",
            $"Weather code: {current.WeatherCode}",
            $"Weather description: {current.Description}",
            $"Daylight: {(current.IsDay ? "day" : "night")}"
        });
    }
}
