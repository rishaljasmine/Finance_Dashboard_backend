using System.Security.Claims;
using FinanceDashboardApi.DTOs.Auth;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceDashboardApi.Controllers;

[ApiController]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("/api/register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await authService.RegisterAsync(request);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }

    // Kept as two routes to the same action for backward compatibility with
    // existing frontend clients calling either path.
    [HttpPost("/api/login")]
    [HttpPost("/api/auth/login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await authService.LoginAsync(request);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }

    [HttpPost("/api/auth/google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequestDto request)
    {
        var result = await authService.GoogleLoginAsync(request.Credential);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }

    [Authorize]
    [HttpGet("/api/me")]
    public async Task<IActionResult> Me()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.GetCurrentUserAsync(userId);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }
}
