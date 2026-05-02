using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SceneKeeper.Services;

public sealed class PlayerSyncLiveSyncService
{
    private static readonly string[] HandledAddressIpcNames =
    {
        "PlayerSync.GetHandledAddresses",
        "MareSynchronos.GetHandledAddresses",
    };

    // These are SceneKeeper bridge endpoint names. They are intentionally optional:
    // current PlayerSync exposes general IPC presence, but not an arbitrary SceneKeeper task payload route.
    // If PlayerSync adds one of these endpoints later, SceneKeeper will use it without rewriting the task system.
    private static readonly string[] SceneKeeperPayloadIpcNames =
    {
        "PlayerSync.SceneKeeper.SendTaskPayload",
        "PlayerSync.SendSceneKeeperTaskPayload",
        "MareSynchronos.SceneKeeper.SendTaskPayload",
        "MareSynchronos.SendSceneKeeperTaskPayload",
    };

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly IPluginLog log;

    public PlayerSyncLiveSyncService(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.log = log;
    }

    public PlayerSyncLiveStatus GetStatus()
    {
        foreach (var name in HandledAddressIpcNames)
        {
            if (this.TryGetHandledAddressCount(name, out var count, out var error))
            {
                return new PlayerSyncLiveStatus(
                    IsPlayerSyncDetected: true,
                    HasSceneKeeperBridge: this.HasSceneKeeperPayloadBridge(out var bridgeName),
                    DetectedIpcName: name,
                    BridgeIpcName: bridgeName,
                    Message: string.IsNullOrWhiteSpace(bridgeName)
                        ? $"PlayerSync IPC detected through {name}. No SceneKeeper task payload bridge was found yet. Manual copy/import remains available."
                        : $"PlayerSync IPC detected through {name}. SceneKeeper task bridge detected through {bridgeName}.",
                    HandledAddressCount: count);
            }

            if (!string.IsNullOrWhiteSpace(error))
                this.log.Debug($"SceneKeeper PlayerSync status check failed for {name}: {error}");
        }

        return new PlayerSyncLiveStatus(
            IsPlayerSyncDetected: false,
            HasSceneKeeperBridge: false,
            DetectedIpcName: string.Empty,
            BridgeIpcName: string.Empty,
            Message: "PlayerSync IPC was not detected. Make sure PlayerSync is installed, enabled, and loaded before trying live task sync.",
            HandledAddressCount: 0);
    }

    public bool TrySendTaskPayload(string payload, out string message)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            message = "No task payload was available to send.";
            return false;
        }

        foreach (var name in SceneKeeperPayloadIpcNames)
        {
            try
            {
                var subscriber = this.pluginInterface.GetIpcSubscriber<string, bool>(name);
                var sent = subscriber.InvokeFunc(payload);
                if (sent)
                {
                    message = $"Sent task update through PlayerSync bridge: {name}.";
                    return true;
                }

                message = $"PlayerSync bridge {name} returned false. The task payload was not sent.";
                return false;
            }
            catch (Exception ex)
            {
                this.log.Debug($"SceneKeeper could not send through {name}: {ex.Message}");
            }
        }

        var status = this.GetStatus();
        message = status.IsPlayerSyncDetected
            ? "PlayerSync is detected, but this PlayerSync build does not expose a SceneKeeper task payload bridge. Use Copy/Import for now, or use a PlayerSync build that exposes PlayerSync.SceneKeeper.SendTaskPayload."
            : "PlayerSync is not detected. Use Copy/Import for now.";
        return false;
    }

    private bool HasSceneKeeperPayloadBridge(out string bridgeName)
    {
        foreach (var name in SceneKeeperPayloadIpcNames)
        {
            try
            {
                // There is no pure 'exists' call, so creating the subscriber is best-effort only.
                // Invoke is still guarded in TrySendTaskPayload.
                _ = this.pluginInterface.GetIpcSubscriber<string, bool>(name);
                bridgeName = name;
                return true;
            }
            catch
            {
                // Missing bridge is expected for current PlayerSync builds.
            }
        }

        bridgeName = string.Empty;
        return false;
    }

    private bool TryGetHandledAddressCount(string ipcName, out int count, out string error)
    {
        count = 0;
        error = string.Empty;
        try
        {
            var subscriber = this.pluginInterface.GetIpcSubscriber<List<nint>>(ipcName);
            var addresses = subscriber.InvokeFunc();
            count = addresses?.Count ?? 0;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

public readonly record struct PlayerSyncLiveStatus(
    bool IsPlayerSyncDetected,
    bool HasSceneKeeperBridge,
    string DetectedIpcName,
    string BridgeIpcName,
    string Message,
    int HandledAddressCount);
