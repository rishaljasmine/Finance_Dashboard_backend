namespace FinanceDashboardApi.Entities;

public class Transaction
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Type { get; set; } = "";        // "income" | "expense" — enforced by DB CHECK constraint
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();
}
