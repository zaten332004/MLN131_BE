namespace MLN131.Api.Contracts.Responses;

public sealed class SubmitResponseRequest
{
    public string QuestionKey { get; set; } = "";
    public string AnswerText { get; set; } = "";
}

