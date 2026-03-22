using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("secondary_phone")]
    [MaxLength(20)]
    public string? SecondaryPhone { get; set; }

    [Column("digital_address")]
    [MaxLength(100)]
    public string? DigitalAddress { get; set; }

    [Column("postal_address")]
    public string? PostalAddress { get; set; }

    [Column("kyc_level")]
    [MaxLength(20)]
    public string KycLevel { get; set; } = "Tier 1";

    [Column("risk_rating")]
    [MaxLength(20)]
    public string RiskRating { get; set; } = "Low";

    // Individual Specific
    [Column("gender")]
    [MaxLength(10)]
    public string? Gender { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("ghana_card")]
    [MaxLength(50)]
    public string? GhanaCard { get; set; }

    [Column("nationality")]
    [MaxLength(50)]
    public string? Nationality { get; set; }

    [Column("marital_status")]
    [MaxLength(20)]
    public string? MaritalStatus { get; set; }

    [Column("spouse_name")]
    [MaxLength(100)]
    public string? SpouseName { get; set; }

    [Column("employer")]
    [MaxLength(100)]
    public string? Employer { get; set; }

    [Column("job_title")]
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [Column("ssnit_no")]
    [MaxLength(50)]
    public string? SsnitNo { get; set; }

    // Corporate Specific
    [Column("business_reg_no")]
    [MaxLength(50)]
    public string? BusinessRegNo { get; set; }

    [Column("registration_date")]
    public DateOnly? RegistrationDate { get; set; }

    [Column("tin")]
    [MaxLength(50)]
    public string? Tin { get; set; }

    [Column("sector")]
    [MaxLength(50)]
    public string? Sector { get; set; }

    [Column("legal_form")]
    [MaxLength(50)]
    public string? LegalForm { get; set; }

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = "BR001";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
