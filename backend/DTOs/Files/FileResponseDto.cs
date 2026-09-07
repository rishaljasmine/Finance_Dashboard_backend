namespace FinanceDashboardApi.DTOs.Files;

public class FileResponseDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string UploadedAt { get; set; } = "";
}
