using System.Text.RegularExpressions;
using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Auth;
using FinanceDashboardApi.DTOs.Users;
using FinanceDashboardApi.Entities;
using FinanceDashboardApi.Repositories.Interfaces;
using FinanceDashboardApi.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanceDashboardApi.Services;

public class AuthService(IUserRepository users, ITokenService tokens, AuthOptions authOptions, ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await users.UsernameExistsAsync(username))
            return ServiceResult<RegisterResponseDto>.Fail(409, "Username already exists.");

        if (await users.EmailExistsAsync(email))
            return ServiceResult<RegisterResponseDto>.Fail(409, "Email already exists.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await users.CreateAsync(user);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Defense in depth: the precheck above has a narrow TOCTOU window
            // under concurrent signups. The DB's unique indexes are the real
            // guarantee; a race here still resolves to a clean 409 instead of
            // an unhandled 500.
            return ServiceResult<RegisterResponseDto>.Fail(409, "Username or email already exists.");
        }

        return ServiceResult<RegisterResponseDto>.Ok(new RegisterResponseDto
        {
            Message = "Account created successfully.",
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        });
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var username = request.Username?.Trim() ?? "";
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";

        if (username.Length == 0 && email.Length == 0)
            return ServiceResult<AuthResponseDto>.Fail(400, "Username or email is required.");

        var user = email.Length > 0
            ? await users.FindByEmailAsync(email)
            : await users.FindByUsernameAsync(username);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return ServiceResult<AuthResponseDto>.Fail(401, "Invalid username/email or password.");

        return ServiceResult<AuthResponseDto>.Ok(BuildAuthResponse(user, "Login successful."));
    }

    public async Task<ServiceResult<AuthResponseDto>> GoogleLoginAsync(string credential)
    {
        if (string.IsNullOrEmpty(authOptions.GoogleClientId))
            return ServiceResult<AuthResponseDto>.Fail(503, "Google sign-in is not configured.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(credential, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [authOptions.GoogleClientId]
            });
        }
        catch (Exception ex)
        {
            // Covers both a bad/expired credential and transient failures
            // fetching Google's public certs — either way, a 401 rather than
            // a 500 is the right signal to the client.
            logger.LogWarning(ex, "Google token verification failed");
            return ServiceResult<AuthResponseDto>.Fail(401, "Invalid Google credential.");
        }

        if (!payload.EmailVerified)
            return ServiceResult<AuthResponseDto>.Fail(401, "Google account email is not verified.");

        var googleSub = payload.Subject;
        var email = payload.Email.Trim().ToLowerInvariant();
        var name = payload.Name ?? email.Split('@')[0];

        var user = await users.FindByGoogleSubAsync(googleSub);

        if (user is null)
        {
            user = await users.FindByEmailAsync(email);

            if (user is not null)
            {
                await users.LinkGoogleSubAsync(user.Id, googleSub);
            }
            else
            {
                var uniqueUsername = await GenerateUniqueUsernameAsync(name);
                user = new User
                {
                    Username = uniqueUsername,
                    Email = email,
                    PasswordHash = null,
                    GoogleSub = googleSub,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await users.CreateAsync(user);
            }
        }

        return ServiceResult<AuthResponseDto>.Ok(BuildAuthResponse(user, "Login successful."));
    }

    public async Task<ServiceResult<UserResponseDto>> GetCurrentUserAsync(long userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserResponseDto>.Fail(404, "User not found.");

        return ServiceResult<UserResponseDto>.Ok(new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        });
    }

    private AuthResponseDto BuildAuthResponse(User user, string message) => new()
    {
        Message = message,
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Token = tokens.CreateToken(user.Id)
    };

    private async Task<string> GenerateUniqueUsernameAsync(string baseName)
    {
        var cleaned = Regex.Replace(baseName, "[^a-zA-Z0-9]", "");
        if (string.IsNullOrEmpty(cleaned)) cleaned = "user";

        var candidate = cleaned;
        var suffix = 1;

        while (await users.UsernameExistsAsync(candidate))
        {
            suffix += 1;
            candidate = $"{cleaned}{suffix}";
        }

        return candidate;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };
}
