namespace MLN131.Api.Contracts.Admin;

public sealed class AdminUpdateUserRequest
{
    public string? FullName { get; set; }
    public int? Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

