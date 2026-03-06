namespace MLN131.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    public string? FullName { get; set; }
    public int? Age { get; set; }
    public string? PhoneNumber { get; set; }
}

