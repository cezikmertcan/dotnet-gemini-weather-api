namespace GeminiWeatherApi.Infrastructure;

public sealed class ApiException(
    int statusCode,
    string title,
    string detail,
    string? code = null) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string Detail { get; } = detail;
    public string? Code { get; } = code;
}
