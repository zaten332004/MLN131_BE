using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Common;
using MLN131.Api.Contracts.Chat;
using MLN131.Api.Data;
using MLN131.Api.Services;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Roles = Roles.User + "," + Roles.Admin)]
public sealed class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly GeminiChatService _gemini;
    private readonly TimeProvider _time;

    public ChatController(ApplicationDbContext db, GeminiChatService gemini, TimeProvider time)
    {
        _db = db;
        _gemini = gemini;
        _time = time;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest req, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid or expired authentication token." });
        }

        var msg = (req.Message ?? "").Trim();
        if (string.IsNullOrWhiteSpace(msg))
        {
            return BadRequest(new { message = "Message is required." });
        }

        var history = await _db.ChatMessages.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(12)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.Role, m.Content })
            .ToListAsync(ct);

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            Role = "user",
            Content = msg,
            CreatedAt = _time.GetUtcNow(),
        });
        await _db.SaveChangesAsync(ct);

        var answer = await _gemini.GenerateAsync(
            msg,
            history.Select(x => (x.Role, x.Content)),
            ct);

        _db.ChatMessages.Add(new ChatMessage
        {
            UserId = userId,
            Role = "assistant",
            Content = answer,
            CreatedAt = _time.GetUtcNow(),
        });
        await _db.SaveChangesAsync(ct);

        return Ok(new ChatResponse { Answer = answer });
    }

    [HttpGet("history")]
    public async Task<ActionResult<ChatHistoryResponse>> History(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid or expired authentication token." });
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.ChatMessages.AsNoTracking()
            .Where(m => m.UserId == userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatHistoryItemResponse
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToArrayAsync(ct);

        return Ok(new ChatHistoryResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }
}

