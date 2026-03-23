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

var builder = WebApplication.CreateBuilder(args);

// Read database connection string from environment variable with fallback to configuration
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string must be provided via DB_CONNECTION_STRING environment variable or ConnectionStrings:DefaultConnection in configuration");
}

// Resolve and validate JWT secret once during startup.
var jwtSecretBytes = JwtSecretResolver.ResolveBytes(builder.Configuration);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<BankingOSMetadataService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<DataMigrationService>();
builder.Services.AddScoped<GlService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<GroupLendingService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<OperationsService>();
builder.Services.AddHostedService<EodSchedulerHostedService>();
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
builder.Services.AddScoped<ICreditBureauProvider, HttpCreditBureauProvider>();
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<ICreditBureauProvider, MockCreditBureauProvider>();
}
builder.Services.AddScoped<IEmailAlertService, EmailAlertService>();
builder.Services.AddScoped<ISuspiciousActivityService, SuspiciousActivityService>();
builder.Services.AddScoped<IDeviceSecurityService, DeviceSecurityService>();
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
builder.Services.AddScoped<IRiskAnalyticsService, RiskAnalyticsService>();
builder.Services.AddHttpClient(); // For Bank of Ghana API integration

// Reporting & Analytics services
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IRegulatoryReportService, RegulatoryReportService>();
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportCatalogRegistry, ReportCatalogRegistry>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IEnterpriseReportingService, EnterpriseReportingService>();

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
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// CORS configuration for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", builder =>
    {
        builder.AllowAnyOrigin()
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(db);
    }
    else
    {
        await db.Database.MigrateAsync();
        await DatabaseSchemaBootstrapper.EnsureAsync(db);
        await DatabaseSeeder.SeedAsync(db);
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
app.UseRateLimiting();          // Rate limiting before authentication
app.UseIpWhitelist();           // IP-based access control
app.UseJintScriptingSandbox();  // Custom Jint Script Interceptors before Auth
app.UseCors("AllowLocalhost");
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











