using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace SceneKeeper.Windows;

public sealed class SettingsWindow : Window
{
    private const string DiscordInviteUrl = "https://discord.gg/Dr836dmbqh";
    private const string KofiUrl = "https://ko-fi.com/rubyblaire";

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
        this.PushSceneKeeperStyle();
        try
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

        DrawSupportFooter();
        }
        finally
        {
            this.PopSceneKeeperStyle();
        }
    }

    private static void DrawSupportFooter()
    {
        var remaining = ImGui.GetContentRegionAvail().Y;
        if (remaining > 88.0f)
            ImGui.Dummy(new Vector2(1.0f, remaining - 88.0f));

        ImGui.Separator();
        ImGui.TextUnformatted("Support & Bug Reports");
        ImGui.TextWrapped("Need help, found a bug, or want to support SceneKeeper? These links open in your browser.");

        if (ImGui.Button("Report a Bug / Join Discord", new Vector2(210.0f, 0.0f)))
            OpenExternalUrl(DiscordInviteUrl);

        ImGui.SameLine();

        if (ImGui.Button("Support on Ko-fi", new Vector2(160.0f, 0.0f)))
            OpenExternalUrl(KofiUrl);
    }

    private void PushSceneKeeperStyle()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 9.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 7.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 7.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 9.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(9.0f, 5.0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, 7.0f));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.045f, 0.038f, 0.055f, 0.96f));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.070f, 0.060f, 0.082f, 0.90f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.78f, 0.64f, 0.40f, 0.22f));
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.105f, 0.084f, 0.115f, 0.96f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.18f, 0.12f, 0.16f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.24f, 0.15f, 0.20f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.33f, 0.20f, 0.27f, 0.95f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.46f, 0.27f, 0.36f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.58f, 0.34f, 0.44f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.31f, 0.19f, 0.26f, 0.72f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.45f, 0.27f, 0.36f, 0.86f));
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.58f, 0.35f, 0.45f, 0.96f));
        ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.78f, 0.64f, 0.40f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.78f, 0.64f, 0.40f, 0.95f));
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.95f, 0.79f, 0.48f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.91f, 0.86f, 0.80f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.62f, 0.56f, 0.60f, 1.0f));
    }

    private void PopSceneKeeperStyle()
    {
        ImGui.PopStyleColor(17);
        ImGui.PopStyleVar(7);
    }

    private static void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception)
        {
            ImGui.SetClipboardText(url);
        }
    }

    private void SetChatKindEnabled(string kind, bool enabled)
    {
        this.configuration.CapturedChatKinds.RemoveAll(k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            this.configuration.CapturedChatKinds.Add(kind);
    }
}
