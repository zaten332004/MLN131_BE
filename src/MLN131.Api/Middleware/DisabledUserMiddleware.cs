using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Data;

namespace MLN131.Api.Middleware;

public sealed class DisabledUserMiddleware : IMiddleware
{
    private readonly ApplicationDbContext _db;

    public DisabledUserMiddleware(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var isDisabled = await _db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.IsDisabled)
                    .FirstOrDefaultAsync(context.RequestAborted);

                if (isDisabled)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Your account has been disabled."
                    });
                    return;
                }
            }
        }

        await next(context);
    }
}

