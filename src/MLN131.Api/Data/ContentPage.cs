namespace MLN131.Api.Data;

public sealed class ContentPage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string BodyMarkdown { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

