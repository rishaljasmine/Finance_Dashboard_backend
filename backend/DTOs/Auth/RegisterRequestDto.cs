using System.ComponentModel.DataAnnotations;

namespace FinanceDashboardApi.DTOs.Auth;

public class RegisterRequestDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = "";

    [Required, StringLength(320, MinimumLength = 3)]
    public string Email { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 8)]
    public string Password { get; set; } = "";
}
