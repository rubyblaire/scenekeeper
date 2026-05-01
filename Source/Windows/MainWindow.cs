using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using SceneKeeper.Services;

namespace SceneKeeper.Windows;

public sealed class MainWindow : Window
{
    private readonly Configuration configuration;
    private readonly PartnerTrackingService partnerTrackingService;
    private readonly AidenAssistService aidenAssistService;
    private readonly Action saveConfig;

    private string partnerNameInput = string.Empty;
    private string ownerPasswordInput = string.Empty;
    private string newOwnerPasswordInput = string.Empty;
    private string apiKeyInput = string.Empty;
    private bool ownerUnlocked;
    private string ownerStatus = string.Empty;
    private string draftIntent = "Reply naturally while preserving Aiden's composure.";
    private string draftTone = "Elegant, restrained, emotionally intelligent.";
    private string draftOutput = string.Empty;
    private string draftError = string.Empty;
    private bool isDrafting;
    private CancellationTokenSource? draftCancellation;

    public MainWindow(Configuration configuration, SceneService sceneService, PartnerTrackingService partnerTrackingService, AidenAssistService aidenAssistService, Action saveConfig)
        : base("SceneKeeper##SceneKeeperMain", ImGuiWindowFlags.NoScrollbar)
    {
        this.configuration = configuration;
        this.SceneService = sceneService;
        this.partnerTrackingService = partnerTrackingService;
        this.aidenAssistService = aidenAssistService;
        this.saveConfig = saveConfig;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(620, 500), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
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
            if (ImGui.BeginTabItem("Aiden Assist")) { this.DrawAidenAssistTab(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Settings")) { this.DrawSettingsTab(); ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }

        ImGui.Separator();
        this.DrawOwnerUnlockFooter();
    }

    private void DrawSceneTab()
    {
        var sceneName = this.configuration.CurrentSceneName;
        if (ImGui.InputText("Scene Name", ref sceneName, 128)) this.SceneService.SetSceneName(sceneName);

        var notes = this.configuration.SceneNotes;
        ImGui.TextUnformatted("Scene Notes");
        if (ImGui.InputTextMultiline("##SceneNotes", ref notes, 12000, new Vector2(-1, 150))) this.SceneService.SetSceneNotes(notes);

        if (ImGui.Button(this.configuration.IsTrackingPaused ? "Resume Tracking" : "Pause Tracking"))
        {
            this.configuration.IsTrackingPaused = !this.configuration.IsTrackingPaused;
            this.saveConfig();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear Scene Log")) this.SceneService.ClearMessages();
        ImGui.TextWrapped("SceneKeeper captures only configured chat kinds and never sends generated text into chat. Use Aiden Assist to draft, then copy/paste manually.");
    }

    private void DrawPartnersTab()
    {
        ImGui.InputText("Partner Name", ref this.partnerNameInput, 128);
        ImGui.SameLine();
        if (ImGui.Button("Add Partner") && this.SceneService.AddPartner(this.partnerNameInput)) this.partnerNameInput = string.Empty;

        var nearby = this.partnerTrackingService.GetNearbyTrackedPlayers().Select(p => p.Name.TextValue).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ImGui.BeginTable("PartnersTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Name"); ImGui.TableSetupColumn("Alias"); ImGui.TableSetupColumn("Nearby"); ImGui.TableSetupColumn("Actions"); ImGui.TableHeadersRow();
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
        ImGui.SameLine(); if (ImGui.Button("Clear Log")) this.SceneService.ClearMessages();
        ImGui.TextDisabled($"{this.SceneService.Messages.Count} captured lines. Selected lines are used by Aiden Assist.");

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

    private void DrawAidenAssistTab()
    {
        if (!this.ownerUnlocked) { ImGui.TextWrapped("Aiden Assist is owner-locked. Use the Owner Unlock section at the bottom of the app."); return; }
        if (!this.configuration.AidenAssistEnabled) { ImGui.TextWrapped("Aiden Assist is unlocked but disabled. Enable it in the owner footer or settings."); return; }
        if (!this.aidenAssistService.HasApiKey) { ImGui.TextWrapped("Add and save an OpenAI API key in the owner footer before drafting."); return; }

        var context = this.SceneService.BuildSelectedContext(this.configuration.IncludePlayerNamesInPrompt);
        ImGui.TextUnformatted("What should the draft do?");
        ImGui.InputTextMultiline("##DraftIntent", ref this.draftIntent, 2000, new Vector2(-1, 70));
        ImGui.TextUnformatted("Tone");
        ImGui.InputText("##DraftTone", ref this.draftTone, 512);
        ImGui.TextUnformatted("Context Preview");
        ImGui.InputTextMultiline("##ContextPreview", ref context, 20000, new Vector2(-1, 120), ImGuiInputTextFlags.ReadOnly);

        if (!this.isDrafting)
        {
            if (ImGui.Button("Draft RP Response")) this.StartDraft(context);
        }
        else
        {
            ImGui.TextUnformatted("Drafting...");
            ImGui.SameLine();
            if (ImGui.Button("Cancel")) this.draftCancellation?.Cancel();
        }

        if (!string.IsNullOrWhiteSpace(this.draftError)) ImGui.TextWrapped($"Error: {this.draftError}");
        ImGui.TextUnformatted("Generated Draft");
        ImGui.InputTextMultiline("##GeneratedDraft", ref this.draftOutput, 12000, new Vector2(-1, 140));
        if (ImGui.Button("Copy Draft")) ImGui.SetClipboardText(this.draftOutput);
    }

    private void DrawSettingsTab()
    {
        var enableChatCapture = this.configuration.EnableChatCapture;
        if (ImGui.Checkbox("Enable chat capture", ref enableChatCapture)) { this.configuration.EnableChatCapture = enableChatCapture; this.saveConfig(); }
        var enableOverlay = this.configuration.EnableOverlayMarkers;
        if (ImGui.Checkbox("Enable soft partner markers", ref enableOverlay)) { this.configuration.EnableOverlayMarkers = enableOverlay; this.saveConfig(); }
        var offset = this.configuration.MarkerVerticalOffset;
        if (ImGui.SliderFloat("Marker vertical offset", ref offset, 0.5f, 4.0f)) { this.configuration.MarkerVerticalOffset = offset; this.saveConfig(); }
        var maxMessages = this.configuration.MaxSceneMessages;
        if (ImGui.SliderInt("Max captured lines", ref maxMessages, 25, 1000)) { this.configuration.MaxSceneMessages = maxMessages; this.saveConfig(); }

        ImGui.Separator();
        ImGui.TextUnformatted("Captured chat kinds");
        ImGui.TextWrapped("These names are compared against XivChatType.ToString(), so they can be adjusted if Dalamud enum names change.");
        var kinds = string.Join(",", this.configuration.CapturedChatKinds);
        if (ImGui.InputText("Kinds CSV", ref kinds, 1024))
        {
            this.configuration.CapturedChatKinds = kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            this.saveConfig();
        }
    }

    private void DrawOwnerUnlockFooter()
    {
        ImGui.TextUnformatted("Owner Unlock");
        var hasPassword = !string.IsNullOrWhiteSpace(this.configuration.OwnerPasswordSaltBase64) && !string.IsNullOrWhiteSpace(this.configuration.OwnerPasswordHashBase64);
        if (!hasPassword)
        {
            ImGui.TextWrapped("Set an owner password to reveal private Aiden Assist settings.");
            ImGui.InputText("Set Owner Password", ref this.newOwnerPasswordInput, 256, ImGuiInputTextFlags.Password);
            if (ImGui.Button("Save Owner Password"))
            {
                try
                {
                    var result = OwnerUnlockService.CreatePasswordHash(this.newOwnerPasswordInput);
                    this.configuration.OwnerPasswordSaltBase64 = result.SaltBase64;
                    this.configuration.OwnerPasswordHashBase64 = result.HashBase64;
                    this.newOwnerPasswordInput = string.Empty;
                    this.ownerStatus = "Owner password saved.";
                    this.saveConfig();
                }
                catch (Exception ex) { this.ownerStatus = ex.Message; }
            }
            if (!string.IsNullOrWhiteSpace(this.ownerStatus)) ImGui.TextWrapped(this.ownerStatus);
            return;
        }

        if (!this.ownerUnlocked)
        {
            ImGui.InputText("Password", ref this.ownerPasswordInput, 256, ImGuiInputTextFlags.Password);
            ImGui.SameLine();
            if (ImGui.Button("Unlock"))
            {
                this.ownerUnlocked = OwnerUnlockService.VerifyPassword(this.ownerPasswordInput, this.configuration.OwnerPasswordSaltBase64, this.configuration.OwnerPasswordHashBase64);
                this.ownerPasswordInput = string.Empty;
                this.ownerStatus = this.ownerUnlocked ? "Unlocked." : "Password did not match.";
                if (this.ownerUnlocked) this.apiKeyInput = LocalSecretService.UnprotectString(this.configuration.OpenAiApiKeyProtectedBase64);
            }
            if (!string.IsNullOrWhiteSpace(this.ownerStatus)) ImGui.TextWrapped(this.ownerStatus);
            return;
        }

        ImGui.TextUnformatted("Aiden Assist Settings");
        var enabled = this.configuration.AidenAssistEnabled;
        if (ImGui.Checkbox("Enable Aiden Assist", ref enabled)) { this.configuration.AidenAssistEnabled = enabled; this.saveConfig(); }
        ImGui.InputText("OpenAI API Key", ref this.apiKeyInput, 512, ImGuiInputTextFlags.Password);
        if (ImGui.Button("Save API Key"))
        {
            this.configuration.OpenAiApiKeyProtectedBase64 = LocalSecretService.ProtectString(this.apiKeyInput);
            this.ownerStatus = "API key saved locally.";
            this.saveConfig();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear API Key"))
        {
            this.apiKeyInput = string.Empty;
            this.configuration.OpenAiApiKeyProtectedBase64 = string.Empty;
            this.ownerStatus = "API key cleared.";
            this.saveConfig();
        }
        var model = this.configuration.OpenAiModel;
        if (ImGui.InputText("Model", ref model, 128)) { this.configuration.OpenAiModel = model; this.saveConfig(); }
        var includeNames = this.configuration.IncludePlayerNamesInPrompt;
        if (ImGui.Checkbox("Include player names in prompt", ref includeNames)) { this.configuration.IncludePlayerNamesInPrompt = includeNames; this.saveConfig(); }

        ImGui.TextUnformatted("Character Context");
        var characterContext = this.configuration.CharacterContext;
        if (ImGui.InputTextMultiline("##CharacterContext", ref characterContext, 12000, new Vector2(-1, 100)))
        {
            this.configuration.CharacterContext = characterContext;
            this.saveConfig();
        }
        if (ImGui.Button("Lock")) { this.ownerUnlocked = false; this.apiKeyInput = string.Empty; this.ownerStatus = "Locked."; }
        if (!string.IsNullOrWhiteSpace(this.ownerStatus)) ImGui.TextWrapped(this.ownerStatus);
    }

    private void StartDraft(string context)
    {
        this.draftCancellation?.Cancel();
        this.draftCancellation?.Dispose();
        this.draftCancellation = new CancellationTokenSource();
        this.isDrafting = true;
        this.draftError = string.Empty;
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await this.aidenAssistService.DraftAsync(context, this.draftIntent, this.draftTone, this.draftCancellation.Token).ConfigureAwait(false);
                if (result.Success) this.draftOutput = result.Text;
                else this.draftError = result.Error;
            }
            catch (OperationCanceledException) { this.draftError = "Draft cancelled."; }
            catch (Exception ex) { this.draftError = ex.Message; }
            finally { this.isDrafting = false; }
        });
    }
}
