using System.Security.Claims;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceDashboardApi.Controllers;

[Authorize]
[ApiController]
[Route("/api/dashboard")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int year)
    {
        if (year is < 2000 or > 2100)
        {
            return Problem(statusCode: 422, detail: "year must be between 2000 and 2100.");
        }

        var result = await dashboardService.GetDashboardAsync(UserId, year);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }
}
