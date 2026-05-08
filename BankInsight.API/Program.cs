using Microsoft.EntityFrameworkCore;
using BankInsight.API.Middleware;
using BankInsight.API.Data;
using BankInsight.API.Services;
using BankInsight.API.Security;
using BankInsight.API.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.ResponseCompression;
using HybridTransfer.Infrastructure.DependencyInjection;
using HybridTransfer.Infrastructure.Persistence;
using HybridTransfer.Infrastructure.Providers;
using HybridPostingEngine = HybridTransfer.Application.Services.PostingEngine;
using HybridAuditTrailService = HybridTransfer.Application.Services.AuditTrailService;
using HybridTransferPostingPolicyService = HybridTransfer.Application.Services.TransferPostingPolicyService;
using HybridOperationsExplorerService = HybridTransfer.Application.Services.OperationsExplorerService;
using HybridComplianceExplorerService = HybridTransfer.Application.Services.ComplianceExplorerService;
using HybridLedgerApplicationService = HybridTransfer.Application.Services.LedgerApplicationService;
using HybridRiskAssessmentService = HybridTransfer.Application.Services.RiskAssessmentService;
using HybridTransferExecutionService = HybridTransfer.Application.Services.TransferExecutionService;
using HybridApprovalService = HybridTransfer.Application.Services.ApprovalService;
using HybridReconciliationService = HybridTransfer.Application.Services.ReconciliationService;
using HybridReportingCatalogService = HybridTransfer.Application.Services.ReportingCatalogService;
using HybridPayoutOrchestrator = HybridTransfer.Application.Services.PayoutOrchestrator;
using HybridWebhookProcessor = HybridTransfer.Application.Services.WebhookProcessor;
using HybridProviderTransferStatusService = HybridTransfer.Application.Services.ProviderTransferStatusService;
using HybridWebhookReceiptService = HybridTransfer.Application.Services.WebhookReceiptService;
using HybridCurrencyPolicyService = HybridTransfer.Application.Services.CurrencyPolicyService;
using HybridTransferRoutingPolicyService = HybridTransfer.Application.Services.TransferRoutingPolicyService;

var builder = WebApplication.CreateBuilder(args);

// Read database connection string from environment variable with fallback to configuration
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string must be provided via DB_CONNECTION_STRING environment variable or ConnectionStrings:DefaultConnection in configuration");
}

// Keep the core API context and the HybridTransfer module pinned to the same resolved
// connection string so Docker/container environments do not fall back to localhost
// values from appsettings.Development.json.
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = connectionString,
    ["ConnectionStrings:HybridTransferDb"] = connectionString
});

// Resolve and validate JWT secret once during startup.
var jwtSecretBytes = JwtSecretResolver.ResolveBytes(builder.Configuration);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClientAuthService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<BankingOSMetadataService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<Customer360Service>();
builder.Services.AddScoped<OperationsControlCenterService>();
builder.Services.AddScoped<CollectionsService>();
builder.Services.AddScoped<MicrofinanceService>();
builder.Services.AddScoped<DeveloperPortalService>();
builder.Services.AddScoped<ReconciliationHubService>();
builder.Services.AddScoped<CollateralManagementService>();
builder.Services.AddScoped<WorkspacePreferencesService>();
builder.Services.AddScoped<ClientChannelService>();
builder.Services.AddScoped<IClientFileStorageService, ClientFileStorageService>();
builder.Services.AddScoped<IClientFileSecurityService, ClientFileSecurityService>();
builder.Services.AddScoped<DataMigrationService>();
builder.Services.AddScoped<GlService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<GroupLendingService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<OperationsService>();
builder.Services.AddScoped<PaymentOperationsService>();
builder.Services.AddHostedService<EodSchedulerHostedService>();
builder.Services.AddHostedService<ClientFileScanHostedService>();
builder.Services.AddScoped<ILoanAccountingPostingService, LoanAccountingPostingService>();
builder.Services.AddScoped<IFeeService, FeeService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<IOrassService, OrassService>();

// Event-Driven Architecture Services
builder.Services.AddScoped<IPostingEngine, PostingEngine>();
builder.Services.AddScoped<IDepositEngine, DepositEngine>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<ProcessAssignmentService>();
builder.Services.AddScoped<ProcessDefinitionService>();
builder.Services.AddScoped<ProcessRuntimeService>();
builder.Services.AddScoped<ProcessTaskService>();
builder.Services.AddScoped<ProcessEventTriggerService>();
builder.Services.AddScoped<ISequenceGeneratorService, SequenceGeneratorService>();

// Advanced features services
builder.Services.AddScoped<ILedgerEngine, LedgerEngine>();
builder.Services.AddScoped<IPrivilegeLeaseService, PrivilegeLeaseService>();

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
builder.Services.AddScoped<ILoginAttemptService, LoginAttemptService>();
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddScoped<ICreditBureauService, CreditBureauService>();
builder.Services.AddScoped<IInternalCreditScoringService, InternalCreditScoringService>();
builder.Services.AddScoped<ICreditBureauProvider, XdsCreditBureauProvider>();
builder.Services.AddScoped<ICreditBureauProvider, HttpCreditBureauProvider>();
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<ICreditBureauProvider, MockCreditBureauProvider>();
}
builder.Services.AddScoped<IEmailAlertService, EmailAlertService>();
builder.Services.AddScoped<ISuspiciousActivityService, SuspiciousActivityService>();
builder.Services.AddScoped<IDeviceSecurityService, DeviceSecurityService>();
builder.Services.AddScoped<IWafService, WafService>();
builder.Services.AddScoped<IBranchHierarchyService, BranchHierarchyService>();
builder.Services.AddScoped<IVaultManagementService, VaultManagementService>();
builder.Services.AddScoped<IInterBranchTransferService, InterBranchTransferService>();
builder.Services.AddScoped<IBranchLimitService, BranchLimitService>();
builder.Services.AddScoped<IBranchConfigService, BranchConfigService>();
builder.Services.AddScoped<ICashControlService, CashControlService>();
builder.Services.AddScoped<ICashIncidentService, CashIncidentService>();

// Clerk integration service
builder.Services.AddScoped<IClerkUserSyncService, ClerkUserSyncService>();

// Treasury Management services
builder.Services.AddScoped<IFxRateService, FxRateService>();
builder.Services.AddScoped<ITreasuryPositionService, TreasuryPositionService>();
builder.Services.AddScoped<IFxTradingService, FxTradingService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<DigitalBankingService>();
builder.Services.AddScoped<SupervisoryIntelligenceService>();
builder.Services.AddScoped<IRiskAnalyticsService, RiskAnalyticsService>();
builder.Services.AddHttpClient(); // For Bank of Ghana API integration
builder.Services.Configure<FintechProviderOptions>(builder.Configuration.GetSection("FintechProviders"));
builder.Services.Configure<FintechLedgerOptions>(builder.Configuration.GetSection("FintechLedger"));

// Reporting & Analytics services
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IRegulatoryReportService, RegulatoryReportService>();
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportCatalogRegistry, ReportCatalogRegistry>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IEnterpriseReportingService, EnterpriseReportingService>();
// Fintech platform module services
builder.Services.AddHybridTransferPersistence(builder.Configuration);
builder.Services.AddSingleton<HybridTransfer.Application.Abstractions.IMobileMoneyProvider, BankInsightMobileMoneyProvider>();
builder.Services.AddSingleton<HybridTransfer.Application.Abstractions.IBankTransferProvider, BankInsightBankTransferProvider>();
builder.Services.AddSingleton<HybridTransfer.Application.Abstractions.ICryptoCustodyProvider, BankInsightCryptoCustodyProvider>();
builder.Services.AddSingleton<HybridTransfer.Application.Abstractions.IWebhookSecurityService, BankInsightWebhookSecurityService>();
builder.Services.AddScoped<HybridPostingEngine>();
builder.Services.AddScoped<HybridAuditTrailService>();
builder.Services.AddScoped<HybridTransferPostingPolicyService>();
builder.Services.AddScoped<HybridOperationsExplorerService>();
builder.Services.AddScoped<HybridComplianceExplorerService>();
builder.Services.AddScoped<HybridLedgerApplicationService>();
builder.Services.AddScoped<HybridRiskAssessmentService>();
builder.Services.AddScoped<HybridTransferExecutionService>();
builder.Services.AddScoped<HybridApprovalService>();
builder.Services.AddScoped<HybridReconciliationService>();
builder.Services.AddScoped<HybridReportingCatalogService>();
builder.Services.AddScoped<HybridCurrencyPolicyService>();
builder.Services.AddScoped<HybridTransferRoutingPolicyService>();
builder.Services.AddScoped<HybridPayoutOrchestrator>();
builder.Services.AddScoped<HybridWebhookProcessor>();
builder.Services.AddScoped<BankTransferLifecycleService>();
builder.Services.AddScoped<HybridWebhookReceiptService>();
builder.Services.AddScoped<HybridProviderTransferStatusService>();
builder.Services.AddScoped<HybridTransfer.Application.Abstractions.IProviderTransferStatusProvider, BankInsightBankTransferProvider>();

// Add antiforgery service for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.FormFieldName = "_csrf_token";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "BankInsight";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "BankInsightAPI";
var clientIssuer = builder.Configuration["JwtSettings:ClientIssuer"] ?? "BankInsightClientAuth";
var clientAudience = builder.Configuration["JwtSettings:ClientAudience"] ?? "BankInsightClientAPI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Legacy";  // Default to legacy for backward compatibility
    options.DefaultChallengeScheme = "Legacy";
})
.AddJwtBearer("Clerk", options =>
{
    // Clerk JWT validation - in production, validate against Clerk's JWKS
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.Authority = builder.Configuration["Clerk:Authority"] ?? "https://clerk.com";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddJwtBearer("Legacy", options =>
{
    // Legacy JWT validation for backward compatibility
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtSecretBytes),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddJwtBearer("Client", options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtSecretBytes),
        ValidateIssuer = true,
        ValidIssuer = clientIssuer,
        ValidateAudience = true,
        ValidAudience = clientAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClientCustomer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes("Client");
        policy.RequireClaim("actor_type", "customer");
        policy.RequireClaim("token_family", "client_channel");
    });
});
builder.Services.AddControllers();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json"
    });
});

static string[] ResolveAllowedCorsOrigins(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configuredOrigins = configuration["Cors:AllowedOrigins"]
        ?? Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");

    if (!string.IsNullOrWhiteSpace(configuredOrigins))
    {
        return configuredOrigins
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        return new[]
        {
            "http://localhost:3000",
            "http://127.0.0.1:3000",
            "http://localhost:4173",
            "http://127.0.0.1:4173",
            "http://localhost:5173",
            "http://127.0.0.1:5173"
        };
    }

    return Array.Empty<string>();
}

var allowedCorsOrigins = ResolveAllowedCorsOrigins(builder.Configuration, builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", corsBuilder =>
    {
        if (allowedCorsOrigins.Length == 0)
        {
            corsBuilder.WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader();

            return;
        }

        corsBuilder.WithOrigins(allowedCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Swagger config with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();
var skipAutomaticMigrations = string.Equals(
    builder.Configuration["SKIP_DB_MIGRATIONS"] ?? Environment.GetEnvironmentVariable("SKIP_DB_MIGRATIONS"),
    "true",
    StringComparison.OrdinalIgnoreCase);
var enableSchemaBootstrap = string.Equals(
    builder.Configuration["ENABLE_SCHEMA_BOOTSTRAP"] ?? Environment.GetEnvironmentVariable("ENABLE_SCHEMA_BOOTSTRAP"),
    "true",
    StringComparison.OrdinalIgnoreCase);
var allowUnsafeStartupShortcuts = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(db);

        if (string.Equals(builder.Configuration["Persistence:Provider"], "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var hybridDb = scope.ServiceProvider.GetRequiredService<HybridTransferDbContext>();
            await hybridDb.Database.MigrateAsync();
        }
    }
    else
    {
        if (skipAutomaticMigrations && !allowUnsafeStartupShortcuts)
        {
            throw new InvalidOperationException("SKIP_DB_MIGRATIONS is only allowed in Development or Testing environments.");
        }

        if (enableSchemaBootstrap && !allowUnsafeStartupShortcuts)
        {
            throw new InvalidOperationException("ENABLE_SCHEMA_BOOTSTRAP is only allowed in Development or Testing environments.");
        }

        if (!skipAutomaticMigrations)
        {
            await db.Database.MigrateAsync();
        }

        if (allowUnsafeStartupShortcuts || enableSchemaBootstrap)
        {
            await DatabaseSchemaBootstrapper.EnsureAsync(db);
        }

        await DatabaseSeeder.SeedAsync(db);

        if (!skipAutomaticMigrations &&
            string.Equals(builder.Configuration["Persistence:Provider"], "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var hybridDb = scope.ServiceProvider.GetRequiredService<HybridTransferDbContext>();
            await hybridDb.Database.MigrateAsync();
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Add HSTS (HTTP Strict Transport Security) in production
    app.UseHsts();
}

// Add middleware in proper order
app.UseGlobalErrorHandling();  // Error handling first
app.UsePerformanceMonitoring(); // Performance monitoring 
app.UseWaf();                   // Web application firewall before rate limiting
app.UseRateLimiting();          // Rate limiting before authentication
app.UseIpWhitelist();           // IP-based access control
app.UseJintScriptingSandbox();  // Custom Jint Script Interceptors before Auth
app.UseResponseCompression();
app.UseCors("ConfiguredOrigins");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery(); // CSRF protection after authentication

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "bankinsight-api",
    environment = app.Environment.EnvironmentName,
    timestampUtc = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();
app.Run();

// Make Program class accessible to integration tests
public partial class Program { }





















