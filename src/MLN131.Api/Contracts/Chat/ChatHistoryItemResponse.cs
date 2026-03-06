namespace MLN131.Api.Contracts.Chat;

public sealed class ChatHistoryItemResponse
{
    public Guid Id { get; set; }
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
