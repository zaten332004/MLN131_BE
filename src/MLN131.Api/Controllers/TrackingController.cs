using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLN131.Api.Contracts.Tracking;
using MLN131.Api.Data;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/track")]
public sealed class TrackingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public TrackingController(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    [HttpPost("pageview")]
    [AllowAnonymous]
    public async Task<IActionResult> PageView(PageViewRequest req, CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue("VisitSessionId", out var sessionIdObj) ||
            sessionIdObj is not Guid sessionId)
        {
            return Ok();
        }

        Guid? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var parsed))
        {
            userId = parsed;
        }

        _db.PageViewEvents.Add(new PageViewEvent
        {
            VisitSessionId = sessionId,
            UserId = userId,
            Path = (req.Path ?? "/").Trim(),
            Referrer = req.Referrer,
            OccurredAt = _time.GetUtcNow(),
        });

        await _db.SaveChangesAsync(ct);
        return Ok();
    }
}

