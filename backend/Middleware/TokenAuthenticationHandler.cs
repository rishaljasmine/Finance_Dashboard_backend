using System.Security.Claims;
using System.Text.Encodings.Web;
using FinanceDashboardApi.Common;
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
///
/// The token is accepted from either the HttpOnly session cookie (browsers and
/// the Angular SSR server forwarding that cookie) or an Authorization: Bearer
/// header (API tools). Cookies are sent by the browser automatically, so a
/// cookie-authenticated state-changing request is additionally CSRF-checked.
/// </summary>
public class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenService tokenService,
    TrustedOrigins trustedOrigins)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        var fromCookie = false;

        var header = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer "))
        {
            token = header["Bearer ".Length..].Trim();
        }
        else if (Request.Cookies.TryGetValue(SessionCookie.Name, out var cookieToken)
                 && !string.IsNullOrEmpty(cookieToken))
        {
            token = cookieToken;
            fromCookie = true;
        }

        if (token is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (fromCookie && !IsSafeMethod() && !IsTrustedOrigin())
        {
            return Task.FromResult(AuthenticateResult.Fail("Untrusted origin."));
        }

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

    private bool IsSafeMethod() =>
        HttpMethods.IsGet(Request.Method) || HttpMethods.IsHead(Request.Method) || HttpMethods.IsOptions(Request.Method);

    // Browsers always attach Origin to cross-origin POST/PUT/DELETE, so a
    // forged request from another site is rejected even if the cookie rides
    // along. A missing Origin means a non-browser client, which has no way to
    // borrow a victim's cookie in the first place.
    private bool IsTrustedOrigin()
    {
        var origin = Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        return trustedOrigins.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase)
               || string.Equals(origin, $"{Request.Scheme}://{Request.Host}", StringComparison.OrdinalIgnoreCase);
    }
}
