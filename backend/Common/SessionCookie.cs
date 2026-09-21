namespace FinanceDashboardApi.Common;

/// <summary>
/// The login session lives in an HttpOnly cookie so browser JavaScript can
/// never read it, and so the Angular SSR server can forward it to this API
/// when it renders a page on the user's behalf. This class is the single place
/// that knows the cookie's name, lifetime and attributes.
/// </summary>
public static class SessionCookie
{
    public const string Name = "fd_session";

    /// <summary>Also the token's server-enforced lifetime (see TokenService).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static void Append(HttpContext context, string token)
    {
        context.Response.Cookies.Append(Name, token, Options(context, DateTimeOffset.UtcNow.Add(Lifetime)));
    }

    public static void Delete(HttpContext context)
    {
        context.Response.Cookies.Delete(Name, Options(context, null));
    }

    private static CookieOptions Options(HttpContext context, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        // Lax: sent on same-site requests (Angular on localhost:4200 -> API on
        // localhost:8002 is same-site; ports don't matter) and on top-level
        // navigations, but never on cross-site sub-requests or form posts.
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps,
        Path = "/",
        Expires = expires,
        // Only needed in production when the Angular app and this API live on
        // different subdomains, e.g. COOKIE_DOMAIN=example.com.
        Domain = Environment.GetEnvironmentVariable("COOKIE_DOMAIN") is { Length: > 0 } domain ? domain : null
    };
}

/// <summary>Origins allowed to make cookie-authenticated state-changing requests.</summary>
public record TrustedOrigins(string[] Origins);
