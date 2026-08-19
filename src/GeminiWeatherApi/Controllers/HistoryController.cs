using System.ComponentModel.DataAnnotations;
using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Models;
using GeminiWeatherApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeminiWeatherApi.Controllers;

[ApiController]
[Authorize]
[Route("api/history")]
public sealed class HistoryController(IWeatherBriefService weatherBriefService) : ControllerBase
{
    /// <summary>Returns the latest saved weather requests for the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<HistoryItem>>> GetHistory(
        [FromQuery, Range(1, 50)] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetRequiredUserId();
        return Ok(await weatherBriefService.GetHistoryAsync(
            userId,
            limit,
            cancellationToken));
    }
}
