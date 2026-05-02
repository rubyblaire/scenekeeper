using Dalamud.Game.Command;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
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
    private readonly IContextMenu contextMenu;
    private readonly IPluginLog log;
    private readonly WindowSystem windowSystem = new("SceneKeeper");
    private readonly MainWindow mainWindow;
    private readonly SettingsWindow settingsWindow;
    private readonly ChatCaptureService chatCaptureService;
    private readonly OverlayService overlayService;
    private readonly SceneService sceneService;
    private readonly MenuItem addScenePartnerMenuItem;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IObjectTable objectTable,
        IGameGui gameGui,
        ITargetManager targetManager,
        IContextMenu contextMenu,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.contextMenu = contextMenu;
        this.log = log;

        this.Configuration = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.sceneService = new SceneService(this.Configuration, this.SaveConfig);
        var partnerTrackingService = new PartnerTrackingService(objectTable, this.sceneService);
        this.overlayService = new OverlayService(this.Configuration, partnerTrackingService, gameGui);
        this.chatCaptureService = new ChatCaptureService(chatGui, this.sceneService, log);

        this.mainWindow = new MainWindow(this.Configuration, this.sceneService, partnerTrackingService, commandManager, chatGui, targetManager, this.SaveConfig);
        this.settingsWindow = new SettingsWindow(this.Configuration, this.SaveConfig);
        this.addScenePartnerMenuItem = new MenuItem
        {
            Name = new SeStringBuilder().AddText("Add to SceneKeeper").Build(),
            PrefixChar = 'S',
            Priority = -10,
            OnClicked = this.OnAddScenePartnerMenuClicked,
        };
        this.contextMenu.AddMenuItem(ContextMenuType.Default, this.addScenePartnerMenuItem);

        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.settingsWindow);

        this.commandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand) { HelpMessage = "Open SceneKeeper. Try /sk scene <name>, /sk save, /sk new, /sk settings." });
        this.commandManager.AddHandler(ShortCommandName, new CommandInfo(this.OnCommand) { HelpMessage = "Open SceneKeeper. Try /sk scene <name>, /sk save, /sk new, /sk settings." });

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
            case "settings":
                this.settingsWindow.IsOpen = !this.settingsWindow.IsOpen;
                break;
            case "remove":
                if (this.mainWindow.SceneService.RemovePartner(rest)) this.chatGui.Print($"Removed scene partner: {rest}", "SceneKeeper");
                else this.chatGui.PrintError("Could not remove partner. Name was not found.", "SceneKeeper");
                break;
            case "clear":
                this.mainWindow.SceneService.ClearCapturedChat();
                this.chatGui.Print("Captured chat cleared.", "SceneKeeper");
                break;
            case "scene":
                this.mainWindow.SceneService.SetSceneName(rest);
                this.chatGui.Print($"Scene set to: {this.Configuration.CurrentSceneName}", "SceneKeeper");
                break;
            case "save":
                var entry = this.mainWindow.SceneService.SaveCurrentSceneToHistory();
                this.chatGui.Print($"Saved scene to history: {entry.SceneName}", "SceneKeeper");
                break;
            case "start":
                this.mainWindow.SceneService.StartScene();
                this.chatGui.Print($"Started scene: {this.Configuration.CurrentSceneName}", "SceneKeeper");
                break;
            case "new":
                this.mainWindow.SceneService.ClearCurrentScene(clearPartners: true);
                this.chatGui.Print("Cleared the current scene and started a new blank scene.", "SceneKeeper");
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

    private void OnAddScenePartnerMenuClicked(IMenuItemClickedArgs args)
    {
        if (!this.Configuration.EnableTargetContextMenu)
            return;

        if (args.Target is not MenuTargetDefault target)
            return;

        var targetName = target.TargetObject?.Name.TextValue ?? target.TargetName;
        var worldName = string.Empty;
        try
        {
            worldName = target.TargetHomeWorld.Value.Name.ToString();
        }
        catch
        {
            // Some context menu targets do not expose a useful home world.
        }

        if (this.sceneService.AddPartner(targetName, worldName))
            this.chatGui.Print($"Added scene partner: {NameWorldParser.Parse(targetName, worldName).DisplayName}", "SceneKeeper");
        else
            this.chatGui.PrintError("Could not add scene partner. They may already be tracked.", "SceneKeeper");
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
        this.contextMenu.RemoveMenuItem(ContextMenuType.Default, this.addScenePartnerMenuItem);
        this.windowSystem.RemoveAllWindows();
        this.chatCaptureService.Dispose();
    }
}
