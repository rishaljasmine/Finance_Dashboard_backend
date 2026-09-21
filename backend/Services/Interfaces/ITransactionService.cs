using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Transactions;

namespace FinanceDashboardApi.Services.Interfaces;

public interface ITransactionService
{
    Task<ServiceResult<List<int>>> GetYearsAsync(long userId);
    Task<ServiceResult<List<TransactionResponseDto>>> GetForYearAsync(
        long userId,
        int year,
        string? type = null,
        string? category = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? sortBy = null,
        string? sortOrder = null);
    Task<ServiceResult<TransactionResponseDto>> CreateAsync(long userId, TransactionCreateDto request);
}
