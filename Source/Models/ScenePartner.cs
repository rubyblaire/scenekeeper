namespace SceneKeeper.Models;

[Serializable]
public sealed class ScenePartner
{
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public string DisplayName => string.IsNullOrWhiteSpace(this.Alias) ? this.Name : this.Alias;
}
