using System.ComponentModel.DataAnnotations;

namespace FinanceDashboardApi.DTOs.Transactions;

public class TransactionCreateDto
{
    [Required, RegularExpression("^(income|expense)$")]
    public string Type { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string Category { get; set; } = "";

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    public DateOnly Date { get; set; }
}
