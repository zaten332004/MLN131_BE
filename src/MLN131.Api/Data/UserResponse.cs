namespace MLN131.Api.Data;

public sealed class UserResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string QuestionKey { get; set; } = "";
    public string AnswerText { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

