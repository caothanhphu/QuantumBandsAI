// QuantumBands.API/Workers/DailyTradingSnapshotWorker.cs
using QuantumBands.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuantumBands.API.Workers;

public class DailyTradingSnapshotWorker : BackgroundService
{
    private readonly ILogger<DailyTradingSnapshotWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    // TODO: Get schedule from configuration
    private readonly TimeSpan _snapshotTimeUtc = new TimeSpan(23, 55, 0); // Run at 23:55 UTC daily (adjust as needed)

    public DailyTradingSnapshotWorker(ILogger<DailyTradingSnapshotWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Trading Snapshot Worker is starting.");

        stoppingToken.Register(() => _logger.LogInformation("Daily Trading Snapshot Worker is stopping."));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                DateTime nextRunTimeUtc = nowUtc.Date.Add(_snapshotTimeUtc);

                if (nowUtc > nextRunTimeUtc)
                {
                    // If current time is past today's snapshot time, schedule for tomorrow
                    nextRunTimeUtc = nextRunTimeUtc.AddDays(1);
                }

                TimeSpan delay = nextRunTimeUtc - nowUtc;
                if (delay < TimeSpan.Zero) // Should not happen if logic above is correct
                {
                    delay = TimeSpan.Zero;
                }

                _logger.LogInformation("Next daily snapshot run scheduled for: {NextRunTimeUtc} (in {Delay}). Current UTC time: {NowUtc}",
                                       nextRunTimeUtc, delay, nowUtc);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("Daily Trading Snapshot Worker is running at {RunTimeUtc}", DateTime.UtcNow);

                // Create a scope to resolve scoped services like DbContext and UnitOfWork
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dailySnapshotService = scope.ServiceProvider.GetRequiredService<IDailySnapshotService>();
                    // Snapshot for "today" (the date part of UtcNow when the job runs)
                    // Or, if running after midnight for previous day, use UtcNow.Date.AddDays(-1)
                    DateTime dateToSnapshot = DateTime.UtcNow.Date;
                    // If you run at 00:05 UTC for previous day, use: DateTime.UtcNow.Date.AddDays(-1);

                    string result = await dailySnapshotService.CreateDailySnapshotsAsync(dateToSnapshot, stoppingToken);
                    _logger.LogInformation("Daily snapshot creation result for {DateToSnapshot}: {Result}", dateToSnapshot, result);
                }
            }
            catch (OperationCanceledException)
            {
                // When stoppingToken is signaled
                _logger.LogInformation("Daily Trading Snapshot Worker execution was cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Daily Trading Snapshot Worker.");
                // Wait for a shorter period before retrying to avoid spamming logs if there's a persistent issue
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        _logger.LogInformation("Daily Trading Snapshot Worker has stopped.");
    }
}