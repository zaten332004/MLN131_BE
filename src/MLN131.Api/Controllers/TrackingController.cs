using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Common;
using MLN131.Api.Contracts.Tracking;
using MLN131.Api.Data;
using MLN131.Api.Hubs;
using MLN131.Api.Services;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/track")]
public sealed class TrackingController : ControllerBase
{
    private static readonly TimeSpan PresenceWindow = TimeSpan.FromSeconds(30);

    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;
    private readonly StatsService _stats;
    private readonly IHubContext<StatsHub> _hub;

    public TrackingController(
        ApplicationDbContext db,
        TimeProvider time,
        StatsService stats,
        IHubContext<StatsHub> hub)
    {
        _db = db;
        _time = time;
        _stats = stats;
        _hub = hub;
    }

    [HttpPost("pageview")]
    [AllowAnonymous]
    public async Task<ActionResult<PageViewResponse>> PageView(PageViewRequest req, CancellationToken ct)
    {
        if (User?.Identity?.IsAuthenticated == true && User.IsInRole(Roles.Admin))
        {
            return Ok(new PageViewResponse
            {
                AsOf = _time.GetUtcNow(),
                Path = (req.Path ?? "/").Trim(),
                Online = 0
            });
        }

        if (!HttpContext.Items.TryGetValue("VisitSessionId", out var sessionIdObj) ||
            sessionIdObj is not Guid sessionId)
        {
            return Ok(new PageViewResponse
            {
                AsOf = _time.GetUtcNow(),
                Path = (req.Path ?? "/").Trim(),
                Online = 0
            });
        }

        Guid? userId = null;
        var userIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var parsed))
        {
            userId = parsed;
        }

        var now = _time.GetUtcNow();
        var path = (req.Path ?? "/").Trim();
        if (string.IsNullOrWhiteSpace(path)) path = "/";

        _db.PageViewEvents.Add(new PageViewEvent
        {
            VisitSessionId = sessionId,
            UserId = userId,
            Path = path,
            Referrer = req.Referrer,
            OccurredAt = now,
        });

        await _db.SaveChangesAsync(ct);

        var onlineCutoff = now.Add(-PresenceWindow);

        IQueryable<Guid> adminUserIds = GetAdminUserIdsQuery(_db);

        var onlineSessionIds = _db.VisitSessions.AsNoTracking()
            .Where(s => s.EndedAt == null && s.LastSeenAt >= onlineCutoff)
            .Select(s => s.Id);

        var onlineOnPath = await _db.PageViewEvents.AsNoTracking()
            .Where(e => e.Path == path && e.OccurredAt >= onlineCutoff)
            .Where(e => e.UserId == null || !adminUserIds.Contains(e.UserId.Value))
            .Where(e => onlineSessionIds.Contains(e.VisitSessionId))
            .Select(e => e.VisitSessionId)
            .Distinct()
            .CountAsync(ct);

        // Push stats update immediately for admin dashboard (also kept fresh by background broadcaster).
        try
        {
            var statsPayload = await _stats.GetRealtimeAsync(ct);
            await _hub.Clients.All.SendAsync("realtimeStats", statsPayload, ct);
        }
        catch
        {
            // Ignore; background broadcaster will retry.
        }

        return Ok(new PageViewResponse
        {
            AsOf = now,
            Path = path,
            Online = onlineOnPath
        });
    }

    private static IQueryable<Guid> GetAdminUserIdsQuery(ApplicationDbContext db)
    {
        var adminNormalized = Roles.Admin.ToUpperInvariant();
        var adminRoleIds = db.Roles.AsNoTracking()
            .Where(r => r.NormalizedName == adminNormalized)
            .Select(r => r.Id);

        return db.UserRoles.AsNoTracking()
            .Where(ur => adminRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId);
    }
}

