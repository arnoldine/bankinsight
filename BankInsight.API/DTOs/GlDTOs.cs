using System;
using System.Collections.Generic;

namespace BankInsight.API.DTOs;

public class CreateGlAccountRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public bool IsHeader { get; set; }
}

public class SeedChartOfAccountsRequest
{
    public string RegionCode { get; set; } = "GH";
}

public class SeedChartOfAccountsResponse
{
    public string RegionCode { get; set; } = string.Empty;
    public string StandardName { get; set; } = string.Empty;
    public int InsertedCount { get; set; }
    public int ExistingCount { get; set; }
    public int TotalStandardAccounts { get; set; }
    public List<string> InsertedCodes { get; set; } = new();
}

public class JournalLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class PostJournalEntryRequest
{
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string? PostedBy { get; set; }
    public List<JournalLineDto> Lines { get; set; } = new();
}

public class JournalEntryResponseDto
{
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string? PostedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<JournalLineResponseDto> Lines { get; set; } = new();
}

public class JournalLineResponseDto
{
    public int Id { get; set; }
    public string JournalId { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
