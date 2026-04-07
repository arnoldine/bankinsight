using HybridTransfer.Application.Abstractions;
using HybridTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HybridTransfer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHybridTransferPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration[$"Persistence:Provider"] ?? "InMemory";

        if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("HybridTransferDb")
                ?? throw new InvalidOperationException("Connection string 'HybridTransferDb' is required when Persistence:Provider is Postgres.");

            services.AddDbContext<HybridTransferDbContext>(db => db.UseNpgsql(connectionString));
            services.AddScoped<ITransactionManager, EfTransactionManager>();
            services.AddScoped<IJournalRepository, EfJournalRepository>();
            services.AddScoped<ITransferOrderRepository, EfTransferOrderRepository>();
            services.AddScoped<IWalletProjectionRepository, EfWalletProjectionRepository>();
            services.AddScoped<IAuditEventRepository, EfAuditEventRepository>();
            services.AddScoped<IWebhookReceiptRepository, EfWebhookReceiptRepository>();
            services.AddScoped<IAlertRepository, EfAlertRepository>();
            services.AddScoped<IApprovalRequestRepository, EfApprovalRequestRepository>();
            services.AddScoped<IReconciliationRepository, EfReconciliationRepository>();
            return services;
        }

        services.AddSingleton<ITransactionManager, InMemoryTransactionManager>();
        services.AddSingleton<IJournalRepository, InMemoryJournalRepository>();
        services.AddSingleton<ITransferOrderRepository, InMemoryTransferOrderRepository>();
        services.AddSingleton<IWalletProjectionRepository, InMemoryWalletProjectionRepository>();
        services.AddSingleton<IAuditEventRepository, InMemoryAuditEventRepository>();
        services.AddSingleton<IWebhookReceiptRepository, InMemoryWebhookReceiptRepository>();
        services.AddSingleton<IAlertRepository, InMemoryAlertRepository>();
        services.AddSingleton<IApprovalRequestRepository, InMemoryApprovalRequestRepository>();
        services.AddSingleton<IReconciliationRepository, InMemoryReconciliationRepository>();
        return services;
    }
}
