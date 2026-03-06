namespace MLN131.Api.Contracts.Chat;

public sealed class ChatHistoryResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public ChatHistoryItemResponse[] Items { get; set; } = Array.Empty<ChatHistoryItemResponse>();
}
