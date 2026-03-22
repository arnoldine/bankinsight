using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface ICashIncidentService
{
    Task<List<CashIncidentDto>> GetIncidentsAsync(string? branchId = null, string? status = null);
    Task<CashIncidentDto> CreateIncidentAsync(CreateCashIncidentRequest request, string reportedBy);
    Task<CashIncidentDto> ResolveIncidentAsync(string incidentId, string resolvedBy, string resolutionNote);
    Task<CashIncidentDto> ReportSystemIncidentAsync(string branchId, string storeType, string storeId, string incidentType, string currency, decimal amount, string? reference, string? narration, string? reportedBy);
}

public class CashIncidentService : ICashIncidentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public CashIncidentService(ApplicationDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    public async Task<List<CashIncidentDto>> GetIncidentsAsync(string? branchId = null, string? status = null)
    {
        var query = _context.CashIncidents
            .Include(incident => incident.Branch)
            .Include(incident => incident.ReportedByStaff)
            .Include(incident => incident.ResolvedByStaff)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(branchId))
        {
            query = query.Where(incident => incident.BranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(incident => incident.Status == status.ToUpperInvariant());
        }

        var incidents = await query
            .OrderByDescending(incident => incident.ReportedAt)
            .Take(100)
            .ToListAsync();

        return incidents.Select(MapToDto).ToList();
    }

    public Task<CashIncidentDto> CreateIncidentAsync(CreateCashIncidentRequest request, string reportedBy)
    {
        return ReportSystemIncidentAsync(
            request.BranchId,
            request.StoreType,
            request.StoreId,
            request.IncidentType,
            request.Currency,
            request.Amount,
            request.Reference,
            request.Narration,
            reportedBy);
    }

    public async Task<CashIncidentDto> ResolveIncidentAsync(string incidentId, string resolvedBy, string resolutionNote)
    {
        var incident = await _context.CashIncidents
            .Include(item => item.Branch)
            .Include(item => item.ReportedByStaff)
            .Include(item => item.ResolvedByStaff)
            .FirstOrDefaultAsync(item => item.Id == incidentId);

        if (incident == null)
        {
            throw new InvalidOperationException("Cash incident not found.");
        }

        incident.Status = "RESOLVED";
        incident.ResolvedBy = resolvedBy;
        incident.ResolvedAt = DateTime.UtcNow;
        incident.Narration = string.IsNullOrWhiteSpace(incident.Narration)
            ? resolutionNote
            : $"{incident.Narration}\nRESOLUTION: {resolutionNote}";

        await _context.SaveChangesAsync();
        await _auditLoggingService.LogActionAsync(
            action: "CASH_INCIDENT_RESOLVED",
            entityType: "CASH_INCIDENT",
            entityId: incident.Id,
            userId: resolvedBy,
            description: $"Cash incident {incident.Id} resolved",
            newValues: new { incident.Status, incident.ResolvedAt, ResolutionNote = resolutionNote });

        incident = await _context.CashIncidents
            .Include(item => item.Branch)
            .Include(item => item.ReportedByStaff)
            .Include(item => item.ResolvedByStaff)
            .FirstAsync(item => item.Id == incidentId);

        return MapToDto(incident);
    }

    public async Task<CashIncidentDto> ReportSystemIncidentAsync(string branchId, string storeType, string storeId, string incidentType, string currency, decimal amount, string? reference, string? narration, string? reportedBy)
    {
        var normalizedIncidentType = incidentType.Trim().ToUpperInvariant();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "GHS" : currency.Trim().ToUpperInvariant();
        var normalizedStoreType = storeType.Trim().ToUpperInvariant();

        var incident = new CashIncident
        {
            Id = $"CIN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            BranchId = branchId,
            StoreType = normalizedStoreType,
            StoreId = storeId,
            IncidentType = normalizedIncidentType,
            Currency = normalizedCurrency,
            Amount = Math.Abs(amount),
            Status = "OPEN",
            Reference = reference,
            Narration = narration,
            ReportedBy = reportedBy,
            ReportedAt = DateTime.UtcNow,
        };

        _context.CashIncidents.Add(incident);
        await _context.SaveChangesAsync();
        await _auditLoggingService.LogActionAsync(
            action: "CASH_INCIDENT_REPORTED",
            entityType: "CASH_INCIDENT",
            entityId: incident.Id,
            userId: reportedBy,
            description: $"Cash incident {incident.Id} reported",
            newValues: new { incident.BranchId, incident.StoreType, incident.StoreId, incident.IncidentType, incident.Amount, incident.Reference });

        incident = await _context.CashIncidents
            .Include(item => item.Branch)
            .Include(item => item.ReportedByStaff)
            .Include(item => item.ResolvedByStaff)
            .FirstAsync(item => item.Id == incident.Id);

        return MapToDto(incident);
    }

    private static CashIncidentDto MapToDto(CashIncident incident)
    {
        return new CashIncidentDto
        {
            Id = incident.Id,
            BranchId = incident.BranchId,
            BranchName = incident.Branch?.Name ?? string.Empty,
            StoreType = incident.StoreType,
            StoreId = incident.StoreId,
            IncidentType = incident.IncidentType,
            Currency = incident.Currency,
            Amount = incident.Amount,
            Status = incident.Status,
            Reference = incident.Reference,
            Narration = incident.Narration,
            ReportedBy = incident.ReportedBy,
            ReportedByName = incident.ReportedByStaff?.Name,
            ResolvedBy = incident.ResolvedBy,
            ResolvedByName = incident.ResolvedByStaff?.Name,
            ReportedAt = incident.ReportedAt,
            ResolvedAt = incident.ResolvedAt,
        };
    }
}
