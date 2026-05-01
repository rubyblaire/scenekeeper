using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using SceneKeeper.Services;

namespace SceneKeeper.Windows;

public sealed class SettingsWindow : Window
{
    private readonly Configuration configuration;
    private readonly AidenAssistService aidenAssistService;
    private readonly Action saveConfig;
    private string ownerPasswordInput = string.Empty;
    private string newOwnerPasswordInput = string.Empty;
    private string apiKeyInput = string.Empty;
    private bool ownerUnlocked;
    private string status = string.Empty;

    public SettingsWindow(Configuration configuration, AidenAssistService aidenAssistService, Action saveConfig)
        : base("SceneKeeper Settings##SceneKeeperSettings")
    {
        this.configuration = configuration;
        this.aidenAssistService = aidenAssistService;
        this.saveConfig = saveConfig;
        this.SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(520, 420), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("SceneKeeper Settings");
        ImGui.Separator();
        var enableChatCapture = this.configuration.EnableChatCapture;
        if (ImGui.Checkbox("Enable chat capture", ref enableChatCapture)) { this.configuration.EnableChatCapture = enableChatCapture; this.saveConfig(); }
        var enableOverlay = this.configuration.EnableOverlayMarkers;
        if (ImGui.Checkbox("Enable soft partner markers", ref enableOverlay)) { this.configuration.EnableOverlayMarkers = enableOverlay; this.saveConfig(); }
        var maxMessages = this.configuration.MaxSceneMessages;
        if (ImGui.SliderInt("Max scene log lines", ref maxMessages, 25, 1000)) { this.configuration.MaxSceneMessages = maxMessages; this.saveConfig(); }
        ImGui.Separator();
        this.DrawOwnerSection();
    }

    private void DrawOwnerSection()
    {
        ImGui.TextUnformatted("Owner Unlock");
        var hasPassword = !string.IsNullOrWhiteSpace(this.configuration.OwnerPasswordSaltBase64) && !string.IsNullOrWhiteSpace(this.configuration.OwnerPasswordHashBase64);
        if (!hasPassword)
        {
            ImGui.InputText("Set Owner Password", ref this.newOwnerPasswordInput, 256, ImGuiInputTextFlags.Password);
            if (ImGui.Button("Save Owner Password"))
            {
                try
                {
                    var result = OwnerUnlockService.CreatePasswordHash(this.newOwnerPasswordInput);
                    this.configuration.OwnerPasswordSaltBase64 = result.SaltBase64;
                    this.configuration.OwnerPasswordHashBase64 = result.HashBase64;
                    this.newOwnerPasswordInput = string.Empty;
                    this.status = "Owner password saved.";
                    this.saveConfig();
                }
                catch (Exception ex) { this.status = ex.Message; }
            }
            ImGui.TextWrapped(this.status);
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
                this.status = this.ownerUnlocked ? "Unlocked." : "Password did not match.";
                if (this.ownerUnlocked) this.apiKeyInput = LocalSecretService.UnprotectString(this.configuration.OpenAiApiKeyProtectedBase64);
            }
            ImGui.TextWrapped(this.status);
            return;
        }

        var enabled = this.configuration.AidenAssistEnabled;
        if (ImGui.Checkbox("Enable Aiden Assist", ref enabled)) { this.configuration.AidenAssistEnabled = enabled; this.saveConfig(); }
        ImGui.InputText("OpenAI API Key", ref this.apiKeyInput, 512, ImGuiInputTextFlags.Password);
        if (ImGui.Button("Save API Key"))
        {
            this.configuration.OpenAiApiKeyProtectedBase64 = LocalSecretService.ProtectString(this.apiKeyInput);
            this.status = "API key saved locally.";
            this.saveConfig();
        }
        ImGui.SameLine();
        if (ImGui.Button("Clear API Key"))
        {
            this.apiKeyInput = string.Empty;
            this.configuration.OpenAiApiKeyProtectedBase64 = string.Empty;
            this.status = "API key cleared.";
            this.saveConfig();
        }
        var model = this.configuration.OpenAiModel;
        if (ImGui.InputText("Model", ref model, 128)) { this.configuration.OpenAiModel = model; this.saveConfig(); }
        ImGui.TextWrapped(this.aidenAssistService.HasApiKey ? "Aiden Assist has a saved API key. Drafting remains manual-only." : "No API key saved yet.");
        if (ImGui.Button("Lock")) { this.ownerUnlocked = false; this.apiKeyInput = string.Empty; this.status = "Locked."; }
        ImGui.TextWrapped(this.status);
    }
}
