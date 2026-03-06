using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLN131.Api.Common;
using MLN131.Api.Contracts.Stats;
using MLN131.Api.Services;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminStatsController : ControllerBase
{
    private readonly StatsService _stats;

    public AdminStatsController(StatsService stats)
    {
        _stats = stats;
    }

    [HttpGet("realtime")]
    public async Task<ActionResult<RealtimeStatsResponse>> GetRealtime(CancellationToken ct)
    {
        return Ok(await _stats.GetRealtimeAsync(ct));
    }
}

