using System;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class CreatePrivilegeLeaseRequest
{
    [Required]
    [MaxLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Permission { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ApprovedBy { get; set; } = string.Empty;

    public DateTime? StartsAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }
}

public class RevokePrivilegeLeaseRequest
{
    [Required]
    [MaxLength(50)]
    public string RevokedBy { get; set; } = string.Empty;
}

public class PrivilegeLeaseDto
{
    public string Id { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActive { get; set; }
}
