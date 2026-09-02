namespace FinanceDashboardApi.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "";
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
}

public class RegisterResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "";
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
}
