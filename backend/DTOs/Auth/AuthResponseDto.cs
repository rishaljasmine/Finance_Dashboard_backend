using System.Text.Json.Serialization;

namespace FinanceDashboardApi.DTOs.Auth;

public class AuthResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "";
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";

    // Never serialised: the session token travels only in the HttpOnly cookie
    // that AuthController sets, so JavaScript cannot read or store it.
    [JsonIgnore]
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
