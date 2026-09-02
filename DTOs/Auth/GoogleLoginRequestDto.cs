using System.ComponentModel.DataAnnotations;

namespace FinanceDashboardApi.DTOs.Auth;

public class GoogleLoginRequestDto
{
    [Required, MinLength(1)]
    public string Credential { get; set; } = "";
}
