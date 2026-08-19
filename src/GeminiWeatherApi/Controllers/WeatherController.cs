using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Models;
using GeminiWeatherApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeminiWeatherApi.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai")]
[Route("api/weather")]
public sealed class WeatherController(IWeatherBriefService weatherBriefService) : ControllerBase
{
    /// <summary>Combines live Open-Meteo measurements with a structured Gemini analysis.</summary>
    [HttpPost("brief")]
    [ProducesResponseType(typeof(WeatherBriefResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<WeatherBriefResponse>> CreateBrief(
        WeatherBriefRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        return Ok(await weatherBriefService.CreateBriefAsync(
            userId,
            request,
            cancellationToken));
    }
}
