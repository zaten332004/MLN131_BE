using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MLN131.Api.Contracts.Profile;
using MLN131.Api.Data;
using MLN131.Api.Services;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly UserProfileService _userProfileService;
    private readonly IWebHostEnvironment _env;

    public ProfileController(
        UserManager<AppUser> userManager,
        UserProfileService userProfileService,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _userProfileService = userProfileService;
        _env = env;
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> Update(UpdateProfileRequest req)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized(new { message = "Invalid or expired authentication token." });
        if (user.IsDisabled)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Your account has been disabled."
            });
        }

        if (req.Email is not null)
        {
            var email = req.Email.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email cannot be empty." });
            }

            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = _userManager.NormalizeEmail(email);
            user.NormalizedUserName = _userManager.NormalizeName(email);
        }

        if (req.FullName is not null)
        {
            var fullName = req.FullName.Trim();
            user.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName;
        }

        if (req.Age is not null) user.Age = req.Age;

        if (req.PhoneNumber is not null)
        {
            var phone = req.PhoneNumber.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Failed to update profile.",
                errors = result.Errors.Select(e => e.Description).ToArray()
            });
        }

        var profile = await _userProfileService.BuildAsync(user);
        return Ok(profile);
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<UserProfileResponse>> UploadAvatar(IFormFile file)
    {
        if (file.Length == 0) return BadRequest(new { message = "Empty file." });
        if (file.Length > 5_000_000) return BadRequest(new { message = "File too large." });

        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized(new { message = "Invalid or expired authentication token." });
        if (user.IsDisabled)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Your account has been disabled."
            });
        }

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "avatars");
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";

        var safeName = $"{user.Id:N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        var path = Path.Combine(uploadsRoot, safeName);

        await using (var fs = System.IO.File.Create(path))
        {
            await file.CopyToAsync(fs);
        }

        user.AvatarUrl = $"/uploads/avatars/{safeName}";
        user.UpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Failed to update avatar.",
                errors = updateResult.Errors.Select(e => e.Description).ToArray()
            });
        }

        var profile = await _userProfileService.BuildAsync(user);
        return Ok(profile);
    }

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(userId);
    }
}

