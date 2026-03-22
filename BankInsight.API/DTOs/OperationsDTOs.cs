using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankInsight.API.DTOs;

public class EodStatusDto
{
    public string BusinessDate { get; set; } = string.Empty;
    public string Mode { get; set; } = "LIVE";
    public string Status { get; set; } = "OPEN";
    public int PendingTransactions { get; set; }
    public int DraftJournalEntries { get; set; }
    public int ActiveLoans { get; set; }
    public List<string> ManualSteps { get; set; } = new();
    public bool SchedulerEnabled { get; set; }
    public string SchedulerTimeUtc { get; set; } = "23:00";
    public string? LastSchedulerRunDate { get; set; }
    public string? NextScheduledRunUtc { get; set; }
}

public class RunEodStepRequest
{
    [Required]
    [StringLength(50)]
    public string StepId { get; set; } = string.Empty;

    public DateOnly? BusinessDate { get; set; }
}

public class EodStepResultDto
{
    public string StepId { get; set; } = string.Empty;
    public string Status { get; set; } = "SUCCESS";
    public string Message { get; set; } = string.Empty;
    public string BusinessDate { get; set; } = string.Empty;
    public object? Details { get; set; }
}
