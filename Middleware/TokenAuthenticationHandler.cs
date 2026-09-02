using System.Security.Claims;
using System.Text.Encodings.Web;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FinanceDashboardApi.Middleware;

/// <summary>
/// Wires the app's opaque bearer token into ASP.NET Core's standard auth
/// pipeline, so controllers just use [Authorize] and User.Identity instead
/// of every handler manually re-parsing the Authorization header. This is
/// the biggest structural fix over the original: the same 4 lines of
/// "read header, validate, 401 if null" were copy-pasted into every
/// protected endpoint before.
/// </summary>
public class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenService tokenService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = header["Bearer ".Length..].Trim();
        var userId = tokenService.GetUserIdFromToken(token);

        if (userId is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token."));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Keep the same response shape ({ "detail": "..." }) the frontend
        // already expects from the old manual 401s.
        Response.StatusCode = 401;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(new { detail = "Authentication required." });
    }
}
