using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IFxRateService
{
    Task<FxRateDto> CreateRateAsync(CreateFxRateRequest request);
    Task<FxRateDto> UpdateRateAsync(int id, UpdateFxRateRequest request);
    Task<FxRateDto?> GetRateAsync(int id);
    Task<List<FxRateDto>> GetAllRatesAsync(DateTime? rateDate = null, bool activeOnly = true);
    Task<FxRateDto?> GetLatestRateAsync(string baseCurrency, string targetCurrency);
    Task<List<FxRateHistoryDto>> GetRateHistoryAsync(string baseCurrency, string targetCurrency, DateTime fromDate, DateTime toDate);
    Task<decimal> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency, DateTime? rateDate = null);
    Task<bool> DeleteRateAsync(int id);
    Task<List<FxRateDto>> SyncRatesFromBogAsync(DateTime rateDate);
    Task DeactivateOldRatesAsync(DateTime newRateDate, string baseCurrency, string targetCurrency);
}

public class FxRateService : IFxRateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FxRateService> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;

    public FxRateService(
        ApplicationDbContext context, 
        ILogger<FxRateService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<FxRateDto> CreateRateAsync(CreateFxRateRequest request)
    {
        // Deactivate old rates for same currency pair
        await DeactivateOldRatesAsync(request.RateDate, request.BaseCurrency, request.TargetCurrency);

        var midRate = request.MidRate ?? (request.BuyRate + request.SellRate) / 2;

        var rate = new FxRate
        {
            BaseCurrency = request.BaseCurrency.ToUpper(),
            TargetCurrency = request.TargetCurrency.ToUpper(),
            BuyRate = request.BuyRate,
            SellRate = request.SellRate,
            MidRate = midRate,
            OfficialRate = request.OfficialRate,
            RateDate = request.RateDate.Date,
            Source = request.Source,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.FxRates.Add(rate);
        await _context.SaveChangesAsync();

        return MapToDto(rate);
    }

    public async Task<FxRateDto> UpdateRateAsync(int id, UpdateFxRateRequest request)
    {
        var rate = await _context.FxRates.FindAsync(id);
        if (rate == null)
            throw new InvalidOperationException($"FX rate with ID {id} not found");

        rate.BuyRate = request.BuyRate;
        rate.SellRate = request.SellRate;
        rate.MidRate = request.MidRate ?? (request.BuyRate + request.SellRate) / 2;
        rate.OfficialRate = request.OfficialRate;
        rate.Notes = request.Notes;
        rate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(rate);
    }

    public async Task<FxRateDto?> GetRateAsync(int id)
    {
        var rate = await _context.FxRates.FindAsync(id);
        return rate != null ? MapToDto(rate) : null;
    }

    public async Task<List<FxRateDto>> GetAllRatesAsync(DateTime? rateDate = null, bool activeOnly = true)
    {
        var query = _context.FxRates.AsQueryable();

        if (rateDate.HasValue)
            query = query.Where(r => r.RateDate.Date == rateDate.Value.Date);

        if (activeOnly)
            query = query.Where(r => r.IsActive);

        var rates = await query
            .OrderByDescending(r => r.RateDate)
            .ThenBy(r => r.TargetCurrency)
            .ToListAsync();

        return rates.Select(MapToDto).ToList();
    }

    public async Task<FxRateDto?> GetLatestRateAsync(string baseCurrency, string targetCurrency)
    {
        var rate = await _context.FxRates
            .Where(r => r.BaseCurrency == baseCurrency.ToUpper() 
                     && r.TargetCurrency == targetCurrency.ToUpper()
                     && r.IsActive)
            .OrderByDescending(r => r.RateDate)
            .FirstOrDefaultAsync();

        return rate != null ? MapToDto(rate) : null;
    }

    public async Task<List<FxRateHistoryDto>> GetRateHistoryAsync(
        string baseCurrency, 
        string targetCurrency, 
        DateTime fromDate, 
        DateTime toDate)
    {
        var rates = await _context.FxRates
            .Where(r => r.BaseCurrency == baseCurrency.ToUpper()
                     && r.TargetCurrency == targetCurrency.ToUpper()
                     && r.RateDate >= fromDate.Date
                     && r.RateDate <= toDate.Date)
            .OrderBy(r => r.RateDate)
            .Select(r => new FxRateHistoryDto(
                r.RateDate,
                r.BuyRate,
                r.SellRate,
                r.MidRate,
                r.OfficialRate,
                r.Source
            ))
            .ToListAsync();

        return rates;
    }

    public async Task<decimal> ConvertCurrencyAsync(
        decimal amount, 
        string fromCurrency, 
        string toCurrency, 
        DateTime? rateDate = null)
    {
        if (fromCurrency.ToUpper() == toCurrency.ToUpper())
            return amount;

        var targetRateDate = rateDate?.Date ?? DateTime.UtcNow.Date;

        // Try to find direct rate
        var rate = await _context.FxRates
            .Where(r => r.BaseCurrency == fromCurrency.ToUpper()
                     && r.TargetCurrency == toCurrency.ToUpper()
                     && r.RateDate <= targetRateDate
                     && r.IsActive)
            .OrderByDescending(r => r.RateDate)
            .FirstOrDefaultAsync();

        if (rate != null)
        {
            return amount * rate.MidRate ?? rate.BuyRate;
        }

        // Try reverse rate
        var reverseRate = await _context.FxRates
            .Where(r => r.BaseCurrency == toCurrency.ToUpper()
                     && r.TargetCurrency == fromCurrency.ToUpper()
                     && r.RateDate <= targetRateDate
                     && r.IsActive)
            .OrderByDescending(r => r.RateDate)
            .FirstOrDefaultAsync();

        if (reverseRate != null)
        {
            var midRate = reverseRate.MidRate ?? reverseRate.SellRate;
            return amount / midRate;
        }

        throw new InvalidOperationException(
            $"No exchange rate found for {fromCurrency} to {toCurrency} as of {targetRateDate:yyyy-MM-dd}");
    }

    public async Task<bool> DeleteRateAsync(int id)
    {
        var rate = await _context.FxRates.FindAsync(id);
        if (rate == null)
            return false;

        _context.FxRates.Remove(rate);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<FxRateDto>> SyncRatesFromBogAsync(DateTime rateDate)
    {
        _logger.LogInformation("Syncing FX rates from Bank of Ghana for date: {RateDate}", rateDate);

        try
        {
            // Bank of Ghana API integration
            // Note: As of 2025, BoG doesn't have a public REST API for FX rates
            // This is a placeholder for future integration or custom implementation
            
            var bogRates = await FetchBogRatesAsync(rateDate);

            var createdRates = new List<FxRateDto>();

            foreach (var bogRate in bogRates)
            {
                var request = new CreateFxRateRequest(
                    BaseCurrency: "GHS",
                    TargetCurrency: bogRate.Currency,
                    BuyRate: bogRate.Rate * 0.995m, // Apply small spread
                    SellRate: bogRate.Rate * 1.005m,
                    MidRate: bogRate.Rate,
                    OfficialRate: bogRate.Rate,
                    RateDate: bogRate.Date,
                    Source: "Bank of Ghana API",
                    Notes: "Auto-synced from BoG"
                );

                var rate = await CreateRateAsync(request);
                createdRates.Add(rate);
            }

            _logger.LogInformation("Successfully synced {Count} rates from BoG", createdRates.Count);
            return createdRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing FX rates from Bank of Ghana");
            throw;
        }
    }

    public async Task DeactivateOldRatesAsync(DateTime newRateDate, string baseCurrency, string targetCurrency)
    {
        var oldRates = await _context.FxRates
            .Where(r => r.BaseCurrency == baseCurrency.ToUpper()
                     && r.TargetCurrency == targetCurrency.ToUpper()
                     && r.RateDate < newRateDate.Date
                     && r.IsActive)
            .ToListAsync();

        foreach (var rate in oldRates)
        {
            rate.IsActive = false;
            rate.UpdatedAt = DateTime.UtcNow;
        }

        if (oldRates.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Deactivated {Count} old rates for {Base}/{Target}", 
                oldRates.Count, baseCurrency, targetCurrency);
        }
    }

    private async Task<List<BogFxRateResponse>> FetchBogRatesAsync(DateTime rateDate)
    {
        // TODO: Implement actual Bank of Ghana API integration
        // For now, return sample rates for common currencies
        // In production, this would call: https://www.bog.gov.gh/treasury-and-the-markets/daily-fx-rates/
        
        _logger.LogWarning("Using sample FX rates - BoG API not yet integrated");

        // Sample rates (as of early 2025 approximate values)
        return await Task.FromResult(new List<BogFxRateResponse>
        {
            new BogFxRateResponse("USD", 11.50m, rateDate),
            new BogFxRateResponse("EUR", 13.00m, rateDate),
            new BogFxRateResponse("GBP", 15.20m, rateDate),
            new BogFxRateResponse("NGN", 0.014m, rateDate), // 100 NGN to GHS
            new BogFxRateResponse("XOF", 0.019m, rateDate)  // 100 XOF to GHS
        });

        /* Production implementation example:
        if (_httpClientFactory == null)
            throw new InvalidOperationException("HttpClientFactory not configured");

        var client = _httpClientFactory.CreateClient("BankOfGhana");
        var response = await client.GetAsync($"/api/fx-rates?date={rateDate:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();
        
        var rates = await response.Content.ReadFromJsonAsync<List<BogFxRateResponse>>();
        return rates ?? new List<BogFxRateResponse>();
        */
    }

    private static FxRateDto MapToDto(FxRate rate)
    {
        return new FxRateDto(
            rate.Id,
            rate.BaseCurrency,
            rate.TargetCurrency,
            rate.BuyRate,
            rate.SellRate,
            rate.MidRate,
            rate.OfficialRate,
            rate.RateDate,
            rate.Source,
            rate.IsActive,
            rate.Notes
        );
    }
}
