using SceneKeeper.Models;

namespace SceneKeeper.Services;

public sealed class AidenAssistService : IDisposable
{
    private readonly Configuration configuration;
    private readonly OpenAiDraftService openAiDraftService = new();

    public AidenAssistService(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(this.configuration.OpenAiApiKeyProtectedBase64);

    public async Task<DraftResult> DraftAsync(string sceneContext, string intent, string tone, CancellationToken cancellationToken = default)
    {
        if (!this.configuration.AidenAssistEnabled)
            return new DraftResult { Success = false, Error = "Aiden Assist is disabled." };

        var apiKey = LocalSecretService.UnprotectString(this.configuration.OpenAiApiKeyProtectedBase64);
        if (string.IsNullOrWhiteSpace(apiKey))
            return new DraftResult { Success = false, Error = "OpenAI API key is missing or could not be decrypted." };

        try
        {
            var request = new DraftRequest
            {
                Model = this.configuration.OpenAiModel,
                CharacterContext = this.configuration.CharacterContext,
                SceneContext = sceneContext,
                Intent = intent,
                Tone = tone
            };
            var text = await this.openAiDraftService.DraftRpResponseAsync(apiKey, request, cancellationToken).ConfigureAwait(false);
            return new DraftResult { Success = true, Text = text };
        }
        catch (Exception ex)
        {
            return new DraftResult { Success = false, Error = ex.Message };
        }
    }

    public void Dispose() => this.openAiDraftService.Dispose();
}
