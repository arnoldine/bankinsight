using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BankInsight.API.Services;

public class EodSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EodSchedulerHostedService> _logger;

    public EodSchedulerHostedService(IServiceScopeFactory scopeFactory, ILogger<EodSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSchedulerPassAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EOD scheduler pass failed");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task RunSchedulerPassAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var operationsService = scope.ServiceProvider.GetRequiredService<OperationsService>();
        await operationsService.ExecuteScheduledBatchAsync();
    }
}
