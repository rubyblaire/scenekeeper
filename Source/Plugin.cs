using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SceneKeeper.Services;
using SceneKeeper.Windows;

namespace SceneKeeper;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/scenekeeper";
    private const string ShortCommandName = "/sk";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IChatGui chatGui;
    private readonly IPluginLog log;
    private readonly WindowSystem windowSystem = new("SceneKeeper");
    private readonly MainWindow mainWindow;
    private readonly SettingsWindow settingsWindow;
    private readonly ChatCaptureService chatCaptureService;
    private readonly OverlayService overlayService;
    private readonly AidenAssistService aidenAssistService;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IObjectTable objectTable,
        IGameGui gameGui,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.log = log;

        this.Configuration = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        var sceneService = new SceneService(this.Configuration, this.SaveConfig);
        var partnerTrackingService = new PartnerTrackingService(objectTable, sceneService);
        this.overlayService = new OverlayService(this.Configuration, partnerTrackingService, gameGui);
        this.aidenAssistService = new AidenAssistService(this.Configuration);
        this.chatCaptureService = new ChatCaptureService(chatGui, sceneService, log);

        this.mainWindow = new MainWindow(this.Configuration, sceneService, partnerTrackingService, this.aidenAssistService, this.SaveConfig);
        this.settingsWindow = new SettingsWindow(this.Configuration, this.aidenAssistService, this.SaveConfig);
        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.settingsWindow);

        this.commandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand) { HelpMessage = "Open SceneKeeper. Try /sk add <name>, /sk scene <name>, /sk settings." });
        this.commandManager.AddHandler(ShortCommandName, new CommandInfo(this.OnCommand) { HelpMessage = "Open SceneKeeper. Try /sk add <name>, /sk scene <name>, /sk settings." });

        this.pluginInterface.UiBuilder.Draw += this.DrawUi;
        this.pluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
    }

    public Configuration Configuration { get; }

    private void OnCommand(string command, string args)
    {
        args = args.Trim();
        if (string.IsNullOrWhiteSpace(args))
        {
            this.mainWindow.IsOpen = !this.mainWindow.IsOpen;
            return;
        }

        var split = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var verb = split[0].ToLowerInvariant();
        var rest = split.Length > 1 ? split[1] : string.Empty;

        switch (verb)
        {
            case "settings": this.settingsWindow.IsOpen = !this.settingsWindow.IsOpen; break;
            case "add":
                if (this.mainWindow.SceneService.AddPartner(rest)) this.chatGui.Print($"Added scene partner: {rest}", "SceneKeeper");
                else this.chatGui.PrintError("Could not add partner. Name may be empty or already tracked.", "SceneKeeper");
                break;
            case "remove":
                if (this.mainWindow.SceneService.RemovePartner(rest)) this.chatGui.Print($"Removed scene partner: {rest}", "SceneKeeper");
                else this.chatGui.PrintError("Could not remove partner. Name was not found.", "SceneKeeper");
                break;
            case "clear":
                this.mainWindow.SceneService.ClearMessages();
                this.chatGui.Print("Scene log cleared.", "SceneKeeper");
                break;
            case "scene":
                this.mainWindow.SceneService.SetSceneName(rest);
                this.chatGui.Print($"Scene set to: {this.Configuration.CurrentSceneName}", "SceneKeeper");
                break;
            case "pause":
                this.Configuration.IsTrackingPaused = true;
                this.SaveConfig();
                this.chatGui.Print("Scene tracking paused.", "SceneKeeper");
                break;
            case "resume":
                this.Configuration.IsTrackingPaused = false;
                this.SaveConfig();
                this.chatGui.Print("Scene tracking resumed.", "SceneKeeper");
                break;
            default:
                this.mainWindow.IsOpen = !this.mainWindow.IsOpen;
                break;
        }
    }

    private void DrawUi()
    {
        try
        {
            this.windowSystem.Draw();
            this.overlayService.Draw();
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "SceneKeeper UI draw failed.");
        }
    }

    private void OpenMainUi() => this.mainWindow.IsOpen = true;
    private void OpenConfigUi() => this.settingsWindow.IsOpen = true;
    private void SaveConfig() => this.pluginInterface.SavePluginConfig(this.Configuration);

    public void Dispose()
    {
        this.pluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.pluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
        this.commandManager.RemoveHandler(CommandName);
        this.commandManager.RemoveHandler(ShortCommandName);
        this.windowSystem.RemoveAllWindows();
        this.chatCaptureService.Dispose();
        this.aidenAssistService.Dispose();
    }
}
