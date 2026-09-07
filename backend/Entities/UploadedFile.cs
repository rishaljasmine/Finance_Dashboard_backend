namespace FinanceDashboardApi.Entities;

public class UploadedFile
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long TransactionId { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";   // Guid-based name on disk — never the original, to avoid collisions and path tricks.
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public User? User { get; set; }
    public Transaction? Transaction { get; set; }
}
