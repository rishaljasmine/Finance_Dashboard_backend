namespace FinanceDashboardApi.Middleware;

/// <summary>
/// Catches unhandled exceptions so they still flow through CORS-tagged
/// responses instead of surfacing as an opaque browser "CORS error" (an
/// exception that bypasses the pipeline never gets CORS headers attached).
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception error)
        {
            logger.LogError(error, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { detail = "Internal server error." });
            }
        }
    }
}
