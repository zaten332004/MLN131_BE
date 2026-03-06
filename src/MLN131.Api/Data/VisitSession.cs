namespace MLN131.Api.Data;

public sealed class VisitSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string VisitorId { get; set; } = "";
    public Guid? UserId { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? PathFirst { get; set; }
}

