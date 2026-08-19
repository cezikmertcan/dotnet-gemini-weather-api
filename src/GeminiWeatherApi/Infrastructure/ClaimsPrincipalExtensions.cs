using System.Security.Claims;

namespace GeminiWeatherApi.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "The access token does not contain a valid user id.",
                "invalid_token");
    }
}
