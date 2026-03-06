using Microsoft.EntityFrameworkCore;
using MLN131.Api.Data;

namespace MLN131.Api.HostedServices;

public sealed class SessionFinalizerHostedService : BackgroundService
{
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _sp;
    private readonly TimeProvider _time;

    public SessionFinalizerHostedService(IServiceProvider sp, TimeProvider time)
    {
        _sp = sp;
        _time = time;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FinalizeInactiveAsync(stoppingToken);
            }
            catch
            {
                // swallow; next tick will retry
            }

            await Task.Delay(Tick, stoppingToken);
        }
    }

    private async Task FinalizeInactiveAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = _time.GetUtcNow();
        var cutoff = now.Add(-InactivityTimeout);

        var sessions = await db.VisitSessions
            .Where(x => x.EndedAt == null && x.LastSeenAt < cutoff)
            .OrderBy(x => x.LastSeenAt)
            .Take(500)
            .ToListAsync(ct);

        if (sessions.Count == 0) return;

        foreach (var s in sessions)
        {
            s.EndedAt = s.LastSeenAt;
        }

        await db.SaveChangesAsync(ct);
    }
}

