using Dalamud.Configuration;
using SceneKeeper.Models;

namespace SceneKeeper;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string CurrentSceneName { get; set; } = "Untitled Scene";
    public string SceneNotes { get; set; } = string.Empty;
    public bool IsTrackingPaused { get; set; }
    public bool EnableChatCapture { get; set; } = true;
    public bool EnableOverlayMarkers { get; set; } = true;
    public float MarkerVerticalOffset { get; set; } = 2.15f;
    public int MaxSceneMessages { get; set; } = 250;

    public List<string> CapturedChatKinds { get; set; } = new()
    {
        "Say", "Yell", "Shout", "Emote", "CustomEmote", "Party", "TellIncoming", "TellOutgoing"
    };

    public List<ScenePartner> Partners { get; set; } = new();

    public string OwnerPasswordSaltBase64 { get; set; } = string.Empty;
    public string OwnerPasswordHashBase64 { get; set; } = string.Empty;

    public bool AidenAssistEnabled { get; set; }
    public string OpenAiApiKeyProtectedBase64 { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-5.1";
    public string CharacterContext { get; set; } = """
Aiden Wolf is an immortal vampyr with a restrained, elegant presence. He is controlled, observant, protective, and emotionally intelligent. His voice is polished, intimate when appropriate, dangerous only under pressure, and never cartoonishly dramatic.
""";

    public bool SendOnlySelectedLines { get; set; } = true;
    public bool IncludePlayerNamesInPrompt { get; set; }
    public bool SaveGeneratedDrafts { get; set; }
}
