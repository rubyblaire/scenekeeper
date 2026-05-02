using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace SceneKeeper.Windows;

public sealed class SettingsWindow : Window
{
    private static readonly (string Kind, string Label)[] ChatKindOptions =
    {
        ("Say", "Say"),
        ("Emote", "Emote"),
        ("CustomEmote", "Custom Emote"),
        ("Yell", "Yell"),
        ("Shout", "Shout"),
        ("Party", "Party"),
        ("Alliance", "Alliance"),
        ("FreeCompany", "Free Company"),
        ("TellIncoming", "Incoming Tells"),
        ("TellOutgoing", "Outgoing Tells"),
        ("Linkshell1", "Linkshell 1"),
        ("Linkshell2", "Linkshell 2"),
        ("Linkshell3", "Linkshell 3"),
        ("Linkshell4", "Linkshell 4"),
        ("Linkshell5", "Linkshell 5"),
        ("Linkshell6", "Linkshell 6"),
        ("Linkshell7", "Linkshell 7"),
        ("Linkshell8", "Linkshell 8"),
        ("CrossLinkshell1", "CWLS 1"),
        ("CrossLinkshell2", "CWLS 2"),
        ("CrossLinkshell3", "CWLS 3"),
        ("CrossLinkshell4", "CWLS 4"),
        ("CrossLinkshell5", "CWLS 5"),
        ("CrossLinkshell6", "CWLS 6"),
        ("CrossLinkshell7", "CWLS 7"),
        ("CrossLinkshell8", "CWLS 8"),
    };

    private readonly Configuration configuration;
    private readonly Action saveConfig;

    public SettingsWindow(Configuration configuration, Action saveConfig)
        : base("SceneKeeper Settings##SceneKeeperSettings")
    {
        this.configuration = configuration;
        this.saveConfig = saveConfig;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(540, 460), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("SceneKeeper Settings");
        ImGui.Separator();

        var enableChatCapture = this.configuration.EnableChatCapture;
        if (ImGui.Checkbox("Enable chat capture", ref enableChatCapture)) { this.configuration.EnableChatCapture = enableChatCapture; this.saveConfig(); }

        var enableOverlay = this.configuration.EnableOverlayMarkers;
        if (ImGui.Checkbox("Enable partner markers", ref enableOverlay)) { this.configuration.EnableOverlayMarkers = enableOverlay; this.saveConfig(); }

        var enableHighlight = this.configuration.EnablePartnerHighlight;
        if (ImGui.Checkbox("Highlight tracked nearby players", ref enableHighlight)) { this.configuration.EnablePartnerHighlight = enableHighlight; this.saveConfig(); }

        var markerRadius = this.configuration.MarkerRadius;
        if (ImGui.SliderFloat("Marker size", ref markerRadius, 6.0f, 30.0f)) { this.configuration.MarkerRadius = markerRadius; this.saveConfig(); }

        var highlightRadius = this.configuration.HighlightRadius;
        if (ImGui.SliderFloat("Highlight size", ref highlightRadius, 18.0f, 90.0f)) { this.configuration.HighlightRadius = highlightRadius; this.saveConfig(); }

        var highlightColor = new Vector3(
            this.configuration.HighlightColorRed,
            this.configuration.HighlightColorGreen,
            this.configuration.HighlightColorBlue);
        if (ImGui.ColorEdit3("Marker/highlight color", ref highlightColor))
        {
            this.configuration.HighlightColorRed = highlightColor.X;
            this.configuration.HighlightColorGreen = highlightColor.Y;
            this.configuration.HighlightColorBlue = highlightColor.Z;
            this.saveConfig();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Reset Color"))
        {
            this.configuration.HighlightColorRed = 0.78f;
            this.configuration.HighlightColorGreen = 0.64f;
            this.configuration.HighlightColorBlue = 0.40f;
            this.saveConfig();
        }

        var offset = this.configuration.MarkerVerticalOffset;
        if (ImGui.SliderFloat("Marker vertical offset", ref offset, 0.5f, 4.0f)) { this.configuration.MarkerVerticalOffset = offset; this.saveConfig(); }

        var maxMessages = this.configuration.MaxSceneMessages;
        if (ImGui.SliderInt("Max captured lines", ref maxMessages, 25, 1000)) { this.configuration.MaxSceneMessages = maxMessages; this.saveConfig(); }

        var maxHistory = this.configuration.MaxSceneHistoryEntries;
        if (ImGui.SliderInt("Max saved scenes", ref maxHistory, 1, 100)) { this.configuration.MaxSceneHistoryEntries = maxHistory; this.saveConfig(); }

        ImGui.Separator();
        ImGui.TextUnformatted("Logged Chat Types");

        if (ImGui.Button("RP Defaults"))
        {
            this.configuration.CapturedChatKinds = new List<string> { "Say", "Emote", "CustomEmote", "Yell", "Shout" };
            this.saveConfig();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear All"))
        {
            this.configuration.CapturedChatKinds.Clear();
            this.saveConfig();
        }

        for (var i = 0; i < ChatKindOptions.Length; i++)
        {
            var option = ChatKindOptions[i];
            var enabled = this.configuration.CapturedChatKinds.Any(k => string.Equals(k, option.Kind, StringComparison.OrdinalIgnoreCase));
            if (ImGui.Checkbox($"{option.Label}##settingschatkind{option.Kind}", ref enabled))
            {
                this.SetChatKindEnabled(option.Kind, enabled);
                this.saveConfig();
            }

            if (i % 2 == 0)
                ImGui.SameLine(260);
        }
    }

    private void SetChatKindEnabled(string kind, bool enabled)
    {
        this.configuration.CapturedChatKinds.RemoveAll(k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            this.configuration.CapturedChatKinds.Add(kind);
    }
}
