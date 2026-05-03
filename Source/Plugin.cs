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
    private readonly IObjectTable objectTable;
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
        this.objectTable = objectTable;
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
            Priority = 10_000,
            OnClicked = this.OnAddScenePartnerMenuClicked,
        };
        this.contextMenu.OnMenuOpened += this.OnContextMenuOpened;

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


    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        try
        {
            if (!this.Configuration.EnableTargetContextMenu)
                return;

            // Character select and other pre-login screens can expose default context menus
            // before the game object table/local player state is safe to query. Do not add
            // SceneKeeper's player action there.
            if (this.objectTable.LocalPlayer is null)
                return;

            if (args.MenuType != ContextMenuType.Default)
                return;

            if (args.Target is not MenuTargetDefault target)
                return;

            var addonName = args.AddonName ?? string.Empty;
            if (this.IsBlockedContextMenuAddon(addonName))
                return;

            var targetName = this.GetSafeContextTargetName(target);
            if (string.IsNullOrWhiteSpace(targetName))
                return;

            if (!this.IsSupportedSceneKeeperTarget(target, targetName, addonName))
                return;

            // Avoid offering "Add to SceneKeeper" on yourself. This also prevents the
            // reported CTD path when right-clicking your own character from character select.
            var localName = this.objectTable.LocalPlayer?.Name.TextValue;
            if (NameWorldParser.NamesMatch(targetName, localName))
                return;

            args.AddMenuItem(this.addScenePartnerMenuItem);
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "SceneKeeper failed while preparing context menu item.");
        }
    }

    private string GetSafeContextTargetName(MenuTargetDefault target)
    {
        try
        {
            var objectName = target.TargetObject?.Name.TextValue;
            if (!string.IsNullOrWhiteSpace(objectName))
                return objectName;
        }
        catch
        {
            // TargetObject can be unavailable on some menu sources. Fall back safely.
        }

        return target.TargetName ?? string.Empty;
    }

    private bool IsSupportedSceneKeeperTarget(MenuTargetDefault target, string targetName, string addonName)
    {
        try
        {
            var targetObject = target.TargetObject;
            if (targetObject is not null)
            {
                var objectKind = targetObject.ObjectKind.ToString();

                // Only live player objects should get SceneKeeper's context menu entry.
                // This prevents minions, mounts, pets, inventory-like objects, retainers, etc.
                // from receiving "Add to SceneKeeper" when they expose Default menus.
                return objectKind.Equals("Player", StringComparison.OrdinalIgnoreCase)
                    || objectKind.Equals("Pc", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // If the object payload is not safe to inspect, fall back to character/chat checks below.
        }

        // No live game object means this must come from a known chat/social addon.
        // Do not trust CharacterData or home world by themselves here: non-player UI
        // windows such as the Minion Guide can still leak Default target payloads.
        if (!this.IsAllowedNoObjectContextMenuAddon(addonName))
            return false;

        try
        {
            // Some chat/social/player lists provide a CharacterData payload even when
            // there is no live object. Only accept it after the addon allow-list check.
            if (target.TargetCharacter is not null)
                return true;
        }
        catch
        {
            // Not every default context menu exposes character data.
        }

        try
        {
            var homeWorld = target.TargetHomeWorld.Value.Name.ToString();
            return !string.IsNullOrWhiteSpace(homeWorld)
                && !NameWorldParser.NamesMatch(targetName, this.objectTable.LocalPlayer?.Name.TextValue);
        }
        catch
        {
            return false;
        }
    }

    private bool IsBlockedContextMenuAddon(string addonName)
    {
        if (string.IsNullOrWhiteSpace(addonName))
            return false;

        string[] blockedTerms =
        [
            "Minion",
            "Mount",
            "Companion",
            "Buddy",
            "Pet",
            "Ornament",
            "Inventory",
            "Armoury",
            "Retainer",
            "Item",
            "Action",
            "Recipe",
            "Gathering",
            "Fishing",
            "Cabinet",
            "Mirage",
            "CharaCard",
        ];

        return blockedTerms.Any(term => addonName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAllowedNoObjectContextMenuAddon(string addonName)
    {
        if (string.IsNullOrWhiteSpace(addonName))
            return false;

        string[] allowedTerms =
        [
            "ChatLog",
            "Social",
            "Friend",
            "Party",
            "Linkshell",
            "LinkShell",
            "CrossWorldLinkshell",
            "FreeCompany",
            "Contact",
            "PlayerSearch",
            "LookingForGroup",
            "BlackList",
            "MuteList",
            "Fellowship",
        ];

        return allowedTerms.Any(term => addonName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private void OnAddScenePartnerMenuClicked(IMenuItemClickedArgs args)
    {
        try
        {
            if (!this.Configuration.EnableTargetContextMenu)
                return;

            if (this.objectTable.LocalPlayer is null)
                return;

            if (args.Target is not MenuTargetDefault target)
                return;

            var addonName = args.AddonName ?? string.Empty;
            if (this.IsBlockedContextMenuAddon(addonName))
                return;

            var targetName = this.GetSafeContextTargetName(target);
            if (string.IsNullOrWhiteSpace(targetName))
                return;

            if (!this.IsSupportedSceneKeeperTarget(target, targetName, addonName))
                return;

            var localName = this.objectTable.LocalPlayer?.Name.TextValue;
            if (NameWorldParser.NamesMatch(targetName, localName))
            {
                this.chatGui.PrintError("SceneKeeper does not add your own character as a scene partner.", "SceneKeeper");
                return;
            }

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
        catch (Exception ex)
        {
            this.log.Error(ex, "SceneKeeper failed while adding context-menu scene partner.");
            this.chatGui.PrintError("SceneKeeper could not add that context-menu target safely.", "SceneKeeper");
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
        this.contextMenu.OnMenuOpened -= this.OnContextMenuOpened;
        this.windowSystem.RemoveAllWindows();
        this.chatCaptureService.Dispose();
    }
}
