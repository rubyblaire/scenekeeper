using Dalamud.Configuration;
using SceneKeeper.Models;

namespace SceneKeeper;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;

    public string CurrentSceneName { get; set; } = "Untitled Scene";
    public string SceneNotes { get; set; } = string.Empty;
    public bool IsTrackingPaused { get; set; }
    public bool EnableChatCapture { get; set; } = true;
    public bool EnableOverlayMarkers { get; set; } = true;
    public bool EnablePartnerHighlight { get; set; } = true;
    public float MarkerVerticalOffset { get; set; } = 2.15f;
    public float MarkerRadius { get; set; } = 11.0f;
    public float HighlightRadius { get; set; } = 38.0f;
    public int MaxSceneMessages { get; set; } = 250;

    public List<string> CapturedChatKinds { get; set; } = new()
    {
        "Say", "Yell", "Shout", "Emote", "CustomEmote"
    };

    public List<ScenePartner> Partners { get; set; } = new();
}
