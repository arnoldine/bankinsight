using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IFxTradingService
{
    Task<FxTradeDto> CreateTradeAsync(string initiatedBy, CreateFxTradeRequest request);
    Task<FxTradeDto> ApproveTradeAsync(string approvedBy, ApproveFxTradeRequest request);
    Task<FxTradeDto> SettleTradeAsync(SettleFxTradeRequest request);
    Task<FxTradeDto?> GetTradeAsync(int id);
    Task<FxTradeDto?> GetTradeByDealNumberAsync(string dealNumber);
    Task<List<FxTradeDto>> GetTradesAsync(DateTime? fromDate = null, DateTime? toDate = null, string? status = null);
    Task<List<FxTradeDto>> GetPendingTradesAsync();
    Task<FxTradeStatsDto> GetTradeStatsAsync(DateTime fromDate, DateTime toDate);
    Task<bool> CancelTradeAsync(int id, string reason);
}

public class FxTradingService : IFxTradingService
{
    private readonly ApplicationDbContext _context;
    private readonly IFxRateService _fxRateService;
    private readonly ILogger<FxTradingService> _logger;

    public FxTradingService(
        ApplicationDbContext context, 
        IFxRateService fxRateService,
        ILogger<FxTradingService> logger)
    {
        _context = context;
        _fxRateService = fxRateService;
        _logger = logger;
    }

    public async Task<FxTradeDto> CreateTradeAsync(string initiatedBy, CreateFxTradeRequest request)
    {
        var dealNumber = GenerateDealNumber();

        // Calculate spread if customer rate is provided
        decimal? spread = null;
        if (request.CustomerRate.HasValue)
        {
            spread = request.Direction.ToUpper() == "BUY" 
                ? request.CustomerRate.Value - request.ExchangeRate
                : request.ExchangeRate - request.CustomerRate.Value;
        }

        var trade = new FxTrade
        {
            DealNumber = dealNumber,
            TradeDate = request.TradeDate.Date.ToUniversalTime(),
            ValueDate = request.ValueDate.Date.ToUniversalTime(),
            TradeType = request.TradeType,
            Direction = request.Direction.ToUpper(),
            BaseCurrency = request.BaseCurrency.ToUpper(),
            BaseAmount = request.BaseAmount,
            CounterCurrency = request.CounterCurrency.ToUpper(),
            CounterAmount = request.CounterAmount,
            ExchangeRate = request.ExchangeRate,
            CustomerRate = request.CustomerRate,
            Spread = spread,
            CustomerId = request.CustomerId,
            Counterparty = request.Counterparty,
            Status = "Pending",
            InitiatedBy = initiatedBy,
            Narration = request.Narration,
            Reference = request.Reference,
            CreatedAt = DateTime.UtcNow
        };

        _context.FxTrades.Add(trade);
        await _context.SaveChangesAsync();

        _logger.LogInformation("FX trade created: {DealNumber}", dealNumber);

        return await GetTradeDtoAsync(trade.Id);
    }

    public async Task<FxTradeDto> ApproveTradeAsync(string approvedBy, ApproveFxTradeRequest request)
    {
        var trade = await _context.FxTrades
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == request.TradeId);

        if (trade == null)
            throw new InvalidOperationException($"FX trade with ID {request.TradeId} not found");

        if (trade.Status != "Pending")
            throw new InvalidOperationException($"Trade {trade.DealNumber} is not pending approval");

        if (request.Approved)
        {
            trade.Status = "Confirmed";
            trade.ApprovedBy = approvedBy;
            trade.ApprovedAt = DateTime.UtcNow;

            _logger.LogInformation("FX trade approved: {DealNumber}", trade.DealNumber);
        }
        else
        {
            trade.Status = "Cancelled";
            trade.Narration = string.IsNullOrEmpty(trade.Narration)
                ? $"Rejected: {request.RejectionReason}"
                : $"{trade.Narration}\nRejected: {request.RejectionReason}";

            _logger.LogWarning("FX trade rejected: {DealNumber}", trade.DealNumber);
        }

        await _context.SaveChangesAsync();

        return MapToDto(trade);
    }

    public async Task<FxTradeDto> SettleTradeAsync(SettleFxTradeRequest request)
    {
        var trade = await _context.FxTrades
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == request.TradeId);

        if (trade == null)
            throw new InvalidOperationException($"FX trade with ID {request.TradeId} not found");

        if (trade.Status != "Confirmed")
            throw new InvalidOperationException($"Trade {trade.DealNumber} must be confirmed before settlement");

        trade.SettlementStatus = "Settled";
        trade.SettledAt = request.SettlementDate;
        trade.Status = "Settled";

        // Calculate P&L if actual rate differs from booked rate
        if (request.ActualRate.HasValue)
        {
            var rateDifference = request.ActualRate.Value - trade.ExchangeRate;
            trade.ProfitLoss = trade.BaseAmount * rateDifference;
        }
        else if (trade.Spread.HasValue)
        {
            trade.ProfitLoss = trade.BaseAmount * trade.Spread.Value;
        }

        if (request.Notes != null)
        {
            trade.Narration = string.IsNullOrEmpty(trade.Narration)
                ? request.Notes
                : $"{trade.Narration}\n{request.Notes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "FX trade settled: {DealNumber}, P&L: {ProfitLoss}", 
            trade.DealNumber, trade.ProfitLoss);

        return MapToDto(trade);
    }

    public async Task<FxTradeDto?> GetTradeAsync(int id)
    {
        return await GetTradeDtoAsync(id);
    }

    public async Task<FxTradeDto?> GetTradeByDealNumberAsync(string dealNumber)
    {
        var trade = await _context.FxTrades
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.DealNumber == dealNumber);

        return trade != null ? MapToDto(trade) : null;
    }

    public async Task<List<FxTradeDto>> GetTradesAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? status = null)
    {
        var query = _context.FxTrades
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Customer)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(t => t.TradeDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(t => t.TradeDate <= toDate.Value.Date);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var trades = await query
            .OrderByDescending(t => t.TradeDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        return trades.Select(MapToDto).ToList();
    }

    public async Task<List<FxTradeDto>> GetPendingTradesAsync()
    {
        var trades = await _context.FxTrades
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Customer)
            .Where(t => t.Status == "Pending")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return trades.Select(MapToDto).ToList();
    }

    public async Task<FxTradeStatsDto> GetTradeStatsAsync(DateTime fromDate, DateTime toDate)
    {
        var trades = await _context.FxTrades
            .Where(t => t.TradeDate >= fromDate.Date && t.TradeDate <= toDate.Date)
            .ToListAsync();

        var totalTrades = trades.Count;
        var totalVolume = trades.Sum(t => t.BaseAmount);
        var totalProfitLoss = trades.Sum(t => t.ProfitLoss ?? 0);

        var volumeByDirection = trades
            .GroupBy(t => t.Direction)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.BaseAmount));

        var volumeByType = trades
            .GroupBy(t => t.TradeType)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.BaseAmount));

        return new FxTradeStatsDto(
            totalTrades,
            totalVolume,
            totalProfitLoss,
            volumeByDirection,
            volumeByType
        );
    }

    public async Task<bool> CancelTradeAsync(int id, string reason)
    {
        var trade = await _context.FxTrades.FindAsync(id);
        if (trade == null)
            return false;

        if (trade.Status == "Settled")
            throw new InvalidOperationException("Cannot cancel a settled trade");

        trade.Status = "Cancelled";
        trade.Narration = string.IsNullOrEmpty(trade.Narration)
            ? $"Cancelled: {reason}"
            : $"{trade.Narration}\nCancelled: {reason}";

        await _context.SaveChangesAsync();

        _logger.LogWarning("FX trade cancelled: {DealNumber}", trade.DealNumber);

        return true;
    }

    private async Task<FxTradeDto> GetTradeDtoAsync(int id)
    {
        var trade = await _context.FxTrades
            .Include(t => t.Initiator)
            .Include(t => t.Approver)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trade == null)
            throw new InvalidOperationException($"FX trade with ID {id} not found");

        return MapToDto(trade);
    }

    private static string GenerateDealNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"FX-{date}-{random}";
    }

    private static FxTradeDto MapToDto(FxTrade trade)
    {
        return new FxTradeDto(
            trade.Id,
            trade.DealNumber,
            trade.TradeDate,
            trade.ValueDate,
            trade.TradeType,
            trade.Direction,
            trade.BaseCurrency,
            trade.BaseAmount,
            trade.CounterCurrency,
            trade.CounterAmount,
            trade.ExchangeRate,
            trade.CustomerRate,
            trade.Spread,
            trade.Customer?.Name,
            trade.Counterparty,
            trade.Status,
            trade.SettlementStatus,
            trade.Initiator.Name,
            trade.Approver?.Name,
            trade.ApprovedAt,
            trade.SettledAt,
            trade.ProfitLoss,
            trade.Narration,
            trade.Reference
        );
    }
}
