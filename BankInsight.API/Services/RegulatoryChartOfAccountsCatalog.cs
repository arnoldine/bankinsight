using System.Collections.Generic;
using System.Linq;
using BankInsight.API.Entities;

namespace BankInsight.API.Services;

/// <summary>
/// Baseline chart of accounts aligned to a Ghana / Bank of Ghana style microfinance ledger structure.
/// This is an inference-based starter set, not a verbatim regulatory publication.
/// </summary>
public static class RegulatoryChartOfAccountsCatalog
{
    public const string GhanaRegionCode = "GH";
    public const string GhanaStandardName = "Bank of Ghana Baseline Chart of Accounts";

    private static readonly IReadOnlyList<GlAccount> GhanaAccounts = new List<GlAccount>
    {
        new() { Code = "10000", Name = "Assets", Category = "ASSET", Currency = "GHS", IsHeader = true },
        new() { Code = "10100", Name = "Cash On Hand", Category = "ASSET", Currency = "GHS" },
        new() { Code = "10200", Name = "Cash At Bank", Category = "ASSET", Currency = "GHS" },
        new() { Code = "10300", Name = "Balances With Other Financial Institutions", Category = "ASSET", Currency = "GHS" },
        new() { Code = "11000", Name = "Investment Securities", Category = "ASSET", Currency = "GHS", IsHeader = true },
        new() { Code = "11100", Name = "Treasury Bills", Category = "ASSET", Currency = "GHS" },
        new() { Code = "11200", Name = "Fixed Deposits Placed", Category = "ASSET", Currency = "GHS" },
        new() { Code = "15000", Name = "Loans And Receivables", Category = "ASSET", Currency = "GHS", IsHeader = true },
        new() { Code = "15100", Name = "Loan Portfolio", Category = "ASSET", Currency = "GHS" },
        new() { Code = "15150", Name = "Interest Receivable On Loans", Category = "ASSET", Currency = "GHS" },
        new() { Code = "15160", Name = "Penalty Receivable On Loans", Category = "ASSET", Currency = "GHS" },
        new() { Code = "15900", Name = "Loan Loss Reserve", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "17000", Name = "Property And Equipment", Category = "ASSET", Currency = "GHS", IsHeader = true },
        new() { Code = "17100", Name = "Furniture And Equipment", Category = "ASSET", Currency = "GHS" },
        new() { Code = "20000", Name = "Liabilities", Category = "LIABILITY", Currency = "GHS", IsHeader = true },
        new() { Code = "20100", Name = "Savings Deposits", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "20200", Name = "Current Deposits", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "20300", Name = "Fixed Deposits Payable", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "21000", Name = "Borrowings", Category = "LIABILITY", Currency = "GHS", IsHeader = true },
        new() { Code = "21100", Name = "Bank Borrowings", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "22000", Name = "Accruals And Provisions", Category = "LIABILITY", Currency = "GHS", IsHeader = true },
        new() { Code = "22100", Name = "Accrued Expenses", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "22200", Name = "Interest Payable", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "22300", Name = "Cash Operations Suspense", Category = "LIABILITY", Currency = "GHS" },
        new() { Code = "30000", Name = "Equity", Category = "EQUITY", Currency = "GHS", IsHeader = true },
        new() { Code = "30100", Name = "Stated Capital", Category = "EQUITY", Currency = "GHS" },
        new() { Code = "30200", Name = "Retained Earnings", Category = "EQUITY", Currency = "GHS" },
        new() { Code = "40000", Name = "Income", Category = "INCOME", Currency = "GHS", IsHeader = true },
        new() { Code = "40100", Name = "Interest Income On Loans", Category = "INCOME", Currency = "GHS" },
        new() { Code = "40200", Name = "Processing Fee Income", Category = "INCOME", Currency = "GHS" },
        new() { Code = "40300", Name = "Penalty Fee Income", Category = "INCOME", Currency = "GHS" },
        new() { Code = "40400", Name = "Recovery Income", Category = "INCOME", Currency = "GHS" },
        new() { Code = "40500", Name = "Commission And Service Fee Income", Category = "INCOME", Currency = "GHS" },
        new() { Code = "50000", Name = "Expenses", Category = "EXPENSE", Currency = "GHS", IsHeader = true },
        new() { Code = "50100", Name = "Impairment Expense", Category = "EXPENSE", Currency = "GHS" },
        new() { Code = "50200", Name = "Personnel Expense", Category = "EXPENSE", Currency = "GHS" },
        new() { Code = "50300", Name = "Occupancy And Administration Expense", Category = "EXPENSE", Currency = "GHS" },
        new() { Code = "50400", Name = "Interest Expense", Category = "EXPENSE", Currency = "GHS" },
    };

    public static string GetStandardName(string regionCode)
    {
        return regionCode.ToUpperInvariant() switch
        {
            GhanaRegionCode => GhanaStandardName,
            _ => $"Unsupported regulatory region: {regionCode}"
        };
    }

    public static IReadOnlyList<GlAccount> GetAccounts(string regionCode)
    {
        return regionCode.ToUpperInvariant() switch
        {
            GhanaRegionCode => GhanaAccounts.Select(Clone).ToList(),
            _ => new List<GlAccount>()
        };
    }

    private static GlAccount Clone(GlAccount account)
    {
        return new GlAccount
        {
            Code = account.Code,
            Name = account.Name,
            Category = account.Category,
            Currency = account.Currency,
            Balance = account.Balance,
            IsHeader = account.IsHeader,
        };
    }
}

