using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class GroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Officer { get; set; }
    public string? MeetingDay { get; set; }
    public DateOnly? FormationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}

public class CreateGroupRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Officer must not exceed 50 characters")]
    public string? Officer { get; set; }

    [StringLength(20, ErrorMessage = "MeetingDay must not exceed 20 characters")]
    public string? MeetingDay { get; set; }

    public DateOnly? FormationDate { get; set; }

    [StringLength(50, ErrorMessage = "Status must not exceed 50 characters")]
    public string? Status { get; set; }
}

public class AddMemberRequest
{
    [Required(ErrorMessage = "CustomerId is required")]
    [StringLength(50, ErrorMessage = "CustomerId must not exceed 50 characters")]
    public string CustomerId { get; set; } = string.Empty;
}
