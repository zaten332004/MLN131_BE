using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Data;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/content")]
[Authorize]
public sealed class ContentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ContentController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("pages")]
    public async Task<ActionResult<object[]>> ListPages(CancellationToken ct)
    {
        var pages = await _db.ContentPages.AsNoTracking()
            .OrderBy(p => p.Title)
            .Select(p => new { p.Slug, p.Title, p.UpdatedAt, p.CreatedAt })
            .ToListAsync(ct);

        return Ok(pages.ToArray());
    }

    [HttpGet("pages/{slug}")]
    public async Task<ActionResult<object>> GetPage(string slug, CancellationToken ct)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        var page = await _db.ContentPages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

        if (page is null)
        {
            return NotFound(new { message = "Content page not found." });
        }

        return Ok(new
        {
            page.Slug,
            page.Title,
            page.BodyMarkdown,
            page.CreatedAt,
            page.UpdatedAt,
        });
    }
}

