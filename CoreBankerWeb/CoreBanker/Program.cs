using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CoreBanker;
using MudBlazor.Services;
using CoreBanker.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrlSetting = builder.Configuration["ApiBaseUrl"];
var resolvedApiBaseAddress = string.IsNullOrWhiteSpace(apiBaseUrlSetting)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : Uri.TryCreate(apiBaseUrlSetting, UriKind.Absolute, out var absoluteApiBaseAddress)
        ? absoluteApiBaseAddress
        : new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiBaseUrlSetting);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = resolvedApiBaseAddress });
builder.Services.AddMudServices();
builder.Services.AddScoped<CoreBanker.State.AppState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthStateProvider, AuthStateProvider>();
builder.Services.AddScoped<CoreBanker.Services.DashboardService>();
builder.Services.AddScoped<CoreBanker.Services.ClientService>();
builder.Services.AddScoped<CoreBanker.Services.AccountService>();
builder.Services.AddScoped<CoreBanker.Services.TellerService>();
builder.Services.AddScoped<CoreBanker.Services.TransactionService>();
builder.Services.AddScoped<CoreBanker.Services.LoanService>();
builder.Services.AddScoped<CoreBanker.Services.ApprovalService>();
builder.Services.AddScoped<CoreBanker.Services.AuditService>();
builder.Services.AddScoped<CoreBanker.Services.AccountingService>();
builder.Services.AddScoped<CoreBanker.Services.ReportingService>();
builder.Services.AddScoped<CoreBanker.Services.SecurityService>();
builder.Services.AddScoped<CoreBanker.Services.MigrationService>();
builder.Services.AddScoped<CoreBanker.Services.ExtensibilityService>();
builder.Services.AddScoped<CoreBanker.Services.GroupLendingService>();

await builder.Build().RunAsync();
