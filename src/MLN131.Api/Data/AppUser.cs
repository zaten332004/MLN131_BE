using Microsoft.AspNetCore.Identity;

namespace MLN131.Api.Data;

public sealed class AppUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsDisabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

