namespace FinanceDashboardApi.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public List<MonthlySummaryDto> MonthlySummary { get; set; } = new();
    public List<CategoryExpenseDto> ExpensesByCategory { get; set; } = new();
}
