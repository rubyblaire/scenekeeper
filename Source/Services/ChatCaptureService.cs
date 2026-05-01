using Dalamud.Game.Chat;
using Dalamud.Plugin.Services;

namespace SceneKeeper.Services;

public sealed class ChatCaptureService : IDisposable
{
    private readonly IChatGui chatGui;
    private readonly SceneService sceneService;
    private readonly IPluginLog log;

    public ChatCaptureService(IChatGui chatGui, SceneService sceneService, IPluginLog log)
    {
        this.chatGui = chatGui;
        this.sceneService = sceneService;
        this.log = log;
        this.chatGui.ChatMessage += this.OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        try
        {
            this.sceneService.CaptureMessage(
                message.LogKind.ToString(),
                message.Sender.TextValue,
                message.Message.TextValue);
        }
        catch (Exception ex)
        {
            this.log.Warning(ex, "SceneKeeper failed to capture a chat message.");
        }
    }

    public void Dispose()
    {
        this.chatGui.ChatMessage -= this.OnChatMessage;
    }
}
