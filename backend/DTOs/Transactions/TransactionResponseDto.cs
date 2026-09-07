using FinanceDashboardApi.DTOs.Files;

namespace FinanceDashboardApi.DTOs.Transactions;

public class TransactionResponseDto
{
    public long Id { get; set; }
    public string Type { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string Date { get; set; } = "";
    public List<FileResponseDto> Files { get; set; } = new();
}
