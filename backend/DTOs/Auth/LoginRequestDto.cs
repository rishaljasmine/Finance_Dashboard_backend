using System.ComponentModel.DataAnnotations;

namespace FinanceDashboardApi.DTOs.Auth;

public class LoginRequestDto
{
    [StringLength(100)]
    public string? Username { get; set; }

    [StringLength(320)]
    public string? Email { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = "";
}
