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
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ApplicationDbContext db,
        GeminiChatService gemini,
        TimeProvider time,
        ILogger<ChatController> logger)
    {
        _db = db;
        _gemini = gemini;
        _time = time;
        _logger = logger;
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

        string answer;
        try
        {
            answer = await _gemini.GenerateAsync(
                msg,
                history.Select(x => (x.Role, x.Content)),
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Missing config: Gemini:ApiKey", StringComparison.Ordinal))
        {
            _logger.LogWarning(ex, "Gemini API key is missing.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "AI chat is not configured. Please set Gemini__ApiKey (environment variable) or Gemini:ApiKey (appsettings)."
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Gemini API.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "AI service is unreachable. Please try again later."
            });
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Gemini request timed out.");
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                message = "AI service timed out. Please try again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini chat failed.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "AI service error. Please try again later."
            });
        }

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

