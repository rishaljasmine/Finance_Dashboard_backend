using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Transactions;
using FinanceDashboardApi.Entities;
using FinanceDashboardApi.Repositories.Interfaces;
using FinanceDashboardApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanceDashboardApi.Services;

public class TransactionService(ITransactionRepository repository) : ITransactionService
{
    public async Task<ServiceResult<List<int>>> GetYearsAsync(long userId)
    {
        try
        {
            var years = await repository.GetDistinctYearsAsync(userId);
            return ServiceResult<List<int>>.Ok(years);
        }
        catch (NpgsqlException)
        {
            return ServiceResult<List<int>>.Fail(503, "Could not read transaction years.");
        }
    }

    public async Task<ServiceResult<List<TransactionResponseDto>>> GetForYearAsync(long userId, int year)
    {
        try
        {
            var transactions = await repository.GetForYearAsync(userId, year);
            return ServiceResult<List<TransactionResponseDto>>.Ok(transactions.Select(ToDto).ToList());
        }
        catch (NpgsqlException)
        {
            return ServiceResult<List<TransactionResponseDto>>.Fail(503, "Could not read transactions.");
        }
    }

    public async Task<ServiceResult<TransactionResponseDto>> CreateAsync(long userId, TransactionCreateDto request)
    {
        var transaction = new Transaction
        {
            UserId = userId,
            Type = request.Type,
            Category = request.Category.Trim(),
            Amount = request.Amount,
            TransactionDate = request.Date,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await repository.CreateAsync(transaction);
            return ServiceResult<TransactionResponseDto>.Ok(ToDto(transaction));
        }
        catch (Exception ex) when (ex is NpgsqlException or DbUpdateException { InnerException: NpgsqlException })
        {
            return ServiceResult<TransactionResponseDto>.Fail(503, "Could not save transaction.");
        }
    }

    private static TransactionResponseDto ToDto(Transaction t) => new()
    {
        Id = t.Id,
        Type = t.Type,
        Category = t.Category,
        Amount = t.Amount,
        Date = t.TransactionDate.ToString("yyyy-MM-dd")
    };
}
