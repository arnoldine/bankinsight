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

    public bool IsConfidential { get; set; }

    [StringLength(50, ErrorMessage = "OwnerStaffId must not exceed 50 characters")]
    public string? OwnerStaffId { get; set; }
}

public class AccountListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string BranchId { get; set; } = "BR001";
    public string Type { get; set; } = "SAVINGS";
    public string Currency { get; set; } = "GHS";
    public decimal Balance { get; set; }
    public decimal LienAmount { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? ProductCode { get; set; }
    public string? LastTransDate { get; set; }
    public string? CreatedAt { get; set; }
}
