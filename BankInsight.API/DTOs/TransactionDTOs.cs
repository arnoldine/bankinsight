using System;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreateTransactionRequest
{
    [Required(ErrorMessage = "AccountId is required")]
    [StringLength(50, ErrorMessage = "AccountId must not exceed 50 characters")]
    public string AccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is required")]
    [StringLength(50, ErrorMessage = "Type must not exceed 50 characters")]
    public string Type { get; set; } = string.Empty;

    [Range(0.01, 999999999.99, ErrorMessage = "Amount must be between 0.01 and 999999999.99")]
    public decimal Amount { get; set; }

    [StringLength(500, ErrorMessage = "Narration must not exceed 500 characters")]
    public string? Narration { get; set; }

    [StringLength(50, ErrorMessage = "TellerId must not exceed 50 characters")]
    public string? TellerId { get; set; }

    [StringLength(100, ErrorMessage = "ClientReference must not exceed 100 characters")]
    public string? ClientReference { get; set; }
}
