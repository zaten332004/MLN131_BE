using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLN131.Api.Contracts.Responses;
using MLN131.Api.Data;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/responses")]
[Authorize(Roles = MLN131.Api.Common.Roles.User + "," + MLN131.Api.Common.Roles.Admin)]
public sealed class ResponsesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public ResponsesController(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
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
        return Ok();
    }
}

