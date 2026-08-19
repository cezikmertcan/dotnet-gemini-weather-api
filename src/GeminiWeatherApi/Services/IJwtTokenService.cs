using GeminiWeatherApi.Data.Entities;

namespace GeminiWeatherApi.Services;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateToken(AppUser user);
}
