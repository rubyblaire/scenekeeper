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
        var markerColor = ImGui.GetColorU32(new Vector4(0.78f, 0.64f, 0.40f, 1.0f));
        var markerFill = ImGui.GetColorU32(new Vector4(0.78f, 0.64f, 0.40f, 0.22f));
        var highlightColor = ImGui.GetColorU32(new Vector4(0.78f, 0.64f, 0.40f, 0.72f));
        var highlightFill = ImGui.GetColorU32(new Vector4(0.78f, 0.64f, 0.40f, 0.08f));
        var shadow = ImGui.GetColorU32(new Vector4(0.02f, 0.02f, 0.02f, 0.86f));

        var markerRadius = Math.Clamp(this.configuration.MarkerRadius, 6.0f, 30.0f);
        var highlightRadius = Math.Clamp(this.configuration.HighlightRadius, 18.0f, 90.0f);

        foreach (var player in this.partnerTrackingService.GetNearbyTrackedPlayers())
        {
            var worldPos = player.Position + new Vector3(0, this.configuration.MarkerVerticalOffset, 0);
            if (!this.gameGui.WorldToScreen(worldPos, out var screenPos))
                continue;

            var center = new Vector2(screenPos.X, screenPos.Y);

            if (this.configuration.EnablePartnerHighlight)
            {
                drawList.AddCircleFilled(center, highlightRadius, highlightFill, 48);
                drawList.AddCircle(center, highlightRadius, highlightColor, 48, 2.5f);
                drawList.AddCircle(center, highlightRadius + 5.0f, shadow, 48, 1.0f);
            }

            drawList.AddCircleFilled(center, markerRadius + 2.0f, shadow, 32);
            drawList.AddCircleFilled(center, markerRadius, markerFill, 32);
            drawList.AddCircle(center, markerRadius, markerColor, 32, 2.0f);

            var label = $"✦ {player.Name.TextValue}";
            var size = ImGui.CalcTextSize(label);
            var pos = new Vector2(center.X - size.X / 2f, center.Y - markerRadius - size.Y - 8f);
            var padding = new Vector2(7, 4);

            drawList.AddRectFilled(pos - padding, pos + size + padding, ImGui.GetColorU32(new Vector4(0.02f, 0.02f, 0.02f, 0.68f)), 6.0f);
            drawList.AddText(pos + new Vector2(1, 1), shadow, label);
            drawList.AddText(pos, markerColor, label);
        }
    }
}
