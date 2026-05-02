namespace SceneKeeper.Models;

[Serializable]
public sealed class TaskSyncPayload
{
    public string Source { get; set; } = "SceneKeeper";
    public int Version { get; set; } = 1;
    public string SceneCode { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SceneTask> Tasks { get; set; } = new();
}
