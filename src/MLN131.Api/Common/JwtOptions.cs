namespace MLN131.Api.Common;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "MLN131";
    public string Audience { get; init; } = "MLN131";
    public string SigningKey { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 60;
}

