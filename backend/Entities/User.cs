namespace FinanceDashboardApi.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>Null for accounts created via Google sign-in only.</summary>
    public string? PasswordHash { get; set; }

    public string? GoogleSub { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
