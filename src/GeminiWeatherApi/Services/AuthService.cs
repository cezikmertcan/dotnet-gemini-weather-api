using GeminiWeatherApi.Data;
using GeminiWeatherApi.Data.Entities;
using GeminiWeatherApi.Infrastructure;
using GeminiWeatherApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GeminiWeatherApi.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                "Email already registered",
                "An account with this email already exists.",
                "email_exists");
        }

        var user = new AppUser
        {
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                "Invalid credentials",
                "The email or password is incorrect.",
                "invalid_credentials");
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(AppUser user)
    {
        var token = jwtTokenService.CreateToken(user);
        return new AuthResponse(
            token.Token,
            token.ExpiresAtUtc,
            new UserResponse(user.Id, user.Email, user.DisplayName, user.CreatedAtUtc));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
