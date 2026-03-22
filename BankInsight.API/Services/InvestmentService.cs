using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IInvestmentService
{
    Task<InvestmentDto> CreateInvestmentAsync(string initiatedBy, CreateInvestmentRequest request);
    Task<InvestmentDto> ApproveInvestmentAsync(string approvedBy, int investmentId);
    Task<InvestmentDto> RolloverInvestmentAsync(RolloverInvestmentRequest request);
    Task<InvestmentDto> LiquidateInvestmentAsync(LiquidateInvestmentRequest request);
    Task<InvestmentDto> MaturityInvestmentAsync(int investmentId);
    Task<InvestmentDto?> GetInvestmentAsync(int id);
    Task<InvestmentDto?> GetInvestmentByNumberAsync(string investmentNumber);
    Task<List<InvestmentDto>> GetInvestmentsAsync(string? status = null, string? type = null, string? currency = null);
    Task<List<InvestmentDto>> GetMaturingInvestmentsAsync(DateTime fromDate, DateTime toDate);
    Task<InvestmentPortfolioDto> GetPortfolioSummaryAsync();
    Task AccrueInterestAsync(int investmentId);
    Task RunDailyAccrualAsync();
}

public class InvestmentService : IInvestmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvestmentService> _logger;

    public InvestmentService(ApplicationDbContext context, ILogger<InvestmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InvestmentDto> CreateInvestmentAsync(string initiatedBy, CreateInvestmentRequest request)
    {
        var investmentNumber = GenerateInvestmentNumber();
        var tenorDays = (request.MaturityDate.Date - request.PlacementDate.Date).Days;

        // Calculate interest and maturity value
        decimal interestAmount;
        decimal maturityValue;
        decimal? purchasePrice = null;
        decimal? yieldToMaturity = null;

        if (request.DiscountRate.HasValue)
        {
            // Discount instrument (T-Bills)
            var discountFactor = 1 - (request.DiscountRate.Value / 100 * tenorDays / 365);
            purchasePrice = request.PrincipalAmount * discountFactor;
            maturityValue = request.PrincipalAmount;
            interestAmount = maturityValue - purchasePrice.Value;
            yieldToMaturity = (interestAmount / purchasePrice.Value) * (365m / tenorDays) * 100;
        }
        else
        {
            // Interest-bearing instrument
            interestAmount = request.PrincipalAmount * (request.InterestRate / 100) * (tenorDays / 365m);
            maturityValue = request.PrincipalAmount + interestAmount;
            yieldToMaturity = request.InterestRate;
        }

        var investment = new Investment
        {
            InvestmentNumber = investmentNumber,
            InvestmentType = request.InvestmentType,
            Instrument = request.Instrument,
            Counterparty = request.Counterparty,
            Currency = request.Currency.ToUpper(),
            PrincipalAmount = request.PrincipalAmount,
            InterestRate = request.InterestRate,
            DiscountRate = request.DiscountRate,
            PlacementDate = request.PlacementDate.Date.ToUniversalTime(),
            MaturityDate = request.MaturityDate.Date.ToUniversalTime(),
            TenorDays = tenorDays,
            InterestAmount = interestAmount,
            MaturityValue = maturityValue,
            PurchasePrice = purchasePrice,
            YieldToMaturity = yieldToMaturity,
            Status = "Active",
            InitiatedBy = initiatedBy,
            SettlementAccount = request.SettlementAccount,
            AccruedInterest = 0,
            Reference = request.Reference,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Investment created: {InvestmentNumber}", investmentNumber);

        return await GetInvestmentDtoAsync(investment.Id);
    }

    public async Task<InvestmentDto> ApproveInvestmentAsync(string approvedBy, int investmentId)
    {
        var investment = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException($"Investment with ID {investmentId} not found");

        if (!string.IsNullOrEmpty(investment.ApprovedBy))
            throw new InvalidOperationException($"Investment {investment.InvestmentNumber} is already approved");

        investment.ApprovedBy = approvedBy;
        investment.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Investment approved: {InvestmentNumber}", investment.InvestmentNumber);

        return MapToDto(investment, null);
    }

    public async Task<InvestmentDto> RolloverInvestmentAsync(RolloverInvestmentRequest request)
    {
        var originalInvestment = await _context.Investments.FindAsync(request.InvestmentId);
        if (originalInvestment == null)
            throw new InvalidOperationException($"Investment with ID {request.InvestmentId} not found");

        if (originalInvestment.Status != "Active")
            throw new InvalidOperationException("Can only rollover active investments");

        // Mark original investment as matured
        originalInvestment.Status = "Rolled-Over";
        originalInvestment.MaturedAt = DateTime.UtcNow;

        // Create new investment
        var newInvestment = new Investment
        {
            InvestmentNumber = GenerateInvestmentNumber(),
            InvestmentType = originalInvestment.InvestmentType,
            Instrument = originalInvestment.Instrument,
            Counterparty = originalInvestment.Counterparty,
            Currency = originalInvestment.Currency,
            PrincipalAmount = originalInvestment.MaturityValue ?? originalInvestment.PrincipalAmount,
            InterestRate = request.NewInterestRate ?? originalInvestment.InterestRate,
            DiscountRate = originalInvestment.DiscountRate,
            PlacementDate = originalInvestment.MaturityDate,
            MaturityDate = request.NewMaturityDate.Date,
            TenorDays = (request.NewMaturityDate.Date - originalInvestment.MaturityDate).Days,
            Status = "Active",
            InitiatedBy = originalInvestment.InitiatedBy,
            ApprovedBy = originalInvestment.ApprovedBy,
            ApprovedAt = DateTime.UtcNow,
            SettlementAccount = originalInvestment.SettlementAccount,
            AccruedInterest = 0,
            Notes = $"Rollover from {originalInvestment.InvestmentNumber}. {request.Notes}",
            CreatedAt = DateTime.UtcNow
        };

        // Recalculate for new investment
        var tenorDays = newInvestment.TenorDays;
        var interestAmount = newInvestment.PrincipalAmount * (newInvestment.InterestRate / 100) * (tenorDays / 365m);
        newInvestment.InterestAmount = interestAmount;
        newInvestment.MaturityValue = newInvestment.PrincipalAmount + interestAmount;
        newInvestment.YieldToMaturity = newInvestment.InterestRate;

        originalInvestment.RolloverTo = newInvestment.Id;

        _context.Investments.Add(newInvestment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Investment rolled over: {Original} -> {New}", 
            originalInvestment.InvestmentNumber, 
            newInvestment.InvestmentNumber);

        return await GetInvestmentDtoAsync(newInvestment.Id);
    }

    public async Task<InvestmentDto> LiquidateInvestmentAsync(LiquidateInvestmentRequest request)
    {
        var investment = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .FirstOrDefaultAsync(i => i.Id == request.InvestmentId);

        if (investment == null)
            throw new InvalidOperationException($"Investment with ID {request.InvestmentId} not found");

        if (investment.Status != "Active")
            throw new InvalidOperationException("Can only liquidate active investments");

        // Accrue interest up to liquidation date
        await AccrueInterestAsync(investment.Id);

        // Apply penalty if provided
        if (request.PenaltyAmount.HasValue && request.PenaltyAmount.Value > 0)
        {
            investment.AccruedInterest -= request.PenaltyAmount.Value;
            investment.Notes = string.IsNullOrEmpty(investment.Notes)
                ? $"Early liquidation penalty: {request.PenaltyAmount:N2}"
                : $"{investment.Notes}\nEarly liquidation penalty: {request.PenaltyAmount:N2}";
        }

        investment.Status = "Liquidated";
        investment.MaturedAt = request.LiquidationDate;
        investment.Notes = string.IsNullOrEmpty(investment.Notes)
            ? $"Liquidation reason: {request.Reason}"
            : $"{investment.Notes}\nLiquidation reason: {request.Reason}";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Investment liquidated: {InvestmentNumber}", investment.InvestmentNumber);

        return MapToDto(investment, null);
    }

    public async Task<InvestmentDto> MaturityInvestmentAsync(int investmentId)
    {
        var investment = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .FirstOrDefaultAsync(i => i.Id == investmentId);

        if (investment == null)
            throw new InvalidOperationException($"Investment with ID {investmentId} not found");

        if (investment.Status != "Active")
            throw new InvalidOperationException("Can only mature active investments");

        // Final interest accrual
        await AccrueInterestAsync(investmentId);

        investment.Status = "Matured";
        investment.MaturedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Investment matured: {InvestmentNumber}", investment.InvestmentNumber);

        return MapToDto(investment, null);
    }

    public async Task<InvestmentDto?> GetInvestmentAsync(int id)
    {
        return await GetInvestmentDtoAsync(id);
    }

    public async Task<InvestmentDto?> GetInvestmentByNumberAsync(string investmentNumber)
    {
        var investment = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .FirstOrDefaultAsync(i => i.InvestmentNumber == investmentNumber);

        return investment != null ? MapToDto(investment, null) : null;
    }

    public async Task<List<InvestmentDto>> GetInvestmentsAsync(
        string? status = null, 
        string? type = null, 
        string? currency = null)
    {
        var query = _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(i => i.InvestmentType == type);

        if (!string.IsNullOrEmpty(currency))
            query = query.Where(i => i.Currency == currency.ToUpper());

        var investments = await query
            .OrderByDescending(i => i.PlacementDate)
            .ToListAsync();

        return investments.Select(i => MapToDto(i, null)).ToList();
    }

    public async Task<List<InvestmentDto>> GetMaturingInvestmentsAsync(DateTime fromDate, DateTime toDate)
    {
        var investments = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .Where(i => i.Status == "Active" 
                     && i.MaturityDate >= fromDate.Date 
                     && i.MaturityDate <= toDate.Date)
            .OrderBy(i => i.MaturityDate)
            .ToListAsync();

        return investments.Select(i => MapToDto(i, null)).ToList();
    }

    public async Task<InvestmentPortfolioDto> GetPortfolioSummaryAsync()
    {
        var activeInvestments = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .Where(i => i.Status == "Active")
            .ToListAsync();

        var totalInvestments = activeInvestments.Count;
        var totalPrincipal = activeInvestments.Sum(i => i.PrincipalAmount);
        var totalAccruedInterest = activeInvestments.Sum(i => i.AccruedInterest);
        var totalMaturityValue = activeInvestments.Sum(i => i.MaturityValue ?? 0);
        var averageYield = activeInvestments.Any() 
            ? activeInvestments.Average(i => i.YieldToMaturity ?? 0) 
            : 0;

        var byType = activeInvestments
            .GroupBy(i => i.InvestmentType)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.PrincipalAmount));

        var byCurrency = activeInvestments
            .GroupBy(i => i.Currency)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.PrincipalAmount));

        var next30Days = DateTime.UtcNow.AddDays(30);
        var maturityCalendar = activeInvestments
            .Where(i => i.MaturityDate <= next30Days)
            .OrderBy(i => i.MaturityDate)
            .Select(i => MapToDto(i, null))
            .ToList();

        return new InvestmentPortfolioDto(
            totalInvestments,
            totalPrincipal,
            totalAccruedInterest,
            totalMaturityValue,
            averageYield,
            byType,
            byCurrency,
            maturityCalendar
        );
    }

    public async Task AccrueInterestAsync(int investmentId)
    {
        var investment = await _context.Investments.FindAsync(investmentId);
        if (investment == null)
            throw new InvalidOperationException($"Investment with ID {investmentId} not found");

        if (investment.Status != "Active")
            return; // Don't accrue for non-active investments

        var accrualDate = DateTime.UtcNow.Date;
        var lastAccrualDate = investment.LastAccrualDate?.Date ?? investment.PlacementDate.Date;

        if (accrualDate <= lastAccrualDate)
            return; // Already accrued for today

        var daysToAccrue = (accrualDate - lastAccrualDate).Days;
        var dailyInterestRate = (investment.InterestRate / 100) / 365m;
        var interestToAccrue = investment.PrincipalAmount * dailyInterestRate * daysToAccrue;

        investment.AccruedInterest += interestToAccrue;
        investment.LastAccrualDate = accrualDate;

        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "Accrued interest for {InvestmentNumber}: {Interest} for {Days} days",
            investment.InvestmentNumber, interestToAccrue, daysToAccrue);
    }

    public async Task RunDailyAccrualAsync()
    {
        var activeInvestments = await _context.Investments
            .Where(i => i.Status == "Active")
            .ToListAsync();

        _logger.LogInformation("Running daily accrual for {Count} active investments", activeInvestments.Count);

        foreach (var investment in activeInvestments)
        {
            try
            {
                await AccrueInterestAsync(investment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accruing interest for investment {Id}", investment.Id);
            }
        }

        _logger.LogInformation("Daily accrual completed");
    }

    private async Task<InvestmentDto> GetInvestmentDtoAsync(int id)
    {
        var investment = await _context.Investments
            .Include(i => i.Initiator)
            .Include(i => i.Approver)
            .Include(i => i.RolloverInvestment)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (investment == null)
            throw new InvalidOperationException($"Investment with ID {id} not found");

        return MapToDto(investment, investment.RolloverInvestment);
    }

    private static string GenerateInvestmentNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..5].ToUpper();
        return $"INV-{date}-{random}";
    }

    private static InvestmentDto MapToDto(Investment investment, Investment? rolloverInvestment)
    {
        var daysToMaturity = investment.Status == "Active" 
            ? (investment.MaturityDate - DateTime.UtcNow.Date).Days 
            : 0;

        return new InvestmentDto(
            investment.Id,
            investment.InvestmentNumber,
            investment.InvestmentType,
            investment.Instrument,
            investment.Counterparty,
            investment.Currency,
            investment.PrincipalAmount,
            investment.InterestRate,
            investment.DiscountRate,
            investment.PlacementDate,
            investment.MaturityDate,
            investment.TenorDays,
            investment.InterestAmount,
            investment.MaturityValue,
            investment.PurchasePrice,
            investment.YieldToMaturity,
            investment.Status,
            investment.Initiator.Name,
            investment.Approver?.Name,
            investment.ApprovedAt,
            investment.MaturedAt,
            investment.AccruedInterest,
            investment.LastAccrualDate,
            investment.Reference,
            investment.Notes,
            daysToMaturity
        );
    }
}
