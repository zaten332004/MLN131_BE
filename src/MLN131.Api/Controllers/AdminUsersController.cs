using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Common;
using MLN131.Api.Contracts.Admin;
using MLN131.Api.Data;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AdminUsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<object[]>> List([FromQuery] string? q, CancellationToken ct)
    {
        q = q?.Trim();

        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(q)) ||
                (u.UserName != null && u.UserName.Contains(q)) ||
                (u.FullName != null && u.FullName.Contains(q)));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var result = new List<object>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new
            {
                id = u.Id,
                email = u.Email,
                fullName = u.FullName,
                age = u.Age,
                phoneNumber = u.PhoneNumber,
                avatarUrl = u.AvatarUrl,
                isDisabled = u.IsDisabled,
                createdAt = u.CreatedAt,
                roles
            });
        }

        return Ok(result.ToArray());
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AdminUpdateUserRequest req)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (req.FullName is not null) user.FullName = req.FullName;
        if (req.Age is not null) user.Age = req.Age;
        if (req.PhoneNumber is not null) user.PhoneNumber = req.PhoneNumber;
        if (req.Email is not null)
        {
            var email = req.Email.Trim();
            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = _userManager.NormalizeEmail(email);
            user.NormalizedUserName = _userManager.NormalizeName(email);
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Failed to update user.",
                errors = result.Errors.Select(e => e.Description).ToArray()
            });
        }

        return Ok();
    }

    [HttpPost("{id:guid}/disabled")]
    public async Task<IActionResult> SetDisabled(Guid id, SetUserDisabledRequest req)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsDisabled = req.Disabled;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
        return Ok();
    }

    [HttpPost("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, SetUserRoleRequest req)
    {
        var role = (req.Role ?? "").Trim().ToLowerInvariant();
        if (!Roles.All.Contains(role))
        {
            return BadRequest(new { message = "Invalid role. Allowed: admin,user,viewer." });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await _userManager.AddToRoleAsync(user, role);
        return Ok();
    }
}

