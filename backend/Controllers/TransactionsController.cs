using System.Security.Claims;
using FinanceDashboardApi.DTOs.Transactions;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceDashboardApi.Controllers;

[Authorize]
[ApiController]
[Route("/api/transactions")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("years")]
    public async Task<IActionResult> GetYears()
    {
        var result = await transactionService.GetYearsAsync(UserId);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }

    [HttpGet]
    public async Task<IActionResult> GetForYear([FromQuery] int year)
    {
        if (year is < 2000 or > 2100)
        {
            return Problem(statusCode: 422, detail: "year must be between 2000 and 2100.");
        }

        var result = await transactionService.GetForYearAsync(UserId, year);
        return result.Success
            ? Ok(result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransactionCreateDto request)
    {
        var result = await transactionService.CreateAsync(UserId, request);
        return result.Success
            ? StatusCode(201, result.Data)
            : Problem(statusCode: result.ErrorStatusCode, detail: result.ErrorMessage);
    }
}
