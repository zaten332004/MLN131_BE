using Microsoft.AspNetCore.Identity;
using MLN131.Api.Contracts.Profile;
using MLN131.Api.Data;

namespace MLN131.Api.Services;

public sealed class UserProfileService
{
    private readonly UserManager<AppUser> _userManager;

    public UserProfileService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserProfileResponse> BuildAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Age = user.Age,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            IsDisabled = user.IsDisabled,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roles.ToArray()
        };
    }
}
