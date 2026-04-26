using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Services;

public sealed class ClientFileScanHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClientFileScanHostedService> _logger;

    public ClientFileScanHostedService(IServiceScopeFactory scopeFactory, ILogger<ClientFileScanHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var securityService = scope.ServiceProvider.GetRequiredService<IClientFileSecurityService>();
                var processed = await securityService.ProcessPendingScansAsync(stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("Processed {ProcessedCount} pending client file scans.", processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Client file scan pass failed.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
