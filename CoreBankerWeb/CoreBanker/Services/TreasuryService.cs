namespace CoreBanker.Services
{
    public class TreasuryService : ApiClientBase
    {
        public TreasuryService(HttpClient httpClient) : base(httpClient) { }

        public async Task<List<TreasuryPositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<TreasuryPositionDto>>("/api/TreasuryPosition", cancellationToken);
            return result ?? new List<TreasuryPositionDto>();
        }

        public async Task<List<PositionSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<PositionSummaryDto>>("/api/TreasuryPosition/summary", cancellationToken);
            return result ?? new List<PositionSummaryDto>();
        }

        public async Task<TreasuryPositionDto?> CreatePositionAsync(CreateTreasuryPositionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateTreasuryPositionRequest, TreasuryPositionDto>("/api/TreasuryPosition", request, cancellationToken);
        }

        public async Task<TreasuryPositionDto?> UpdatePositionAsync(int id, UpdateTreasuryPositionRequest request, CancellationToken cancellationToken = default)
        {
            return await PutAsync<UpdateTreasuryPositionRequest, TreasuryPositionDto>($"/api/TreasuryPosition/{id}", request, cancellationToken);
        }

        public async Task<TreasuryPositionDto?> ReconcilePositionAsync(int id, ReconcilePositionRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<ReconcilePositionRequest, TreasuryPositionDto>($"/api/TreasuryPosition/{id}/reconcile", request, cancellationToken);
        }

        public async Task<TreasuryPositionDto?> ClosePositionAsync(int id, decimal closingBalance, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, TreasuryPositionDto>($"/api/TreasuryPosition/{id}/close?closingBalance={closingBalance}", new { }, cancellationToken);
        }

        public async Task<List<FxTradeDto>> GetTradesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<FxTradeDto>>("/api/FxTrading", cancellationToken);
            return result ?? new List<FxTradeDto>();
        }

        public async Task<List<FxTradeDto>> GetPendingTradesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<FxTradeDto>>("/api/FxTrading/pending", cancellationToken);
            return result ?? new List<FxTradeDto>();
        }

        public async Task<FxTradeDto?> CreateTradeAsync(CreateFxTradeRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateFxTradeRequest, FxTradeDto>("/api/FxTrading", request, cancellationToken);
        }

        public async Task<FxTradeDto?> ApproveTradeAsync(ApproveFxTradeRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<ApproveFxTradeRequest, FxTradeDto>("/api/FxTrading/approve", request, cancellationToken);
        }

        public async Task<FxTradeDto?> SettleTradeAsync(SettleFxTradeRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<SettleFxTradeRequest, FxTradeDto>("/api/FxTrading/settle", request, cancellationToken);
        }

        public async Task<List<InvestmentDto>> GetInvestmentsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<InvestmentDto>>("/api/treasury/investments", cancellationToken);
            return result ?? new List<InvestmentDto>();
        }

        public async Task<InvestmentPortfolioDto?> GetInvestmentPortfolioAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync<InvestmentPortfolioDto>("/api/treasury/investments/portfolio", cancellationToken);
        }

        public async Task<InvestmentDto?> CreateInvestmentAsync(CreateInvestmentRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<CreateInvestmentRequest, InvestmentDto>("/api/treasury/investments", request, cancellationToken);
        }

        public async Task<InvestmentDto?> ApproveInvestmentAsync(int id, CancellationToken cancellationToken = default)
        {
            return await PostAsync<object, InvestmentDto>($"/api/treasury/investments/{id}/approve", new { }, cancellationToken);
        }

        public async Task<InvestmentDto?> RolloverInvestmentAsync(RolloverInvestmentRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<RolloverInvestmentRequest, InvestmentDto>("/api/treasury/investments/rollover", request, cancellationToken);
        }

        public async Task<InvestmentDto?> LiquidateInvestmentAsync(LiquidateInvestmentRequest request, CancellationToken cancellationToken = default)
        {
            return await PostAsync<LiquidateInvestmentRequest, InvestmentDto>("/api/treasury/investments/liquidate", request, cancellationToken);
        }
    }

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
}
