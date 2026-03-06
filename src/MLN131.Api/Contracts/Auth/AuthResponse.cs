using MLN131.Api.Contracts.Profile;

namespace MLN131.Api.Contracts.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public UserProfileResponse? User { get; set; }
}

