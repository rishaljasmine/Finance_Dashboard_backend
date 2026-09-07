namespace FinanceDashboardApi.Common;

public class AuthOptions
{
    public required string AuthSecret { get; init; }
    public string? GoogleClientId { get; init; }
}
