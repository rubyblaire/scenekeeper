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
    public bool IsPinned { get; set; }

    public string ToPromptLine(bool includeSender)
    {
        var prefix = includeSender && !string.IsNullOrWhiteSpace(this.Sender) ? $"{this.Sender}: " : string.Empty;
        return $"[{this.ChatKind}] {prefix}{this.Body}";
    }

    public string ToMarkdownLine(bool includeSender)
    {
        var prefix = includeSender && !string.IsNullOrWhiteSpace(this.Sender) ? $"**{EscapeMarkdown(this.Sender)}:** " : string.Empty;
        return $"- `{this.LocalTime:HH:mm:ss}` *{EscapeMarkdown(this.ChatKind)}* {prefix}{EscapeMarkdown(this.Body)}";
    }

    public SceneMessage Clone()
    {
        return new SceneMessage
        {
            Id = Guid.NewGuid(),
            LocalTime = this.LocalTime,
            ChatKind = this.ChatKind,
            Sender = this.Sender,
            Body = this.Body,
            Selected = this.Selected,
            FromTrackedPartner = this.FromTrackedPartner,
            IsPinned = this.IsPinned,
        };
    }

    private static string EscapeMarkdown(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Replace("\\", "\\\\")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("`", "\\`")
            .Replace("#", "\\#")
            .Trim();
    }
}
