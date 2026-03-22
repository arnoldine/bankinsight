using BankInsight.API.Controllers;
using BankInsight.API.Data;
using BankInsight.API.Infrastructure;
using BankInsight.API.Middleware;
using BankInsight.API.Security;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "BankingOS API requires a database connection string via DB_CONNECTION_STRING or ConnectionStrings:DefaultConnection.");
}

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSettings:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("BankingOS API requires JWT_SECRET or JwtSettings:Secret.");
}

var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "BankingOS";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "BankingOSAPI";

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
builder.Services.AddScoped<IClerkUserSyncService, ClerkUserSyncService>();
builder.Services.AddScoped<IFxRateService, FxRateService>();
builder.Services.AddScoped<ITreasuryPositionService, TreasuryPositionService>();
builder.Services.AddScoped<IFxTradingService, FxTradingService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();
builder.Services.AddScoped<IRiskAnalyticsService, RiskAnalyticsService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IRegulatoryReportService, RegulatoryReportService>();
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IReportCatalogRegistry, ReportCatalogRegistry>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IEnterpriseReportingService, EnterpriseReportingService>();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.FormFieldName = "_csrf_token";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Legacy";
    options.DefaultChallengeScheme = "Legacy";
})
.AddJwtBearer("Clerk", options =>
{
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
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
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
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(AuthController).Assembly);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBankingOSLocalhost", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3005",
                "http://localhost:3006",
                "http://localhost:3007",
                "http://localhost:4175",
                "http://localhost:4176",
                "http://localhost:4177")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BankingOS API",
        Version = "v1",
        Description = "Standalone BankingOS API host built on the shared BankInsight banking engine."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseGlobalErrorHandling();
app.UsePerformanceMonitoring();
app.UseRateLimiting();
app.UseIpWhitelist();
app.UseJintScriptingSandbox();
app.UseCors("AllowBankingOSLocalhost");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.Run();

public partial class Program
{
}
