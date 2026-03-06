using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MLN131.Api.Common;
using MLN131.Api.Contracts.Auth;
using MLN131.Api.Contracts.Profile;
using MLN131.Api.Data;
using MLN131.Api.Services;

namespace MLN131.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly UserProfileService _userProfileService;
    private readonly TimeProvider _time;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenService jwtTokenService,
        UserProfileService userProfileService,
        TimeProvider time,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _userProfileService = userProfileService;
        _time = time;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = req.Email.Trim(),
            UserName = req.Email.Trim(),
            FullName = req.FullName,
            Age = req.Age,
            PhoneNumber = req.PhoneNumber,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = result.Errors.Select(e => e.Description).ToArray()
            });
        }

        await _userManager.AddToRoleAsync(user, Roles.User);

        var token = await _jwtTokenService.CreateAccessTokenAsync(user, ct);
        var profile = await _userProfileService.BuildAsync(user);
        var jwt = _configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        return Ok(new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = _time.GetUtcNow().Add(TimeSpan.FromMinutes(jwt.AccessTokenMinutes)),
            User = profile,
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var identifier = req.Email.Trim();

        AppUser? user;
        if (identifier.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(identifier);
        }
        else
        {
            user = await _userManager.FindByNameAsync(identifier);
        }

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        if (user.IsDisabled)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Your account has been disabled."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email/username or password." });
        }

        var token = await _jwtTokenService.CreateAccessTokenAsync(user, ct);
        var profile = await _userProfileService.BuildAsync(user);
        var jwt = _configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        return Ok(new AuthResponse
        {
            AccessToken = token,
            ExpiresAt = _time.GetUtcNow().Add(TimeSpan.FromMinutes(jwt.AccessTokenMinutes)),
            User = profile,
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid or expired authentication token." });
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Unauthorized(new { message = "User account not found." });
        }

        var profile = await _userProfileService.BuildAsync(user);
        return Ok(profile);
    }
}

