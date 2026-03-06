namespace MLN131.Api.Data;

public sealed class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>
    /// "user" | "assistant" | "system"
    /// </summary>
    public string Role { get; set; } = "user";

    public string Content { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

