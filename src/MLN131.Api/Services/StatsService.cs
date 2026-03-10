using Microsoft.EntityFrameworkCore;
using MLN131.Api.Common;
using MLN131.Api.Contracts.Stats;
using MLN131.Api.Data;

namespace MLN131.Api.Services;

public sealed class StatsService
{
    // If you want "instant" online counts on FE, call /api/track/pageview periodically (heartbeat).
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(30);

    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public StatsService(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<RealtimeStatsResponse> GetRealtimeAsync(CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var onlineCutoff = now.Add(-OnlineWindow);
        var last24h = now.AddHours(-24);

        var onlineSessionsQuery = _db.VisitSessions.AsNoTracking()
            .Where(x => x.EndedAt == null && x.LastSeenAt >= onlineCutoff);

        onlineSessionsQuery = ExcludeAdminSessions(onlineSessionsQuery);

        var visitorsOnline = await onlineSessionsQuery.CountAsync(ct);
        var loggedInOnline = await onlineSessionsQuery.Where(x => x.UserId != null).CountAsync(ct);

        var distinctAnsweredTotal = await _db.UserResponses.AsNoTracking()
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(ct);

        var distinctAnswered24h = await _db.UserResponses.AsNoTracking()
            .Where(x => x.CreatedAt >= last24h)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync(ct);

        var sessions24h = _db.VisitSessions.AsNoTracking()
            .Where(x => x.StartedAt >= last24h);

        var avgSeconds = await sessions24h
            .Select(x => (double?)EF.Functions.DateDiffSecond(
                x.StartedAt,
                x.EndedAt ?? now))
            .AverageAsync(ct) ?? 0;

        return new RealtimeStatsResponse
        {
            AsOf = now,
            VisitorsOnline = visitorsOnline,
            LoggedInOnline = loggedInOnline,
            DistinctUsersAnsweredTotal = distinctAnsweredTotal,
            DistinctUsersAnsweredLast24h = distinctAnswered24h,
            AvgSessionDurationSecondsLast24h = avgSeconds,
        };
    }

    private IQueryable<VisitSession> ExcludeAdminSessions(IQueryable<VisitSession> query)
    {
        var adminNormalized = Roles.Admin.ToUpperInvariant();
        var adminRoleIds = _db.Roles.AsNoTracking()
            .Where(r => r.NormalizedName == adminNormalized)
            .Select(r => r.Id);

        var adminUserIds = _db.UserRoles.AsNoTracking()
            .Where(ur => adminRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId);

        return query.Where(s => s.UserId == null || !adminUserIds.Contains(s.UserId.Value));
    }
}

