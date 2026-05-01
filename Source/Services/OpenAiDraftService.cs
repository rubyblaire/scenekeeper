using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SceneKeeper.Models;

namespace SceneKeeper.Services;

public sealed class OpenAiDraftService : IDisposable
{
    private readonly HttpClient httpClient = new();

    public async Task<string> DraftRpResponseAsync(string apiKey, DraftRequest draftRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is missing.");

        var model = string.IsNullOrWhiteSpace(draftRequest.Model) ? "gpt-5.1" : draftRequest.Model.Trim();

        const string systemPrompt = """
You are Aiden Assist, a private roleplay writing companion inside SceneKeeper.

You help draft SFW in-character roleplay responses.
You do not write explicit sexual content.
You preserve the user's character voice and intent.
You do not control other characters, force outcomes, or assume consent.
You write copy-ready prose suitable for FFXIV roleplay chat.
Avoid modern slang unless the scene calls for it.
Keep the response immersive, elegant, emotionally intelligent, and concise enough to paste.
""";

        var userPrompt = $"""
Character Context:
{draftRequest.CharacterContext}

Scene Context:
{draftRequest.SceneContext}

Tone:
{draftRequest.Tone}

User Intent:
{draftRequest.Intent}

Draft one in-character RP response.
Return only the draft text.
""";

        var requestBody = new
        {
            model,
            store = false,
            input = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_output_tokens = 650
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await this.httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI request failed: {(int)response.StatusCode} {response.ReasonPhrase}\n{json}");

        return ExtractOutputText(json);
    }

    private static string ExtractOutputText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
            return outputText.GetString() ?? string.Empty;

        if (!root.TryGetProperty("output", out var outputArray) || outputArray.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var outputItem in outputArray.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var contentItem in contentArray.EnumerateArray())
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    builder.AppendLine(text.GetString());
        }
        return builder.ToString().Trim();
    }

    public void Dispose() => this.httpClient.Dispose();
}
