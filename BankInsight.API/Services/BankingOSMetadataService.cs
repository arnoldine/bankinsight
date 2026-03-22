using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using BankInsight.API.DTOs;
using BankInsight.API.Data;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BankInsight.API.Services;

public class BankingOSMetadataService
{
    private const string BankingOSProcessConfigKeyPrefix = "bankingos.process.";
    private const string BankingOSFormConfigKeyPrefix = "bankingos.form.";
    private const string BankingOSThemeConfigKeyPrefix = "bankingos.theme.";
    private const string BankingOSPublishBundleConfigKeyPrefix = "bankingos.publish-bundle.";
    private readonly IHostEnvironment _environment;
    private readonly ApplicationDbContext _context;
    private readonly ProductService _productService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public BankingOSMetadataService(IHostEnvironment environment, ApplicationDbContext context, ProductService productService)
    {
        _environment = environment;
        _context = context;
        _productService = productService;
    }

    public async Task<BankingOSProcessPackDto> GetProcessPackAsync()
    {
        var path = GetProcessPackPath();
        if (!File.Exists(path))
        {
            return new BankingOSProcessPackDto
            {
                ProductName = "BankingOS",
                Version = 1,
                LifecycleEnvelope = new List<string>(),
                Processes = new List<BankingOSProcessDefinitionDto>()
            };
        }

        await using var stream = File.OpenRead(path);
        var processPack = await JsonSerializer.DeserializeAsync<BankingOSProcessPackDto>(stream, JsonOptions);
        if (processPack == null)
        {
            throw new InvalidOperationException("BankingOS process pack could not be loaded.");
        }

        var mergedProcesses = processPack.Processes
            .Select(NormalizeProcess)
            .ToDictionary(process => process.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var process in (await GetPersistedProcessesAsync()).Select(NormalizeProcess))
        {
            mergedProcesses[process.Code] = process;
        }

        processPack.Processes = mergedProcesses.Values.OrderBy(process => process.Name).ToList();

        return processPack;
    }

    public async Task<BankingOSProcessDefinitionDto?> GetProcessByCodeAsync(string processCode)
    {
        var processPack = await GetProcessPackAsync();
        return processPack.Processes.FirstOrDefault(process =>
            string.Equals(process.Code, processCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<BankingOSLaunchContextDto?> GetLaunchContextAsync(string processCode)
    {
        var process = await GetProcessByCodeAsync(processCode);
        if (process == null)
        {
            return null;
        }

        var primaryFormCode = process.Stages
            .Select(stage => stage.FormCode)
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code));

        BankingOSSeedFormDto? primaryForm = null;
        if (!string.IsNullOrWhiteSpace(primaryFormCode))
        {
            primaryForm = await GetSeedFormByCodeAsync(primaryFormCode);
            primaryForm = await EnrichFormWithProductOptionsAsync(primaryForm, processCode, process.EntityType);
        }

        var productOptions = await BuildLaunchProductOptionsAsync(processCode, process.EntityType);

        return new BankingOSLaunchContextDto
        {
            ProcessCode = process.Code,
            ProcessName = process.Name,
            EntityType = process.EntityType,
            PrimaryForm = primaryForm,
            ProductOptions = productOptions,
            ValidationHints = BuildLaunchValidationHints(processCode, productOptions)
        };
    }

    public async Task<BankingOSTaskContextDto?> GetTaskContextAsync(ProcessTask task)
    {
        var processCode = task.ProcessInstance.ProcessDefinitionVersion.ProcessDefinition.Code;
        var process = await GetProcessByCodeAsync(processCode);
        if (process == null)
        {
            return null;
        }

        var stage = process.Stages.FirstOrDefault(item =>
            string.Equals(item.StageCode, task.ProcessStepDefinition.StepCode, StringComparison.OrdinalIgnoreCase));

        BankingOSSeedFormDto? form = null;
        if (!string.IsNullOrWhiteSpace(stage?.FormCode))
        {
            form = await GetSeedFormByCodeAsync(stage.FormCode);
            form = await EnrichFormWithProductOptionsAsync(form, process.Code, process.EntityType);
        }

        var allowedActions = BuildAllowedTaskActions(task, stage);
        var selectedProduct = await ResolveSelectedProductAsync(task.ProcessInstance, process.Code, process.EntityType);

        return new BankingOSTaskContextDto
        {
            TaskId = task.Id,
            TaskStatus = task.Status,
            StepCode = task.ProcessStepDefinition.StepCode,
            StepName = task.ProcessStepDefinition.StepName,
            StepType = task.ProcessStepDefinition.StepType,
            ProcessCode = process.Code,
            ProcessName = process.Name,
            EntityType = task.ProcessInstance.EntityType,
            EntityId = task.ProcessInstance.EntityId,
            Stage = stage,
            Form = form,
            AllowedActions = allowedActions,
            Actions = BuildTaskActionDescriptors(task, allowedActions),
            RequiresClaim = string.Equals(task.Status, "Claimable", StringComparison.OrdinalIgnoreCase),
            RequiredFieldIds = form?.Fields.Where(field => field.Required).Select(field => field.Id).ToList() ?? new List<string>(),
            ValidationRules = BuildValidationRules(form),
            Screen = BuildTaskScreenSchema(task, process, stage, form),
            SelectedProduct = selectedProduct,
            CompletionOutcome = string.Equals(task.ProcessStepDefinition.StepType, "ApprovalTask", StringComparison.OrdinalIgnoreCase) ? "Approve" : "Complete",
            RejectionAllowed = string.Equals(task.ProcessStepDefinition.StepType, "ApprovalTask", StringComparison.OrdinalIgnoreCase)
        };
    }

    public async Task<(BankingOSTaskContextDto? Context, List<string> Errors)> ValidateTaskActionAsync(ProcessTask task, string action, string? payloadJson)
    {
        var context = await GetTaskContextAsync(task);
        if (context == null)
        {
            return (null, new List<string> { "No BankingOS task context could be resolved." });
        }

        var errors = new List<string>();

        if (context.RequiresClaim)
        {
            errors.Add("This task must be claimed before it can be actioned.");
            return (context, errors);
        }

        if (!context.AllowedActions.Contains(action, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Action '{action}' is not allowed for this task.");
        }

        if (string.Equals(action, "reject", StringComparison.OrdinalIgnoreCase) && !context.RejectionAllowed)
        {
            errors.Add("Reject is not allowed for this task.");
        }

        if (context.Form != null)
        {
            using var document = TryParseJson(payloadJson, out var parseError);
            if (parseError != null)
            {
                errors.Add(parseError);
                return (context, errors);
            }

            if (document == null)
            {
                errors.Add("Task payload is required.");
                return (context, errors);
            }

            var root = document.RootElement;
            if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Task payload must contain a 'data' object.");
                return (context, errors);
            }

            foreach (var field in context.Form.Fields.Where(field => field.Required))
            {
                if (!dataElement.TryGetProperty(field.Id, out var fieldValue) || string.IsNullOrWhiteSpace(fieldValue.ToString()))
                {
                    errors.Add($"{field.Label} is required.");
                }
            }
        }

        return (context, errors);
    }

    public async Task<List<string>> ValidateLaunchRequestAsync(string processCode, string? payloadJson)
    {
        var errors = new List<string>();
        var launchContext = await GetLaunchContextAsync(processCode);
        if (launchContext == null)
        {
            return new List<string> { $"No BankingOS launch context was found for process '{processCode}'." };
        }

        if (launchContext.PrimaryForm == null)
        {
            return errors;
        }

        using var document = TryParseJson(payloadJson, out var parseError);
        if (parseError != null)
        {
            errors.Add(parseError);
            return errors;
        }

        if (document == null)
        {
            errors.Add("Launch payload is required.");
            return errors;
        }

        var root = document.RootElement;
        if (!root.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
        {
            errors.Add("Launch payload must contain a 'data' object.");
            return errors;
        }

        foreach (var field in launchContext.PrimaryForm.Fields.Where(field => field.Required))
        {
            if (!dataElement.TryGetProperty(field.Id, out var fieldValue) || string.IsNullOrWhiteSpace(fieldValue.ToString()))
            {
                errors.Add($"{field.Label} is required.");
            }
        }

        if (dataElement.TryGetProperty("product_code", out var productCodeElement))
        {
            var selectedProductCode = productCodeElement.ToString();
            var selectedProduct = launchContext.ProductOptions.FirstOrDefault(product =>
                string.Equals(product.ProductId, selectedProductCode, StringComparison.OrdinalIgnoreCase));

            if (selectedProduct == null)
            {
                errors.Add("The selected product is not valid for this process.");
                return errors;
            }

            if (dataElement.TryGetProperty("requested_amount", out var requestedAmountElement) &&
                decimal.TryParse(requestedAmountElement.ToString(), out var requestedAmount))
            {
                if (selectedProduct.MinAmount.HasValue && requestedAmount < selectedProduct.MinAmount.Value)
                {
                    errors.Add($"Requested amount is below the product minimum of {selectedProduct.MinAmount.Value}.");
                }

                if (selectedProduct.MaxAmount.HasValue && requestedAmount > selectedProduct.MaxAmount.Value)
                {
                    errors.Add($"Requested amount is above the product maximum of {selectedProduct.MaxAmount.Value}.");
                }
            }

            if (dataElement.TryGetProperty("requested_tenor_months", out var tenorElement) &&
                int.TryParse(tenorElement.ToString(), out var tenor))
            {
                if (selectedProduct.MinTerm.HasValue && tenor < selectedProduct.MinTerm.Value)
                {
                    errors.Add($"Requested tenor is below the product minimum of {selectedProduct.MinTerm.Value} months.");
                }

                if (selectedProduct.MaxTerm.HasValue && tenor > selectedProduct.MaxTerm.Value)
                {
                    errors.Add($"Requested tenor is above the product maximum of {selectedProduct.MaxTerm.Value} months.");
                }
            }

            if (dataElement.TryGetProperty("repayment_frequency", out var frequencyElement))
            {
                var selectedFrequency = frequencyElement.ToString();
                if (!string.IsNullOrWhiteSpace(selectedFrequency) &&
                    selectedProduct.AllowedRepaymentFrequencies.Count > 0 &&
                    !selectedProduct.AllowedRepaymentFrequencies.Contains(selectedFrequency, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Repayment frequency '{selectedFrequency}' is not allowed for product '{selectedProduct.Name}'.");
                }
            }
        }

        return errors;
    }

    public async Task<IReadOnlyList<BankingOSProcessCatalogItemDto>> GetProcessCatalogAsync()
    {
        var processPack = await GetProcessPackAsync();
        var persistedCodes = (await GetPersistedProcessesAsync())
            .Select(process => process.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return processPack.Processes
            .Select(process => new BankingOSProcessCatalogItemDto
            {
                Code = process.Code,
                Name = process.Name,
                Module = process.Module,
                EntityType = process.EntityType,
                TriggerType = process.TriggerType,
                Version = process.Version,
                Status = process.Status,
                IsSeeded = !persistedCodes.Contains(process.Code),
                IsPublished = string.Equals(process.Status, "Published", StringComparison.OrdinalIgnoreCase),
                StageCount = process.Stages.Count
            })
            .OrderBy(process => process.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<BankingOSSeedFormDto>> GetSeedFormsAsync()
    {
        var persistedForms = await GetPersistedFormsAsync();
        var formsDirectory = GetSeedDirectoryPath("forms");
        var forms = new Dictionary<string, BankingOSSeedFormDto>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(formsDirectory))
        {
            foreach (var path in Directory.EnumerateFiles(formsDirectory, "*.json"))
            {
                await using var stream = File.OpenRead(path);
                var form = await JsonSerializer.DeserializeAsync<BankingOSSeedFormDto>(stream, JsonOptions);
                if (form != null)
                {
                    forms[form.Code] = form;
                }
            }
        }

        foreach (var form in persistedForms)
        {
            forms[form.Code] = form;
        }

        return forms.Values.OrderBy(form => form.Name).ToList();
    }

    public async Task<BankingOSSeedFormDto?> GetSeedFormByCodeAsync(string formCode)
    {
        var forms = await GetSeedFormsAsync();
        return forms.FirstOrDefault(form => string.Equals(form.Code, formCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<BankingOSThemePackDto> GetThemePackAsync()
    {
        var themes = (await LoadFileThemePackAsync()).Themes
            .Select(NormalizeTheme)
            .ToDictionary(theme => theme.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var theme in (await GetPersistedThemesAsync()).Select(NormalizeTheme))
        {
            themes[theme.Code] = theme;
        }

        return new BankingOSThemePackDto
        {
            Themes = themes.Values.OrderBy(theme => theme.Name).ToList()
        };
    }

    public async Task<IReadOnlyList<BankingOSPublishBundleDto>> GetPublishBundlesAsync()
    {
        var bundles = (await LoadFilePublishBundlesAsync())
            .Select(NormalizeBundle)
            .ToDictionary(bundle => bundle.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var bundle in (await GetPersistedPublishBundlesAsync()).Select(NormalizeBundle))
        {
            bundles[bundle.Code] = bundle;
        }

        return bundles.Values.OrderBy(bundle => bundle.Name).ToList();
    }

    public async Task<BankingOSPublishBundleDto?> SubmitPublishBundleAsync(string code, BankingOSBundleActionRequest request)
    {
        var bundle = await GetPublishBundleByCodeAsync(code);
        if (bundle == null)
        {
            return null;
        }

        bundle = NormalizeBundle(bundle);
        bundle.Status = bundle.RequiresApproval ? "PendingApproval" : "ReadyToPromote";
        ApplyBundleActionMetadata(bundle, "Submit", request.Actor, request.Notes);
        await UpsertPublishBundleConfigAsync(bundle);
        return bundle;
    }

    public async Task<BankingOSPublishBundleDto?> ApprovePublishBundleAsync(string code, BankingOSBundleActionRequest request)
    {
        var bundle = await GetPublishBundleByCodeAsync(code);
        if (bundle == null)
        {
            return null;
        }

        bundle = NormalizeBundle(bundle);
        bundle.Status = "Approved";
        ApplyBundleActionMetadata(bundle, "Approve", request.Actor, request.Notes);
        await UpsertPublishBundleConfigAsync(bundle);
        return bundle;
    }

    public async Task<BankingOSPublishBundleDto?> RejectPublishBundleAsync(string code, BankingOSBundleActionRequest request)
    {
        var bundle = await GetPublishBundleByCodeAsync(code);
        if (bundle == null)
        {
            return null;
        }

        bundle = NormalizeBundle(bundle);
        bundle.Status = "Rejected";
        ApplyBundleActionMetadata(bundle, "Reject", request.Actor, request.Notes);
        await UpsertPublishBundleConfigAsync(bundle);
        return bundle;
    }

    public async Task<BankingOSPublishBundleDto?> PromotePublishBundleAsync(string code, BankingOSBundleActionRequest request)
    {
        var bundle = await GetPublishBundleByCodeAsync(code);
        if (bundle == null)
        {
            return null;
        }

        bundle = NormalizeBundle(bundle);
        var isPromotionAllowed = !bundle.RequiresApproval
            || string.Equals(bundle.Status, "Approved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(bundle.Status, "ReadyToPromote", StringComparison.OrdinalIgnoreCase);

        if (!isPromotionAllowed)
        {
            throw new InvalidOperationException($"Bundle '{code}' cannot be promoted while in status '{bundle.Status}'.");
        }

        bundle.Status = "Promoted";
        ApplyBundleActionMetadata(bundle, "Promote", request.Actor, request.Notes);
        await UpsertPublishBundleConfigAsync(bundle);
        return bundle;
    }

    public async Task<IReadOnlyList<BankingOSThemeCatalogItemDto>> GetThemeCatalogAsync()
    {
        var seededThemes = (await LoadFileThemePackAsync()).Themes.Select(NormalizeTheme);
        var persistedThemes = (await GetPersistedThemesAsync()).Select(NormalizeTheme);

        var catalog = seededThemes.ToDictionary(
            theme => theme.Code,
            theme => new BankingOSThemeCatalogItemDto
            {
                Code = theme.Code,
                Name = theme.Name,
                Version = theme.Version,
                Status = theme.Status,
                IsSeeded = true,
                IsPublished = string.Equals(theme.Status, "Published", StringComparison.OrdinalIgnoreCase),
                TokenCount = theme.Tokens.Count
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (var theme in persistedThemes)
        {
            catalog[theme.Code] = new BankingOSThemeCatalogItemDto
            {
                Code = theme.Code,
                Name = theme.Name,
                Version = theme.Version,
                Status = theme.Status,
                IsSeeded = false,
                IsPublished = string.Equals(theme.Status, "Published", StringComparison.OrdinalIgnoreCase),
                TokenCount = theme.Tokens.Count
            };
        }

        return catalog.Values.OrderBy(item => item.Name).ToList();
    }

    public async Task<IReadOnlyList<BankingOSFormCatalogItemDto>> GetFormCatalogAsync()
    {
        var seededForms = await LoadFileSeedFormsAsync();
        var persistedForms = await GetPersistedFormsAsync();
        var catalog = seededForms.ToDictionary(
            form => form.Code,
            form => new BankingOSFormCatalogItemDto
            {
                Code = form.Code,
                Name = form.Name,
                Module = form.Module,
                Version = form.Version,
                Status = form.Status,
                IsSeeded = true,
                IsPublished = string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase),
                FieldCount = form.Fields.Count
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (var form in persistedForms)
        {
            catalog[form.Code] = new BankingOSFormCatalogItemDto
            {
                Code = form.Code,
                Name = form.Name,
                Module = form.Module,
                Version = form.Version,
                Status = form.Status,
                IsSeeded = false,
                IsPublished = string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase),
                FieldCount = form.Fields.Count
            };
        }

        return catalog.Values.OrderBy(item => item.Name).ToList();
    }

    public async Task<BankingOSSeedFormDto> SaveFormDraftAsync(BankingOSSaveFormDraftRequest request)
    {
        var existing = await GetConfiguredFormAsync(request.Code) ?? await GetSeedFormByCodeAsync(request.Code);
        var form = new BankingOSSeedFormDto
        {
            Code = request.Code,
            Name = request.Name,
            Module = request.Module,
            Version = (existing?.Version ?? 0) + 1,
            Status = "Draft",
            Layout = request.Layout,
            Fields = request.Fields
        };

        await UpsertFormConfigAsync(form);
        return form;
    }

    public async Task<BankingOSSeedFormDto?> PublishFormAsync(string code)
    {
        var existing = await GetConfiguredFormAsync(code) ?? await GetSeedFormByCodeAsync(code);
        if (existing == null)
        {
            return null;
        }

        existing.Status = "Published";
        await UpsertFormConfigAsync(existing);
        return existing;
    }

    public async Task<BankingOSProcessDefinitionDto> SaveProcessDraftAsync(BankingOSSaveProcessDraftRequest request)
    {
        var existing = await GetConfiguredProcessAsync(request.Code) ?? await GetProcessByCodeAsync(request.Code);
        var process = NormalizeProcess(new BankingOSProcessDefinitionDto
        {
            Code = request.Code,
            Name = request.Name,
            Module = request.Module,
            EntityType = request.EntityType,
            TriggerType = request.TriggerType,
            Version = (existing?.Version ?? 0) + 1,
            Status = "Draft",
            Stages = request.Stages
        });

        await UpsertProcessConfigAsync(process);
        return process;
    }

    public async Task<BankingOSProcessDefinitionDto?> PublishProcessAsync(string code)
    {
        var existing = await GetConfiguredProcessAsync(code) ?? await GetProcessByCodeAsync(code);
        if (existing == null)
        {
            return null;
        }

        existing = NormalizeProcess(existing);
        existing.Status = "Published";
        await UpsertProcessConfigAsync(existing);
        return existing;
    }

    public async Task<BankingOSSeedThemeDto> SaveThemeDraftAsync(BankingOSSaveThemeDraftRequest request)
    {
        var existing = await GetConfiguredThemeAsync(request.Code)
            ?? (await GetThemePackAsync()).Themes.FirstOrDefault(theme => string.Equals(theme.Code, request.Code, StringComparison.OrdinalIgnoreCase));

        var theme = NormalizeTheme(new BankingOSSeedThemeDto
        {
            Code = request.Code,
            Name = request.Name,
            Version = (existing?.Version ?? 0) + 1,
            Status = "Draft",
            Tokens = request.Tokens
        });

        await UpsertThemeConfigAsync(theme);
        return theme;
    }

    public async Task<BankingOSSeedThemeDto?> PublishThemeAsync(string code)
    {
        var existing = await GetConfiguredThemeAsync(code)
            ?? (await GetThemePackAsync()).Themes.FirstOrDefault(theme => string.Equals(theme.Code, code, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            return null;
        }

        existing = NormalizeTheme(existing);
        existing.Status = "Published";
        await UpsertThemeConfigAsync(existing);
        return existing;
    }

    private string GetProcessPackPath()
    {
        return Path.Combine(GetSeedDirectoryPath("processes"), "banking-process-pack.json");
    }

    private string GetSeedDirectoryPath(string category)
    {
        return Path.Combine(GetBankingOSRootPath(), "seed", category);
    }

    private string GetConfigDirectoryPath(string category)
    {
        return Path.Combine(GetBankingOSRootPath(), "config", category);
    }

    private async Task<BankingOSSeedFormDto?> GetConfiguredFormAsync(string code)
    {
        return await GetConfiguredItemAsync<BankingOSSeedFormDto>(GetBankingOSFormConfigKey(code));
    }

    private async Task<BankingOSProcessDefinitionDto?> GetConfiguredProcessAsync(string code)
    {
        var process = await GetConfiguredItemAsync<BankingOSProcessDefinitionDto>(GetBankingOSProcessConfigKey(code));
        return process == null ? null : NormalizeProcess(process);
    }

    private async Task<BankingOSPublishBundleDto?> GetPublishBundleByCodeAsync(string code)
    {
        var configured = await GetConfiguredItemAsync<BankingOSPublishBundleDto>(GetBankingOSPublishBundleConfigKey(code));
        if (configured != null)
        {
            return NormalizeBundle(configured);
        }

        return (await LoadFilePublishBundlesAsync())
            .Select(NormalizeBundle)
            .FirstOrDefault(bundle => string.Equals(bundle.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<BankingOSSeedThemeDto?> GetConfiguredThemeAsync(string code)
    {
        var theme = await GetConfiguredItemAsync<BankingOSSeedThemeDto>(GetBankingOSThemeConfigKey(code));
        return theme == null ? null : NormalizeTheme(theme);
    }

    private async Task<List<BankingOSSeedFormDto>> LoadFileSeedFormsAsync()
    {
        var formsDirectory = GetSeedDirectoryPath("forms");
        var forms = new List<BankingOSSeedFormDto>();

        if (!Directory.Exists(formsDirectory))
        {
            return forms;
        }

        foreach (var path in Directory.EnumerateFiles(formsDirectory, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var form = await JsonSerializer.DeserializeAsync<BankingOSSeedFormDto>(stream, JsonOptions);
            if (form != null)
            {
                forms.Add(form);
            }
        }

        return forms;
    }

    private async Task<List<BankingOSSeedFormDto>> GetPersistedFormsAsync()
    {
        return await GetConfiguredItemsAsync<BankingOSSeedFormDto>(BankingOSFormConfigKeyPrefix);
    }

    private async Task<List<BankingOSProcessDefinitionDto>> GetPersistedProcessesAsync()
    {
        return await GetConfiguredItemsAsync<BankingOSProcessDefinitionDto>(BankingOSProcessConfigKeyPrefix);
    }

    private async Task UpsertFormConfigAsync(BankingOSSeedFormDto form)
    {
        await UpsertConfiguredItemAsync(
            BuildBankingOSFormConfigId(form.Code),
            GetBankingOSFormConfigKey(form.Code),
            $"BankingOS form configuration for {form.Name}",
            form);
    }

    private async Task UpsertProcessConfigAsync(BankingOSProcessDefinitionDto process)
    {
        await UpsertConfiguredItemAsync(
            BuildBankingOSProcessConfigId(process.Code),
            GetBankingOSProcessConfigKey(process.Code),
            $"BankingOS process configuration for {process.Name}",
            process);
    }

    private async Task UpsertThemeConfigAsync(BankingOSSeedThemeDto theme)
    {
        await UpsertConfiguredItemAsync(
            BuildBankingOSThemeConfigId(theme.Code),
            GetBankingOSThemeConfigKey(theme.Code),
            $"BankingOS theme configuration for {theme.Name}",
            theme);
    }

    private async Task UpsertPublishBundleConfigAsync(BankingOSPublishBundleDto bundle)
    {
        await UpsertConfiguredItemAsync(
            BuildBankingOSPublishBundleConfigId(bundle.Code),
            GetBankingOSPublishBundleConfigKey(bundle.Code),
            $"BankingOS publish bundle configuration for {bundle.Name}",
            bundle);
    }

    private static string GetBankingOSProcessConfigKey(string code) => $"{BankingOSProcessConfigKeyPrefix}{code}";
    private static string GetBankingOSFormConfigKey(string code) => $"{BankingOSFormConfigKeyPrefix}{code}";
    private static string GetBankingOSThemeConfigKey(string code) => $"{BankingOSThemeConfigKeyPrefix}{code}";
    private static string GetBankingOSPublishBundleConfigKey(string code) => $"{BankingOSPublishBundleConfigKeyPrefix}{code}";

    private static string BuildBankingOSProcessConfigId(string code)
    {
        var normalizedCode = new string(code
            .ToUpperInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray());

        var id = $"CFG_BOS_PROCESS_{normalizedCode}";
        return id.Length <= 50 ? id : id[..50];
    }

    private static string BuildBankingOSFormConfigId(string code)
    {
        var normalizedCode = new string(code
            .ToUpperInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray());

        var id = $"CFG_BOS_FORM_{normalizedCode}";
        return id.Length <= 50 ? id : id[..50];
    }

    private static string BuildBankingOSThemeConfigId(string code)
    {
        var normalizedCode = new string(code
            .ToUpperInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray());

        var id = $"CFG_BOS_THEME_{normalizedCode}";
        return id.Length <= 50 ? id : id[..50];
    }

    private static string BuildBankingOSPublishBundleConfigId(string code)
    {
        var normalizedCode = new string(code
            .ToUpperInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray());

        var id = $"CFG_BOS_BUNDLE_{normalizedCode}";
        return id.Length <= 50 ? id : id[..50];
    }

    private async Task<BankingOSThemePackDto> LoadFileThemePackAsync()
    {
        var path = Path.Combine(GetSeedDirectoryPath("themes"), "sample-themes.json");
        if (!File.Exists(path))
        {
            return new BankingOSThemePackDto
            {
                Themes = new List<BankingOSSeedThemeDto>()
            };
        }

        await using var stream = File.OpenRead(path);
        var themePack = await JsonSerializer.DeserializeAsync<BankingOSThemePackDto>(stream, JsonOptions);
        if (themePack == null)
        {
            throw new InvalidOperationException("BankingOS theme pack could not be loaded.");
        }

        return themePack;
    }

    private async Task<List<BankingOSPublishBundleDto>> LoadFilePublishBundlesAsync()
    {
        var bundlesDirectory = GetConfigDirectoryPath("publish-bundles");
        if (!Directory.Exists(bundlesDirectory))
        {
            return new List<BankingOSPublishBundleDto>();
        }

        var bundles = new List<BankingOSPublishBundleDto>();
        foreach (var path in Directory.EnumerateFiles(bundlesDirectory, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var bundle = await JsonSerializer.DeserializeAsync<BankingOSPublishBundleDto>(stream, JsonOptions);
            if (bundle != null)
            {
                bundles.Add(bundle);
            }
        }

        return bundles;
    }

    private async Task<List<BankingOSSeedThemeDto>> GetPersistedThemesAsync()
    {
        return await GetConfiguredItemsAsync<BankingOSSeedThemeDto>(BankingOSThemeConfigKeyPrefix);
    }

    private static BankingOSSeedThemeDto NormalizeTheme(BankingOSSeedThemeDto theme)
    {
        theme.Version = theme.Version <= 0 ? 1 : theme.Version;
        theme.Status = string.IsNullOrWhiteSpace(theme.Status) ? "Published" : theme.Status;
        theme.Tokens ??= new Dictionary<string, string>();
        return theme;
    }

    private static BankingOSProcessDefinitionDto NormalizeProcess(BankingOSProcessDefinitionDto process)
    {
        process.Version = process.Version <= 0 ? 1 : process.Version;
        process.Status = string.IsNullOrWhiteSpace(process.Status) ? "Published" : process.Status;
        process.Stages ??= new List<BankingOSProcessStageDto>();

        foreach (var stage in process.Stages)
        {
            stage.Actions ??= new List<string>();
            if (stage.Screen != null)
            {
                stage.Screen.Title ??= string.Empty;
                stage.Screen.Description ??= string.Empty;
                stage.Screen.BannerTone = string.IsNullOrWhiteSpace(stage.Screen.BannerTone) ? "neutral" : stage.Screen.BannerTone;
                stage.Screen.BannerMessage ??= string.Empty;
                stage.Screen.Sections ??= new List<BankingOSStageScreenSectionDto>();

                foreach (var section in stage.Screen.Sections)
                {
                    section.Id ??= string.Empty;
                    section.Title ??= string.Empty;
                    section.Kind = string.IsNullOrWhiteSpace(section.Kind) ? "form" : section.Kind;
                    section.FieldIds ??= new List<string>();
                }
            }
        }

        return process;
    }

    private static BankingOSPublishBundleDto NormalizeBundle(BankingOSPublishBundleDto bundle)
    {
        bundle.Status = string.IsNullOrWhiteSpace(bundle.Status) ? "Draft" : bundle.Status;
        bundle.ReleaseChannel = string.IsNullOrWhiteSpace(bundle.ReleaseChannel) ? "Manual" : bundle.ReleaseChannel;
        bundle.Processes ??= new List<string>();
        bundle.Forms ??= new List<string>();
        bundle.Themes ??= new List<string>();
        bundle.Notes ??= string.Empty;
        bundle.LastAction ??= string.Empty;
        bundle.LastActionBy ??= string.Empty;
        bundle.LastActionAtUtc ??= string.Empty;
        return bundle;
    }

    private static void ApplyBundleActionMetadata(BankingOSPublishBundleDto bundle, string action, string actor, string? notes)
    {
        bundle.LastAction = action;
        bundle.LastActionBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor;
        bundle.LastActionAtUtc = DateTime.UtcNow.ToString("O");

        if (!string.IsNullOrWhiteSpace(notes))
        {
            bundle.Notes = notes;
        }
    }

    private static JsonDocument? TryParseJson(string? payloadJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(payloadJson);
        }
        catch (JsonException)
        {
            error = "Task payload is not valid JSON.";
            return null;
        }
    }

    private static List<string> BuildAllowedTaskActions(ProcessTask task, BankingOSProcessStageDto? stage)
    {
        var actions = new List<string>();
        if (task.Status == "Claimable")
        {
            actions.Add("claim");
        }

        if (stage?.Actions != null)
        {
            actions.AddRange(stage.Actions);
        }

        if (string.Equals(task.ProcessStepDefinition.StepType, "ApprovalTask", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("approve");
            actions.Add("reject");
        }
        else
        {
            actions.Add("complete");
        }

        return actions
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<BankingOSTaskActionDescriptorDto> BuildTaskActionDescriptors(ProcessTask task, List<string> allowedActions)
    {
        var requiresClaim = string.Equals(task.Status, "Claimable", StringComparison.OrdinalIgnoreCase);

        return allowedActions
            .Select(action => BuildTaskActionDescriptor(action, requiresClaim))
            .Where(descriptor => descriptor != null)
            .Cast<BankingOSTaskActionDescriptorDto>()
            .ToList();
    }

    private static BankingOSTaskActionDescriptorDto? BuildTaskActionDescriptor(string action, bool requiresClaim)
    {
        var normalizedAction = action.Trim().ToLowerInvariant();
        var isExecutionAction = normalizedAction is "complete" or "approve" or "reject";

        return normalizedAction switch
        {
            "claim" => new BankingOSTaskActionDescriptorDto
            {
                Code = "claim",
                Label = "Claim Task",
                Tone = "neutral",
                IsPrimary = false,
                IsEnabled = true
            },
            "complete" => new BankingOSTaskActionDescriptorDto
            {
                Code = "complete",
                Label = "Complete Task",
                Tone = "primary",
                IsPrimary = true,
                IsEnabled = !requiresClaim,
                DisabledReason = requiresClaim ? "Claim this task before completing it." : null
            },
            "approve" => new BankingOSTaskActionDescriptorDto
            {
                Code = "approve",
                Label = "Approve Task",
                Tone = "primary",
                IsPrimary = true,
                IsEnabled = !requiresClaim,
                DisabledReason = requiresClaim ? "Claim this task before approving it." : null
            },
            "reject" => new BankingOSTaskActionDescriptorDto
            {
                Code = "reject",
                Label = "Reject Task",
                Tone = "danger",
                RequiresRemarks = true,
                IsPrimary = false,
                IsEnabled = !requiresClaim,
                DisabledReason = requiresClaim ? "Claim this task before rejecting it." : null
            },
            _ when isExecutionAction => null,
            _ => new BankingOSTaskActionDescriptorDto
            {
                Code = normalizedAction,
                Label = ToDisplayLabel(normalizedAction),
                Tone = "neutral",
                IsPrimary = false,
                IsEnabled = !requiresClaim,
                DisabledReason = requiresClaim ? "Claim this task before running this action." : null
            }
        };
    }

    private static List<BankingOSFieldValidationRuleDto> BuildValidationRules(BankingOSSeedFormDto? form)
    {
        if (form == null)
        {
            return new List<BankingOSFieldValidationRuleDto>();
        }

        return form.Fields
            .Where(field => field.Required)
            .Select(field => new BankingOSFieldValidationRuleDto
            {
                FieldId = field.Id,
                Label = field.Label,
                Required = true,
                Message = $"{field.Label} must be completed before this task can move forward."
            })
            .ToList();
    }

    private static BankingOSTaskScreenSchemaDto BuildTaskScreenSchema(
        ProcessTask task,
        BankingOSProcessDefinitionDto process,
        BankingOSProcessStageDto? stage,
        BankingOSSeedFormDto? form)
    {
        if (stage?.Screen != null)
        {
            return new BankingOSTaskScreenSchemaDto
            {
                Title = string.IsNullOrWhiteSpace(stage.Screen.Title) ? stage.DisplayName : stage.Screen.Title,
                Description = string.IsNullOrWhiteSpace(stage.Screen.Description)
                    ? $"{process.Name} | {task.ProcessStepDefinition.StepName}"
                    : stage.Screen.Description,
                BannerTone = string.IsNullOrWhiteSpace(stage.Screen.BannerTone) ? "neutral" : stage.Screen.BannerTone,
                BannerMessage = stage.Screen.BannerMessage ?? string.Empty,
                Sections = stage.Screen.Sections.Select(section => new BankingOSTaskScreenSectionDto
                {
                    Id = section.Id,
                    Title = section.Title,
                    Kind = section.Kind,
                    FieldIds = section.FieldIds ?? new List<string>()
                }).ToList()
            };
        }

        var sections = new List<BankingOSTaskScreenSectionDto>
        {
            new()
            {
                Id = "task-summary",
                Title = "Task Summary",
                Kind = "summary"
            }
        };

        if (form != null)
        {
            sections.Add(new BankingOSTaskScreenSectionDto
            {
                Id = "task-inputs",
                Title = form.Name,
                Kind = "form",
                FieldIds = form.Fields.Select(field => field.Id).ToList()
            });
        }

        if (stage != null)
        {
            sections.Add(new BankingOSTaskScreenSectionDto
            {
                Id = "stage-guidance",
                Title = "Stage Guidance",
                Kind = "guidance"
            });
        }

        return new BankingOSTaskScreenSchemaDto
        {
            Title = stage?.DisplayName ?? task.ProcessStepDefinition.StepName,
            Description = $"{process.Name} | {task.ProcessStepDefinition.StepName}",
            BannerTone = string.Equals(task.ProcessStepDefinition.StepType, "ApprovalTask", StringComparison.OrdinalIgnoreCase) ? "info" : "neutral",
            BannerMessage = stage == null
                ? "This task is running without a mapped BankingOS stage, so only generic runtime guidance is available."
                : $"This workbench is driven by the BankingOS stage '{stage.DisplayName}' for role '{stage.ActorRole}'.",
            Sections = sections
        };
    }

    private static string ToDisplayLabel(string action)
    {
        return string.Join(" ",
            action
                .Split('-', '_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private async Task<List<BankingOSPublishBundleDto>> GetPersistedPublishBundlesAsync()
    {
        return await GetConfiguredItemsAsync<BankingOSPublishBundleDto>(BankingOSPublishBundleConfigKeyPrefix);
    }

    private async Task<T?> GetConfiguredItemAsync<T>(string key) where T : class
    {
        var config = await _context.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Key == key);

        if (config == null || string.IsNullOrWhiteSpace(config.Value))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(config.Value, JsonOptions);
    }

    private async Task<List<T>> GetConfiguredItemsAsync<T>(string keyPrefix) where T : class
    {
        var configs = await _context.SystemConfigs
            .AsNoTracking()
            .Where(item => item.Key.StartsWith(keyPrefix))
            .ToListAsync();

        return configs
            .Select(item => JsonSerializer.Deserialize<T>(item.Value, JsonOptions))
            .Where(item => item != null)
            .Cast<T>()
            .ToList();
    }

    private async Task UpsertConfiguredItemAsync<T>(string id, string key, string description, T payload)
    {
        var config = await _context.SystemConfigs.FirstOrDefaultAsync(item => item.Key == key);
        var serializedPayload = JsonSerializer.Serialize(payload, JsonOptions);

        if (config == null)
        {
            config = new SystemConfig
            {
                Id = id,
                Key = key,
                Description = description,
                Value = serializedPayload,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SystemConfigs.Add(config);
        }
        else
        {
            config.Description = description;
            config.Value = serializedPayload;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private string GetBankingOSRootPath()
    {
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "..", "BankingOS"),
            Path.Combine(_environment.ContentRootPath, ".."),
            Path.Combine(_environment.ContentRootPath, "BankingOS"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BankingOS"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "BankingOS")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(Path.Combine(fullPath, "seed")))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException($"Unable to resolve BankingOS root from content root '{_environment.ContentRootPath}'.");
    }

    private async Task<BankingOSSeedFormDto?> EnrichFormWithProductOptionsAsync(BankingOSSeedFormDto? form, string processCode, string entityType)
    {
        if (form == null)
        {
            return null;
        }

        var clonedForm = new BankingOSSeedFormDto
        {
            Code = form.Code,
            Name = form.Name,
            Module = form.Module,
            Version = form.Version,
            Status = form.Status,
            Layout = form.Layout,
            Fields = form.Fields.Select(field => new BankingOSSeedFieldDto
            {
                Id = field.Id,
                Label = field.Label,
                Type = field.Type,
                Required = field.Required,
                Options = field.Options?.ToList()
            }).ToList()
        };

        var launchProducts = await BuildLaunchProductOptionsAsync(processCode, entityType);
        var productField = clonedForm.Fields.FirstOrDefault(field =>
            string.Equals(field.Id, "product_code", StringComparison.OrdinalIgnoreCase));

        if (productField != null)
        {
            productField.Options = launchProducts.Select(product => product.ProductId).ToList();
        }

        var repaymentField = clonedForm.Fields.FirstOrDefault(field =>
            string.Equals(field.Id, "repayment_frequency", StringComparison.OrdinalIgnoreCase));

        if (repaymentField != null)
        {
            repaymentField.Options = launchProducts
                .SelectMany(product => product.AllowedRepaymentFrequencies)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();
        }

        return clonedForm;
    }

    private async Task<List<BankingOSProductLaunchOptionDto>> BuildLaunchProductOptionsAsync(string processCode, string entityType)
    {
        var products = await _productService.GetProductsAsync();
        var allowedTypes = ResolveAllowedProductTypes(processCode, entityType);

        return products
            .Where(product => allowedTypes.Count == 0 || allowedTypes.Contains(product.Type, StringComparer.OrdinalIgnoreCase))
            .OrderBy(product => product.Name)
            .Select(product => new BankingOSProductLaunchOptionDto
            {
                ProductId = product.Id,
                Name = product.Name,
                Type = product.Type,
                Status = product.Status,
                Currency = product.Currency,
                InterestRate = product.InterestRate,
                MinAmount = product.MinAmount,
                MaxAmount = product.MaxAmount,
                DefaultTerm = product.DefaultTerm,
                MinTerm = product.MinTerm,
                MaxTerm = product.MaxTerm,
                DefaultRepaymentFrequency = product.DefaultRepaymentFrequency,
                AllowedRepaymentFrequencies = DeserializeFrequencyJson(product.AllowedRepaymentFrequenciesJson),
                EligibilityHints = BuildProductEligibilityHints(product)
            })
            .ToList();
    }

    private static List<string> ResolveAllowedProductTypes(string processCode, string entityType)
    {
        if (string.Equals(processCode, "LOAN_ORIGINATION", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Loan", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { "LOAN" };
        }

        if (string.Equals(processCode, "ACCOUNT_OPENING", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Account", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { "SAVINGS", "CURRENT", "FIXED", "DEPOSIT" };
        }

        return new List<string>();
    }

    private static List<string> BuildLaunchValidationHints(string processCode, List<BankingOSProductLaunchOptionDto> products)
    {
        var hints = new List<string>();
        if (products.Count == 0)
        {
            hints.Add("No eligible products are currently configured for this BankingOS process.");
            return hints;
        }

        if (string.Equals(processCode, "LOAN_ORIGINATION", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add("Requested amount and tenor are validated against the selected loan product.");
            hints.Add("Repayment frequency must match the selected loan product configuration.");
        }
        else if (string.Equals(processCode, "ACCOUNT_OPENING", StringComparison.OrdinalIgnoreCase))
        {
            hints.Add("Available products are filtered to account-opening compatible products.");
            hints.Add("Operators should confirm the selected product currency and opening rules before launch.");
        }

        return hints;
    }

    private static List<string> BuildProductEligibilityHints(Product product)
    {
        var hints = new List<string>();
        if (product.MinAmount.HasValue || product.MaxAmount.HasValue)
        {
            hints.Add($"Amount band: {product.MinAmount?.ToString() ?? "0"} to {product.MaxAmount?.ToString() ?? "No max"} {product.Currency}");
        }

        if (product.MinTerm.HasValue || product.MaxTerm.HasValue)
        {
            hints.Add($"Term band: {product.MinTerm?.ToString() ?? "0"} to {product.MaxTerm?.ToString() ?? "No max"} months");
        }

        if (product.EligibilityRule?.RequiresKycComplete == true)
        {
            hints.Add("Requires completed KYC.");
        }

        if (product.EligibilityRule?.RequireCreditBureauCheck == true)
        {
            hints.Add("Requires credit bureau check.");
        }

        if (product.RequiresGroup)
        {
            hints.Add("Requires group context.");
        }

        return hints;
    }

    private static List<string> DeserializeFrequencyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task<BankingOSProductLaunchOptionDto?> ResolveSelectedProductAsync(ProcessInstance instance, string processCode, string entityType)
    {
        var startedPayloadJson = instance.History
            .Where(history => string.Equals(history.ActionType, "Started", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(history => history.ActionAtUtc)
            .Select(history => history.PayloadJson)
            .FirstOrDefault(payload => !string.IsNullOrWhiteSpace(payload));

        using var document = TryParseJson(startedPayloadJson, out _);
        if (document == null)
        {
            return null;
        }

        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!dataElement.TryGetProperty("product_code", out var productCodeElement))
        {
            return null;
        }

        var selectedProductCode = productCodeElement.ToString();
        if (string.IsNullOrWhiteSpace(selectedProductCode))
        {
            return null;
        }

        var launchProducts = await BuildLaunchProductOptionsAsync(processCode, entityType);
        return launchProducts.FirstOrDefault(product =>
            string.Equals(product.ProductId, selectedProductCode, StringComparison.OrdinalIgnoreCase));
    }
}
