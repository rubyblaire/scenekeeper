namespace SceneKeeper.Models;

[Serializable]
public sealed class SceneHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SavedAtLocal { get; set; } = DateTime.Now;
    public string SceneName { get; set; } = "Untitled Scene";
    public string SceneNotes { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public List<ScenePartner> Partners { get; set; } = new();
    public List<SceneMessage> Messages { get; set; } = new();
    public List<SceneTask> FollowUpTasks { get; set; } = new();

    public int PartnerCount => this.Partners.Count;
    public int MessageCount => this.Messages.Count;
    public int PinnedCount => this.Messages.Count(m => m.IsPinned);
    public int TaskCount => this.FollowUpTasks.Count;
    public int OpenTaskCount => this.FollowUpTasks.Count(t => !t.IsDone);
}
