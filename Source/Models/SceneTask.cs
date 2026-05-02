namespace SceneKeeper.Models;

[Serializable]
public sealed class SceneTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtLocal { get; set; } = DateTime.Now;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public bool IsShared { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public SceneTask Clone()
    {
        return new SceneTask
        {
            Id = this.Id,
            CreatedAtLocal = this.CreatedAtLocal,
            UpdatedAtUtc = this.UpdatedAtUtc,
            Text = this.Text,
            IsDone = this.IsDone,
            IsShared = this.IsShared,
            UpdatedBy = this.UpdatedBy,
        };
    }
}
