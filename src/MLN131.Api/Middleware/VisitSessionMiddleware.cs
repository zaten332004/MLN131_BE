using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Common;
using MLN131.Api.Data;

namespace MLN131.Api.Middleware;

public sealed class VisitSessionMiddleware : IMiddleware
{
    private const string VisitorCookieName = "mln131_vid";
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public VisitSessionMiddleware(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Do not track admin activity as page views / online visitors.
        if (context.User?.Identity?.IsAuthenticated == true && context.User.IsInRole(Roles.Admin))
        {
            await next(context);
            return;
        }

        var now = _time.GetUtcNow();

        var visitorId = context.Request.Cookies[VisitorCookieName];
        if (string.IsNullOrWhiteSpace(visitorId) || visitorId.Length > 64)
        {
            visitorId = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(VisitorCookieName, visitorId, new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = now.AddDays(365),
                IsEssential = true,
            });
        }

        Guid? userId = null;
        var userIdStr = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var parsed))
        {
            userId = parsed;
        }

        var session = await _db.VisitSessions
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(x =>
                x.VisitorId == visitorId &&
                x.EndedAt == null &&
                x.LastSeenAt >= now.Add(-InactivityTimeout), context.RequestAborted);

        if (session is null)
        {
            session = new VisitSession
            {
                VisitorId = visitorId,
                UserId = userId,
                StartedAt = now,
                LastSeenAt = now,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                PathFirst = context.Request.Path.Value,
            };
            _db.VisitSessions.Add(session);
        }
        else
        {
            session.LastSeenAt = now;
            if (userId is not null)
            {
                session.UserId ??= userId;
            }
        }

        await _db.SaveChangesAsync(context.RequestAborted);

        context.Items["VisitSessionId"] = session.Id;
        context.Items["VisitorId"] = visitorId;

        await next(context);
    }
}

