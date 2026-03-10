namespace MLN131.Api.Contracts.Tracking;

public sealed class PageViewRequest
{
    public string Path { get; set; } = "/";
    public string? Referrer { get; set; }

    // FE can send Active=false on tab close / route leave to decrement online counters immediately.
    public bool Active { get; set; } = true;
}

