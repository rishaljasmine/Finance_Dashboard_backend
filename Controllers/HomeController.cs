using FinanceDashboardApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceDashboardApi.Controllers;

[ApiController]
public class HomeController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Root() => Ok(new { message = "Finance Dashboard API is running" });

    [HttpGet("/api/health")]
    public async Task<IActionResult> Health()
    {
        if (string.IsNullOrEmpty(config["DATABASE_URL"]))
        {
            return Ok(new { status = "ok", database = "not configured" });
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
            return Ok(new { status = "ok", database = "connected" });
        }
        catch (Exception)
        {
            return Problem(statusCode: 503, detail: "Database connection failed");
        }
    }
}
