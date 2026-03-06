namespace MLN131.Api.Contracts.Profile;

public sealed class UserProfileResponse
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsDisabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}
