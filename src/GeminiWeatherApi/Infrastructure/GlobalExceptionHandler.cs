using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeminiWeatherApi.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (statusCode, title, detail, code) = exception switch
        {
            ApiException apiException =>
                (apiException.StatusCode, apiException.Title, apiException.Detail, apiException.Code),
            BadHttpRequestException badRequest =>
                (StatusCodes.Status400BadRequest, "Bad request", badRequest.Message, "bad_request"),
            DbUpdateException =>
                (StatusCodes.Status409Conflict, "Data conflict",
                    "The request could not be saved because it conflicts with existing data.",
                    "data_conflict"),
            JsonException =>
                (StatusCodes.Status502BadGateway, "Invalid upstream response",
                    "An upstream service returned an unreadable response.",
                    "upstream_invalid_json"),
            _ =>
                (StatusCodes.Status500InternalServerError, "Unexpected error",
                    "The server could not complete the request.", "internal_error")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Request {TraceId} failed with status {StatusCode}.",
                httpContext.TraceIdentifier, statusCode);
        }
        else
        {
            logger.LogWarning("Request {TraceId} returned {StatusCode}: {Code}.",
                httpContext.TraceIdentifier, statusCode, code ?? "request_error");
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(code))
        {
            problem.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
