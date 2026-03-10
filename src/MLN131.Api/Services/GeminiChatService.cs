using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MLN131.Api.Common;

namespace MLN131.Api.Services;

public sealed class GeminiChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiOptions _opt;
    private readonly IHostEnvironment _env;
    private string? _faqCache;

    public GeminiChatService(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> opt, IHostEnvironment env)
    {
        _httpClientFactory = httpClientFactory;
        _opt = opt.Value;
        _env = env;
    }

    public async Task<string> GenerateAsync(string userMessage, IEnumerable<(string Role, string Content)> history, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
        {
            throw new InvalidOperationException("Missing config: Gemini:ApiKey");
        }

        var faqs = await GetFaqsAsync(ct);
        var systemText = BuildSystemText(faqs);

        var contents = new List<object>();

        foreach (var (role, content) in history)
        {
            var r = role is "assistant" or "model" ? "model" : "user";
            contents.Add(new { role = r, parts = new[] { new { text = content } } });
        }

        contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemText } } },
            contents,
            generationConfig = new
            {
                temperature = GetTemperature(),
                maxOutputTokens = GetMaxOutputTokens()
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_opt.Model}:generateContent?key={_opt.ApiKey}";
        var client = _httpClientFactory.CreateClient();
        using var resp = await client.PostAsJsonAsync(url, payload, cancellationToken: ct);

        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            var providerMessage = TryExtractProviderErrorMessage(json);
            var suffix = string.IsNullOrWhiteSpace(providerMessage) ? "" : $" - {providerMessage}";
            throw new InvalidOperationException($"Gemini error: {(int)resp.StatusCode} {resp.ReasonPhrase}{suffix}");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorEl))
        {
            var msg = errorEl.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
            throw new InvalidOperationException($"Gemini error: {msg ?? "Unknown provider error."}");
        }

        if (!root.TryGetProperty("candidates", out var candidatesEl) ||
            candidatesEl.ValueKind != JsonValueKind.Array ||
            candidatesEl.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Unexpected Gemini response format: missing candidates.");
        }

        var candidate0 = candidatesEl[0];
        if (!candidate0.TryGetProperty("content", out var contentEl))
        {
            throw new InvalidOperationException("Unexpected Gemini response format: missing content.");
        }

        if (!contentEl.TryGetProperty("parts", out var partsEl) ||
            partsEl.ValueKind != JsonValueKind.Array ||
            partsEl.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Unexpected Gemini response format: missing parts.");
        }

        var part0 = partsEl[0];
        var text = part0.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
        return text?.Trim() ?? "";
    }

    private async Task<string> GetFaqsAsync(CancellationToken ct)
    {
        if (_faqCache is not null) return _faqCache;
        var path = Path.Combine(_env.ContentRootPath, "Resources", "faqs_vi.txt");
        _faqCache = File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : "";
        return _faqCache;
    }

    private string BuildSystemText(string faqs)
    {
        var faqText = string.IsNullOrWhiteSpace(faqs)
            ? "(No internal FAQ content available.)"
            : faqs;

        if (_opt.FaqOnly)
        {
            return """
                   You are an MLN131 learning assistant.
                   Answer only from internal FAQ/content below.
                   If the question is outside this content, reply: "Minh chua co du lieu trong trang web de tra loi cau nay."
                   Keep answers concise, clear, and in the same language as the user.

                   Internal FAQ/content:
                   """ + "\n" + faqText;
        }

        return """
               You are an MLN131 learning assistant.
               Prioritize internal FAQ/content below whenever relevant.
               If FAQ is not enough, you are allowed to answer using reliable general knowledge.
               For parts that are not from FAQ, clearly mark them as "(Kien thuc chung)".
               Keep answers concise, clear, and in the same language as the user.

               Internal FAQ/content:
               """ + "\n" + faqText;
    }

    private double GetTemperature()
    {
        return Math.Clamp(_opt.Temperature, 0.0, 1.0);
    }

    private int GetMaxOutputTokens()
    {
        return Math.Clamp(_opt.MaxOutputTokens, 256, 4096);
    }

    private static string? TryExtractProviderErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errorEl) &&
                errorEl.TryGetProperty("message", out var msgEl))
            {
                var msg = msgEl.GetString();
                if (string.IsNullOrWhiteSpace(msg)) return null;
                return msg.Length <= 300 ? msg : msg[..300];
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
