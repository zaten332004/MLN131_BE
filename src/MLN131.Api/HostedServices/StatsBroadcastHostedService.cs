using Microsoft.AspNetCore.SignalR;
using MLN131.Api.Contracts.Stats;
using MLN131.Api.Hubs;
using MLN131.Api.Services;

namespace MLN131.Api.HostedServices;

public sealed class StatsBroadcastHostedService : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(3);

    private readonly IServiceProvider _sp;
    private readonly IHubContext<StatsHub> _hub;

    public StatsBroadcastHostedService(IServiceProvider sp, IHubContext<StatsHub> hub)
    {
        _sp = sp;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BroadcastAsync(stoppingToken);
            }
            catch
            {
                // swallow; next tick will retry
            }

            await Task.Delay(Tick, stoppingToken);
        }
    }

    private async Task BroadcastAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var stats = scope.ServiceProvider.GetRequiredService<StatsService>();
        RealtimeStatsResponse payload = await stats.GetRealtimeAsync(ct);

        await _hub.Clients.All.SendAsync("realtimeStats", payload, ct);
    }
}

