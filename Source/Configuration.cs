using Dalamud.Configuration;
using SceneKeeper.Models;

namespace SceneKeeper;

[Serializable]
public enum CapturedChatFilterMode
{
    All,
    PartnersOnly,
    PartnersAndMe,
    PinnedOnly
}

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 14;

    public string CurrentSceneName { get; set; } = "Untitled Scene";
    public string SceneNotes { get; set; } = string.Empty;
    public string CurrentSceneTags { get; set; } = string.Empty;
    public bool IsTrackingPaused { get; set; }
    public bool EnableChatCapture { get; set; } = true;
    public bool EnableOverlayMarkers { get; set; } = true;
    public bool EnablePartnerHighlight { get; set; } = true;
    public bool EnableTargetContextMenu { get; set; } = true;
    public float MarkerVerticalOffset { get; set; } = 2.15f;
    public float MarkerRadius { get; set; } = 11.0f;
    public float HighlightRadius { get; set; } = 38.0f;
    public float HighlightColorRed { get; set; } = 0.78f;
    public float HighlightColorGreen { get; set; } = 0.64f;
    public float HighlightColorBlue { get; set; } = 0.40f;
    public int MaxSceneMessages { get; set; } = 250;
    public int MaxSceneHistoryEntries { get; set; } = 30;

    public List<string> SceneBuilderParagraphs { get; set; } = new() { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
    public string SceneBuilderChannel { get; set; } = "/emote";
    public float SceneBuilderPostDelaySeconds { get; set; } = 2.0f;
    public int SceneBuilderCharacterLimit { get; set; } = 350; // Legacy setting retained for older configs; Scene Builder now uses a hard 350-character limit.

    public CapturedChatFilterMode CapturedChatFilterMode { get; set; } = CapturedChatFilterMode.All;


    public bool EnableSpellCheck { get; set; } = true;
    public bool SpellCheckSkipCapitalizedWords { get; set; } = true;
    public List<string> CustomDictionaryWords { get; set; } = new();
    public List<string> IgnoredSpellCheckWords { get; set; } = new();

    public bool EnableTaskSharingTools { get; set; } = false;
    public string TaskSyncSceneCode { get; set; } = string.Empty;
    public string TaskSyncDisplayName { get; set; } = string.Empty;
    public bool RequireMatchingSceneSyncCode { get; set; } = true;
    public bool IncludeOwnMessagesInPartnerFilter { get; set; } = true;

    public List<string> CapturedChatKinds { get; set; } = new()
    {
        "Say", "Yell", "Shout", "Emote", "CustomEmote"
    };

    public List<ScenePartner> Partners { get; set; } = new();
    public List<SceneTask> FollowUpTasks { get; set; } = new();
    public List<SceneHistoryEntry> SceneHistory { get; set; } = new();
}
