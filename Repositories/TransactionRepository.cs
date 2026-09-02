using FinanceDashboardApi.Data;
using FinanceDashboardApi.Entities;
using FinanceDashboardApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceDashboardApi.Repositories;

public class TransactionRepository(AppDbContext db) : ITransactionRepository
{
    public Task<List<int>> GetDistinctYearsAsync(long userId) =>
        db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => t.TransactionDate.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync();

    public Task<List<Transaction>> GetForYearAsync(long userId, int year) =>
        db.Transactions
            .Where(t => t.UserId == userId && t.TransactionDate.Year == year)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }
}
