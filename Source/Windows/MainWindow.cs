using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using SceneKeeper;
using SceneKeeper.Models;
using SceneKeeper.Services;

namespace SceneKeeper.Windows;

public sealed class MainWindow : Window
{
    private const int SceneBuilderHardCharacterLimit = 350;
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

    private static readonly (string Command, string Label)[] BuilderChannelOptions =
    {
        ("/emote", "Emote"),
        ("/say", "Say"),
        ("/yell", "Yell"),
        ("/shout", "Shout"),
        ("/party", "Party"),
        ("/echo", "Echo/Test"),
    };

    private readonly Configuration configuration;
    private readonly PartnerTrackingService partnerTrackingService;
    private readonly ICommandManager commandManager;
    private readonly IChatGui chatGui;
    private readonly ITargetManager targetManager;
    private readonly Action saveConfig;
    private readonly SpellCheckService spellCheckService = new();
    private readonly TaskShareService taskShareService = new();
    private string followUpInput = string.Empty;
    private string historySearchInput = string.Empty;
    private string capturedChatSearchInput = string.Empty;
    private string taskSyncImportText = string.Empty;
    private string customDictionaryInput = string.Empty;
    private string statusMessage = string.Empty;
    private List<string> pendingPostParagraphs = new();
    private string pendingPostChannel = "/emote";
    private readonly Queue<string> sceneBuilderPostQueue = new();
    private bool isSceneBuilderPosting;
    private int sceneBuilderPostTotal;
    private int sceneBuilderPostIndex;
    private DateTime nextSceneBuilderPostAt = DateTime.MinValue;

    public MainWindow(Configuration configuration, SceneService sceneService, PartnerTrackingService partnerTrackingService, ICommandManager commandManager, IChatGui chatGui, ITargetManager targetManager, Action saveConfig)
        : base("SceneKeeper##SceneKeeperMain", ImGuiWindowFlags.NoScrollbar)
    {
        this.configuration = configuration;
        this.SceneService = sceneService;
        this.partnerTrackingService = partnerTrackingService;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.targetManager = targetManager;
        this.saveConfig = saveConfig;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(780, 620), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
    }

    public SceneService SceneService { get; }

    public override void Draw()
    {
        this.ProcessSceneBuilderPostQueue();

        ImGui.TextUnformatted("SceneKeeper");
        ImGui.SameLine();
        ImGui.TextDisabled(this.configuration.IsTrackingPaused ? "Paused" : "Tracking");
        ImGui.Separator();

        if (!string.IsNullOrWhiteSpace(this.statusMessage))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.78f, 0.64f, 0.40f, 1.0f));
            ImGui.TextWrapped($"✓ {this.statusMessage}");
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }

        if (ImGui.BeginTabBar("SceneKeeperTabs"))
        {
            if (ImGui.BeginTabItem("Scene")) { this.DrawSceneTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Scene Builder")) { this.DrawSceneBuilderTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Captured Chat")) { this.DrawCapturedChatTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Partners")) { this.DrawPartnersTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("History")) { this.DrawHistoryTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Settings")) { this.DrawSettingsTab(); ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }
    }

    private void DrawSceneTab()
    {
        var sceneName = this.configuration.CurrentSceneName;
        if (ImGui.InputText("Scene Name", ref sceneName, 128))
            this.SceneService.SetSceneName(sceneName);

        var tags = this.configuration.CurrentSceneTags;
        if (ImGui.InputText("Scene Tags", ref tags, 512))
            this.SceneService.SetSceneTags(tags);
        ImGui.TextDisabled("Comma-separated tags work well, e.g. Storybrook, mystery, follow-up.");

        var notes = this.configuration.SceneNotes;
        ImGui.TextUnformatted("Scene Notes");
        if (ImGui.InputTextMultiline("##SceneNotes", ref notes, 12000, new Vector2(-1, 135)))
            this.SceneService.SetSceneNotes(notes);

        if (ImGui.Button(this.configuration.IsTrackingPaused ? "Resume Tracking" : "Pause Tracking"))
        {
            this.configuration.IsTrackingPaused = !this.configuration.IsTrackingPaused;
            this.saveConfig();
        }

        ImGui.SameLine();
        if (ImGui.Button("Save Scene to History"))
        {
            var entry = this.SceneService.SaveCurrentSceneToHistory();
            this.statusMessage = $"Saved '{entry.SceneName}' to history.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Start Scene"))
        {
            this.SceneService.StartScene();
            this.statusMessage = $"Started scene: {this.configuration.CurrentSceneName}";
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Scene"))
        {
            this.SceneService.ClearCurrentScene(clearPartners: true);
            this.statusMessage = "Cleared the current scene.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy Markdown"))
        {
            ImGui.SetClipboardText(this.SceneService.BuildCurrentMarkdown(selectedOnly: false));
            this.statusMessage = "Copied! Current scene Markdown copied to clipboard.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy Summary"))
        {
            ImGui.SetClipboardText(this.SceneService.BuildCurrentSummary());
            this.statusMessage = "Copied! Scene summary copied to clipboard.";
        }

        ImGui.Separator();
        this.DrawFollowUpsSection();

        ImGui.Separator();
        this.DrawPinnedLinesSection();
    }

    private void DrawFollowUpsSection()
    {
        ImGui.TextUnformatted("Follow-Up Tasks");
        ImGui.InputText("New Follow-Up", ref this.followUpInput, 256);
        ImGui.SameLine();
        if (ImGui.Button("Add Task") && this.SceneService.AddFollowUpTask(this.followUpInput))
            this.followUpInput = string.Empty;

        if (this.configuration.FollowUpTasks.Count == 0)
        {
            ImGui.TextDisabled("No follow-ups yet. Add reminders for next scene, journal updates, screenshots, or story hooks.");
            return;
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Completed"))
        {
            this.SceneService.ClearCompletedTasks();
            this.statusMessage = "Completed follow-ups cleared.";
        }

        if (ImGui.BeginChild("FollowUpsChild", new Vector2(0, 105), true))
        {
            for (var i = this.configuration.FollowUpTasks.Count - 1; i >= 0; i--)
            {
                var task = this.configuration.FollowUpTasks[i];
                var isDone = task.IsDone;
                if (ImGui.Checkbox($"##taskdone{task.Id}", ref isDone))
                {
                    task.IsDone = isDone;
                    task.UpdatedAtUtc = DateTime.UtcNow;
                    task.UpdatedBy = this.GetTaskSyncDisplayName();
                    this.saveConfig();
                }

                ImGui.SameLine();
                var text = task.Text;
                ImGui.SetNextItemWidth(-95);
                if (ImGui.InputText($"##tasktext{task.Id}", ref text, 256))
                {
                    task.Text = text;
                    task.UpdatedAtUtc = DateTime.UtcNow;
                    task.UpdatedBy = this.GetTaskSyncDisplayName();
                    this.saveConfig();
                }

                ImGui.SameLine();
                if (ImGui.SmallButton($"Remove##task{task.Id}"))
                {
                    this.SceneService.RemoveFollowUpTask(task.Id);
                    this.statusMessage = "Follow-up removed.";
                }
            }
        }
        ImGui.EndChild();

        this.DrawTaskSyncSection();
    }

    private void DrawTaskSyncSection()
    {
        ImGui.Separator();
        ImGui.TextUnformatted("Manual Task Sharing");
        ImGui.TextDisabled("Share follow-up tasks manually by copying a compact SceneKeeper task share string and sending it to another writer. They can import it to merge task updates.");

        var enabled = this.configuration.EnableTaskSharingTools;
        if (ImGui.Checkbox("Enable manual task sharing tools", ref enabled))
        {
            this.configuration.EnableTaskSharingTools = enabled;
            this.saveConfig();
        }

        if (!this.configuration.EnableTaskSharingTools)
            return;

        var sceneCode = this.configuration.TaskSyncSceneCode;
        ImGui.SetNextItemWidth(220);
        if (ImGui.InputText("Scene Share Code", ref sceneCode, 128))
        {
            this.configuration.TaskSyncSceneCode = sceneCode.Trim();
            this.saveConfig();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("Use the same code with your writing partner to prevent accidental imports.");

        var displayName = this.configuration.TaskSyncDisplayName;
        ImGui.SetNextItemWidth(220);
        if (ImGui.InputText("Your Share Name", ref displayName, 128))
        {
            this.configuration.TaskSyncDisplayName = displayName.Trim();
            this.saveConfig();
        }

        var requireCode = this.configuration.RequireMatchingSceneSyncCode;
        if (ImGui.Checkbox("Require matching scene code when importing", ref requireCode))
        {
            this.configuration.RequireMatchingSceneSyncCode = requireCode;
            this.saveConfig();
        }

        if (ImGui.Button("Copy Task Share String"))
        {
            ImGui.SetClipboardText(this.BuildCurrentTaskSyncPayload());
            this.statusMessage = "Copied! Task share string copied to clipboard.";
        }

        ImGui.TextUnformatted("Import Task Share String");
        ImGui.InputTextMultiline("##tasksyncimport", ref this.taskSyncImportText, 12000, new Vector2(-1, 78));
        if (ImGui.Button("Import Task Share String"))
        {
            if (this.taskShareService.TryImportPayload(
                    this.taskSyncImportText,
                    this.configuration.TaskSyncSceneCode,
                    this.configuration.RequireMatchingSceneSyncCode,
                    this.configuration.FollowUpTasks,
                    out var message))
            {
                this.SceneService.SaveAfterExternalTaskMerge();
                this.taskSyncImportText = string.Empty;
            }

            this.statusMessage = message;
        }
    }

    private string BuildCurrentTaskSyncPayload()
    {
        return this.taskShareService.BuildPayload(
            this.configuration.TaskSyncSceneCode,
            this.configuration.CurrentSceneName,
            this.GetTaskSyncDisplayName(),
            this.configuration.FollowUpTasks);
    }

    private string GetTaskSyncDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(this.configuration.TaskSyncDisplayName))
            return this.configuration.TaskSyncDisplayName.Trim();

        var local = this.partnerTrackingService.LocalPlayerName;
        return string.IsNullOrWhiteSpace(local) ? "SceneKeeper User" : local;
    }

    private void DrawPinnedLinesSection()
    {
        var pinned = this.SceneService.PinnedMessages.ToList();
        ImGui.TextUnformatted($"Pinned Lines ({pinned.Count})");
        ImGui.SameLine();
        if (ImGui.SmallButton("Copy Pinned"))
        {
            var text = pinned.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, pinned.Select(m => m.ToPromptLine(includeSender: true)));
            ImGui.SetClipboardText(text);
            this.statusMessage = pinned.Count == 0 ? "No pinned lines to copy." : "Copied! Pinned lines copied to clipboard.";
        }

        if (pinned.Count == 0)
        {
            ImGui.TextDisabled("Pin important captured lines from the Captured Chat tab so they stay easy to find and export with the scene.");
            return;
        }

        if (ImGui.BeginChild("PinnedLinesChild", new Vector2(0, 110), true))
        {
            foreach (var message in pinned)
            {
                ImGui.TextWrapped($"★ [{message.LocalTime:HH:mm:ss}] {message.Sender}: {message.Body}");
            }
        }
        ImGui.EndChild();
    }

    private void DrawPartnersTab()
    {
        ImGui.TextWrapped("Add partners from captured chat, by targeting a visible player, or from the in-game right-click context menu when available.");
        if (ImGui.Button("Add Current Target"))
            this.AddCurrentTargetAsPartner();

        ImGui.SameLine();
        if (ImGui.Button("Refresh Partner Indicators"))
        {
            this.SceneService.RefreshTrackedMessageFlags();
            this.statusMessage = "Partner indicators refreshed.";
        }

        var nearby = this.partnerTrackingService.GetNearbyTrackedPlayers().Select(p => p.Name.TextValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ImGui.BeginTable("PartnersTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 170);
            ImGui.TableSetupColumn("Relationship", ImGuiTableColumnFlags.WidthFixed, 130);
            ImGui.TableSetupColumn("Tags", ImGuiTableColumnFlags.WidthFixed, 135);
            ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 155);
            ImGui.TableSetupColumn("Notes");
            ImGui.TableSetupColumn("Nearby", ImGuiTableColumnFlags.WidthFixed, 65);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            for (var i = this.configuration.Partners.Count - 1; i >= 0; i--)
            {
                var partner = this.configuration.Partners[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted(partner.DisplayName);

                ImGui.TableSetColumnIndex(1);
                var relationship = partner.Relationship;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText($"##partnerrelation{i}", ref relationship, 128)) { partner.Relationship = relationship; this.saveConfig(); }

                ImGui.TableSetColumnIndex(2);
                var tags = partner.Tags;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText($"##partnertags{i}", ref tags, 128)) { partner.Tags = tags; this.saveConfig(); }

                ImGui.TableSetColumnIndex(3);
                var useCustomColor = partner.UseCustomColor;
                if (ImGui.Checkbox($"Use##partnercoloruse{i}", ref useCustomColor)) { partner.UseCustomColor = useCustomColor; this.saveConfig(); }
                ImGui.SameLine();
                var partnerColor = new Vector3(partner.ColorRed, partner.ColorGreen, partner.ColorBlue);
                if (ImGui.ColorEdit3($"##partnercolor{i}", ref partnerColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel))
                {
                    partner.ColorRed = partnerColor.X;
                    partner.ColorGreen = partnerColor.Y;
                    partner.ColorBlue = partnerColor.Z;
                    this.saveConfig();
                }

                ImGui.TableSetColumnIndex(4);
                var notes = partner.Notes;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText($"##partnernotes{i}", ref notes, 256)) { partner.Notes = notes; this.saveConfig(); }

                ImGui.TableSetColumnIndex(5); ImGui.TextUnformatted(this.partnerTrackingService.IsNearby(partner.DisplayName) ? "Yes" : "No");
                ImGui.TableSetColumnIndex(6);
                if (ImGui.SmallButton($"Remove##partner{i}")) { this.configuration.Partners.RemoveAt(i); this.saveConfig(); }
            }

            ImGui.EndTable();
        }
    }

    private void DrawCapturedChatTab()
    {
        this.DrawCapturedChatFilters();
        var visibleMessages = this.GetVisibleCapturedMessages().ToList();

        if (ImGui.Button("Select Visible"))
        {
            foreach (var message in visibleMessages)
                message.Selected = true;
            this.statusMessage = visibleMessages.Count == 0 ? "No visible captured lines to select." : $"Selected {visibleMessages.Count} visible captured line(s).";
        }
        ImGui.SameLine();
        if (ImGui.Button("Select None"))
        {
            this.SceneService.SelectAllMessages(false);
            this.statusMessage = "Captured chat selection cleared.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Copy Selected"))
        {
            ImGui.SetClipboardText(this.SceneService.BuildSelectedContext(true));
            this.statusMessage = "Copied! Selected captured chat copied to clipboard.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Copy Selected Markdown"))
        {
            ImGui.SetClipboardText(this.SceneService.BuildCurrentMarkdown(selectedOnly: true));
            this.statusMessage = "Copied! Selected captured chat Markdown copied to clipboard.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear Captured Chat"))
        {
            this.SceneService.ClearCapturedChat();
            this.statusMessage = "Captured chat cleared.";
        }

        ImGui.TextDisabled($"{visibleMessages.Count} of {this.SceneService.Messages.Count} captured lines shown. Pin important lines before saving/exporting if you want them featured in recaps.");

        if (visibleMessages.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextWrapped("No captured chat lines match the current filter. Try switching to All Captured Chat or clearing the search box.");
            return;
        }

        if (ImGui.BeginChild("SceneLogChild", new Vector2(0, -1), true))
        {
            if (ImGui.BeginTable("SceneLogTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Use", ImGuiTableColumnFlags.WidthFixed, 38);
                ImGui.TableSetupColumn("Pin", ImGuiTableColumnFlags.WidthFixed, 38);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthFixed, 95);
                ImGui.TableSetupColumn("Sender", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Message");
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableHeadersRow();

                foreach (var message in visibleMessages)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    var selected = message.Selected;
                    if (ImGui.Checkbox($"##selected{message.Id}", ref selected)) message.Selected = selected;

                    ImGui.TableSetColumnIndex(1);
                    var pinned = message.IsPinned;
                    if (ImGui.Checkbox($"##pinned{message.Id}", ref pinned)) message.IsPinned = pinned;

                    ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(message.LocalTime.ToString("HH:mm:ss"));
                    ImGui.TableSetColumnIndex(3); ImGui.TextUnformatted(message.ChatKind);
                    ImGui.TableSetColumnIndex(4); ImGui.TextUnformatted(this.SceneService.IsTracked(message.Sender) ? $"✦ {message.Sender}" : message.Sender);
                    ImGui.TableSetColumnIndex(5); ImGui.TextWrapped(message.Body);
                    ImGui.TableSetColumnIndex(6);
                    if (!string.IsNullOrWhiteSpace(message.Sender) && !this.SceneService.IsTracked(message.Sender))
                    {
                        if (ImGui.SmallButton($"Add##sender{message.Id}"))
                        {
                            if (this.SceneService.AddPartnerFromSender(message.Sender))
                                this.statusMessage = $"Added scene partner: {message.Sender}.";
                            else
                                this.statusMessage = "Could not add sender as a partner.";
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("—");
                    }
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void DrawCapturedChatFilters()
    {
        ImGui.TextUnformatted("Chat Filter");
        ImGui.SetNextItemWidth(210);

        var currentMode = this.configuration.CapturedChatFilterMode;
        if (ImGui.BeginCombo("##capturedchatfiltermode", GetCapturedChatFilterLabel(currentMode)))
        {
            foreach (var mode in Enum.GetValues<CapturedChatFilterMode>())
            {
                var selected = currentMode == mode;
                if (ImGui.Selectable(GetCapturedChatFilterLabel(mode), selected))
                {
                    this.configuration.CapturedChatFilterMode = mode;
                    this.saveConfig();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Clear Filter"))
        {
            this.capturedChatSearchInput = string.Empty;
            this.configuration.CapturedChatFilterMode = CapturedChatFilterMode.All;
            this.saveConfig();
            this.statusMessage = "Captured chat filter cleared.";
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Search");
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var searchWidth = Math.Clamp(availableWidth - 95.0f, 180.0f, 360.0f);
        ImGui.SetNextItemWidth(searchWidth);
        ImGui.InputText("##capturedchatsearch", ref this.capturedChatSearchInput, 256);
        ImGui.SameLine();
        if (ImGui.SmallButton("Clear Search"))
        {
            this.capturedChatSearchInput = string.Empty;
            this.statusMessage = "Captured chat search cleared.";
        }

        if (this.configuration.CapturedChatFilterMode == CapturedChatFilterMode.PartnersAndMe)
        {
            var includeOwn = this.configuration.IncludeOwnMessagesInPartnerFilter;
            if (ImGui.Checkbox("Include my own messages", ref includeOwn))
            {
                this.configuration.IncludeOwnMessagesInPartnerFilter = includeOwn;
                this.saveConfig();
            }
        }

        if (this.configuration.CapturedChatFilterMode == CapturedChatFilterMode.PartnersOnly || this.configuration.CapturedChatFilterMode == CapturedChatFilterMode.PartnersAndMe)
        {
            ImGui.TextDisabled("Partner filters use the current Partners tab list. Use Add on a chat line, Add Current Target, or the right-click context menu.");
        }
    }

    private IEnumerable<SceneMessage> GetVisibleCapturedMessages()
    {
        return this.SceneService.Messages.Where(this.ShouldShowCapturedMessage);
    }

    private bool ShouldShowCapturedMessage(SceneMessage message)
    {
        if (!string.IsNullOrWhiteSpace(this.capturedChatSearchInput) &&
            !ContainsIgnoreCase(message.Sender, this.capturedChatSearchInput) &&
            !ContainsIgnoreCase(message.Body, this.capturedChatSearchInput) &&
            !ContainsIgnoreCase(message.ChatKind, this.capturedChatSearchInput))
        {
            return false;
        }

        return this.configuration.CapturedChatFilterMode switch
        {
            CapturedChatFilterMode.All => true,
            CapturedChatFilterMode.PartnersOnly => this.SceneService.IsTracked(message.Sender),
            CapturedChatFilterMode.PartnersAndMe => this.SceneService.IsTracked(message.Sender) || (this.configuration.IncludeOwnMessagesInPartnerFilter && this.IsOwnMessage(message)),
            CapturedChatFilterMode.PinnedOnly => message.IsPinned,
            _ => true,
        };
    }

    private bool IsOwnMessage(SceneMessage message)
    {
        var localName = this.partnerTrackingService.LocalPlayerName;
        return !string.IsNullOrWhiteSpace(localName) && NamesMatch(message.Sender, localName);
    }

    private static string GetCapturedChatFilterLabel(CapturedChatFilterMode mode)
    {
        return mode switch
        {
            CapturedChatFilterMode.All => "All Captured Chat",
            CapturedChatFilterMode.PartnersOnly => "Scene Partners Only",
            CapturedChatFilterMode.PartnersAndMe => "Scene Partners + Me",
            CapturedChatFilterMode.PinnedOnly => "Pinned Only",
            _ => "All Captured Chat",
        };
    }

    private static bool ContainsIgnoreCase(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static bool NamesMatch(string? left, string? right) => NameWorldParser.NamesMatch(left, right);

    private void AddCurrentTargetAsPartner()
    {
        var target = this.targetManager.Target;
        if (target is null || string.IsNullOrWhiteSpace(target.Name.TextValue))
        {
            this.statusMessage = "No current target to add.";
            return;
        }

        var name = target.Name.TextValue;
        if (this.SceneService.AddPartner(name))
            this.statusMessage = $"Added current target as scene partner: {name}.";
        else
            this.statusMessage = $"Could not add current target: {name}. They may already be tracked.";
    }

    private void DrawSceneBuilderTab()
    {
        this.EnsureSceneBuilderSlots();

        ImGui.TextWrapped($"Draft up to five RP paragraphs. Each paragraph is hard-limited to {SceneBuilderHardCharacterLimit} characters, then copied one at a time through a guided queue with a panic stop available.");
        ImGui.Spacing();

        var currentChannel = this.configuration.SceneBuilderChannel;
        if (!BuilderChannelOptions.Any(o => string.Equals(o.Command, currentChannel, StringComparison.OrdinalIgnoreCase)))
            currentChannel = "/emote";

        if (ImGui.BeginCombo("Channel Prefix", BuilderChannelOptions.First(o => string.Equals(o.Command, currentChannel, StringComparison.OrdinalIgnoreCase)).Label))
        {
            foreach (var option in BuilderChannelOptions)
            {
                var selected = string.Equals(currentChannel, option.Command, StringComparison.OrdinalIgnoreCase);
                if (ImGui.Selectable(option.Label, selected))
                {
                    this.configuration.SceneBuilderChannel = option.Command;
                    this.saveConfig();
                }

                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.TextDisabled($"Paragraph limit: {SceneBuilderHardCharacterLimit} characters each.");

        ImGui.Separator();

        var max = SceneBuilderHardCharacterLimit;
        for (var i = 0; i < 5; i++)
        {
            var text = this.configuration.SceneBuilderParagraphs[i] ?? string.Empty;
            ImGui.TextUnformatted($"Paragraph {i + 1}/5");
            ImGui.SameLine();
            var countText = $"{text.Length}/{max}";
            if (text.Length >= max)
                ImGui.TextDisabled(countText + "  limit reached");
            else
                ImGui.TextDisabled(countText);

            if (ImGui.InputTextMultiline($"##builderparagraph{i}", ref text, 2500, new Vector2(-1, 74)))
            {
                if (text.Length > max)
                {
                    text = text[..max];
                    this.statusMessage = $"Paragraph {i + 1}/5 was trimmed to the chat limit.";
                }

                this.configuration.SceneBuilderParagraphs[i] = text;
                this.saveConfig();
            }
        }

        ImGui.Separator();

        if (this.isSceneBuilderPosting)
        {
            ImGui.TextUnformatted($"Guided queue active: {this.sceneBuilderPostIndex}/{this.sceneBuilderPostTotal} copied.");
            ImGui.SameLine();
            if (ImGui.Button("Copy Next"))
            {
                this.CopyNextSceneBuilderParagraph();
            }
            ImGui.SameLine();
            if (ImGui.Button("PANIC STOP"))
            {
                this.StopSceneBuilderPosting("Guided queue stopped.");
            }
        }
        else
        {
            if (ImGui.Button("Start Guided Queue"))
            {
                this.pendingPostParagraphs = this.GetSceneBuilderParagraphsForPosting();
                this.pendingPostChannel = this.configuration.SceneBuilderChannel;

                if (this.pendingPostParagraphs.Count == 0)
                    this.statusMessage = "Scene Builder has no paragraphs to copy.";
                else
                    ImGui.OpenPopup("Confirm Scene Builder Post");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy All"))
        {
            var text = string.Join(Environment.NewLine + Environment.NewLine, this.GetSceneBuilderParagraphsForPosting());
            ImGui.SetClipboardText(text);
            this.statusMessage = string.IsNullOrWhiteSpace(text) ? "Scene Builder has nothing to copy." : "Copied! Scene Builder paragraphs copied to clipboard.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Builder"))
        {
            for (var i = 0; i < this.configuration.SceneBuilderParagraphs.Count; i++)
                this.configuration.SceneBuilderParagraphs[i] = string.Empty;
            this.saveConfig();
            this.statusMessage = "Scene Builder cleared.";
        }

        if (ImGui.BeginPopupModal("Confirm Scene Builder Post", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped($"Start a guided clipboard queue for {this.pendingPostParagraphs.Count} paragraph(s) using {this.pendingPostChannel} as the chat prefix?");
            ImGui.TextDisabled("SceneKeeper will copy the first prepared chat line immediately. Paste/send it in-game, then click Copy Next for the following paragraph. Use PANIC STOP if you need to stop the queue.");
            ImGui.Spacing();

            if (ImGui.Button("Start Queue"))
            {
                this.StartSceneBuilderPosting(this.pendingPostParagraphs, this.pendingPostChannel);
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        this.DrawSpellCheckSection();
    }

    private void DrawSpellCheckSection()
    {
        ImGui.Separator();
        ImGui.TextUnformatted("Spell Check");

        var enabled = this.configuration.EnableSpellCheck;
        if (ImGui.Checkbox("Enable spell check", ref enabled))
        {
            this.configuration.EnableSpellCheck = enabled;
            this.saveConfig();
        }

        ImGui.SameLine();
        var skipCaps = this.configuration.SpellCheckSkipCapitalizedWords;
        if (ImGui.Checkbox("Ignore capitalized words", ref skipCaps))
        {
            this.configuration.SpellCheckSkipCapitalizedWords = skipCaps;
            this.saveConfig();
        }

        if (!this.configuration.EnableSpellCheck)
            return;

        ImGui.TextDisabled("Checks Scene Builder paragraphs with a local dictionary. Add character names, lore terms, and FFXIV words to your custom dictionary.");

        ImGui.SetNextItemWidth(220);
        ImGui.InputText("Add Custom Word", ref this.customDictionaryInput, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add Word"))
        {
            this.AddCustomDictionaryWord(this.customDictionaryInput);
            this.customDictionaryInput = string.Empty;
        }

        var issues = this.spellCheckService.FindIssues(
            this.configuration.SceneBuilderParagraphs,
            this.configuration.CustomDictionaryWords,
            this.configuration.IgnoredSpellCheckWords,
            this.configuration.SpellCheckSkipCapitalizedWords);

        ImGui.TextDisabled(issues.Count == 0 ? "No spelling issues found." : $"{issues.Count} possible spelling issue(s) found.");

        if (issues.Count == 0)
            return;

        if (ImGui.BeginChild("SpellCheckIssuesChild", new Vector2(0, 160), true))
        {
            foreach (var issue in issues.Take(20))
            {
                ImGui.TextUnformatted($"Paragraph {issue.ParagraphIndex + 1}/5: {issue.Word}");

                if (issue.Suggestions.Count > 0)
                {
                    ImGui.SameLine();
                    foreach (var suggestion in issue.Suggestions.Take(3))
                    {
                        if (ImGui.SmallButton($"Replace: {suggestion}##replace{issue.ParagraphIndex}{issue.Word}{suggestion}"))
                        {
                            this.ReplaceWordInSceneBuilderParagraph(issue.ParagraphIndex, issue.Word, suggestion);
                            this.statusMessage = $"Replaced {issue.Word} with {suggestion}.";
                        }
                        ImGui.SameLine();
                    }
                    ImGui.NewLine();
                }

                if (ImGui.SmallButton($"Ignore##ignore{issue.ParagraphIndex}{issue.Word}"))
                {
                    this.AddIgnoredSpellCheckWord(issue.Word);
                    this.statusMessage = $"Ignored spelling word: {issue.Word}.";
                }
                ImGui.SameLine();
                if (ImGui.SmallButton($"Add to Dictionary##dict{issue.ParagraphIndex}{issue.Word}"))
                {
                    this.AddCustomDictionaryWord(issue.Word);
                    this.statusMessage = $"Added {issue.Word} to the custom dictionary.";
                }
                ImGui.Separator();
            }
        }
        ImGui.EndChild();
    }

    private void AddCustomDictionaryWord(string word)
    {
        word = CleanDictionaryWord(word);
        if (string.IsNullOrWhiteSpace(word))
            return;

        if (!this.configuration.CustomDictionaryWords.Any(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
        {
            this.configuration.CustomDictionaryWords.Add(word);
            this.saveConfig();
        }
    }

    private void AddIgnoredSpellCheckWord(string word)
    {
        word = CleanDictionaryWord(word);
        if (string.IsNullOrWhiteSpace(word))
            return;

        if (!this.configuration.IgnoredSpellCheckWords.Any(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
        {
            this.configuration.IgnoredSpellCheckWords.Add(word);
            this.saveConfig();
        }
    }

    private void ReplaceWordInSceneBuilderParagraph(int paragraphIndex, string word, string replacement)
    {
        this.EnsureSceneBuilderSlots();
        if (paragraphIndex < 0 || paragraphIndex >= this.configuration.SceneBuilderParagraphs.Count)
            return;

        var text = this.configuration.SceneBuilderParagraphs[paragraphIndex] ?? string.Empty;
        var pattern = $@"\b{Regex.Escape(word)}\b";
        this.configuration.SceneBuilderParagraphs[paragraphIndex] = Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        this.saveConfig();
    }

    private static string CleanDictionaryWord(string word)
    {
        return new string((word ?? string.Empty).Trim().Where(c => char.IsLetter(c) || c == '\'').ToArray());
    }

    private void EnsureSceneBuilderSlots()
    {
        this.configuration.SceneBuilderParagraphs ??= new List<string>();
        while (this.configuration.SceneBuilderParagraphs.Count < 5)
            this.configuration.SceneBuilderParagraphs.Add(string.Empty);
        while (this.configuration.SceneBuilderParagraphs.Count > 5)
            this.configuration.SceneBuilderParagraphs.RemoveAt(this.configuration.SceneBuilderParagraphs.Count - 1);
    }

    private void TrimSceneBuilderParagraphsToLimit()
    {
        this.EnsureSceneBuilderSlots();
        var max = SceneBuilderHardCharacterLimit;
        for (var i = 0; i < this.configuration.SceneBuilderParagraphs.Count; i++)
        {
            var text = this.configuration.SceneBuilderParagraphs[i] ?? string.Empty;
            if (text.Length > max)
                this.configuration.SceneBuilderParagraphs[i] = text[..max];
        }
    }

    private List<string> GetSceneBuilderParagraphsForPosting()
    {
        this.EnsureSceneBuilderSlots();
        var max = SceneBuilderHardCharacterLimit;
        return this.configuration.SceneBuilderParagraphs
            .Select(p => (p ?? string.Empty).Replace("\r", string.Empty).Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Length > max ? p[..max] : p)
            .ToList();
    }

    private void StartSceneBuilderPosting(IReadOnlyList<string> paragraphs, string channel)
    {
        if (paragraphs.Count == 0)
        {
            this.statusMessage = "Scene Builder has no paragraphs to copy.";
            return;
        }

        this.sceneBuilderPostQueue.Clear();
        foreach (var paragraph in paragraphs)
            this.sceneBuilderPostQueue.Enqueue(paragraph);

        this.pendingPostParagraphs = new List<string>();
        this.isSceneBuilderPosting = true;
        this.sceneBuilderPostTotal = paragraphs.Count;
        this.sceneBuilderPostIndex = 0;
        this.pendingPostChannel = string.IsNullOrWhiteSpace(channel) ? "/emote" : channel;
        this.nextSceneBuilderPostAt = DateTime.MinValue;
        this.statusMessage = $"Guided queue started: {paragraphs.Count} paragraph(s) ready.";
        this.CopyNextSceneBuilderParagraph();
    }

    private void StopSceneBuilderPosting(string message)
    {
        this.sceneBuilderPostQueue.Clear();
        this.isSceneBuilderPosting = false;
        this.sceneBuilderPostIndex = 0;
        this.sceneBuilderPostTotal = 0;
        this.statusMessage = message;
    }

    private void ProcessSceneBuilderPostQueue()
    {
        // Scene Builder uses a guided clipboard queue rather than dispatching native game chat commands.
        // Native commands such as /say and /emote are not registered Dalamud commands, so automatic dispatch is unreliable.
    }

    private void CopyNextSceneBuilderParagraph()
    {
        if (!this.isSceneBuilderPosting)
            return;

        if (this.sceneBuilderPostQueue.Count == 0)
        {
            this.StopSceneBuilderPosting("Guided queue complete.");
            return;
        }

        var paragraph = this.sceneBuilderPostQueue.Dequeue();
        this.sceneBuilderPostIndex++;

        var command = BuildSceneBuilderCommand(this.pendingPostChannel, paragraph);
        ImGui.SetClipboardText(command);

        if (this.sceneBuilderPostQueue.Count == 0)
        {
            this.isSceneBuilderPosting = false;
            this.statusMessage = $"Copied! Paragraph {this.sceneBuilderPostIndex}/{this.sceneBuilderPostTotal} copied. Guided queue complete.";
            return;
        }

        this.statusMessage = $"Copied! Paragraph {this.sceneBuilderPostIndex}/{this.sceneBuilderPostTotal} copied. Paste/send it, then click Copy Next.";
    }

    private static string BuildSceneBuilderCommand(string channel, string paragraph)
    {
        channel = string.IsNullOrWhiteSpace(channel) ? "/emote" : channel.Trim();
        paragraph = paragraph.Replace("\r", " ").Replace("\n", " ").Trim();
        return $"{channel} {paragraph}";
    }

    private void DrawHistoryTab()
    {
        ImGui.InputText("Search History", ref this.historySearchInput, 256);
        ImGui.SameLine();
        if (ImGui.SmallButton("Clear Search")) this.historySearchInput = string.Empty;

        if (ImGui.Button("Save Current Scene"))
        {
            var entry = this.SceneService.SaveCurrentSceneToHistory();
            this.statusMessage = $"Saved '{entry.SceneName}' to history.";
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear History"))
        {
            this.SceneService.ClearHistory();
            this.statusMessage = "Scene history cleared.";
        }

        var entries = this.SceneService.SceneHistory
            .Where(entry => this.SceneService.HistoryEntryMatches(entry, this.historySearchInput))
            .ToList();

        ImGui.TextDisabled($"{entries.Count} of {this.SceneService.SceneHistory.Count} saved scenes shown. Loading a scene replaces your current workspace.");

        if (this.SceneService.SceneHistory.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextWrapped("No saved scenes yet. Save a scene when you want to revisit its notes, partners, follow-ups, pinned lines, and captured chat later.");
            return;
        }

        if (entries.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextWrapped("No saved scenes match your search.");
            return;
        }

        if (ImGui.BeginTable("HistoryTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
        {
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 92);
            ImGui.TableSetupColumn("Saved", ImGuiTableColumnFlags.WidthFixed, 115);
            ImGui.TableSetupColumn("Scene", ImGuiTableColumnFlags.WidthFixed, 220);
            ImGui.TableSetupColumn("Tags", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Partners", ImGuiTableColumnFlags.WidthFixed, 65);
            ImGui.TableSetupColumn("Lines", ImGuiTableColumnFlags.WidthFixed, 55);
            ImGui.TableSetupColumn("Pins/Tasks", ImGuiTableColumnFlags.WidthFixed, 75);
            ImGui.TableSetupScrollFreeze(1, 1);
            ImGui.TableHeadersRow();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                if (ImGui.SmallButton($"Load##history{entry.Id}"))
                {
                    this.SceneService.LoadSceneFromHistory(entry.Id);
                    this.statusMessage = $"Loaded '{entry.SceneName}' from history.";
                }
                if (ImGui.SmallButton($"Copy##history{entry.Id}"))
                {
                    ImGui.SetClipboardText(this.SceneService.BuildHistoryContext(entry, true));
                    this.statusMessage = $"Copied! '{entry.SceneName}' copied to clipboard.";
                }
                if (ImGui.SmallButton($"Markdown##history{entry.Id}"))
                {
                    ImGui.SetClipboardText(this.SceneService.BuildHistoryMarkdown(entry));
                    this.statusMessage = $"Copied! '{entry.SceneName}' Markdown copied to clipboard.";
                }
                if (ImGui.SmallButton($"Summary##history{entry.Id}"))
                {
                    ImGui.SetClipboardText(this.SceneService.BuildHistorySummary(entry));
                    this.statusMessage = $"Copied! Summary for '{entry.SceneName}' copied to clipboard.";
                }
                if (ImGui.SmallButton($"Delete##history{entry.Id}"))
                {
                    this.SceneService.DeleteHistoryEntry(entry.Id);
                    this.statusMessage = $"Deleted '{entry.SceneName}' from history.";
                }

                ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(entry.SavedAtLocal.ToString("MM/dd HH:mm"));
                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(entry.SceneName);
                if (!string.IsNullOrWhiteSpace(entry.SceneNotes))
                    ImGui.TextDisabled(entry.SceneNotes.Length > 70 ? entry.SceneNotes[..70] + "..." : entry.SceneNotes);
                ImGui.TableSetColumnIndex(3); ImGui.TextWrapped(entry.Tags);
                ImGui.TableSetColumnIndex(4); ImGui.TextUnformatted(entry.PartnerCount.ToString());
                ImGui.TableSetColumnIndex(5); ImGui.TextUnformatted(entry.MessageCount.ToString());
                ImGui.TableSetColumnIndex(6); ImGui.TextUnformatted($"{entry.PinnedCount}/{entry.OpenTaskCount}");
            }

            ImGui.EndTable();
        }
    }

    private void DrawSettingsTab()
    {
        var enableChatCapture = this.configuration.EnableChatCapture;
        if (ImGui.Checkbox("Enable chat capture", ref enableChatCapture)) { this.configuration.EnableChatCapture = enableChatCapture; this.saveConfig(); }

        var enableOverlay = this.configuration.EnableOverlayMarkers;
        if (ImGui.Checkbox("Enable partner markers", ref enableOverlay)) { this.configuration.EnableOverlayMarkers = enableOverlay; this.saveConfig(); }

        var enableHighlight = this.configuration.EnablePartnerHighlight;
        if (ImGui.Checkbox("Highlight tracked nearby players", ref enableHighlight)) { this.configuration.EnablePartnerHighlight = enableHighlight; this.saveConfig(); }

        var enableTargetContextMenu = this.configuration.EnableTargetContextMenu;
        if (ImGui.Checkbox("Add SceneKeeper option to player right-click menu", ref enableTargetContextMenu)) { this.configuration.EnableTargetContextMenu = enableTargetContextMenu; this.saveConfig(); }

        var offset = this.configuration.MarkerVerticalOffset;
        if (ImGui.SliderFloat("Marker vertical offset", ref offset, 0.5f, 4.0f)) { this.configuration.MarkerVerticalOffset = offset; this.saveConfig(); }

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

        var maxMessages = this.configuration.MaxSceneMessages;
        if (ImGui.SliderInt("Max captured lines", ref maxMessages, 25, 1000)) { this.configuration.MaxSceneMessages = maxMessages; this.saveConfig(); }

        var maxHistory = this.configuration.MaxSceneHistoryEntries;
        if (ImGui.SliderInt("Max saved scenes", ref maxHistory, 1, 100)) { this.configuration.MaxSceneHistoryEntries = maxHistory; this.saveConfig(); }

        ImGui.Separator();
        ImGui.TextUnformatted("Support & Bug Reports");
        ImGui.TextWrapped("Need help, found a bug, or want to support SceneKeeper? These links open in your browser.");

        if (ImGui.Button("Report a Bug / Join Discord"))
            this.OpenExternalUrl(DiscordInviteUrl, "Discord invite copied to clipboard.");

        ImGui.SameLine();

        if (ImGui.Button("Support on Ko-fi"))
            this.OpenExternalUrl(KofiUrl, "Ko-fi link copied to clipboard.");

        ImGui.Separator();
        ImGui.TextUnformatted("Logged Chat Types");
        ImGui.TextWrapped("Toggle which visible chat types SceneKeeper should capture into the Captured Chat tab.");

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

        ImGui.Separator();
        ImGui.TextUnformatted("Spell Check Dictionary");
        ImGui.TextDisabled($"Custom words: {this.configuration.CustomDictionaryWords.Count}. Ignored words this session/config: {this.configuration.IgnoredSpellCheckWords.Count}.");
        if (ImGui.Button("Clear Ignored Spellcheck Words"))
        {
            this.configuration.IgnoredSpellCheckWords.Clear();
            this.saveConfig();
            this.statusMessage = "Ignored spellcheck words cleared.";
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear Custom Dictionary"))
        {
            this.configuration.CustomDictionaryWords.Clear();
            this.saveConfig();
            this.statusMessage = "Custom spellcheck dictionary cleared.";
        }
    }

    private void OpenExternalUrl(string url, string fallbackStatusMessage)
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
            this.statusMessage = fallbackStatusMessage;
        }
    }

    private void SetChatKindEnabled(string kind, bool enabled)
    {
        this.configuration.CapturedChatKinds.RemoveAll(k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
        if (enabled)
            this.configuration.CapturedChatKinds.Add(kind);
    }
}
