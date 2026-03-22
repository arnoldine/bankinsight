using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankInsight.API.Entities;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await EnsureBranchesAsync(context);
        await EnsureBranchVaultsAsync(context);
        await EnsurePermissionsAsync(context);
        await EnsureRolesAsync(context);
        await EnsureAdminUserAsync(context);
        await EnsureSystemConfigAsync(context);
        await EnsureProductsAsync(context);
        await EnsureLoanProductsAsync(context);
        await EnsureGroupLendingSeedDataAsync(context);
        await EnsureGlAccountsAsync(context);
        await EnsureLoanAccountingProfilesAsync(context);
        await EnsureCashControlOpeningBalanceAsync(context);
        await EnsureWorkflowAsync(context);
        await EnsureBankingOSProcessDefinitionsAsync(context);
        await EnsureBankingOSFormConfigurationsAsync(context);
        await EnsureBankingOSThemeConfigurationsAsync(context);
        await EnsureBankingOSPublishBundleConfigurationsAsync(context);
        await EnsureReportDefinitionsAsync(context);
        await BackfillLegacyLoanMetadataAsync(context);
    }

    private static async Task EnsureBranchesAsync(ApplicationDbContext context)
    {
        var defaults = new List<Branch>
        {
            new()
            {
                Id = "BR001",
                Name = "Head Office",
                Code = "HO",
                Location = "Accra",
                Status = "ACTIVE"
            },
            new()
            {
                Id = "BR002",
                Name = "Kumasi Branch",
                Code = "KSI",
                Location = "Kumasi",
                Status = "ACTIVE"
            }
        };

        foreach (var branch in defaults)
        {
            var existing = await context.Branches.FirstOrDefaultAsync(b => b.Id == branch.Id);
            if (existing == null)
            {
                context.Branches.Add(branch);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.Name))
            {
                existing.Name = branch.Name;
            }

            if (string.IsNullOrWhiteSpace(existing.Code))
            {
                existing.Code = branch.Code;
            }

            if (string.IsNullOrWhiteSpace(existing.Location))
            {
                existing.Location = branch.Location;
            }

            if (string.IsNullOrWhiteSpace(existing.Status))
            {
                existing.Status = branch.Status;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureBranchVaultsAsync(ApplicationDbContext context)
    {
        var activeBranchIds = await context.Branches
            .Where(b => b.Status == "ACTIVE")
            .Select(b => b.Id)
            .ToListAsync();

        foreach (var branchId in activeBranchIds)
        {
            var existingVault = await context.BranchVaults
                .FirstOrDefaultAsync(v => v.BranchId == branchId && v.Currency == "GHS");

            if (existingVault != null)
            {
                if (!existingVault.VaultLimit.HasValue)
                {
                    existingVault.VaultLimit = 500000m;
                }

                if (!existingVault.MinBalance.HasValue)
                {
                    existingVault.MinBalance = 10000m;
                }

                continue;
            }

            context.BranchVaults.Add(new BranchVault
            {
                Id = $"VLT-{branchId}-GHS-SEED",
                BranchId = branchId,
                Currency = "GHS",
                CashOnHand = 0m,
                VaultLimit = 500000m,
                MinBalance = 10000m,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsurePermissionsAsync(ApplicationDbContext context)
    {
        var allPermissions = AppPermissions.GetAll().ToList();
        var existing = await context.Permissions.Select(p => p.Code).ToListAsync();

        foreach (var code in allPermissions.Where(code => !existing.Contains(code)))
        {
            context.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = code.Replace('.', ' ').ToUpperInvariant(),
                Module = code.Contains('.') ? code.Split('.')[0] : "System",
                Description = $"Grants {code} access",
                IsSystemPermission = true
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureRolesAsync(ApplicationDbContext context)
    {
        await EnsureRoleWithPermissionsAsync(
            context,
            roleId: "ROLE_ADMIN",
            name: "Administrator",
            description: "System administrator",
            permissionCodes: AppPermissions.GetAll());

        await EnsureRoleWithPermissionsAsync(
            context,
            roleId: "ROLE_TELLER",
            name: "Teller",
            description: "Front desk teller operations",
            permissionCodes: new[]
            {
                AppPermissions.Accounts.View,
                AppPermissions.Transactions.View,
                AppPermissions.Transactions.Post,
                AppPermissions.Users.View,
                AppPermissions.Dashboard.View,
                AppPermissions.Tasks.View,
                AppPermissions.Tasks.Claim,
                AppPermissions.Tasks.Complete,
                AppPermissions.Processes.Start,
            });

        await EnsureRoleWithPermissionsAsync(
            context,
            roleId: "ROLE_MANAGER",
            name: "Branch Manager",
            description: "Branch operations manager",
            permissionCodes: new[]
            {
                AppPermissions.Accounts.View,
                AppPermissions.Accounts.Open,
                AppPermissions.Transactions.View,
                AppPermissions.Transactions.Post,
                AppPermissions.Loans.View,
                AppPermissions.Loans.Approve,
                AppPermissions.Loans.Disburse,
                AppPermissions.GeneralLedger.View,
                AppPermissions.Workflow.Approve,
                AppPermissions.Workflow.View,
                AppPermissions.Users.View,
                AppPermissions.Dashboard.View,
                AppPermissions.Tasks.View,
                AppPermissions.Tasks.Claim,
                AppPermissions.Tasks.Complete,
                AppPermissions.Processes.Manage,
                AppPermissions.Processes.Start,
                AppPermissions.Processes.View,
            });
    }

    private static async Task EnsureRoleWithPermissionsAsync(
        ApplicationDbContext context,
        string roleId,
        string name,
        string description,
        IEnumerable<string> permissionCodes)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role == null)
        {
            role = new Role
            {
                Id = roleId,
                Name = name,
                Description = description,
                IsSystemRole = true,
            };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var existingCodes = await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        var permissions = await context.Permissions
            .Where(p => permissionCodes.Contains(p.Code) && !existingCodes.Contains(p.Code))
            .ToListAsync();

        foreach (var permission in permissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureSystemConfigAsync(ApplicationDbContext context)
    {
        var defaults = new List<SystemConfig>
        {
            new SystemConfig
            {
                Id = "CFG_BASE_CURRENCY",
                Key = "base_currency",
                Value = "GHS",
                Description = "Default base currency",
                UpdatedAt = DateTime.UtcNow,
            },
            new SystemConfig
            {
                Id = "CFG_BUSINESS_DATE",
                Key = "business_date",
                Value = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Description = "Current business date",
                UpdatedAt = DateTime.UtcNow,
            },
            new SystemConfig
            {
                Id = "CFG_EOD_SCHED_ENABLED",
                Key = "eod_scheduler_enabled",
                Value = "false",
                Description = "Enable automatic EOD scheduler",
                UpdatedAt = DateTime.UtcNow,
            },
            new SystemConfig
            {
                Id = "CFG_EOD_SCHED_TIME",
                Key = "eod_scheduler_time_utc",
                Value = "23:00",
                Description = "UTC time for the automatic EOD scheduler",
                UpdatedAt = DateTime.UtcNow,
            },
            new SystemConfig
            {
                Id = "CFG_EOD_SCHED_LASTRUN",
                Key = "eod_scheduler_last_run_date",
                Value = string.Empty,
                Description = "Last business date processed by the automatic EOD scheduler",
                UpdatedAt = DateTime.UtcNow,
            },
            new SystemConfig
            {
                Id = "CFG_DEVICE_MINVER",
                Key = "device_min_supported_version",
                Value = "2.0.0",
                Description = "Minimum supported software version for managed terminals and devices",
                UpdatedAt = DateTime.UtcNow,
            },
        };

        var existingKeys = await context.SystemConfigs.Select(config => config.Key).ToListAsync();
        foreach (var config in defaults.Where(config => !existingKeys.Contains(config.Key)))
        {
            context.SystemConfigs.Add(config);
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureProductsAsync(ApplicationDbContext context)
    {
        var existing = await context.Products.Select(p => p.Id).ToListAsync();
        var products = new List<Product>
        {
            new Product
            {
                Id = "PRD_SAVINGS",
                Name = "Savings Account",
                Type = "SAVINGS",
                Currency = "GHS",
                Status = "ACTIVE",
                MinAmount = 10,
            },
            new Product
            {
                Id = "PRD_LOAN_STD",
                Name = "Standard Loan",
                Type = "LOAN",
                Currency = "GHS",
                Status = "ACTIVE",
                InterestRate = 18,
                InterestMethod = "FLAT",
                MinAmount = 100,
                MaxAmount = 100000,
                MinTerm = 3,
                MaxTerm = 36,
                DefaultTerm = 12,
            },
        };

        foreach (var product in products.Where(product => !existing.Contains(product.Id)))
        {
            context.Products.Add(product);
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureLoanProductsAsync(ApplicationDbContext context)
    {
        var existing = await context.LoanProducts.Select(lp => lp.Id).ToListAsync();
        var products = new List<LoanProduct>
        {
            new LoanProduct
            {
                Id = "LP_DIGITAL_30",
                Code = "DIGITAL_30D",
                Name = "Digital Loans (30 Days)",
                ProductType = LoanProductType.DigitalLoan30Days,
                InterestMethod = LoanInterestMethod.Flat,
                RepaymentFrequency = LoanRepaymentFrequency.Bullet,
                TermInPeriods = 1,
                AnnualInterestRate = 24,
                MinAmount = 100,
                MaxAmount = 10000,
                Currency = "GHS",
                IsActive = true,
            },
            new LoanProduct
            {
                Id = "LP_GROUP_WEEKLY",
                Code = "GROUP_WEEKLY",
                Name = "Weekly Group Loans",
                ProductType = LoanProductType.WeeklyGroupLoan,
                InterestMethod = LoanInterestMethod.ReducingBalance,
                RepaymentFrequency = LoanRepaymentFrequency.Weekly,
                TermInPeriods = 24,
                AnnualInterestRate = 20,
                MinAmount = 500,
                MaxAmount = 50000,
                Currency = "GHS",
                IsActive = true,
            },
            new LoanProduct
            {
                Id = "LP_BIZ_MONTHLY",
                Code = "BIZ_MONTHLY",
                Name = "Monthly Business Loans",
                ProductType = LoanProductType.MonthlyBusinessLoan,
                InterestMethod = LoanInterestMethod.ReducingBalance,
                RepaymentFrequency = LoanRepaymentFrequency.Monthly,
                TermInPeriods = 24,
                AnnualInterestRate = 18,
                MinAmount = 1000,
                MaxAmount = 250000,
                Currency = "GHS",
                IsActive = true,
            },
            new LoanProduct
            {
                Id = "LP_CONS_MONTHLY",
                Code = "CONS_MONTHLY",
                Name = "Monthly Consumer Loans",
                ProductType = LoanProductType.MonthlyConsumerLoan,
                InterestMethod = LoanInterestMethod.Flat,
                RepaymentFrequency = LoanRepaymentFrequency.Monthly,
                TermInPeriods = 18,
                AnnualInterestRate = 22,
                MinAmount = 300,
                MaxAmount = 75000,
                Currency = "GHS",
                IsActive = true,
            },
        };

        foreach (var product in products.Where(product => !existing.Contains(product.Id)))
        {
            context.LoanProducts.Add(product);
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureGlAccountsAsync(ApplicationDbContext context)
    {
        var existingCodes = await context.GlAccounts.Select(a => a.Code).ToListAsync();
        var regulatoryAccounts = RegulatoryChartOfAccountsCatalog.GetAccounts(RegulatoryChartOfAccountsCatalog.GhanaRegionCode);

        foreach (var account in regulatoryAccounts.Where(account => !existingCodes.Contains(account.Code)))
        {
            context.GlAccounts.Add(account);
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureLoanAccountingProfilesAsync(ApplicationDbContext context)
    {
        if (await context.LoanAccountingProfiles.AnyAsync(p => p.IsActive))
        {
            return;
        }

        context.LoanAccountingProfiles.Add(new LoanAccountingProfile
        {
            Id = "LAP-GROUP-WEEKLY-SEED",
            LoanProductId = "LP_GROUP_WEEKLY",
            LoanPortfolioGl = "15100",
            InterestIncomeGl = "40100",
            ProcessingFeeIncomeGl = "40200",
            PenaltyIncomeGl = "40300",
            InterestReceivableGl = "15150",
            PenaltyReceivableGl = "15160",
            ImpairmentExpenseGl = "50100",
            ImpairmentAllowanceGl = "15900",
            RecoveryIncomeGl = "40400",
            DisbursementFundingGl = "10100",
            RepaymentAllocationOrder = "[\"Penalty\",\"Fees\",\"Interest\",\"Principal\"]",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
    }
    private static async Task EnsureCashControlOpeningBalanceAsync(ApplicationDbContext context)
    {
        const string reference = "OPENING-CASH-CONTROL-SYNC";
        if (await context.JournalEntries.AnyAsync(entry => entry.Reference == reference))
        {
            return;
        }

        var cashOnHand = await context.GlAccounts.FirstOrDefaultAsync(account => account.Code == "10100" && account.Currency == "GHS");
        var suspense = await context.GlAccounts.FirstOrDefaultAsync(account => account.Code == "22300" && account.Currency == "GHS");
        if (cashOnHand == null || suspense == null)
        {
            return;
        }

        var totalVaultCash = await context.BranchVaults
            .Where(vault => vault.Currency == "GHS")
            .SumAsync(vault => (decimal?)vault.CashOnHand) ?? 0m;

        var delta = totalVaultCash - cashOnHand.Balance;
        if (Math.Abs(delta) < 0.01m)
        {
            return;
        }

        var journalId = $"JRN-CASHSYNC-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var isIncrease = delta > 0;
        var amount = Math.Abs(delta);
        var lines = isIncrease
            ? new[]
            {
                new JournalLine { JournalId = journalId, AccountCode = "10100", Debit = amount, Credit = 0m },
                new JournalLine { JournalId = journalId, AccountCode = "22300", Debit = 0m, Credit = amount },
            }
            : new[]
            {
                new JournalLine { JournalId = journalId, AccountCode = "22300", Debit = amount, Credit = 0m },
                new JournalLine { JournalId = journalId, AccountCode = "10100", Debit = 0m, Credit = amount },
            };

        context.JournalEntries.Add(new JournalEntry
        {
            Id = journalId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Reference = reference,
            Description = "Opening sync for operational vault cash and GL cash on hand",
            PostedBy = null,
            Status = "POSTED",
            CreatedAt = DateTime.UtcNow,
        });
        context.JournalLines.AddRange(lines);

        foreach (var line in lines)
        {
            var account = line.AccountCode == "10100" ? cashOnHand : suspense;
            var balanceChange = account.Category == "ASSET" || account.Category == "EXPENSE"
                ? line.Debit - line.Credit
                : line.Credit - line.Debit;
            account.Balance += balanceChange;
        }

        await context.SaveChangesAsync();
    }
    private static async Task EnsureWorkflowAsync(ApplicationDbContext context)
    {
        if (await context.Workflows.AnyAsync(w => w.Id == "WF_APPROVAL_BASIC"))
        {
            return;
        }

        context.Workflows.Add(new Workflow
        {
            Id = "WF_APPROVAL_BASIC",
            Name = "Default Approval",
            TriggerType = "TRANSACTION",
            Steps = "[]",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
    }

    private static async Task EnsureBankingOSProcessDefinitionsAsync(ApplicationDbContext context)
    {
        if (await context.ProcessDefinitions.AnyAsync(process => process.Code == "CUSTOMER_ONBOARDING"))
        {
            return;
        }

        var definitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        var definition = new ProcessDefinition
        {
            Id = definitionId,
            Code = "CUSTOMER_ONBOARDING",
            Name = "Customer Onboarding",
            Module = "Core Banking",
            EntityType = "Customer",
            TriggerType = "Manual",
            IsSystemProcess = true,
            IsActive = true,
            CreatedAtUtc = startedAtUtc,
            CreatedByUserId = "system"
        };

        var version = new ProcessDefinitionVersion
        {
            Id = versionId,
            ProcessDefinitionId = definitionId,
            VersionNo = 1,
            Status = "Published",
            IsPublished = true,
            Notes = "Seeded BankingOS onboarding flow",
            CreatedAtUtc = startedAtUtc,
            CreatedByUserId = "system",
            PublishedAtUtc = startedAtUtc,
            PublishedByUserId = "system"
        };

        var startStepId = Guid.NewGuid();
        var captureStepId = Guid.NewGuid();
        var activateStepId = Guid.NewGuid();
        var completeStepId = Guid.NewGuid();
        var rejectStepId = Guid.NewGuid();

        var steps = new List<ProcessStepDefinition>
        {
            new()
            {
                Id = startStepId,
                ProcessDefinitionVersionId = versionId,
                StepCode = "start",
                StepName = "Start",
                StepType = "Start",
                OrderNo = 1,
                IsStartStep = true,
                IsEndStep = false
            },
            new()
            {
                Id = captureStepId,
                ProcessDefinitionVersionId = versionId,
                StepCode = "lead_capture",
                StepName = "Lead Capture",
                StepType = "UserTask",
                OrderNo = 2,
                IsStartStep = false,
                IsEndStep = false,
                AssignmentType = "Permission",
                AssignedPermissionCode = AppPermissions.Accounts.Open,
                SlaHours = 4
            },
            new()
            {
                Id = activateStepId,
                ProcessDefinitionVersionId = versionId,
                StepCode = "customer_activation",
                StepName = "Customer Activation",
                StepType = "ApprovalTask",
                OrderNo = 3,
                IsStartStep = false,
                IsEndStep = false,
                AssignmentType = "Permission",
                AssignedPermissionCode = AppPermissions.Customers.Approve,
                SlaHours = 8,
                RequireMakerCheckerSeparation = true
            },
            new()
            {
                Id = completeStepId,
                ProcessDefinitionVersionId = versionId,
                StepCode = "completed",
                StepName = "Completed",
                StepType = "End",
                OrderNo = 4,
                IsStartStep = false,
                IsEndStep = true
            },
            new()
            {
                Id = rejectStepId,
                ProcessDefinitionVersionId = versionId,
                StepCode = "rejected",
                StepName = "Rejected",
                StepType = "End",
                OrderNo = 5,
                IsStartStep = false,
                IsEndStep = true
            }
        };

        var transitions = new List<ProcessTransitionDefinition>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProcessDefinitionVersionId = versionId,
                FromStepId = startStepId,
                ToStepId = captureStepId,
                TransitionName = "Begin onboarding",
                IsDefault = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProcessDefinitionVersionId = versionId,
                FromStepId = captureStepId,
                ToStepId = activateStepId,
                TransitionName = "Submit onboarding",
                RequiredOutcome = "Complete"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProcessDefinitionVersionId = versionId,
                FromStepId = activateStepId,
                ToStepId = completeStepId,
                TransitionName = "Approve activation",
                RequiredOutcome = "Approve"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProcessDefinitionVersionId = versionId,
                FromStepId = activateStepId,
                ToStepId = rejectStepId,
                TransitionName = "Reject onboarding",
                RequiredOutcome = "Reject"
            }
        };

        context.ProcessDefinitions.Add(definition);
        context.ProcessDefinitionVersions.Add(version);
        context.ProcessStepDefinitions.AddRange(steps);
        context.ProcessTransitionDefinitions.AddRange(transitions);

        await context.SaveChangesAsync();
    }

    private static async Task EnsureBankingOSFormConfigurationsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;
        var forms = new[]
        {
            new
            {
                Id = "CFG_BOS_FORM_CUST_ONBOARD",
                Key = "bankingos.form.customer-onboarding-form",
                Description = "BankingOS form configuration for Customer Onboarding Form",
                Value = JsonSerializer.Serialize(new
                {
                    code = "customer-onboarding-form",
                    name = "Customer Onboarding Form",
                    module = "Core Banking",
                    version = 1,
                    status = "Published",
                    layout = new
                    {
                        type = "stepper",
                        steps = new[] { "basic-details", "identity", "contacts", "kyc-documents" }
                    },
                    fields = new object[]
                    {
                        new { id = "full_name", label = "Full Name", type = "text", required = true },
                        new { id = "customer_type", label = "Customer Type", type = "select", required = true, options = new[] { "Individual", "Business" } },
                        new { id = "ghana_card_number", label = "Ghana Card Number", type = "text", required = true },
                        new { id = "mobile_number", label = "Mobile Number", type = "phone", required = true },
                        new { id = "risk_notes", label = "Risk Notes", type = "textarea", required = false }
                    }
                })
            },
            new
            {
                Id = "CFG_BOS_FORM_ACCT_OPEN",
                Key = "bankingos.form.account-opening-form",
                Description = "BankingOS form configuration for Account Opening Form",
                Value = JsonSerializer.Serialize(new
                {
                    code = "account-opening-form",
                    name = "Account Opening Form",
                    module = "Core Banking",
                    version = 1,
                    status = "Published",
                    layout = new
                    {
                        type = "review-workbench",
                        regions = new[] { "form", "summary", "actions" }
                    },
                    fields = new object[]
                    {
                        new { id = "customer_id", label = "Customer", type = "lookup/reference", required = true },
                        new { id = "product_code", label = "Product", type = "select", required = true, options = new[] { "SAV_STD", "CURR_STD", "FIXED_90D" } },
                        new { id = "opening_balance", label = "Opening Balance", type = "currency", required = true },
                        new { id = "mandate_type", label = "Mandate Type", type = "select", required = true, options = new[] { "Single", "Joint", "Corporate" } }
                    }
                })
            },
            new
            {
                Id = "CFG_BOS_FORM_LOAN_APP",
                Key = "bankingos.form.loan-application-form",
                Description = "BankingOS form configuration for Loan Application Form",
                Value = JsonSerializer.Serialize(new
                {
                    code = "loan-application-form",
                    name = "Loan Application Form",
                    module = "Lending",
                    version = 1,
                    status = "Published",
                    layout = new
                    {
                        type = "stepper",
                        steps = new[] { "applicant", "facility", "repayment", "collateral", "review" }
                    },
                    fields = new object[]
                    {
                        new { id = "customer_id", label = "Customer", type = "lookup/reference", required = true },
                        new { id = "product_code", label = "Loan Product", type = "select", required = true, options = new[] { "GROUP_LOAN", "SME_TERM", "PERSONAL_LOAN" } },
                        new { id = "requested_amount", label = "Requested Amount", type = "currency", required = true },
                        new { id = "requested_tenor_months", label = "Requested Tenor (Months)", type = "number", required = true },
                        new { id = "repayment_frequency", label = "Repayment Frequency", type = "select", required = true, options = new[] { "Weekly", "Biweekly", "Monthly" } }
                    }
                })
            }
        };

        foreach (var form in forms)
        {
            if (await context.SystemConfigs.AnyAsync(config => config.Key == form.Key))
            {
                continue;
            }

            context.SystemConfigs.Add(new SystemConfig
            {
                Id = form.Id,
                Key = form.Key,
                Description = form.Description,
                Value = form.Value,
                UpdatedAt = now
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureBankingOSThemeConfigurationsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;
        var themes = new[]
        {
            new
            {
                Id = "CFG_BOS_THEME_MODERN_BLUE",
                Key = "bankingos.theme.modern-blue-finance",
                Description = "BankingOS theme configuration for Modern Blue Finance",
                Value = JsonSerializer.Serialize(new
                {
                    code = "modern-blue-finance",
                    name = "Modern Blue Finance",
                    tokens = new Dictionary<string, string>
                    {
                        ["color.primary"] = "#0F4C81",
                        ["color.secondary"] = "#4FA3D9",
                        ["color.surface"] = "#F7FAFC",
                        ["font.heading"] = "Manrope",
                        ["font.body"] = "Inter",
                        ["radius.card"] = "20px"
                    }
                })
            },
            new
            {
                Id = "CFG_BOS_THEME_EMERALD_MICRO",
                Key = "bankingos.theme.emerald-microfinance",
                Description = "BankingOS theme configuration for Emerald Microfinance",
                Value = JsonSerializer.Serialize(new
                {
                    code = "emerald-microfinance",
                    name = "Emerald Microfinance",
                    tokens = new Dictionary<string, string>
                    {
                        ["color.primary"] = "#0F766E",
                        ["color.secondary"] = "#34D399",
                        ["color.surface"] = "#F0FDFA",
                        ["font.heading"] = "Sora",
                        ["font.body"] = "Inter",
                        ["radius.card"] = "18px"
                    }
                })
            },
            new
            {
                Id = "CFG_BOS_THEME_GOLD_CORP",
                Key = "bankingos.theme.gold-corporate-bank",
                Description = "BankingOS theme configuration for Gold Corporate Bank",
                Value = JsonSerializer.Serialize(new
                {
                    code = "gold-corporate-bank",
                    name = "Gold Corporate Bank",
                    tokens = new Dictionary<string, string>
                    {
                        ["color.primary"] = "#8B6F1E",
                        ["color.secondary"] = "#D4AF37",
                        ["color.surface"] = "#FFFBEB",
                        ["font.heading"] = "DM Serif Display",
                        ["font.body"] = "Source Sans 3",
                        ["radius.card"] = "14px"
                    }
                })
            },
            new
            {
                Id = "CFG_BOS_THEME_MINIMAL_NEUTR",
                Key = "bankingos.theme.minimal-neutral",
                Description = "BankingOS theme configuration for Minimal Neutral",
                Value = JsonSerializer.Serialize(new
                {
                    code = "minimal-neutral",
                    name = "Minimal Neutral",
                    tokens = new Dictionary<string, string>
                    {
                        ["color.primary"] = "#334155",
                        ["color.secondary"] = "#94A3B8",
                        ["color.surface"] = "#F8FAFC",
                        ["font.heading"] = "Plus Jakarta Sans",
                        ["font.body"] = "Inter",
                        ["radius.card"] = "16px"
                    }
                })
            },
            new
            {
                Id = "CFG_BOS_THEME_DARK_EXEC",
                Key = "bankingos.theme.dark-executive",
                Description = "BankingOS theme configuration for Dark Executive",
                Value = JsonSerializer.Serialize(new
                {
                    code = "dark-executive",
                    name = "Dark Executive",
                    tokens = new Dictionary<string, string>
                    {
                        ["color.primary"] = "#E2E8F0",
                        ["color.secondary"] = "#38BDF8",
                        ["color.surface"] = "#0F172A",
                        ["font.heading"] = "Space Grotesk",
                        ["font.body"] = "Inter",
                        ["radius.card"] = "18px"
                    }
                })
            }
        };

        foreach (var theme in themes)
        {
            if (await context.SystemConfigs.AnyAsync(config => config.Key == theme.Key))
            {
                continue;
            }

            context.SystemConfigs.Add(new SystemConfig
            {
                Id = theme.Id,
                Key = theme.Key,
                Description = theme.Description,
                Value = theme.Value,
                UpdatedAt = now
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureBankingOSPublishBundleConfigurationsAsync(ApplicationDbContext context)
    {
        const string key = "bankingos.publish-bundle.core-banking-starter";
        if (await context.SystemConfigs.AnyAsync(config => config.Key == key))
        {
            return;
        }

        context.SystemConfigs.Add(new SystemConfig
        {
            Id = "CFG_BOS_BUNDLE_COREBANK_START",
            Key = key,
            Description = "BankingOS publish bundle configuration for Core Banking Starter",
            Value = JsonSerializer.Serialize(new
            {
                code = "core-banking-starter",
                name = "Core Banking Starter",
                status = "Published",
                releaseChannel = "production-ready-seed",
                requiresApproval = true,
                processes = new[]
                {
                    "CUSTOMER_ONBOARDING",
                    "ACCOUNT_OPENING",
                    "SAVINGS_CASH_DEPOSIT",
                    "LOAN_ORIGINATION",
                    "END_OF_DAY_OPERATIONS"
                },
                forms = new[]
                {
                    "customer-onboarding-form",
                    "account-opening-form",
                    "loan-application-form"
                },
                themes = new[]
                {
                    "modern-blue-finance",
                    "emerald-microfinance",
                    "gold-corporate-bank"
                },
                notes = "Starter BankingOS release bundle for onboarding, account opening, lending intake, and first-wave theming."
            }),
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task EnsureReportDefinitionsAsync(ApplicationDbContext context)
    {
        var existingCodes = await context.ReportDefinitions.Select(report => report.ReportCode).ToListAsync();
        var now = DateTime.UtcNow;
        var regulatoryReports = new List<ReportDefinition>
        {
            new()
            {
                ReportCode = "REG-BOG-DBK-ORASS",
                ReportName = "BoG Daily Bank Return (ORASS)",
                Description = "Daily Bank Return submission package aligned to the ORASS supervisory channel for regulated institutions.",
                ReportType = "Regulatory - ORASS",
                DataSource = "REGULATORY_DAILY_POSITION",
                Frequency = "Daily",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-BOG-MR1",
                ReportName = "BoG Monthly Return 1",
                Description = "Monthly regulatory return for account and deposit positions.",
                ReportType = "Regulatory - BoG",
                DataSource = "REGULATORY_MONTHLY_RETURN_1",
                Frequency = "Monthly",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-BOG-MR2",
                ReportName = "BoG Monthly Return 2",
                Description = "Monthly regulatory return for loan portfolio and lending exposure positions.",
                ReportType = "Regulatory - BoG",
                DataSource = "REGULATORY_MONTHLY_RETURN_2",
                Frequency = "Monthly",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-BOG-MR3",
                ReportName = "BoG Monthly Return 3",
                Description = "Monthly regulatory return for off-balance-sheet and contingent exposure positions.",
                ReportType = "Regulatory - BoG",
                DataSource = "REGULATORY_MONTHLY_RETURN_3",
                Frequency = "Monthly",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-BOG-PRUDENTIAL",
                ReportName = "BoG Prudential Return",
                Description = "Capital, liquidity, and risk position report for prudential supervision.",
                ReportType = "Regulatory - BoG",
                DataSource = "REGULATORY_PRUDENTIAL_RETURN",
                Frequency = "Monthly",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-BOG-LARGE-EXPOSURE",
                ReportName = "BoG Large Exposure Report",
                Description = "Large exposure monitoring report for concentration and related-party review.",
                ReportType = "Regulatory - BoG",
                DataSource = "REGULATORY_LARGE_EXPOSURE",
                Frequency = "Monthly",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-CRB-CONSUMER-CREDIT",
                ReportName = "CRB Consumer Credit Data Submission",
                Description = "Consumer credit data extract aligned to Bank of Ghana credit reporting data formats for licensed credit reference bureaus.",
                ReportType = "Regulatory - CRB",
                DataSource = "CRB_CONSUMER_CREDIT",
                Frequency = "Monthly",
                TemplateFormat = "CSV",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-CRB-CONSUMER-DISHONOURED",
                ReportName = "CRB Consumer Dishonoured Cheque Submission",
                Description = "Consumer dishonoured cheque extract aligned to Bank of Ghana credit reporting data formats.",
                ReportType = "Regulatory - CRB",
                DataSource = "CRB_CONSUMER_DISHONOURED_CHEQUE",
                Frequency = "Monthly",
                TemplateFormat = "CSV",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-CRB-CONSUMER-JUDGMENT",
                ReportName = "CRB Consumer Judgment Submission",
                Description = "Consumer judgment extract aligned to Bank of Ghana credit reporting data formats.",
                ReportType = "Regulatory - CRB",
                DataSource = "CRB_CONSUMER_JUDGMENT",
                Frequency = "Monthly",
                TemplateFormat = "CSV",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-CRB-BUSINESS-CREDIT",
                ReportName = "CRB Business Credit Data Submission",
                Description = "Business credit data extract aligned to Bank of Ghana credit reporting data formats for licensed credit reference bureaus.",
                ReportType = "Regulatory - CRB",
                DataSource = "CRB_BUSINESS_CREDIT",
                Frequency = "Monthly",
                TemplateFormat = "CSV",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-CRB-BUSINESS-DISHONOURED",
                ReportName = "CRB Business Dishonoured Cheque Submission",
                Description = "Business dishonoured cheque extract aligned to Bank of Ghana credit reporting data formats.",
                ReportType = "Regulatory - CRB",
                DataSource = "CRB_BUSINESS_DISHONOURED_CHEQUE",
                Frequency = "Monthly",
                TemplateFormat = "CSV",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-CRB-BUSINESS-JUDGMENT",
                ReportName = "CRB Business Judgment Submission",
                Description = "Business judgment extract aligned to Bank of Ghana credit reporting data formats.",
                ReportType = "Regulatory - CRB",
                DataSource = "CRB_BUSINESS_JUDGMENT",
                Frequency = "Monthly",
                TemplateFormat = "CSV",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-ORASS-SUBMISSION-QUEUE",
                ReportName = "ORASS Submission Queue",
                Description = "Operational queue for regulatory returns prepared for ORASS submission, review, and upload control.",
                ReportType = "Regulatory - ORASS",
                DataSource = "ORASS_SUBMISSION_QUEUE",
                Frequency = "Daily",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
            new()
            {
                ReportCode = "REG-ORASS-VALIDATION-EXCEPTIONS",
                ReportName = "ORASS Validation Exceptions",
                Description = "Exception report for regulatory submissions awaiting cleanup before final ORASS completion.",
                ReportType = "Regulatory - ORASS",
                DataSource = "ORASS_VALIDATION_EXCEPTIONS",
                Frequency = "Daily",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = true,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true,
            },
        };

        foreach (var report in regulatoryReports.Where(report => !existingCodes.Contains(report.ReportCode)))
        {
            context.ReportDefinitions.Add(report);
        }

        var registry = new ReportCatalogRegistry();
        foreach (var definition in registry.GetAll().Where(item => !existingCodes.Contains(item.ReportCode)))
        {
            context.ReportDefinitions.Add(new ReportDefinition
            {
                ReportCode = definition.ReportCode,
                ReportName = definition.ReportName,
                Description = definition.Description,
                ReportType = definition.Category,
                DataSource = definition.DataSource,
                Frequency = "OnDemand",
                TemplateFormat = "JSON",
                TemplateContent = "{}",
                RequiresApproval = definition.RequiresApprovalBeforeFinalExport,
                CreatedBy = "system",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = definition.IsActive,
            });
        }

        await context.SaveChangesAsync();
    }
    private static async Task EnsureAdminUserAsync(ApplicationDbContext context)
    {
        var adminUser = await context.Staff.FirstOrDefaultAsync(s => s.Email == "admin@bankinsight.local");
        if (adminUser == null)
        {
            adminUser = new Staff
            {
                Id = "STF0001",
                Name = "Admin User",
                Email = "admin@bankinsight.local",
                Phone = "0000000000",
                PasswordHash = "password123",
                BranchId = "BR001",
                Status = "Active",
                AvatarInitials = "AU",
                AccessScopeType = AccessScopeType.All,
                CreatedAt = DateTime.UtcNow,
            };
            context.Staff.Add(adminUser);
            await context.SaveChangesAsync();
        }

        var hasAdminRole = await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == "ROLE_ADMIN");
        if (!hasAdminRole)
        {
            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = "ROLE_ADMIN",
                AssignedAtUtc = DateTime.UtcNow,
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task BackfillLegacyLoanMetadataAsync(ApplicationDbContext context)
    {
        var defaultRetailLoanProductId = await context.Products
            .Where(p => p.Type == "LOAN" && p.Status == "ACTIVE")
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(defaultRetailLoanProductId))
        {
            return;
        }

        var legacyLoans = await context.Loans
            .Where(l => string.IsNullOrWhiteSpace(l.ProductCode) && !string.IsNullOrWhiteSpace(l.LoanProductId))
            .ToListAsync();

        if (legacyLoans.Count == 0)
        {
            return;
        }

        foreach (var loan in legacyLoans)
        {
            loan.ProductCode = defaultRetailLoanProductId;
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureGroupLendingSeedDataAsync(ApplicationDbContext context)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == "PRD_GRP_WEEKLY");
        if (product == null)
        {
            product = new Product
            {
                Id = "PRD_GRP_WEEKLY",
                Name = "Weekly Joint Liability Group Loan",
                Description = "Sample Ghana-style weekly cycle group loan product",
                Type = "LOAN",
                Currency = "GHS",
                InterestRate = 36m,
                InterestMethod = "FLAT",
                MinAmount = 500m,
                MaxAmount = 5000m,
                MinTerm = 8,
                MaxTerm = 24,
                DefaultTerm = 16,
                Status = "ACTIVE",
                LendingMethodology = "GROUP",
                IsGroupLoanEnabled = true,
                SupportsJointLiability = true,
                RequiresGroup = true,
                DefaultRepaymentFrequency = "Weekly",
                AllowedRepaymentFrequenciesJson = "[\"Weekly\",\"Monthly\"]",
                SupportsWeeklyRepayment = true,
                MinimumGroupSize = 5,
                MaximumGroupSize = 30,
                RequiresCompulsorySavings = true,
                MinimumSavingsToLoanRatio = 0.10m,
                RequiresGroupApprovalMeeting = true,
                UsesMemberLevelUnderwriting = true,
                UsesGroupLevelApproval = true,
                MaxCycleNumber = 5,
                GroupGuaranteePolicyType = "JOINT_LIABILITY",
                MeetingCollectionMode = "MEETING_BATCH",
                AllowBatchDisbursement = true,
                AllowRescheduleWithinGroup = true,
                GroupPenaltyPolicy = "MEETING_FINE",
                GroupDelinquencyPolicy = "WATCHLIST_ESCALATION",
                GroupOfficerAssignmentMode = "GROUP_OFFICER"
            };
            context.Products.Add(product);
        }

        if (!await context.ProductGroupRules.AnyAsync(r => r.ProductId == "PRD_GRP_WEEKLY"))
        {
            context.ProductGroupRules.Add(new ProductGroupRule
            {
                ProductId = "PRD_GRP_WEEKLY",
                MinMembersRequired = 5,
                MaxMembersAllowed = 30,
                MinWeeks = 8,
                MaxWeeks = 24,
                RequiresCompulsorySavings = true,
                MinSavingsToLoanRatio = 0.10m,
                RequiresGroupApprovalMeeting = true,
                RequiresJointLiability = true,
                AllowReschedule = true,
                DefaultRepaymentFrequency = "Weekly",
                DefaultInterestMethod = "Flat",
                AllocationOrderJson = "[\"Penalty\",\"Fees\",\"Interest\",\"Principal\",\"Savings\"]",
                MeetingCollectionRuleJson = "{\"mode\":\"MEETING_BATCH\",\"allowPartial\":true}",
                AccountingProfileJson = "{\"portfolioGl\":\"1300\",\"incomeGl\":\"4100\"}",
                DisclosureTemplate = "GH_GROUP_WEEKLY_STANDARD"
            });
        }

        if (!await context.ProductEligibilityRules.AnyAsync(r => r.ProductId == "PRD_GRP_WEEKLY"))
        {
            context.ProductEligibilityRules.Add(new ProductEligibilityRule
            {
                ProductId = "PRD_GRP_WEEKLY",
                RequiresKycComplete = true,
                BlockOnSevereArrears = true,
                MaxAllowedExposure = 5000m,
                MinMembershipDays = 30,
                MinAttendanceRate = 0.75m,
                RequireCreditBureauCheck = false,
                RuleJson = "{\"attendance\":\"MIN_75_PERCENT\",\"savingsDiscipline\":true}"
            });
        }

        if (!await context.LendingCenters.AnyAsync(c => c.Id == "CTR-SEED-001"))
        {
            context.LendingCenters.Add(new LendingCenter
            {
                Id = "CTR-SEED-001",
                BranchId = "BR001",
                CenterCode = "CENTER-ADENTA",
                CenterName = "Adenta Tuesday Center",
                MeetingDayOfWeek = "Tuesday",
                MeetingLocation = "Adenta Market Square",
                AssignedOfficerId = "STF0001",
                Status = "ACTIVE"
            });
        }

        if (!await context.Groups.AnyAsync(g => g.Id == "GLG-SEED-001"))
        {
            context.Groups.Add(new Group
            {
                Id = "GLG-SEED-001",
                Name = "Adenta Traders Trust Group",
                GroupCode = "ADT-TRUST-01",
                BranchId = "BR001",
                CenterId = "CTR-SEED-001",
                MeetingDay = "Tuesday",
                MeetingDayOfWeek = "Tuesday",
                MeetingFrequency = "Weekly",
                MeetingLocation = "Adenta Market Square",
                OfficerId = "STF0001",
                AssignedOfficerId = "STF0001",
                FormationDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-90)),
                Status = "ACTIVE",
                IsJointLiabilityEnabled = true,
                MaxMembers = 25,
                Notes = "Seed group for group lending demos",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

}






