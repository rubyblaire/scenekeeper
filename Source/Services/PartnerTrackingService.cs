using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace SceneKeeper.Services;

public sealed class PartnerTrackingService
{
    private readonly IObjectTable objectTable;
    private readonly SceneService sceneService;

    public PartnerTrackingService(IObjectTable objectTable, SceneService sceneService)
    {
        this.objectTable = objectTable;
        this.sceneService = sceneService;
    }

    public List<IBattleChara> GetNearbyTrackedPlayers()
    {
        var nearby = new List<IBattleChara>();
        foreach (var player in this.objectTable.PlayerObjects)
        {
            var name = player.Name.TextValue;
            if (this.sceneService.IsTracked(name))
                nearby.Add(player);
        }
        return nearby;
    }

    public string LocalPlayerName => this.objectTable.LocalPlayer?.Name.TextValue ?? string.Empty;

    public bool IsNearby(string partnerName)
    {
        return this.GetNearbyTrackedPlayers()
            .Any(player => NameWorldParser.NamesMatch(player.Name.TextValue, partnerName));
    }

    public SceneKeeper.Models.ScenePartner? GetTrackedPartnerForPlayer(string playerName)
    {
        return this.sceneService.FindTrackedPartner(playerName);
    }
}
