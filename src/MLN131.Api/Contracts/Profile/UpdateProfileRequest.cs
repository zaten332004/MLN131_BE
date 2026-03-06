using System.ComponentModel.DataAnnotations;

namespace MLN131.Api.Contracts.Profile;

public sealed class UpdateProfileRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? FullName { get; set; }

    [Range(0, 120)]
    public int? Age { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
}

