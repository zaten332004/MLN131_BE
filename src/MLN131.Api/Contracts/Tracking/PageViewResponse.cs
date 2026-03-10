namespace MLN131.Api.Contracts.Tracking;

public sealed class PageViewResponse
{
    public DateTimeOffset AsOf { get; set; }
    public string Path { get; set; } = "/";
    public int Online { get; set; }
}

