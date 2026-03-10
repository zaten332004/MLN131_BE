using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MLN131.Api.Contracts.Responses;
using MLN131.Api.Data;
using MLN131.Api.Hubs;
using MLN131.Api.Services;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/responses")]
[Authorize(Roles = MLN131.Api.Common.Roles.User + "," + MLN131.Api.Common.Roles.Admin)]
public sealed class ResponsesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;
    private readonly StatsService _stats;
    private readonly IHubContext<StatsHub> _hub;

    public ResponsesController(
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

    [HttpPost]
    public async Task<IActionResult> Submit(SubmitResponseRequest req, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid or expired authentication token." });
        }

        var qk = (req.QuestionKey ?? "").Trim();
        var ans = (req.AnswerText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(qk) || string.IsNullOrWhiteSpace(ans))
        {
            return BadRequest(new { message = "QuestionKey and AnswerText are required." });
        }

        _db.UserResponses.Add(new UserResponse
        {
            UserId = userId,
            QuestionKey = qk,
            AnswerText = ans,
            CreatedAt = _time.GetUtcNow(),
        });

        await _db.SaveChangesAsync(ct);

        // Push stats update immediately for admin dashboard (also kept fresh by background broadcaster).
        try
        {
            var payload = await _stats.GetRealtimeAsync(ct);
            await _hub.Clients.All.SendAsync("realtimeStats", payload, ct);
        }
        catch
        {
            // Ignore; background broadcaster will retry.
        }

        return Ok();
    }
}

