using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class CollateralManagementService
{
    private readonly ApplicationDbContext _context;

    public CollateralManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CollateralManagementSummaryDto> GetSummaryAsync()
    {
        await SynchronizeAsync();

        var collateral = await _context.CollateralRecords
            .AsNoTracking()
            .Include(item => item.Customer)
            .OrderBy(item => item.ValuationExpiryDate)
            .ToListAsync();

        var covenants = await _context.CovenantRecords
            .AsNoTracking()
            .OrderBy(item => item.DueDate)
            .ToListAsync();

        return new CollateralManagementSummaryDto
        {
            CollateralItems = collateral.Select(MapCollateral).ToList(),
            Covenants = covenants.Select(MapCovenant).ToList(),
            ExpiringValuationsCount = collateral.Count(item => item.ValuationExpiryDate != null && item.ValuationExpiryDate <= DateTime.UtcNow.AddDays(30)),
            OverdueCovenantsCount = covenants.Count(item => item.DueDate != null && item.DueDate < DateTime.UtcNow && item.Status != "SATISFIED")
        };
    }

    public async Task<CollateralRecordDto?> UpdateCollateralAsync(string id, UpdateCollateralRecordRequest request)
    {
        var record = await _context.CollateralRecords
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (record == null)
        {
            return null;
        }

        record.CurrentValuation = request.CurrentValuation ?? record.CurrentValuation;
        record.ValuationDate = request.ValuationDate ?? record.ValuationDate;
        record.ValuationExpiryDate = request.ValuationExpiryDate ?? record.ValuationExpiryDate;
        record.PerfectionStatus = string.IsNullOrWhiteSpace(request.PerfectionStatus) ? record.PerfectionStatus : request.PerfectionStatus.Trim().ToUpperInvariant();
        record.DocumentReference = string.IsNullOrWhiteSpace(request.DocumentReference) ? record.DocumentReference : request.DocumentReference.Trim();
        record.CustodyLocation = string.IsNullOrWhiteSpace(request.CustodyLocation) ? record.CustodyLocation : request.CustodyLocation.Trim();
        record.Status = string.IsNullOrWhiteSpace(request.Status) ? record.Status : request.Status.Trim().ToUpperInvariant();
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapCollateral(record);
    }

    public async Task<CovenantRecordDto?> UpdateCovenantAsync(string id, UpdateCovenantRecordRequest request)
    {
        var record = await _context.CovenantRecords.FirstOrDefaultAsync(item => item.Id == id);
        if (record == null)
        {
            return null;
        }

        record.Status = string.IsNullOrWhiteSpace(request.Status) ? record.Status : request.Status.Trim().ToUpperInvariant();
        record.DueDate = request.DueDate ?? record.DueDate;
        record.LastReviewedAt = request.LastReviewedAt ?? DateTime.UtcNow;
        record.Detail = string.IsNullOrWhiteSpace(request.Detail) ? record.Detail : request.Detail.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapCovenant(record);
    }

    private async Task SynchronizeAsync()
    {
        var loans = await _context.Loans
            .AsNoTracking()
            .Where(loan => loan.Status == "ACTIVE" || loan.Status == "APPROVED")
            .ToListAsync();

        foreach (var loan in loans)
        {
            if (!string.IsNullOrWhiteSpace(loan.CollateralType))
            {
                var collateral = await _context.CollateralRecords.FirstOrDefaultAsync(item => item.LoanId == loan.Id);
                if (collateral == null)
                {
                    _context.CollateralRecords.Add(new CollateralRecord
                    {
                        Id = $"COLL-{loan.Id}",
                        LoanId = loan.Id,
                        CustomerId = loan.CustomerId ?? string.Empty,
                        CollateralType = loan.CollateralType,
                        Description = $"Collateral registered from loan {loan.Id}",
                        RegisteredValue = loan.CollateralValue ?? 0m,
                        CurrentValuation = loan.CollateralValue ?? 0m,
                        ValuationDate = loan.DisbursementDate?.ToDateTime(TimeOnly.MinValue),
                        ValuationExpiryDate = loan.DisbursementDate?.ToDateTime(TimeOnly.MinValue).AddYears(1),
                        PerfectionStatus = "PENDING",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            var covenant = await _context.CovenantRecords.FirstOrDefaultAsync(item => item.LoanId == loan.Id);
            if (covenant == null)
            {
                _context.CovenantRecords.Add(new CovenantRecord
                {
                    Id = $"COV-{loan.Id}",
                    LoanId = loan.Id,
                    Name = "Quarterly portfolio review covenant",
                    CovenantType = "REPORTING",
                    Status = "PENDING",
                    DueDate = DateTime.UtcNow.Date.AddDays(90),
                    Detail = "Borrower relationship and collateral position must be reviewed quarterly.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private static CollateralRecordDto MapCollateral(CollateralRecord item)
    {
        return new CollateralRecordDto
        {
            Id = item.Id,
            LoanId = item.LoanId,
            CustomerId = item.CustomerId,
            CustomerName = item.Customer?.Name ?? item.CustomerId,
            CollateralType = item.CollateralType,
            Description = item.Description,
            RegisteredValue = item.RegisteredValue,
            CurrentValuation = item.CurrentValuation,
            ValuationDate = item.ValuationDate,
            ValuationExpiryDate = item.ValuationExpiryDate,
            PerfectionStatus = item.PerfectionStatus,
            DocumentReference = item.DocumentReference,
            CustodyLocation = item.CustodyLocation,
            Status = item.Status
        };
    }

    private static CovenantRecordDto MapCovenant(CovenantRecord item)
    {
        return new CovenantRecordDto
        {
            Id = item.Id,
            LoanId = item.LoanId,
            Name = item.Name,
            CovenantType = item.CovenantType,
            Status = item.Status,
            DueDate = item.DueDate,
            LastReviewedAt = item.LastReviewedAt,
            Detail = item.Detail
        };
    }
}
