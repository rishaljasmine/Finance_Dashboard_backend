using FinanceDashboardApi.Common;
using FinanceDashboardApi.DTOs.Dashboard;
using FinanceDashboardApi.Repositories.Interfaces;
using FinanceDashboardApi.Services.Interfaces;
using Npgsql;

namespace FinanceDashboardApi.Services;

public class DashboardService(ITransactionRepository repository) : IDashboardService
{
    public async Task<ServiceResult<DashboardSummaryDto>> GetDashboardAsync(long userId, int year)
    {
        try
        {
            var transactions = await repository.GetForYearAsync(userId, year);

            var monthlySummary = transactions
                .GroupBy(t => t.TransactionDate.Month)
                .Select(g => new MonthlySummaryDto
                {
                    Month = g.Key,
                    Income = g.Where(t => t.Type == "income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.Type == "expense").Sum(t => t.Amount)
                })
                .OrderBy(m => m.Month)
                .ToList();

            var expensesByCategory = transactions
                .Where(t => t.Type == "expense")
                .GroupBy(t => t.Category)
                .Select(g => new CategoryExpenseDto { Category = g.Key, Amount = g.Sum(t => t.Amount) })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var totalIncome = monthlySummary.Sum(m => m.Income);
            var totalExpense = monthlySummary.Sum(m => m.Expense);

            return ServiceResult<DashboardSummaryDto>.Ok(new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = totalIncome - totalExpense,
                MonthlySummary = monthlySummary,
                ExpensesByCategory = expensesByCategory
            });
        }
        catch (NpgsqlException)
        {
            return ServiceResult<DashboardSummaryDto>.Fail(503, "Could not read dashboard data.");
        }
    }
}
