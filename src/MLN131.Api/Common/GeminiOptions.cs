namespace MLN131.Api.Common;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; init; } = "";
    public string Model { get; init; } = "gemini-2.0-flash";
    public bool FaqOnly { get; init; } = false;
    public double Temperature { get; init; } = 0.35;
    public int MaxOutputTokens { get; init; } = 1536;
}

