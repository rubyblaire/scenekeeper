namespace SceneKeeper.Models;

[Serializable]
public sealed class ScenePartner
{
    public string Name { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;

    // Kept for config migration from older builds. Public UI now uses Notes instead.
    public string Alias { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }

    public bool UseCustomColor { get; set; }
    public float ColorRed { get; set; } = 0.78f;
    public float ColorGreen { get; set; } = 0.64f;
    public float ColorBlue { get; set; } = 0.40f;

    public string DisplayName => string.IsNullOrWhiteSpace(this.World) ? this.Name : $"{this.Name}@{this.World}";

    public ScenePartner Clone()
    {
        return new ScenePartner
        {
            Name = this.Name,
            World = this.World,
            Alias = this.Alias,
            Notes = this.Notes,
            Relationship = this.Relationship,
            Tags = this.Tags,
            IsFavorite = this.IsFavorite,
            UseCustomColor = this.UseCustomColor,
            ColorRed = this.ColorRed,
            ColorGreen = this.ColorGreen,
            ColorBlue = this.ColorBlue,
        };
    }
}
