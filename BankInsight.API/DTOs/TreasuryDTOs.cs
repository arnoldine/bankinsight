namespace BankInsight.API.DTOs;

// FX Rate DTOs
public record FxRateDto(
    int Id,
    string BaseCurrency,
    string TargetCurrency,
    decimal BuyRate,
    decimal SellRate,
    decimal? MidRate,
    decimal? OfficialRate,
    DateTime RateDate,
    string Source,
    bool IsActive,
    string? Notes
);

public record CreateFxRateRequest(
    string BaseCurrency,
    string TargetCurrency,
    decimal BuyRate,
    decimal SellRate,
    decimal? MidRate,
    decimal? OfficialRate,
    DateTime RateDate,
    string Source,
    string? Notes
);

public record UpdateFxRateRequest(
    decimal BuyRate,
    decimal SellRate,
    decimal? MidRate,
    decimal? OfficialRate,
    string? Notes
);

public record FxRateHistoryDto(
    DateTime RateDate,
    decimal BuyRate,
    decimal SellRate,
    decimal? MidRate,
    decimal? OfficialRate,
    string Source
);

public record BogFxRateResponse(
    string Currency,
    decimal Rate,
    DateTime Date
);

// Treasury Position DTOs
public record TreasuryPositionDto(
    int Id,
    DateTime PositionDate,
    string Currency,
    decimal OpeningBalance,
    decimal Deposits,
    decimal Withdrawals,
    decimal FxGainsLosses,
    decimal OtherMovements,
    decimal ClosingBalance,
    decimal? NostroBalance,
    decimal? VaultBalance,
    decimal? OvernightPlacement,
    decimal? ExposureLimit,
    string PositionStatus,
    DateTime? ReconciledAt,
    string? ReconciledByName,
    string? Notes
);

public record CreateTreasuryPositionRequest(
    DateTime PositionDate,
    string Currency,
    decimal OpeningBalance,
    decimal? ExposureLimit
);

public record UpdateTreasuryPositionRequest(
    decimal? Deposits,
    decimal? Withdrawals,
    decimal? FxGainsLosses,
    decimal? OtherMovements,
    decimal? NostroBalance,
    decimal? VaultBalance,
    decimal? OvernightPlacement,
    string? Notes
);

public record ReconcilePositionRequest(
    decimal ActualBalance,
    string? Notes
);

public record PositionSummaryDto(
    string Currency,
    decimal CurrentBalance,
    decimal ExposureLimit,
    decimal UtilizationPercent,
    string Status
);

// FX Trade DTOs
public record FxTradeDto(
    int Id,
    string DealNumber,
    DateTime TradeDate,
    DateTime ValueDate,
    string TradeType,
    string Direction,
    string BaseCurrency,
    decimal BaseAmount,
    string CounterCurrency,
    decimal CounterAmount,
    decimal ExchangeRate,
    decimal? CustomerRate,
    decimal? Spread,
    string? CustomerName,
    string? Counterparty,
    string Status,
    string? SettlementStatus,
    string InitiatedByName,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime? SettledAt,
    decimal? ProfitLoss,
    string? Narration,
    string? Reference
);

public record CreateFxTradeRequest(
    DateTime TradeDate,
    DateTime ValueDate,
    string TradeType,
    string Direction,
    string BaseCurrency,
    decimal BaseAmount,
    string CounterCurrency,
    decimal CounterAmount,
    decimal ExchangeRate,
    decimal? CustomerRate,
    string? CustomerId,
    string? Counterparty,
    string? Narration,
    string? Reference
);

public record ApproveFxTradeRequest(
    int TradeId,
    bool Approved,
    string? RejectionReason
);

public record SettleFxTradeRequest(
    int TradeId,
    DateTime SettlementDate,
    decimal? ActualRate,
    string? Notes
);

public record FxTradeStatsDto(
    int TotalTrades,
    decimal TotalVolume,
    decimal TotalProfitLoss,
    Dictionary<string, decimal> VolumeByDirection,
    Dictionary<string, decimal> VolumeByType
);

// Investment DTOs
public record InvestmentDto(
    int Id,
    string InvestmentNumber,
    string InvestmentType,
    string Instrument,
    string Counterparty,
    string Currency,
    decimal PrincipalAmount,
    decimal InterestRate,
    decimal? DiscountRate,
    DateTime PlacementDate,
    DateTime MaturityDate,
    int TenorDays,
    decimal? InterestAmount,
    decimal? MaturityValue,
    decimal? PurchasePrice,
    decimal? YieldToMaturity,
    string Status,
    string InitiatedByName,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime? MaturedAt,
    decimal AccruedInterest,
    DateTime? LastAccrualDate,
    string? Reference,
    string? Notes,
    int DaysToMaturity
);

public record CreateInvestmentRequest(
    string InvestmentType,
    string Instrument,
    string Counterparty,
    string Currency,
    decimal PrincipalAmount,
    decimal InterestRate,
    decimal? DiscountRate,
    DateTime PlacementDate,
    DateTime MaturityDate,
    string? SettlementAccount,
    string? Reference,
    string? Notes
);

public record RolloverInvestmentRequest(
    int InvestmentId,
    DateTime NewMaturityDate,
    decimal? NewInterestRate,
    string? Notes
);

public record LiquidateInvestmentRequest(
    int InvestmentId,
    DateTime LiquidationDate,
    decimal? PenaltyAmount,
    string? Reason
);

public record InvestmentPortfolioDto(
    decimal TotalInvestments,
    decimal TotalPrincipal,
    decimal TotalAccruedInterest,
    decimal TotalMaturityValue,
    decimal AverageYield,
    Dictionary<string, decimal> ByType,
    Dictionary<string, decimal> ByCurrency,
    List<InvestmentDto> MaturityCalendar
);

// Risk Metric DTOs
public record RiskMetricDto(
    int Id,
    DateTime MetricDate,
    string MetricType,
    string? Currency,
    decimal MetricValue,
    decimal? Threshold,
    bool ThresholdBreached,
    decimal? ConfidenceLevel,
    int? TimeHorizonDays,
    string? CalculationMethod,
    string Status,
    string? CalculatedByName,
    DateTime CalculatedAt,
    string? ReviewedByName,
    DateTime? ReviewedAt,
    bool AlertTriggered,
    string? Notes
);

public record CreateRiskMetricRequest(
    DateTime MetricDate,
    string MetricType,
    string? Currency,
    decimal MetricValue,
    decimal? Threshold,
    decimal? ConfidenceLevel,
    int? TimeHorizonDays,
    string? CalculationMethod,
    string? PositionSnapshot,
    string? ExposureDetails,
    string? Notes
);

public record ReviewRiskMetricRequest(
    int MetricId,
    string Status,
    string? Notes
);

public record RiskDashboardDto(
    DateTime AsOfDate,
    decimal VaRValue,
    decimal VaRThreshold,
    bool VaRBreached,
    decimal LcrValue,
    decimal LcrThreshold,
    bool LcrBreached,
    Dictionary<string, decimal> CurrencyExposure,
    List<RiskMetricDto> RecentAlerts
);

// Liquidity Management DTOs
public record LiquidityForecastDto(
    DateTime ForecastDate,
    string Currency,
    decimal OpeningBalance,
    decimal ExpectedInflows,
    decimal ExpectedOutflows,
    decimal ProjectedBalance,
    decimal MinimumRequired,
    decimal Surplus,
    string Status
);

public record CashFlowProjectionDto(
    DateTime Date,
    decimal Inflows,
    decimal Outflows,
    decimal NetCashFlow,
    decimal CumulativeBalance
);
