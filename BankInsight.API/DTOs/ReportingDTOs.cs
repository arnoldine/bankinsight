using System;
using System.Collections.Generic;

namespace BankInsight.API.DTOs
{
    // Report Definition DTOs
    public class ReportDefinitionDTO
    {
        public int Id { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string TemplateFormat { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool RequiresApproval { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReportCatalogItemDTO
    {
        public int Id { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool RequiresApproval { get; set; }
    }

    public class ReportResultDTO
    {
        public int RunId { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public long? ExecutionTimeMs { get; set; }
    }

    public class CreateReportDefinitionRequest
    {
        public string ReportName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string TemplateFormat { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
    }

    // Report Run DTOs
    public class ReportRunDTO
    {
        public int Id { get; set; }
        public int ReportDefinitionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public string Format { get; set; } = string.Empty;
        public long? ExecutionTimeMs { get; set; }
    }

    // Regulatory Report DTOs
    public class DailyPositionReportDTO
    {
        public DateTime ReportDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<DailyPositionDetailDTO> Positions { get; set; } = new();
        public decimal TotalPositionGHS { get; set; }
        public decimal TotalPositionUSD { get; set; }
    }

    public class DailyPositionDetailDTO
    {
        public string Currency { get; set; } = string.Empty;
        public decimal ClosingBalance { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public DateTime? LastReconciliationDate { get; set; }
    }

    public class MonthlyReturnDTO
    {
        public string ReturnType { get; set; } = string.Empty;
        public string ReportingPeriod { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public List<MonthlyReturnDetailDTO> ReturnDetails { get; set; } = new();
        public int TotalAccounts { get; set; }
        public decimal TotalDepositBalance { get; set; }
    }

    public class MonthlyReturnDetailDTO
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; }
    }

    public class PrudentialReturnDTO
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public CapitalMetricsDTO CapitalMetrics { get; set; } = new();
        public RiskMetricsDetailDTO RiskMetrics { get; set; } = new();
        public string FormulaVersion { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public List<string> ValidationFindings { get; set; } = new();
        public List<RegulatorySourceBalanceDTO> SourceBalances { get; set; } = new();
    }

    public class CapitalMetricsDTO
    {
        public decimal Tier1Capital { get; set; }
        public decimal Tier2Capital { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal RiskWeightedAssets { get; set; }
        public decimal CapitalAdequacyRatio { get; set; }
        public decimal Tier1Ratio { get; set; }
    }

    public class RiskMetricsDetailDTO
    {
        public decimal ValueAtRisk { get; set; }
        public decimal LiquidityCoverageRatio { get; set; }
        public decimal AverageCurrencyExposure { get; set; }
    }

    public class LargeExposureReportDTO
    {
        public DateTime ReportDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<LargeExposureDetailDTO> LargeExposures { get; set; } = new();
        public decimal TotalLargeExposures { get; set; }
        public decimal CapitalBase { get; set; }
        public decimal ReportingThreshold { get; set; }
        public string FormulaVersion { get; set; } = string.Empty;
        public List<string> ValidationFindings { get; set; } = new();
        public List<RegulatorySourceBalanceDTO> SourceBalances { get; set; } = new();
    }

    public class LargeExposureDetailDTO
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public decimal TotalExposure { get; set; }
        public decimal PercentageOfCapital { get; set; }
        public string ExposureCategory { get; set; } = string.Empty;
        public bool BreachesReportingThreshold { get; set; }
        public int ActiveFacilityCount { get; set; }
    }

    public class RegulatorySourceBalanceDTO
    {
        public string SourceType { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GHS";
    }

    public class RegulatoryReturnDTO
    {
        public int Id { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string SubmissionStatus { get; set; } = string.Empty;
        public DateTime? SubmissionDate { get; set; }
        public string BogReferenceNumber { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ValidationStatus { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
        public bool RequiresApproval { get; set; }
        public bool IsReadyForSubmission { get; set; }
    }

    // Financial Report DTOs
    public class BalanceSheetDTO
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<BalanceSheetLineItemDTO> Assets { get; set; } = new();
        public List<BalanceSheetLineItemDTO> Liabilities { get; set; } = new();
        public List<BalanceSheetLineItemDTO> Equity { get; set; } = new();
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
    }

    public class BalanceSheetLineItemDTO
    {
        public string LineItem { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class IncomeStatementDTO
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<IncomeStatementLineItemDTO> RevenueItems { get; set; } = new();
        public List<IncomeStatementLineItemDTO> ExpenseItems { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class IncomeStatementLineItemDTO
    {
        public string LineItem { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CashFlowStatementDTO
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<CashFlowLineItemDTO> OperatingActivities { get; set; } = new();
        public List<CashFlowLineItemDTO> InvestingActivities { get; set; } = new();
        public List<CashFlowLineItemDTO> FinancingActivities { get; set; } = new();
        public decimal NetOperatingCashFlow { get; set; }
        public decimal NetInvestingCashFlow { get; set; }
        public decimal NetFinancingCashFlow { get; set; }
        public decimal NetChangeInCash { get; set; }
    }

    public class CashFlowLineItemDTO
    {
        public string Activity { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class TrialBalanceDTO
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<TrialBalanceAccountDTO> Accounts { get; set; } = new();
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class TrialBalanceAccountDTO
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
    }

    // Analytics DTOs
    public class CustomerSegmentationDTO
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<CustomerSegmentDTO> Segments { get; set; } = new();
    }

    public class CustomerSegmentDTO
    {
        public string SegmentName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal AverageAge { get; set; }
        public decimal ChurnRate { get; set; }
    }

    public class TransactionTrendsDTO
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<DailyTransactionTrendDTO> DailyTrends { get; set; } = new();
        public long TotalTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal AverageDailyVolume { get; set; }
        public decimal PeakVolume { get; set; }
    }

    public class DailyTransactionTrendDTO
    {
        public DateTime Date { get; set; }
        public long TransactionCount { get; set; }
        public decimal Volume { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class ProductAnalyticsDTO
    {
        public DateTime AsOfDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<ProductMetricDTO> ProductMetrics { get; set; } = new();
        public int TotalProducts { get; set; }
        public long TotalAccounts { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class ProductMetricDTO
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public long AccountCount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal RevenueContribution { get; set; }
    }

    public class ChannelAnalyticsDTO
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<ChannelMetricDTO> ChannelMetrics { get; set; } = new();
    }

    public class ChannelMetricDTO
    {
        public string ChannelName { get; set; } = string.Empty;
        public long TransactionCount { get; set; }
        public decimal TransactionVolume { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    public class StaffProductivityDTO
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<StaffMetricDTO> StaffMetrics { get; set; } = new();
        public long TotalLoansOriginated { get; set; }
        public decimal TotalLoanValue { get; set; }
    }

    public class StaffMetricDTO
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public long LoansOriginated { get; set; }
        public decimal LoanValue { get; set; }
        public decimal AverageLoanSize { get; set; }
        public decimal DefaultRate { get; set; }
    }
}




