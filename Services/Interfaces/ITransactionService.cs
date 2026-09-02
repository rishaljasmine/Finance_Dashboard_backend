using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Transactions;

namespace FinanceDashboardApi.Services.Interfaces;

public interface ITransactionService
{
    Task<ServiceResult<List<int>>> GetYearsAsync(long userId);
    Task<ServiceResult<List<TransactionResponseDto>>> GetForYearAsync(long userId, int year);
    Task<ServiceResult<TransactionResponseDto>> CreateAsync(long userId, TransactionCreateDto request);
}
