using System.Text;
using SceneKeeper.Models;

namespace SceneKeeper.Services;

public sealed class SceneService
{
    private readonly Configuration configuration;
    private readonly Action saveConfig;
    private readonly List<SceneMessage> messages = new();

    public SceneService(Configuration configuration, Action saveConfig)
    {
        this.configuration = configuration;
        this.saveConfig = saveConfig;
    }

    public IReadOnlyList<SceneMessage> Messages => this.messages;
    public IReadOnlyList<ScenePartner> Partners => this.configuration.Partners;

    public void SetSceneName(string name)
    {
        this.configuration.CurrentSceneName = string.IsNullOrWhiteSpace(name) ? "Untitled Scene" : name.Trim();
        this.saveConfig();
    }

    public void SetSceneNotes(string notes)
    {
        this.configuration.SceneNotes = notes;
        this.saveConfig();
    }

    public bool AddPartner(string name)
    {
        name = NormalizeName(name);
        if (string.IsNullOrWhiteSpace(name) || this.IsTracked(name))
            return false;
        this.configuration.Partners.Add(new ScenePartner { Name = name });
        this.saveConfig();
        return true;
    }

    public bool RemovePartner(string name)
    {
        name = NormalizeName(name);
        var removed = this.configuration.Partners.RemoveAll(p => NamesMatch(p.Name, name)) > 0;
        if (removed) this.saveConfig();
        return removed;
    }

    public void ClearPartners()
    {
        this.configuration.Partners.Clear();
        this.saveConfig();
    }

    public bool IsTracked(string senderOrName)
    {
        senderOrName = NormalizeName(senderOrName);
        return this.configuration.Partners.Any(p => NamesMatch(p.Name, senderOrName) || NamesMatch(p.Alias, senderOrName));
    }

    public void CaptureMessage(string chatKind, string sender, string body)
    {
        if (!this.configuration.EnableChatCapture || this.configuration.IsTrackingPaused || string.IsNullOrWhiteSpace(body))
            return;

        var normalizedKind = chatKind.Trim();
        if (this.configuration.CapturedChatKinds.Count > 0 &&
            !this.configuration.CapturedChatKinds.Any(k => string.Equals(k, normalizedKind, StringComparison.OrdinalIgnoreCase)))
            return;

        var item = new SceneMessage
        {
            ChatKind = normalizedKind,
            Sender = CleanText(sender),
            Body = CleanText(body),
            FromTrackedPartner = this.IsTracked(sender)
        };
        item.Selected = item.FromTrackedPartner;
        this.messages.Add(item);

        var max = Math.Clamp(this.configuration.MaxSceneMessages, 25, 1000);
        while (this.messages.Count > max)
            this.messages.RemoveAt(0);
    }

    public void ClearMessages() => this.messages.Clear();

    public void SelectAllMessages(bool selected)
    {
        foreach (var message in this.messages)
            message.Selected = selected;
    }

    public string BuildSelectedContext(bool includeSender)
    {
        var selected = this.messages.Where(m => m.Selected).ToList();
        if (selected.Count == 0)
            selected = this.messages.TakeLast(20).ToList();

        var builder = new StringBuilder();
        builder.AppendLine($"Scene: {this.configuration.CurrentSceneName}");
        if (!string.IsNullOrWhiteSpace(this.configuration.SceneNotes))
        {
            builder.AppendLine();
            builder.AppendLine("Scene Notes:");
            builder.AppendLine(this.configuration.SceneNotes.Trim());
        }

        builder.AppendLine();
        builder.AppendLine("Selected Scene Lines:");
        foreach (var message in selected)
            builder.AppendLine(message.ToPromptLine(includeSender));

        return builder.ToString().Trim();
    }

    private static string CleanText(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    private static string NormalizeName(string value) => CleanText(value).Replace("\u00a0", " ");

    private static bool NamesMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        var l = NormalizeName(left);
        var r = NormalizeName(right);
        return string.Equals(l, r, StringComparison.OrdinalIgnoreCase) ||
               l.StartsWith(r + "@", StringComparison.OrdinalIgnoreCase) ||
               r.StartsWith(l + "@", StringComparison.OrdinalIgnoreCase);
    }
}
