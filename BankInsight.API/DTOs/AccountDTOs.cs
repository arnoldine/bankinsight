using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreateAccountRequest
{
    [Required(ErrorMessage = "CustomerId is required")]
    [StringLength(50, ErrorMessage = "CustomerId must not exceed 50 characters")]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "BranchId must not exceed 50 characters")]
    public string? BranchId { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [StringLength(50, ErrorMessage = "Type must not exceed 50 characters")]
    public string Type { get; set; } = string.Empty;

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string? Currency { get; set; }

    [StringLength(50, ErrorMessage = "ProductCode must not exceed 50 characters")]
    public string? ProductCode { get; set; }
}
