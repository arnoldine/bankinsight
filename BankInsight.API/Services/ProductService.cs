using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class ProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        return await _context.Products
            .Include(p => p.GroupRule)
            .Include(p => p.EligibilityRule)
            .Include(p => p.Charges)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<ProductListItemDto>> GetProductListItemsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                ProductCode = p.Id,
                Code = p.Id,
                Name = p.Name,
                Description = p.Description,
                Type = p.Type,
                Category = p.Type,
                Currency = p.Currency,
                InterestRate = p.InterestRate,
                InterestMethod = p.InterestMethod,
                MinAmount = p.MinAmount,
                MaxAmount = p.MaxAmount,
                DefaultTerm = p.DefaultTerm,
                Status = p.Status
            })
            .ToListAsync();
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = request.Id,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Currency = string.IsNullOrEmpty(request.Currency) ? "GHS" : request.Currency,
            InterestRate = request.InterestRate,
            InterestMethod = request.InterestMethod,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            MinTerm = request.MinTerm,
            MaxTerm = request.MaxTerm,
            DefaultTerm = request.DefaultTerm,
            Status = string.IsNullOrEmpty(request.Status) ? "ACTIVE" : request.Status,
            LendingMethodology = string.IsNullOrWhiteSpace(request.LendingMethodology) ? "INDIVIDUAL" : request.LendingMethodology!,
            IsGroupLoanEnabled = request.IsGroupLoanEnabled,
            SupportsJointLiability = request.SupportsJointLiability,
            RequiresCenter = request.RequiresCenter,
            RequiresGroup = request.RequiresGroup,
            DefaultRepaymentFrequency = string.IsNullOrWhiteSpace(request.DefaultRepaymentFrequency) ? "Monthly" : request.DefaultRepaymentFrequency!,
            AllowedRepaymentFrequenciesJson = JsonSerializer.Serialize(request.AllowedRepaymentFrequencies ?? new[] { request.DefaultRepaymentFrequency ?? "Monthly" }),
            SupportsWeeklyRepayment = request.SupportsWeeklyRepayment,
            MinimumGroupSize = request.MinimumGroupSize,
            MaximumGroupSize = request.MaximumGroupSize,
            RequiresCompulsorySavings = request.RequiresCompulsorySavings,
            MinimumSavingsToLoanRatio = request.MinimumSavingsToLoanRatio,
            RequiresGroupApprovalMeeting = request.RequiresGroupApprovalMeeting,
            UsesMemberLevelUnderwriting = request.UsesMemberLevelUnderwriting,
            UsesGroupLevelApproval = request.UsesGroupLevelApproval,
            LoanCyclePolicyType = request.LoanCyclePolicyType,
            MaxCycleNumber = request.MaxCycleNumber,
            GraduatedCycleLimitRulesJson = request.GraduatedCycleLimitRulesJson,
            AttendanceRuleType = request.AttendanceRuleType,
            ArrearsEligibilityRuleType = request.ArrearsEligibilityRuleType,
            GroupGuaranteePolicyType = request.GroupGuaranteePolicyType,
            MeetingCollectionMode = request.MeetingCollectionMode,
            AllowBatchDisbursement = request.AllowBatchDisbursement,
            AllowMemberLevelDisbursementAdjustment = request.AllowMemberLevelDisbursementAdjustment,
            AllowTopUpWithinGroup = request.AllowTopUpWithinGroup,
            AllowRescheduleWithinGroup = request.AllowRescheduleWithinGroup,
            GroupPenaltyPolicy = request.GroupPenaltyPolicy,
            GroupDelinquencyPolicy = request.GroupDelinquencyPolicy,
            GroupOfficerAssignmentMode = request.GroupOfficerAssignmentMode,
        };

        _context.Products.Add(product);
        UpsertRules(product.Id, request);
        UpsertCharges(product.Id, request.Charges);
        await _context.SaveChangesAsync();

        return await _context.Products
            .Include(p => p.GroupRule)
            .Include(p => p.EligibilityRule)
            .Include(p => p.Charges)
            .FirstAsync(p => p.Id == product.Id);
    }

    public async Task<Product?> UpdateProductAsync(string id, UpdateProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.GroupRule)
            .Include(p => p.EligibilityRule)
            .Include(p => p.Charges)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Type = request.Type;
        product.Currency = request.Currency ?? product.Currency;
        product.InterestRate = request.InterestRate;
        product.InterestMethod = request.InterestMethod;
        product.MinAmount = request.MinAmount;
        product.MaxAmount = request.MaxAmount;
        product.MinTerm = request.MinTerm;
        product.MaxTerm = request.MaxTerm;
        product.DefaultTerm = request.DefaultTerm;
        product.Status = request.Status ?? product.Status;
        product.LendingMethodology = string.IsNullOrWhiteSpace(request.LendingMethodology) ? product.LendingMethodology : request.LendingMethodology!;
        product.IsGroupLoanEnabled = request.IsGroupLoanEnabled;
        product.SupportsJointLiability = request.SupportsJointLiability;
        product.RequiresCenter = request.RequiresCenter;
        product.RequiresGroup = request.RequiresGroup;
        product.DefaultRepaymentFrequency = string.IsNullOrWhiteSpace(request.DefaultRepaymentFrequency) ? product.DefaultRepaymentFrequency : request.DefaultRepaymentFrequency!;
        product.AllowedRepaymentFrequenciesJson = JsonSerializer.Serialize(request.AllowedRepaymentFrequencies ?? DeserializeFrequencies(product.AllowedRepaymentFrequenciesJson));
        product.SupportsWeeklyRepayment = request.SupportsWeeklyRepayment;
        product.MinimumGroupSize = request.MinimumGroupSize;
        product.MaximumGroupSize = request.MaximumGroupSize;
        product.RequiresCompulsorySavings = request.RequiresCompulsorySavings;
        product.MinimumSavingsToLoanRatio = request.MinimumSavingsToLoanRatio;
        product.RequiresGroupApprovalMeeting = request.RequiresGroupApprovalMeeting;
        product.UsesMemberLevelUnderwriting = request.UsesMemberLevelUnderwriting;
        product.UsesGroupLevelApproval = request.UsesGroupLevelApproval;
        product.LoanCyclePolicyType = request.LoanCyclePolicyType;
        product.MaxCycleNumber = request.MaxCycleNumber;
        product.GraduatedCycleLimitRulesJson = request.GraduatedCycleLimitRulesJson;
        product.AttendanceRuleType = request.AttendanceRuleType;
        product.ArrearsEligibilityRuleType = request.ArrearsEligibilityRuleType;
        product.GroupGuaranteePolicyType = request.GroupGuaranteePolicyType;
        product.MeetingCollectionMode = request.MeetingCollectionMode;
        product.AllowBatchDisbursement = request.AllowBatchDisbursement;
        product.AllowMemberLevelDisbursementAdjustment = request.AllowMemberLevelDisbursementAdjustment;
        product.AllowTopUpWithinGroup = request.AllowTopUpWithinGroup;
        product.AllowRescheduleWithinGroup = request.AllowRescheduleWithinGroup;
        product.GroupPenaltyPolicy = request.GroupPenaltyPolicy;
        product.GroupDelinquencyPolicy = request.GroupDelinquencyPolicy;
        product.GroupOfficerAssignmentMode = request.GroupOfficerAssignmentMode;

        UpsertRules(product.Id, request);
        UpsertCharges(product.Id, request.Charges);
        await _context.SaveChangesAsync();

        return product;
    }

    private void UpsertRules(string productId, CreateProductRequest request)
    {
        if (request.GroupRules != null)
        {
            var groupRule = _context.ProductGroupRules.Local.FirstOrDefault(r => r.ProductId == productId)
                ?? _context.ProductGroupRules.FirstOrDefault(r => r.ProductId == productId)
                ?? new ProductGroupRule { ProductId = productId };

            groupRule.MinMembersRequired = request.GroupRules.MinMembersRequired;
            groupRule.MaxMembersAllowed = request.GroupRules.MaxMembersAllowed;
            groupRule.MinWeeks = request.GroupRules.MinWeeks;
            groupRule.MaxWeeks = request.GroupRules.MaxWeeks;
            groupRule.RequiresCompulsorySavings = request.GroupRules.RequiresCompulsorySavings;
            groupRule.MinSavingsToLoanRatio = request.GroupRules.MinSavingsToLoanRatio;
            groupRule.RequiresGroupApprovalMeeting = request.GroupRules.RequiresGroupApprovalMeeting;
            groupRule.RequiresJointLiability = request.GroupRules.RequiresJointLiability;
            groupRule.AllowTopUp = request.GroupRules.AllowTopUp;
            groupRule.AllowReschedule = request.GroupRules.AllowReschedule;
            groupRule.MaxCycleNumber = request.GroupRules.MaxCycleNumber;
            groupRule.CycleIncrementRulesJson = request.GroupRules.CycleIncrementRulesJson;
            groupRule.DefaultRepaymentFrequency = request.GroupRules.DefaultRepaymentFrequency;
            groupRule.DefaultInterestMethod = request.GroupRules.DefaultInterestMethod;
            groupRule.PenaltyPolicyJson = request.GroupRules.PenaltyPolicyJson;
            groupRule.AttendanceRuleJson = request.GroupRules.AttendanceRuleJson;
            groupRule.EligibilityRuleJson = request.GroupRules.EligibilityRuleJson;
            groupRule.MeetingCollectionRuleJson = request.GroupRules.MeetingCollectionRuleJson;
            groupRule.AllocationOrderJson = request.GroupRules.AllocationOrderJson;
            groupRule.AccountingProfileJson = request.GroupRules.AccountingProfileJson;
            groupRule.DisclosureTemplate = request.GroupRules.DisclosureTemplate;
            groupRule.UpdatedAt = DateTime.UtcNow;

            if (_context.Entry(groupRule).State == EntityState.Detached)
            {
                _context.ProductGroupRules.Add(groupRule);
            }
        }

        if (request.EligibilityRules != null)
        {
            var eligibilityRule = _context.ProductEligibilityRules.Local.FirstOrDefault(r => r.ProductId == productId)
                ?? _context.ProductEligibilityRules.FirstOrDefault(r => r.ProductId == productId)
                ?? new ProductEligibilityRule { ProductId = productId };

            eligibilityRule.RequiresKycComplete = request.EligibilityRules.RequiresKycComplete;
            eligibilityRule.BlockOnSevereArrears = request.EligibilityRules.BlockOnSevereArrears;
            eligibilityRule.MaxAllowedExposure = request.EligibilityRules.MaxAllowedExposure;
            eligibilityRule.MinMembershipDays = request.EligibilityRules.MinMembershipDays;
            eligibilityRule.MinAttendanceRate = request.EligibilityRules.MinAttendanceRate;
            eligibilityRule.RequireCreditBureauCheck = request.EligibilityRules.RequireCreditBureauCheck;
            eligibilityRule.CreditBureauProvider = request.EligibilityRules.CreditBureauProvider;
            eligibilityRule.MinimumCreditScore = request.EligibilityRules.MinimumCreditScore;
            eligibilityRule.RuleJson = request.EligibilityRules.RuleJson;
            eligibilityRule.UpdatedAt = DateTime.UtcNow;

            if (_context.Entry(eligibilityRule).State == EntityState.Detached)
            {
                _context.ProductEligibilityRules.Add(eligibilityRule);
            }
        }
    }

    private static string[] DeserializeFrequencies(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new[] { "Monthly" };
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? new[] { "Monthly" };
        }
        catch
        {
            return new[] { "Monthly" };
        }
    }

    private void UpsertCharges(string productId, List<ProductChargeDefinitionDto>? charges)
    {
        var existingCharges = _context.ProductChargeDefinitions
            .Where(charge => charge.ProductId == productId)
            .ToList();

        if (charges == null || charges.Count == 0)
        {
            if (existingCharges.Count > 0)
            {
                _context.ProductChargeDefinitions.RemoveRange(existingCharges);
            }

            return;
        }

        var requestedCodes = charges
            .Where(charge => !string.IsNullOrWhiteSpace(charge.Code))
            .Select(charge => charge.Code.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var staleCharge in existingCharges.Where(charge => !requestedCodes.Contains(charge.Code)))
        {
            _context.ProductChargeDefinitions.Remove(staleCharge);
        }

        foreach (var chargeRequest in charges.Where(charge => !string.IsNullOrWhiteSpace(charge.Code)))
        {
            var normalizedCode = chargeRequest.Code.Trim().ToUpperInvariant();
            var charge = existingCharges.FirstOrDefault(existing => existing.Code == normalizedCode)
                ?? new ProductChargeDefinition
                {
                    ProductId = productId,
                    Code = normalizedCode,
                    CreatedAt = DateTime.UtcNow,
                };

            charge.Name = string.IsNullOrWhiteSpace(chargeRequest.Name) ? normalizedCode : chargeRequest.Name.Trim();
            charge.ChargeType = string.IsNullOrWhiteSpace(chargeRequest.ChargeType) ? "FEE" : chargeRequest.ChargeType.Trim().ToUpperInvariant();
            charge.CalculationType = string.IsNullOrWhiteSpace(chargeRequest.CalculationType) ? "FLAT" : chargeRequest.CalculationType.Trim().ToUpperInvariant();
            charge.FlatAmount = chargeRequest.FlatAmount;
            charge.Rate = chargeRequest.Rate;
            charge.MinimumAmount = chargeRequest.MinimumAmount;
            charge.MaximumAmount = chargeRequest.MaximumAmount;
            charge.ApplyOn = string.IsNullOrWhiteSpace(chargeRequest.ApplyOn) ? "MANUAL" : chargeRequest.ApplyOn.Trim().ToUpperInvariant();
            charge.IncomeGlCode = string.IsNullOrWhiteSpace(chargeRequest.IncomeGlCode) ? null : chargeRequest.IncomeGlCode.Trim().ToUpperInvariant();
            charge.Status = string.IsNullOrWhiteSpace(chargeRequest.Status) ? "ACTIVE" : chargeRequest.Status.Trim().ToUpperInvariant();
            charge.UpdatedAt = DateTime.UtcNow;

            if (_context.Entry(charge).State == EntityState.Detached)
            {
                _context.ProductChargeDefinitions.Add(charge);
            }
        }
    }
}

