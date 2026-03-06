namespace MLN131.Api.Data;

public sealed class PageViewEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VisitSessionId { get; set; }
    public VisitSession? VisitSession { get; set; }

    public Guid? UserId { get; set; }

    public string Path { get; set; } = "";
    public string? Referrer { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

