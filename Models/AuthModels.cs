using System.ComponentModel.DataAnnotations;

namespace FinanceDashboardApi.Models;

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = "";

    [Required, StringLength(320, MinimumLength = 3)]
    public string Email { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 8)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [StringLength(100)]
    public string? Username { get; set; }

    [StringLength(320)]
    public string? Email { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = "";
}

public class GoogleLoginRequest
{
    [Required, MinLength(1)]
    public string Credential { get; set; } = "";
}
