using FinanceDashboardApi.Entities;

namespace FinanceDashboardApi.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<List<int>> GetDistinctYearsAsync(long userId);
    Task<List<Transaction>> GetForYearAsync(long userId, int year);
    Task<Transaction?> GetByIdAsync(long id, long userId);
    Task<Transaction> CreateAsync(Transaction transaction);
}
