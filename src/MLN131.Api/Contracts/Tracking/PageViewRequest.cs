namespace MLN131.Api.Contracts.Tracking;

public sealed class PageViewRequest
{
    public string Path { get; set; } = "/";
    public string? Referrer { get; set; }
}

