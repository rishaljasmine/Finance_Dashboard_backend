using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Files;
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

    public async Task<ServiceResult<List<TransactionResponseDto>>> GetForYearAsync(
        long userId,
        int year,
        string? type = null,
        string? category = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? sortBy = null,
        string? sortOrder = null)
    {
        try
        {
            var transactions = await repository.GetForYearAsync(userId, year);
            IEnumerable<TransactionResponseDto> dtos = transactions.Select(ToDto);

            if (!string.IsNullOrWhiteSpace(type))
            {
                dtos = dtos.Where(t => string.Equals(t.Type, type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                dtos = dtos.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (minAmount.HasValue)
            {
                dtos = dtos.Where(t => t.Amount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                dtos = dtos.Where(t => t.Amount <= maxAmount.Value);
            }

            if (dateFrom.HasValue)
            {
                dtos = dtos.Where(t => DateOnly.Parse(t.Date) >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                dtos = dtos.Where(t => DateOnly.Parse(t.Date) <= dateTo.Value);
            }

            var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
            dtos = sortBy?.ToLowerInvariant() switch
            {
                "amount" => descending ? dtos.OrderByDescending(t => t.Amount) : dtos.OrderBy(t => t.Amount),
                "category" => descending ? dtos.OrderByDescending(t => t.Category) : dtos.OrderBy(t => t.Category),
                "type" => descending ? dtos.OrderByDescending(t => t.Type) : dtos.OrderBy(t => t.Type),
                "date" => descending ? dtos.OrderByDescending(t => t.Date) : dtos.OrderBy(t => t.Date),
                _ => dtos
            };

            return ServiceResult<List<TransactionResponseDto>>.Ok(dtos.ToList());
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
        Date = t.TransactionDate.ToString("yyyy-MM-dd"),
        Files = t.Files.Select(f => new FileResponseDto
        {
            Id = f.Id,
            FileName = f.OriginalFileName,
            ContentType = f.ContentType,
            SizeBytes = f.SizeBytes,
            UploadedAt = f.UploadedAt.ToString("O")
        }).ToList()
    };
}
