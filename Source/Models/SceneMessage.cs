namespace SceneKeeper.Models;

[Serializable]
public sealed class SceneMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime LocalTime { get; set; } = DateTime.Now;
    public string ChatKind { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public bool FromTrackedPartner { get; set; }

    public string ToPromptLine(bool includeSender)
    {
        var prefix = includeSender && !string.IsNullOrWhiteSpace(this.Sender) ? $"{this.Sender}: " : string.Empty;
        return $"[{this.ChatKind}] {prefix}{this.Body}";
    }
}
