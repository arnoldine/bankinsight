using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/bankingos")]
public class BankingOSController : ControllerBase
{
    private readonly BankingOSMetadataService _metadataService;
    private readonly ProcessTaskService _processTaskService;
    private readonly ProductService _productService;
    private readonly ProcessRuntimeService _runtimeService;
    private readonly Security.ICurrentUserContext _currentUser;

    public BankingOSController(
        BankingOSMetadataService metadataService,
        ProcessTaskService processTaskService,
        ProductService productService,
        ProcessRuntimeService runtimeService,
        Security.ICurrentUserContext currentUser)
    {
        _metadataService = metadataService;
        _processTaskService = processTaskService;
        _productService = productService;
        _runtimeService = runtimeService;
        _currentUser = currentUser;
    }

    [HttpGet("process-pack")]
    public async Task<IActionResult> GetProcessPack()
    {
        var processPack = await _metadataService.GetProcessPackAsync();
        return Ok(processPack);
    }

    [HttpGet("processes/{code}")]
    public async Task<IActionResult> GetProcessByCode(string code)
    {
        var process = await _metadataService.GetProcessByCodeAsync(code);
        if (process == null)
        {
            return NotFound(new { message = $"No BankingOS process found for code '{code}'." });
        }

        return Ok(process);
    }

    [HttpGet("processes/{code}/launch-context")]
    public async Task<IActionResult> GetLaunchContext(string code)
    {
        var context = await _metadataService.GetLaunchContextAsync(code);
        if (context == null)
        {
            return NotFound(new { message = $"No BankingOS launch context found for process '{code}'." });
        }

        return Ok(context);
    }

    [HttpPost("processes/{code}/launch")]
    public async Task<IActionResult> LaunchProcess(string code, [FromBody] BankingOSLaunchProcessRequest request)
    {
        var validationErrors = await _metadataService.ValidateLaunchRequestAsync(code, request.PayloadJson);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { message = "Launch validation failed.", errors = validationErrors });
        }

        var instance = await _runtimeService.StartProcessAsync(new StartProcessRequest
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CorrelationId = request.CorrelationId,
            PayloadJson = request.PayloadJson
        }, _currentUser.UserId, code);

        return Ok(new { instanceId = instance.Id, status = instance.Status });
    }

    [HttpGet("process-catalog")]
    public async Task<IActionResult> GetProcessCatalog()
    {
        var catalog = await _metadataService.GetProcessCatalogAsync();
        return Ok(catalog);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _productService.GetProductsAsync();
        return Ok(products.Select(MapProduct));
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] BankingOSProductConfigurationDto request)
    {
        var product = await _productService.CreateProductAsync(MapCreateProductRequest(request));
        return StatusCode(201, MapProduct(product));
    }

    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] BankingOSProductConfigurationDto request)
    {
        var product = await _productService.UpdateProductAsync(id, MapUpdateProductRequest(request));
        if (product == null)
        {
            return NotFound(new { message = $"No BankingOS product found for id '{id}'." });
        }

        return Ok(MapProduct(product));
    }

    [HttpGet("tasks/{taskId}/context")]
    public async Task<IActionResult> GetTaskContext(Guid taskId)
    {
        var task = await GetAccessibleTaskAsync(taskId);

        if (task == null)
        {
            return NotFound(new { message = $"No accessible BankingOS task found for id '{taskId}'." });
        }

        var context = await _metadataService.GetTaskContextAsync(task);
        if (context == null)
        {
            return NotFound(new { message = $"No BankingOS runtime context could be resolved for task '{taskId}'." });
        }

        return Ok(context);
    }

    [HttpPost("tasks/{taskId}/complete")]
    public async Task<IActionResult> CompleteTaskFromWorkbench(Guid taskId, [FromBody] BankingOSTaskActionRequest request)
    {
        var task = await GetAccessibleTaskAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = $"No accessible BankingOS task found for id '{taskId}'." });
        }

        var (context, errors) = await _metadataService.ValidateTaskActionAsync(task, "complete", request.PayloadJson);
        if (context == null)
        {
            return BadRequest(new { message = "Task context could not be resolved.", errors });
        }

        if (errors.Count > 0)
        {
            return BadRequest(new { message = "Task validation failed.", errors });
        }

        await _processTaskService.CompleteTaskAsync(taskId, _currentUser.UserId, new CompleteTaskRequest
        {
            Outcome = context.CompletionOutcome,
            Remarks = request.Remarks,
            PayloadJson = request.PayloadJson
        });

        return Ok(new { message = "Task completed successfully." });
    }

    [HttpPost("tasks/{taskId}/reject")]
    public async Task<IActionResult> RejectTaskFromWorkbench(Guid taskId, [FromBody] BankingOSTaskActionRequest request)
    {
        var task = await GetAccessibleTaskAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = $"No accessible BankingOS task found for id '{taskId}'." });
        }

        var (context, errors) = await _metadataService.ValidateTaskActionAsync(task, "reject", request.PayloadJson);
        if (context == null)
        {
            return BadRequest(new { message = "Task context could not be resolved.", errors });
        }

        if (errors.Count > 0)
        {
            return BadRequest(new { message = "Task validation failed.", errors });
        }

        await _processTaskService.RejectTaskAsync(taskId, _currentUser.UserId, new CompleteTaskRequest
        {
            Outcome = "Reject",
            Remarks = request.Remarks,
            PayloadJson = request.PayloadJson
        });

        return Ok(new { message = "Task rejected successfully." });
    }

    [HttpGet("forms")]
    public async Task<IActionResult> GetSeedForms()
    {
        var forms = await _metadataService.GetSeedFormsAsync();
        return Ok(forms);
    }

    [HttpGet("forms/{code}")]
    public async Task<IActionResult> GetSeedFormByCode(string code)
    {
        var form = await _metadataService.GetSeedFormByCodeAsync(code);
        if (form == null)
        {
            return NotFound(new { message = $"No BankingOS form found for code '{code}'." });
        }

        return Ok(form);
    }

    [HttpGet("themes")]
    public async Task<IActionResult> GetThemePack()
    {
        var themes = await _metadataService.GetThemePackAsync();
        return Ok(themes);
    }

    [HttpGet("theme-catalog")]
    public async Task<IActionResult> GetThemeCatalog()
    {
        var catalog = await _metadataService.GetThemeCatalogAsync();
        return Ok(catalog);
    }

    [HttpGet("publish-bundles")]
    public async Task<IActionResult> GetPublishBundles()
    {
        var bundles = await _metadataService.GetPublishBundlesAsync();
        return Ok(bundles);
    }

    [HttpGet("form-catalog")]
    public async Task<IActionResult> GetFormCatalog()
    {
        var catalog = await _metadataService.GetFormCatalogAsync();
        return Ok(catalog);
    }

    [HttpPost("forms/drafts")]
    public async Task<IActionResult> SaveFormDraft([FromBody] BankingOSSaveFormDraftRequest request)
    {
        var draft = await _metadataService.SaveFormDraftAsync(request);
        return Ok(draft);
    }

    [HttpPost("processes/drafts")]
    public async Task<IActionResult> SaveProcessDraft([FromBody] BankingOSSaveProcessDraftRequest request)
    {
        var draft = await _metadataService.SaveProcessDraftAsync(request);
        return Ok(draft);
    }

    [HttpPost("forms/{code}/publish")]
    public async Task<IActionResult> PublishForm(string code)
    {
        var published = await _metadataService.PublishFormAsync(code);
        if (published == null)
        {
            return NotFound(new { message = $"No BankingOS configured form found for code '{code}'." });
        }

        return Ok(published);
    }

    [HttpPost("processes/{code}/publish")]
    public async Task<IActionResult> PublishProcess(string code)
    {
        var published = await _metadataService.PublishProcessAsync(code);
        if (published == null)
        {
            return NotFound(new { message = $"No BankingOS process found for code '{code}'." });
        }

        return Ok(published);
    }

    [HttpPost("themes/drafts")]
    public async Task<IActionResult> SaveThemeDraft([FromBody] BankingOSSaveThemeDraftRequest request)
    {
        var draft = await _metadataService.SaveThemeDraftAsync(request);
        return Ok(draft);
    }

    [HttpPost("themes/{code}/publish")]
    public async Task<IActionResult> PublishTheme(string code)
    {
        var published = await _metadataService.PublishThemeAsync(code);
        if (published == null)
        {
            return NotFound(new { message = $"No BankingOS configured theme found for code '{code}'." });
        }

        return Ok(published);
    }

    [HttpPost("publish-bundles/{code}/submit")]
    public async Task<IActionResult> SubmitPublishBundle(string code, [FromBody] BankingOSBundleActionRequest request)
    {
        var bundle = await _metadataService.SubmitPublishBundleAsync(code, request);
        if (bundle == null)
        {
            return NotFound(new { message = $"No BankingOS publish bundle found for code '{code}'." });
        }

        return Ok(bundle);
    }

    [HttpPost("publish-bundles/{code}/approve")]
    public async Task<IActionResult> ApprovePublishBundle(string code, [FromBody] BankingOSBundleActionRequest request)
    {
        var bundle = await _metadataService.ApprovePublishBundleAsync(code, request);
        if (bundle == null)
        {
            return NotFound(new { message = $"No BankingOS publish bundle found for code '{code}'." });
        }

        return Ok(bundle);
    }

    [HttpPost("publish-bundles/{code}/reject")]
    public async Task<IActionResult> RejectPublishBundle(string code, [FromBody] BankingOSBundleActionRequest request)
    {
        var bundle = await _metadataService.RejectPublishBundleAsync(code, request);
        if (bundle == null)
        {
            return NotFound(new { message = $"No BankingOS publish bundle found for code '{code}'." });
        }

        return Ok(bundle);
    }

    [HttpPost("publish-bundles/{code}/promote")]
    public async Task<IActionResult> PromotePublishBundle(string code, [FromBody] BankingOSBundleActionRequest request)
    {
        try
        {
            var bundle = await _metadataService.PromotePublishBundleAsync(code, request);
            if (bundle == null)
            {
                return NotFound(new { message = $"No BankingOS publish bundle found for code '{code}'." });
            }

            return Ok(bundle);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private Task<BankInsight.API.Entities.ProcessTask?> GetAccessibleTaskAsync(Guid taskId)
    {
        return _processTaskService.GetAccessibleTaskAsync(
            taskId,
            _currentUser.UserId,
            new List<string>(),
            _currentUser.Permissions.ToList());
    }

    private static BankingOSProductConfigurationDto MapProduct(Product product)
    {
        return new BankingOSProductConfigurationDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Type = product.Type,
            Currency = product.Currency,
            InterestRate = product.InterestRate,
            InterestMethod = product.InterestMethod,
            MinAmount = product.MinAmount,
            MaxAmount = product.MaxAmount,
            MinTerm = product.MinTerm,
            MaxTerm = product.MaxTerm,
            DefaultTerm = product.DefaultTerm,
            Status = product.Status,
            LendingMethodology = product.LendingMethodology,
            IsGroupLoanEnabled = product.IsGroupLoanEnabled,
            SupportsJointLiability = product.SupportsJointLiability,
            RequiresCenter = product.RequiresCenter,
            RequiresGroup = product.RequiresGroup,
            DefaultRepaymentFrequency = product.DefaultRepaymentFrequency,
            AllowedRepaymentFrequencies = DeserializeFrequencies(product.AllowedRepaymentFrequenciesJson).ToList(),
            SupportsWeeklyRepayment = product.SupportsWeeklyRepayment,
            MinimumGroupSize = product.MinimumGroupSize,
            MaximumGroupSize = product.MaximumGroupSize,
            RequiresCompulsorySavings = product.RequiresCompulsorySavings,
            MinimumSavingsToLoanRatio = product.MinimumSavingsToLoanRatio,
            RequiresGroupApprovalMeeting = product.RequiresGroupApprovalMeeting,
            UsesMemberLevelUnderwriting = product.UsesMemberLevelUnderwriting,
            UsesGroupLevelApproval = product.UsesGroupLevelApproval,
            LoanCyclePolicyType = product.LoanCyclePolicyType,
            MaxCycleNumber = product.MaxCycleNumber,
            GraduatedCycleLimitRulesJson = product.GraduatedCycleLimitRulesJson,
            AttendanceRuleType = product.AttendanceRuleType,
            ArrearsEligibilityRuleType = product.ArrearsEligibilityRuleType,
            GroupGuaranteePolicyType = product.GroupGuaranteePolicyType,
            MeetingCollectionMode = product.MeetingCollectionMode,
            AllowBatchDisbursement = product.AllowBatchDisbursement,
            AllowMemberLevelDisbursementAdjustment = product.AllowMemberLevelDisbursementAdjustment,
            AllowTopUpWithinGroup = product.AllowTopUpWithinGroup,
            AllowRescheduleWithinGroup = product.AllowRescheduleWithinGroup,
            GroupPenaltyPolicy = product.GroupPenaltyPolicy,
            GroupDelinquencyPolicy = product.GroupDelinquencyPolicy,
            GroupOfficerAssignmentMode = product.GroupOfficerAssignmentMode,
            GroupRules = product.GroupRule == null ? null : new BankingOSProductGroupRulesDto
            {
                MinMembersRequired = product.GroupRule.MinMembersRequired,
                MaxMembersAllowed = product.GroupRule.MaxMembersAllowed,
                MinWeeks = product.GroupRule.MinWeeks,
                MaxWeeks = product.GroupRule.MaxWeeks,
                RequiresCompulsorySavings = product.GroupRule.RequiresCompulsorySavings,
                MinSavingsToLoanRatio = product.GroupRule.MinSavingsToLoanRatio,
                RequiresGroupApprovalMeeting = product.GroupRule.RequiresGroupApprovalMeeting,
                RequiresJointLiability = product.GroupRule.RequiresJointLiability,
                AllowTopUp = product.GroupRule.AllowTopUp,
                AllowReschedule = product.GroupRule.AllowReschedule,
                MaxCycleNumber = product.GroupRule.MaxCycleNumber,
                CycleIncrementRulesJson = product.GroupRule.CycleIncrementRulesJson,
                DefaultRepaymentFrequency = product.GroupRule.DefaultRepaymentFrequency,
                DefaultInterestMethod = product.GroupRule.DefaultInterestMethod,
                PenaltyPolicyJson = product.GroupRule.PenaltyPolicyJson,
                AttendanceRuleJson = product.GroupRule.AttendanceRuleJson,
                EligibilityRuleJson = product.GroupRule.EligibilityRuleJson,
                MeetingCollectionRuleJson = product.GroupRule.MeetingCollectionRuleJson,
                AllocationOrderJson = product.GroupRule.AllocationOrderJson,
                AccountingProfileJson = product.GroupRule.AccountingProfileJson,
                DisclosureTemplate = product.GroupRule.DisclosureTemplate
            },
            EligibilityRules = product.EligibilityRule == null ? null : new BankingOSProductEligibilityRulesDto
            {
                RequiresKycComplete = product.EligibilityRule.RequiresKycComplete,
                BlockOnSevereArrears = product.EligibilityRule.BlockOnSevereArrears,
                MaxAllowedExposure = product.EligibilityRule.MaxAllowedExposure,
                MinMembershipDays = product.EligibilityRule.MinMembershipDays,
                MinAttendanceRate = product.EligibilityRule.MinAttendanceRate,
                RequireCreditBureauCheck = product.EligibilityRule.RequireCreditBureauCheck,
                CreditBureauProvider = product.EligibilityRule.CreditBureauProvider,
                MinimumCreditScore = product.EligibilityRule.MinimumCreditScore,
                RuleJson = product.EligibilityRule.RuleJson
            }
        };
    }

    private static CreateProductRequest MapCreateProductRequest(BankingOSProductConfigurationDto request)
    {
        return new CreateProductRequest
        {
            Id = request.Id,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Currency = request.Currency,
            InterestRate = request.InterestRate,
            InterestMethod = request.InterestMethod,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            MinTerm = request.MinTerm,
            MaxTerm = request.MaxTerm,
            DefaultTerm = request.DefaultTerm,
            Status = request.Status,
            LendingMethodology = request.LendingMethodology,
            IsGroupLoanEnabled = request.IsGroupLoanEnabled,
            SupportsJointLiability = request.SupportsJointLiability,
            RequiresCenter = request.RequiresCenter,
            RequiresGroup = request.RequiresGroup,
            DefaultRepaymentFrequency = request.DefaultRepaymentFrequency,
            AllowedRepaymentFrequencies = request.AllowedRepaymentFrequencies.ToArray(),
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
            GroupRules = request.GroupRules == null ? null : new ProductGroupRulesDto
            {
                ProductId = request.Id,
                MinMembersRequired = request.GroupRules.MinMembersRequired,
                MaxMembersAllowed = request.GroupRules.MaxMembersAllowed,
                MinWeeks = request.GroupRules.MinWeeks,
                MaxWeeks = request.GroupRules.MaxWeeks,
                RequiresCompulsorySavings = request.GroupRules.RequiresCompulsorySavings,
                MinSavingsToLoanRatio = request.GroupRules.MinSavingsToLoanRatio,
                RequiresGroupApprovalMeeting = request.GroupRules.RequiresGroupApprovalMeeting,
                RequiresJointLiability = request.GroupRules.RequiresJointLiability,
                AllowTopUp = request.GroupRules.AllowTopUp,
                AllowReschedule = request.GroupRules.AllowReschedule,
                MaxCycleNumber = request.GroupRules.MaxCycleNumber,
                CycleIncrementRulesJson = request.GroupRules.CycleIncrementRulesJson,
                DefaultRepaymentFrequency = request.GroupRules.DefaultRepaymentFrequency,
                DefaultInterestMethod = request.GroupRules.DefaultInterestMethod,
                PenaltyPolicyJson = request.GroupRules.PenaltyPolicyJson,
                AttendanceRuleJson = request.GroupRules.AttendanceRuleJson,
                EligibilityRuleJson = request.GroupRules.EligibilityRuleJson,
                MeetingCollectionRuleJson = request.GroupRules.MeetingCollectionRuleJson,
                AllocationOrderJson = request.GroupRules.AllocationOrderJson,
                AccountingProfileJson = request.GroupRules.AccountingProfileJson,
                DisclosureTemplate = request.GroupRules.DisclosureTemplate
            },
            EligibilityRules = request.EligibilityRules == null ? null : new ProductEligibilityRulesDto
            {
                ProductId = request.Id,
                RequiresKycComplete = request.EligibilityRules.RequiresKycComplete,
                BlockOnSevereArrears = request.EligibilityRules.BlockOnSevereArrears,
                MaxAllowedExposure = request.EligibilityRules.MaxAllowedExposure,
                MinMembershipDays = request.EligibilityRules.MinMembershipDays,
                MinAttendanceRate = request.EligibilityRules.MinAttendanceRate,
                RequireCreditBureauCheck = request.EligibilityRules.RequireCreditBureauCheck,
                CreditBureauProvider = request.EligibilityRules.CreditBureauProvider,
                MinimumCreditScore = request.EligibilityRules.MinimumCreditScore,
                RuleJson = request.EligibilityRules.RuleJson
            }
        };
    }

    private static UpdateProductRequest MapUpdateProductRequest(BankingOSProductConfigurationDto request)
    {
        var createRequest = MapCreateProductRequest(request);
        return new UpdateProductRequest
        {
            Id = createRequest.Id,
            Name = createRequest.Name,
            Description = createRequest.Description,
            Type = createRequest.Type,
            Currency = createRequest.Currency,
            InterestRate = createRequest.InterestRate,
            InterestMethod = createRequest.InterestMethod,
            MinAmount = createRequest.MinAmount,
            MaxAmount = createRequest.MaxAmount,
            MinTerm = createRequest.MinTerm,
            MaxTerm = createRequest.MaxTerm,
            DefaultTerm = createRequest.DefaultTerm,
            Status = createRequest.Status,
            LendingMethodology = createRequest.LendingMethodology,
            IsGroupLoanEnabled = createRequest.IsGroupLoanEnabled,
            SupportsJointLiability = createRequest.SupportsJointLiability,
            RequiresCenter = createRequest.RequiresCenter,
            RequiresGroup = createRequest.RequiresGroup,
            DefaultRepaymentFrequency = createRequest.DefaultRepaymentFrequency,
            AllowedRepaymentFrequencies = createRequest.AllowedRepaymentFrequencies,
            SupportsWeeklyRepayment = createRequest.SupportsWeeklyRepayment,
            MinimumGroupSize = createRequest.MinimumGroupSize,
            MaximumGroupSize = createRequest.MaximumGroupSize,
            RequiresCompulsorySavings = createRequest.RequiresCompulsorySavings,
            MinimumSavingsToLoanRatio = createRequest.MinimumSavingsToLoanRatio,
            RequiresGroupApprovalMeeting = createRequest.RequiresGroupApprovalMeeting,
            UsesMemberLevelUnderwriting = createRequest.UsesMemberLevelUnderwriting,
            UsesGroupLevelApproval = createRequest.UsesGroupLevelApproval,
            LoanCyclePolicyType = createRequest.LoanCyclePolicyType,
            MaxCycleNumber = createRequest.MaxCycleNumber,
            GraduatedCycleLimitRulesJson = createRequest.GraduatedCycleLimitRulesJson,
            AttendanceRuleType = createRequest.AttendanceRuleType,
            ArrearsEligibilityRuleType = createRequest.ArrearsEligibilityRuleType,
            GroupGuaranteePolicyType = createRequest.GroupGuaranteePolicyType,
            MeetingCollectionMode = createRequest.MeetingCollectionMode,
            AllowBatchDisbursement = createRequest.AllowBatchDisbursement,
            AllowMemberLevelDisbursementAdjustment = createRequest.AllowMemberLevelDisbursementAdjustment,
            AllowTopUpWithinGroup = createRequest.AllowTopUpWithinGroup,
            AllowRescheduleWithinGroup = createRequest.AllowRescheduleWithinGroup,
            GroupPenaltyPolicy = createRequest.GroupPenaltyPolicy,
            GroupDelinquencyPolicy = createRequest.GroupDelinquencyPolicy,
            GroupOfficerAssignmentMode = createRequest.GroupOfficerAssignmentMode,
            GroupRules = createRequest.GroupRules,
            EligibilityRules = createRequest.EligibilityRules
        };
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
}
