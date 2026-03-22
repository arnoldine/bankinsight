using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankInsight.API.Entities;

[Table("groups")]
public class Group
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("branch_id")]
    [MaxLength(50)]
    public string BranchId { get; set; } = "BR001";

    [Column("center_id")]
    [MaxLength(50)]
    public string? CenterId { get; set; }

    [ForeignKey(nameof(CenterId))]
    public LendingCenter? Center { get; set; }

    [Column("group_code")]
    [MaxLength(50)]
    public string? GroupCode { get; set; }

    [Column("officer_id")]
    [MaxLength(50)]
    public string? OfficerId { get; set; }

    [ForeignKey(nameof(OfficerId))]
    public Staff? Officer { get; set; }

    [Column("meeting_day")]
    [MaxLength(20)]
    public string? MeetingDay { get; set; }

    [Column("meeting_day_of_week")]
    [MaxLength(20)]
    public string? MeetingDayOfWeek { get; set; }

    [Column("meeting_frequency")]
    [MaxLength(20)]
    public string MeetingFrequency { get; set; } = "Weekly";

    [Column("meeting_location")]
    [MaxLength(200)]
    public string? MeetingLocation { get; set; }

    [Column("assigned_officer_id")]
    [MaxLength(50)]
    public string? AssignedOfficerId { get; set; }

    [ForeignKey(nameof(AssignedOfficerId))]
    public Staff? AssignedOfficer { get; set; }

    [Column("chairperson_customer_id")]
    [MaxLength(50)]
    public string? ChairpersonCustomerId { get; set; }

    [Column("secretary_customer_id")]
    [MaxLength(50)]
    public string? SecretaryCustomerId { get; set; }

    [Column("treasurer_customer_id")]
    [MaxLength(50)]
    public string? TreasurerCustomerId { get; set; }

    [Column("formation_date")]
    public DateOnly? FormationDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("is_joint_liability_enabled")]
    public bool IsJointLiabilityEnabled { get; set; }

    [Column("max_members")]
    public int? MaxMembers { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<GroupLoanApplication> Applications { get; set; } = new List<GroupLoanApplication>();
    public ICollection<GroupMeeting> Meetings { get; set; } = new List<GroupMeeting>();
}

[Table("group_members")]
public class GroupMember
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    [MaxLength(50)]
    public string GroupId { get; set; } = string.Empty;

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [Column("customer_id")]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Column("member_no")]
    [MaxLength(30)]
    public string? MemberNo { get; set; }

    [Column("join_date")]
    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column("exit_date")]
    public DateOnly? ExitDate { get; set; }

    [Column("member_role")]
    [MaxLength(30)]
    public string MemberRole { get; set; } = "MEMBER";

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ACTIVE";

    [Column("is_founding_member")]
    public bool IsFoundingMember { get; set; }

    [Column("compulsory_savings_account_id")]
    [MaxLength(50)]
    public string? CompulsorySavingsAccountId { get; set; }

    [Column("voluntary_savings_account_id")]
    [MaxLength(50)]
    public string? VoluntarySavingsAccountId { get; set; }

    [Column("kyc_status")]
    [MaxLength(20)]
    public string KycStatus { get; set; } = "PENDING";

    [Column("is_eligible_for_loan")]
    public bool IsEligibleForLoan { get; set; }

    [Column("current_loan_cycle")]
    public int CurrentLoanCycle { get; set; }

    [Column("current_exposure")]
    public decimal CurrentExposure { get; set; }

    [Column("arrears_flag")]
    public bool ArrearsFlag { get; set; }

    [Column("share_contribution")]
    public decimal ShareContribution { get; set; }

    [Column("guarantor_indicator")]
    public bool GuarantorIndicator { get; set; }

    [Column("social_collateral_notes")]
    [MaxLength(500)]
    public string? SocialCollateralNotes { get; set; }
}
