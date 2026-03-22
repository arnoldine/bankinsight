using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface ITreasuryPositionService
{
    Task<TreasuryPositionDto> CreatePositionAsync(CreateTreasuryPositionRequest request);
    Task<TreasuryPositionDto> UpdatePositionAsync(int id, UpdateTreasuryPositionRequest request);
    Task<TreasuryPositionDto> ReconcilePositionAsync(int id, string reconciledBy, ReconcilePositionRequest request);
    Task<TreasuryPositionDto?> GetPositionAsync(int id);
    Task<List<TreasuryPositionDto>> GetPositionsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? currency = null);
    Task<TreasuryPositionDto?> GetLatestPositionAsync(string currency);
    Task<List<PositionSummaryDto>> GetPositionSummaryAsync();
    Task<TreasuryPositionDto> ClosePositionAsync(int id, decimal closingBalance);
}

public class TreasuryPositionService : ITreasuryPositionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TreasuryPositionService> _logger;

    public TreasuryPositionService(ApplicationDbContext context, ILogger<TreasuryPositionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TreasuryPositionDto> CreatePositionAsync(CreateTreasuryPositionRequest request)
    {
        var position = new TreasuryPosition
        {
            PositionDate = request.PositionDate.Date.ToUniversalTime(),
            Currency = request.Currency.ToUpper(),
            OpeningBalance = request.OpeningBalance,
            Deposits = 0,
            Withdrawals = 0,
            FxGainsLosses = 0,
            OtherMovements = 0,
            ClosingBalance = request.OpeningBalance,
            ExposureLimit = request.ExposureLimit,
            PositionStatus = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.TreasuryPositions.Add(position);
        await _context.SaveChangesAsync();

        return MapToDto(position);
    }

    public async Task<TreasuryPositionDto> UpdatePositionAsync(int id, UpdateTreasuryPositionRequest request)
    {
        var position = await _context.TreasuryPositions
            .Include(p => p.Reconciler)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
            throw new InvalidOperationException($"Treasury position with ID {id} not found");

        if (position.PositionStatus == "Closed")
            throw new InvalidOperationException("Cannot update a closed position");

        if (request.Deposits.HasValue)
            position.Deposits += request.Deposits.Value;

        if (request.Withdrawals.HasValue)
            position.Withdrawals += request.Withdrawals.Value;

        if (request.FxGainsLosses.HasValue)
            position.FxGainsLosses += request.FxGainsLosses.Value;

        if (request.OtherMovements.HasValue)
            position.OtherMovements += request.OtherMovements.Value;

        if (request.NostroBalance.HasValue)
            position.NostroBalance = request.NostroBalance.Value;

        if (request.VaultBalance.HasValue)
            position.VaultBalance = request.VaultBalance.Value;

        if (request.OvernightPlacement.HasValue)
            position.OvernightPlacement = request.OvernightPlacement.Value;

        if (request.Notes != null)
            position.Notes = request.Notes;

        // Recalculate closing balance
        position.ClosingBalance = position.OpeningBalance 
            + position.Deposits 
            - position.Withdrawals 
            + position.FxGainsLosses 
            + position.OtherMovements;

        await _context.SaveChangesAsync();

        return MapToDto(position);
    }

    public async Task<TreasuryPositionDto> ReconcilePositionAsync(
        int id, 
        string reconciledBy, 
        ReconcilePositionRequest request)
    {
        var position = await _context.TreasuryPositions
            .Include(p => p.Reconciler)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
            throw new InvalidOperationException($"Treasury position with ID {id} not found");

        // Check if reconciliation is needed
        var difference = request.ActualBalance - position.ClosingBalance;
        if (Math.Abs(difference) > 0.01m)
        {
            // Record difference in OtherMovements
            position.OtherMovements += difference;
            position.ClosingBalance = request.ActualBalance;
            
            _logger.LogWarning(
                "Position {Id} reconciled with difference of {Difference} {Currency}",
                id, difference, position.Currency);
        }

        position.PositionStatus = "Reconciled";
        position.ReconciledBy = reconciledBy;
        position.ReconciledAt = DateTime.UtcNow;
        position.Notes = string.IsNullOrEmpty(position.Notes) 
            ? request.Notes 
            : $"{position.Notes}\n{request.Notes}";

        await _context.SaveChangesAsync();

        return MapToDto(position);
    }

    public async Task<TreasuryPositionDto?> GetPositionAsync(int id)
    {
        var position = await _context.TreasuryPositions
            .Include(p => p.Reconciler)
            .FirstOrDefaultAsync(p => p.Id == id);

        return position != null ? MapToDto(position) : null;
    }

    public async Task<List<TreasuryPositionDto>> GetPositionsAsync(
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        string? currency = null)
    {
        var query = _context.TreasuryPositions
            .Include(p => p.Reconciler)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(p => p.PositionDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(p => p.PositionDate <= toDate.Value.Date);

        if (!string.IsNullOrEmpty(currency))
            query = query.Where(p => p.Currency == currency.ToUpper());

        var positions = await query
            .OrderByDescending(p => p.PositionDate)
            .ThenBy(p => p.Currency)
            .ToListAsync();

        return positions.Select(MapToDto).ToList();
    }

    public async Task<TreasuryPositionDto?> GetLatestPositionAsync(string currency)
    {
        var position = await _context.TreasuryPositions
            .Include(p => p.Reconciler)
            .Where(p => p.Currency == currency.ToUpper())
            .OrderByDescending(p => p.PositionDate)
            .FirstOrDefaultAsync();

        return position != null ? MapToDto(position) : null;
    }

    public async Task<List<PositionSummaryDto>> GetPositionSummaryAsync()
    {
        var latestPositions = await _context.TreasuryPositions
            .GroupBy(p => p.Currency)
            .Select(g => g.OrderByDescending(p => p.PositionDate).FirstOrDefault())
            .Where(p => p != null)
            .ToListAsync();

        return latestPositions.Select(p =>
        {
            var utilizationPercent = p!.ExposureLimit.HasValue && p.ExposureLimit.Value > 0
                ? (p.ClosingBalance / p.ExposureLimit.Value) * 100
                : 0;

            var status = p.ClosingBalance < 0 ? "Negative" 
                : p.ExposureLimit.HasValue && p.ClosingBalance > p.ExposureLimit.Value ? "Over Limit"
                : "Normal";

            return new PositionSummaryDto(
                p.Currency,
                p.ClosingBalance,
                p.ExposureLimit ?? 0,
                utilizationPercent,
                status
            );
        }).ToList();
    }

    public async Task<TreasuryPositionDto> ClosePositionAsync(int id, decimal closingBalance)
    {
        var position = await _context.TreasuryPositions
            .Include(p => p.Reconciler)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null)
            throw new InvalidOperationException($"Treasury position with ID {id} not found");

        position.ClosingBalance = closingBalance;
        position.PositionStatus = "Closed";

        await _context.SaveChangesAsync();

        return MapToDto(position);
    }

    private static TreasuryPositionDto MapToDto(TreasuryPosition position)
    {
        return new TreasuryPositionDto(
            position.Id,
            position.PositionDate,
            position.Currency,
            position.OpeningBalance,
            position.Deposits,
            position.Withdrawals,
            position.FxGainsLosses,
            position.OtherMovements,
            position.ClosingBalance,
            position.NostroBalance,
            position.VaultBalance,
            position.OvernightPlacement,
            position.ExposureLimit,
            position.PositionStatus,
            position.ReconciledAt,
            position.Reconciler?.Name,
            position.Notes
        );
    }
}
