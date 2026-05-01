using System.Numerics;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;

namespace SceneKeeper.Services;

public sealed class OverlayService
{
    private readonly Configuration configuration;
    private readonly PartnerTrackingService partnerTrackingService;
    private readonly IGameGui gameGui;

    public OverlayService(Configuration configuration, PartnerTrackingService partnerTrackingService, IGameGui gameGui)
    {
        this.configuration = configuration;
        this.partnerTrackingService = partnerTrackingService;
        this.gameGui = gameGui;
    }

    public void Draw()
    {
        if (!this.configuration.EnableOverlayMarkers || this.configuration.IsTrackingPaused || this.gameGui.GameUiHidden)
            return;

        var drawList = ImGui.GetForegroundDrawList();
        var color = ImGui.GetColorU32(new Vector4(0.78f, 0.64f, 0.40f, 1.0f));
        var shadow = ImGui.GetColorU32(new Vector4(0.02f, 0.02f, 0.02f, 0.85f));

        foreach (var player in this.partnerTrackingService.GetNearbyTrackedPlayers())
        {
            var worldPos = player.Position + new Vector3(0, this.configuration.MarkerVerticalOffset, 0);
            if (!this.gameGui.WorldToScreen(worldPos, out var screenPos))
                continue;

            var label = $"✦ {player.Name.TextValue}";
            var size = ImGui.CalcTextSize(label);
            var pos = new Vector2(screenPos.X - size.X / 2f, screenPos.Y - size.Y - 8f);
            drawList.AddText(pos + new Vector2(1, 1), shadow, label);
            drawList.AddText(pos, color, label);
        }
    }
}
