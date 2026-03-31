using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public interface IFeeService
{
    Task<AccountFeeDto> AssessAccountFeeAsync(AssessAccountFeeRequest request);
    Task<List<ProductChargeAssessmentDto>> GetApplicableAccountChargesAsync(string accountId, string? applyOn = null);
    Task<AccountChargeDto> ApplyAccountChargeAsync(ApplyAccountChargeRequest request);
}

public class FeeService : IFeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ISuspiciousActivityService _suspiciousActivityService;

    public FeeService(
        ApplicationDbContext context,
        IAuditLoggingService auditLoggingService,
        ISuspiciousActivityService suspiciousActivityService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _suspiciousActivityService = suspiciousActivityService;
    }

    public async Task<AccountFeeDto> AssessAccountFeeAsync(AssessAccountFeeRequest request)
    {
        var matchingConfiguredCharge = await _context.Accounts
            .Where(a => a.Id == request.AccountId)
            .SelectMany(a => _context.ProductChargeDefinitions.Where(c =>
                c.ProductId == a.ProductCode &&
                c.Code == request.FeeCode &&
                c.Status == "ACTIVE"))
            .Select(c => c.Code)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(matchingConfiguredCharge))
        {
            var appliedCharge = await ApplyAccountChargeAsync(new ApplyAccountChargeRequest
            {
                AccountId = request.AccountId,
                ChargeCode = matchingConfiguredCharge,
                OverrideAmount = request.Amount,
                Narration = request.Narration,
                ClientReference = request.ClientReference,
            });

            return new AccountFeeDto
            {
                TransactionId = appliedCharge.TransactionId,
                AccountId = appliedCharge.AccountId,
                FeeCode = appliedCharge.ChargeCode,
                Amount = appliedCharge.Amount,
                Narration = appliedCharge.Narration,
                PostedAt = appliedCharge.PostedAt
            };
        }

        return await AssessManualAccountFeeAsync(request);
    }

    public async Task<List<ProductChargeAssessmentDto>> GetApplicableAccountChargesAsync(string accountId, string? applyOn = null)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null)
        {
            throw new InvalidOperationException("Account not found");
        }

        if (string.IsNullOrWhiteSpace(account.ProductCode))
        {
            return new List<ProductChargeAssessmentDto>();
        }

        var normalizedApplyOn = string.IsNullOrWhiteSpace(applyOn) ? null : applyOn.Trim().ToUpperInvariant();

        return await _context.ProductChargeDefinitions
            .AsNoTracking()
            .Where(charge => charge.ProductId == account.ProductCode && charge.Status == "ACTIVE")
            .Where(charge => normalizedApplyOn == null || charge.ApplyOn == normalizedApplyOn || charge.ApplyOn == "MANUAL")
            .OrderBy(charge => charge.ChargeType)
            .ThenBy(charge => charge.Name)
            .Select(charge => new ProductChargeAssessmentDto
            {
                Id = charge.Id,
                ProductCode = charge.ProductId,
                ChargeCode = charge.Code,
                ChargeName = charge.Name,
                ChargeType = charge.ChargeType,
                CalculationType = charge.CalculationType,
                FlatAmount = charge.FlatAmount,
                Rate = charge.Rate,
                MinimumAmount = charge.MinimumAmount,
                MaximumAmount = charge.MaximumAmount,
                ApplyOn = charge.ApplyOn,
                Status = charge.Status
            })
            .ToListAsync();
    }

    public async Task<AccountChargeDto> ApplyAccountChargeAsync(ApplyAccountChargeRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            if (account.Status != "ACTIVE" && account.Status != "Active")
            {
                throw new InvalidOperationException("Account must be ACTIVE to assess fees");
            }

            if (string.IsNullOrWhiteSpace(account.ProductCode))
            {
                throw new InvalidOperationException("Account product is not configured for charges");
            }

            var charge = await _context.ProductChargeDefinitions
                .FirstOrDefaultAsync(c =>
                    c.ProductId == account.ProductCode &&
                    c.Code == request.ChargeCode &&
                    c.Status == "ACTIVE");

            if (charge == null)
            {
                throw new InvalidOperationException("Charge definition not found for the selected product");
            }

            var calculatedAmount = CalculateChargeAmount(charge, request.BaseAmount, request.OverrideAmount);
            if (calculatedAmount <= 0)
            {
                throw new InvalidOperationException("Charge amount must be greater than zero");
            }

            var chargeReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"{charge.ChargeType}-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}"
                : $"{charge.ChargeType}-{request.ClientReference.Trim()}";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existingTxn = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Reference == chargeReference && t.Type == charge.ChargeType);

                if (existingTxn != null)
                {
                    return new AccountChargeDto
                    {
                        TransactionId = existingTxn.Id,
                        AccountId = existingTxn.AccountId ?? string.Empty,
                        ProductCode = account.ProductCode,
                        ChargeCode = charge.Code,
                        ChargeName = charge.Name,
                        ChargeType = charge.ChargeType,
                        Amount = existingTxn.Amount,
                        Narration = existingTxn.Narration ?? string.Empty,
                        PostedAt = existingTxn.Date
                    };
                }
            }

            var availableBalance = account.Balance - account.LienAmount;
            if (availableBalance < calculatedAmount)
            {
                throw new InvalidOperationException($"Insufficient available balance. Account has {availableBalance:N2} available (Balance: {account.Balance:N2}, Lien: {account.LienAmount:N2})");
            }

            var narration = string.IsNullOrWhiteSpace(request.Narration)
                ? $"{charge.ChargeType}: {charge.Name}"
                : request.Narration.Trim();

            var txn = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = request.AccountId,
                Type = charge.ChargeType,
                Amount = calculatedAmount,
                Narration = narration,
                Date = DateTime.UtcNow,
                Status = "POSTED",
                Reference = chargeReference,
                TellerId = null
            };

            account.Balance = Math.Round(account.Balance - calculatedAmount, 2, MidpointRounding.AwayFromZero);
            account.LastTransDate = DateTime.UtcNow;

            _context.Transactions.Add(txn);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _auditLoggingService.LogActionAsync(
                action: $"{charge.ChargeType}_APPLIED",
                entityType: "ACCOUNT",
                entityId: request.AccountId,
                userId: null,
                description: $"{charge.ChargeType} {charge.Code} applied on account {request.AccountId}",
                status: "SUCCESS",
                newValues: new { txn.Id, charge.Code, calculatedAmount, account.Balance, chargeReference, account.ProductCode });

            await _suspiciousActivityService.HandleLargeTransactionAsync(request.AccountId, calculatedAmount, charge.ChargeType, null);

            return new AccountChargeDto
            {
                TransactionId = txn.Id,
                AccountId = request.AccountId,
                ProductCode = account.ProductCode,
                ChargeCode = charge.Code,
                ChargeName = charge.Name,
                ChargeType = charge.ChargeType,
                Amount = calculatedAmount,
                Narration = narration,
                PostedAt = txn.Date
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "ACCOUNT_CHARGE_FAILED",
                entityType: "ACCOUNT",
                entityId: request.AccountId,
                userId: null,
                description: $"Failed to apply charge on account {request.AccountId}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { request.AccountId, request.ChargeCode, request.OverrideAmount, request.ClientReference });

            throw;
        }
    }

    private async Task<AccountFeeDto> AssessManualAccountFeeAsync(AssessAccountFeeRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            if (account.Status != "ACTIVE" && account.Status != "Active")
            {
                throw new InvalidOperationException("Account must be ACTIVE to assess fees");
            }

            if (request.Amount <= 0)
            {
                throw new InvalidOperationException("Fee amount must be greater than zero");
            }

            var feeReference = string.IsNullOrWhiteSpace(request.ClientReference)
                ? $"FEE-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}"
                : $"FEE-{request.ClientReference.Trim()}";

            if (!string.IsNullOrWhiteSpace(request.ClientReference))
            {
                var existingTxn = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Reference == feeReference && t.Type == "FEE");

                if (existingTxn != null)
                {
                    return new AccountFeeDto
                    {
                        TransactionId = existingTxn.Id,
                        AccountId = existingTxn.AccountId ?? string.Empty,
                        FeeCode = request.FeeCode,
                        Amount = existingTxn.Amount,
                        Narration = existingTxn.Narration ?? string.Empty,
                        PostedAt = existingTxn.Date
                    };
                }
            }

            var availableBalance = account.Balance - account.LienAmount;
            if (availableBalance < request.Amount)
            {
                throw new InvalidOperationException($"Insufficient available balance. Account has {availableBalance:N2} available (Balance: {account.Balance:N2}, Lien: {account.LienAmount:N2})");
            }

            var narration = string.IsNullOrWhiteSpace(request.Narration)
                ? $"Fee: {request.FeeCode}"
                : request.Narration.Trim();

            var txn = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = request.AccountId,
                Type = "FEE",
                Amount = request.Amount,
                Narration = narration,
                Date = DateTime.UtcNow,
                Status = "POSTED",
                Reference = feeReference,
                TellerId = null
            };

            account.Balance = Math.Round(account.Balance - request.Amount, 2, MidpointRounding.AwayFromZero);
            account.LastTransDate = DateTime.UtcNow;

            _context.Transactions.Add(txn);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _auditLoggingService.LogActionAsync(
                action: "FEE_ASSESSED",
                entityType: "ACCOUNT",
                entityId: request.AccountId,
                userId: null,
                description: $"Fee {request.FeeCode} assessed on account {request.AccountId}",
                status: "SUCCESS",
                newValues: new { txn.Id, request.FeeCode, request.Amount, account.Balance, feeReference });

            await _suspiciousActivityService.HandleLargeTransactionAsync(request.AccountId, request.Amount, "FEE", null);

            return new AccountFeeDto
            {
                TransactionId = txn.Id,
                AccountId = request.AccountId,
                FeeCode = request.FeeCode,
                Amount = request.Amount,
                Narration = narration,
                PostedAt = txn.Date
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();

            await _auditLoggingService.LogActionAsync(
                action: "FEE_ASSESSMENT_FAILED",
                entityType: "ACCOUNT",
                entityId: request.AccountId,
                userId: null,
                description: $"Failed to assess fee on account {request.AccountId}",
                status: "FAILED",
                errorMessage: ex.Message,
                newValues: new { request.AccountId, request.FeeCode, request.Amount, request.ClientReference });

            throw;
        }
    }

    private static decimal CalculateChargeAmount(ProductChargeDefinition charge, decimal? baseAmount, decimal? overrideAmount)
    {
        decimal amount;

        if (overrideAmount.HasValue && overrideAmount.Value > 0)
        {
            amount = overrideAmount.Value;
        }
        else if (string.Equals(charge.CalculationType, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
        {
            var effectiveBase = baseAmount ?? 0m;
            if (effectiveBase <= 0 || !charge.Rate.HasValue || charge.Rate.Value <= 0)
            {
                throw new InvalidOperationException("A positive base amount is required for percentage-based charges.");
            }

            amount = Math.Round(effectiveBase * (charge.Rate.Value / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            amount = charge.FlatAmount ?? 0m;
        }

        if (charge.MinimumAmount.HasValue && amount < charge.MinimumAmount.Value)
        {
            amount = charge.MinimumAmount.Value;
        }

        if (charge.MaximumAmount.HasValue && amount > charge.MaximumAmount.Value)
        {
            amount = charge.MaximumAmount.Value;
        }

        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
