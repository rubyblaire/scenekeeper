using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using SceneKeeper.Services;

namespace SceneKeeper.Windows;

public sealed class MainWindow : Window
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
    private readonly PartnerTrackingService partnerTrackingService;
    private readonly Action saveConfig;
    private string partnerNameInput = string.Empty;

    public MainWindow(Configuration configuration, SceneService sceneService, PartnerTrackingService partnerTrackingService, Action saveConfig)
        : base("SceneKeeper##SceneKeeperMain", ImGuiWindowFlags.NoScrollbar)
    {
        this.configuration = configuration;
        this.SceneService = sceneService;
        this.partnerTrackingService = partnerTrackingService;
        this.saveConfig = saveConfig;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(650, 540), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
    }

    public SceneService SceneService { get; }

    public override void Draw()
    {
        ImGui.TextUnformatted("SceneKeeper");
        ImGui.SameLine();
        ImGui.TextDisabled(this.configuration.IsTrackingPaused ? "Paused" : "Tracking");
        ImGui.Separator();

        if (ImGui.BeginTabBar("SceneKeeperTabs"))
        {
            if (ImGui.BeginTabItem("Scene")) { this.DrawSceneTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Partners")) { this.DrawPartnersTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Scene Log")) { this.DrawSceneLogTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Settings")) { this.DrawSettingsTab(); ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }
    }

    private void DrawSceneTab()
    {
        var sceneName = this.configuration.CurrentSceneName;
        if (ImGui.InputText("Scene Name", ref sceneName, 128))
            this.SceneService.SetSceneName(sceneName);

        var notes = this.configuration.SceneNotes;
        ImGui.TextUnformatted("Scene Notes");
        if (ImGui.InputTextMultiline("##SceneNotes", ref notes, 12000, new Vector2(-1, 150)))
            this.SceneService.SetSceneNotes(notes);

        if (ImGui.Button(this.configuration.IsTrackingPaused ? "Resume Tracking" : "Pause Tracking"))
        {
            this.configuration.IsTrackingPaused = !this.configuration.IsTrackingPaused;
            this.saveConfig();
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Scene Log"))
            this.SceneService.ClearMessages();

        ImGui.TextWrapped("SceneKeeper captures only the chat types you enable in Settings. It helps keep scenes organized without sending messages or taking actions for you.");
    }

    private void DrawPartnersTab()
    {
        ImGui.InputText("Partner Name", ref this.partnerNameInput, 128);
        ImGui.SameLine();
        if (ImGui.Button("Add Partner") && this.SceneService.AddPartner(this.partnerNameInput))
            this.partnerNameInput = string.Empty;

        var nearby = this.partnerTrackingService.GetNearbyTrackedPlayers().Select(p => p.Name.TextValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ImGui.BeginTable("PartnersTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Alias");
            ImGui.TableSetupColumn("Nearby");
            ImGui.TableSetupColumn("Actions");
            ImGui.TableHeadersRow();

            for (var i = this.configuration.Partners.Count - 1; i >= 0; i--)
            {
                var partner = this.configuration.Partners[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted(partner.Name);
                ImGui.TableSetColumnIndex(1);
                var alias = partner.Alias;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText($"##alias{i}", ref alias, 128)) { partner.Alias = alias; this.saveConfig(); }
                ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(nearby.Contains(partner.Name) ? "Yes" : "No");
                ImGui.TableSetColumnIndex(3);
                if (ImGui.SmallButton($"Remove##partner{i}")) { this.configuration.Partners.RemoveAt(i); this.saveConfig(); }
            }

            ImGui.EndTable();
        }
    }

    private void DrawSceneLogTab()
    {
        if (ImGui.Button("Select All")) this.SceneService.SelectAllMessages(true);
        ImGui.SameLine(); if (ImGui.Button("Select None")) this.SceneService.SelectAllMessages(false);
        ImGui.SameLine(); if (ImGui.Button("Copy Selected")) ImGui.SetClipboardText(this.SceneService.BuildSelectedContext(true));
        ImGui.SameLine(); if (ImGui.Button("Clear Log")) this.SceneService.ClearMessages();
        ImGui.TextDisabled($"{this.SceneService.Messages.Count} captured lines. Use selected lines for personal notes, recaps, or summaries.");

        if (ImGui.BeginChild("SceneLogChild", new Vector2(0, -1), true))
        {
            if (ImGui.BeginTable("SceneLogTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Use", ImGuiTableColumnFlags.WidthFixed, 38);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthFixed, 95);
                ImGui.TableSetupColumn("Sender", ImGuiTableColumnFlags.WidthFixed, 140);
                ImGui.TableSetupColumn("Message");
                ImGui.TableHeadersRow();

                foreach (var message in this.SceneService.Messages)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0); var selected = message.Selected; if (ImGui.Checkbox($"##selected{message.Id}", ref selected)) message.Selected = selected;
                    ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(message.LocalTime.ToString("HH:mm:ss"));
                    ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(message.ChatKind);
                    ImGui.TableSetColumnIndex(3); ImGui.TextUnformatted(message.FromTrackedPartner ? $"✦ {message.Sender}" : message.Sender);
                    ImGui.TableSetColumnIndex(4); ImGui.TextWrapped(message.Body);
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void DrawSettingsTab()
    {
        var enableChatCapture = this.configuration.EnableChatCapture;
        if (ImGui.Checkbox("Enable chat capture", ref enableChatCapture)) { this.configuration.EnableChatCapture = enableChatCapture; this.saveConfig(); }

        var enableOverlay = this.configuration.EnableOverlayMarkers;
        if (ImGui.Checkbox("Enable partner markers", ref enableOverlay)) { this.configuration.EnableOverlayMarkers = enableOverlay; this.saveConfig(); }

        var enableHighlight = this.configuration.EnablePartnerHighlight;
        if (ImGui.Checkbox("Highlight tracked nearby players", ref enableHighlight)) { this.configuration.EnablePartnerHighlight = enableHighlight; this.saveConfig(); }

        var offset = this.configuration.MarkerVerticalOffset;
        if (ImGui.SliderFloat("Marker vertical offset", ref offset, 0.5f, 4.0f)) { this.configuration.MarkerVerticalOffset = offset; this.saveConfig(); }

        var markerRadius = this.configuration.MarkerRadius;
        if (ImGui.SliderFloat("Marker size", ref markerRadius, 6.0f, 30.0f)) { this.configuration.MarkerRadius = markerRadius; this.saveConfig(); }

        var highlightRadius = this.configuration.HighlightRadius;
        if (ImGui.SliderFloat("Highlight size", ref highlightRadius, 18.0f, 90.0f)) { this.configuration.HighlightRadius = highlightRadius; this.saveConfig(); }

        var maxMessages = this.configuration.MaxSceneMessages;
        if (ImGui.SliderInt("Max captured lines", ref maxMessages, 25, 1000)) { this.configuration.MaxSceneMessages = maxMessages; this.saveConfig(); }

        ImGui.Separator();
        ImGui.TextUnformatted("Logged Chat Types");
        ImGui.TextWrapped("Toggle which visible chat types SceneKeeper should capture into the scene log.");

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
            if (ImGui.Checkbox($"{option.Label}##chatkind{option.Kind}", ref enabled))
            {
                this.SetChatKindEnabled(option.Kind, enabled);
                this.saveConfig();
            }

            if (i % 2 == 0)
                ImGui.SameLine(260);
        }

        ImGui.NewLine();
        ImGui.TextDisabled("If a chat type does not log as expected, its internal Dalamud name may differ. Send me the kind shown in the log and I can add it.");
    }

    private void SetChatKindEnabled(string kind, bool enabled)
    {
        this.configuration.CapturedChatKinds.RemoveAll(k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            this.configuration.CapturedChatKinds.Add(kind);
    }
}
