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
    public IReadOnlyList<SceneTask> FollowUpTasks => this.configuration.FollowUpTasks;
    public IReadOnlyList<SceneHistoryEntry> SceneHistory => this.configuration.SceneHistory;
    public IEnumerable<SceneMessage> PinnedMessages => this.messages.Where(m => m.IsPinned);

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

    public void SetSceneTags(string tags)
    {
        this.configuration.CurrentSceneTags = CleanText(tags);
        this.saveConfig();
    }

    public bool AddPartner(string name) => this.AddPartner(name, null);

    public bool AddPartner(string name, string? world)
    {
        var parsed = NameWorldParser.Parse(name, world);
        if (string.IsNullOrWhiteSpace(parsed.Name) || this.IsTracked(parsed.DisplayName))
            return false;

        this.configuration.Partners.Add(new ScenePartner
        {
            Name = parsed.Name,
            World = parsed.World,
        });

        this.RefreshTrackedMessageFlags();
        this.saveConfig();
        return true;
    }

    public bool AddPartnerFromSender(string sender)
    {
        var parsed = NameWorldParser.Parse(sender);
        return this.AddPartner(parsed.Name, parsed.World);
    }

    public bool RemovePartner(string name)
    {
        var parsed = NameWorldParser.Parse(name);
        var removed = this.configuration.Partners.RemoveAll(p => NamesMatch(p.DisplayName, parsed.DisplayName)) > 0;
        if (removed) this.saveConfig();
        return removed;
    }

    public void ClearPartners()
    {
        this.configuration.Partners.Clear();
        this.saveConfig();
    }

    public bool IsTracked(string senderOrName) => this.FindTrackedPartner(senderOrName) is not null;

    public ScenePartner? FindTrackedPartner(string senderOrName)
    {
        var parsed = NameWorldParser.Parse(senderOrName);
        if (string.IsNullOrWhiteSpace(parsed.Name))
            return null;

        return this.configuration.Partners.FirstOrDefault(p =>
            NamesMatch(p.DisplayName, parsed.DisplayName) ||
            NamesMatch(p.Name, parsed.Name) ||
            // Legacy support for older configs that had Alias populated.
            NamesMatch(p.Alias, parsed.DisplayName));
    }

    public void CaptureMessage(string chatKind, string sender, string body)
    {
        if (!this.configuration.EnableChatCapture || this.configuration.IsTrackingPaused || string.IsNullOrWhiteSpace(body))
            return;

        var normalizedKind = chatKind.Trim();
        if (this.configuration.CapturedChatKinds.Count == 0)
            return;

        if (!this.configuration.CapturedChatKinds.Any(k => string.Equals(k, normalizedKind, StringComparison.OrdinalIgnoreCase)))
            return;

        var parsedSender = NameWorldParser.Parse(sender);
        var senderDisplay = parsedSender.DisplayName;
        var item = new SceneMessage
        {
            ChatKind = normalizedKind,
            Sender = string.IsNullOrWhiteSpace(senderDisplay) ? CleanText(sender) : senderDisplay,
            Body = CleanText(body),
            FromTrackedPartner = this.IsTracked(senderDisplay)
        };
        item.Selected = item.FromTrackedPartner;
        this.messages.Add(item);

        var max = Math.Clamp(this.configuration.MaxSceneMessages, 25, 1000);
        while (this.messages.Count > max)
            this.messages.RemoveAt(0);
    }

    public void ClearCapturedChat() => this.messages.Clear();

    // Kept so older call sites or commands still compile if referenced.
    public void ClearMessages() => this.ClearCapturedChat();

    public void StartScene()
    {
        if (string.IsNullOrWhiteSpace(this.configuration.CurrentSceneName))
            this.configuration.CurrentSceneName = "Untitled Scene";

        this.configuration.IsTrackingPaused = false;
        this.saveConfig();
    }

    public void ClearCurrentScene(bool clearPartners)
    {
        this.configuration.CurrentSceneName = "Untitled Scene";
        this.configuration.SceneNotes = string.Empty;
        this.configuration.CurrentSceneTags = string.Empty;
        this.configuration.FollowUpTasks.Clear();
        if (clearPartners)
            this.configuration.Partners.Clear();
        this.messages.Clear();
        this.saveConfig();
    }

    // Kept so older call sites or commands still compile if referenced.
    public void StartNewScene(bool clearPartners) => this.ClearCurrentScene(clearPartners);

    public void SelectAllMessages(bool selected)
    {
        foreach (var message in this.messages)
            message.Selected = selected;
    }

    public bool TogglePinned(Guid id)
    {
        var message = this.messages.FirstOrDefault(m => m.Id == id);
        if (message is null)
            return false;

        message.IsPinned = !message.IsPinned;
        return true;
    }

    public bool AddFollowUpTask(string text)
    {
        text = CleanText(text);
        if (string.IsNullOrWhiteSpace(text))
            return false;

        this.configuration.FollowUpTasks.Add(new SceneTask { Text = text, UpdatedAtUtc = DateTime.UtcNow });
        this.saveConfig();
        return true;
    }

    public bool RemoveFollowUpTask(Guid id)
    {
        var removed = this.configuration.FollowUpTasks.RemoveAll(t => t.Id == id) > 0;
        if (removed)
            this.saveConfig();
        return removed;
    }

    public void SaveAfterExternalTaskMerge()
    {
        this.saveConfig();
    }


    public void ClearCompletedTasks()
    {
        this.configuration.FollowUpTasks.RemoveAll(t => t.IsDone);
        this.saveConfig();
    }

    public SceneHistoryEntry SaveCurrentSceneToHistory()
    {
        var entry = new SceneHistoryEntry
        {
            SceneName = string.IsNullOrWhiteSpace(this.configuration.CurrentSceneName)
                ? "Untitled Scene"
                : this.configuration.CurrentSceneName.Trim(),
            SceneNotes = this.configuration.SceneNotes,
            Tags = this.configuration.CurrentSceneTags,
            Partners = this.configuration.Partners.Select(p => p.Clone()).ToList(),
            Messages = this.messages.Select(m => m.Clone()).ToList(),
            FollowUpTasks = this.configuration.FollowUpTasks.Select(t => t.Clone()).ToList(),
            SavedAtLocal = DateTime.Now,
        };

        this.configuration.SceneHistory.Insert(0, entry);

        var max = Math.Clamp(this.configuration.MaxSceneHistoryEntries, 1, 100);
        while (this.configuration.SceneHistory.Count > max)
            this.configuration.SceneHistory.RemoveAt(this.configuration.SceneHistory.Count - 1);

        this.saveConfig();
        return entry;
    }

    public bool LoadSceneFromHistory(Guid id)
    {
        var entry = this.configuration.SceneHistory.FirstOrDefault(h => h.Id == id);
        if (entry is null)
            return false;

        this.configuration.CurrentSceneName = entry.SceneName;
        this.configuration.SceneNotes = entry.SceneNotes;
        this.configuration.CurrentSceneTags = entry.Tags;
        this.configuration.Partners.Clear();
        this.configuration.Partners.AddRange(entry.Partners.Select(p => p.Clone()));
        this.configuration.FollowUpTasks.Clear();
        this.configuration.FollowUpTasks.AddRange(entry.FollowUpTasks.Select(t => t.Clone()));

        this.messages.Clear();
        this.messages.AddRange(entry.Messages.Select(m => m.Clone()));

        this.saveConfig();
        return true;
    }

    public bool DeleteHistoryEntry(Guid id)
    {
        var removed = this.configuration.SceneHistory.RemoveAll(h => h.Id == id) > 0;
        if (removed)
            this.saveConfig();
        return removed;
    }

    public void ClearHistory()
    {
        this.configuration.SceneHistory.Clear();
        this.saveConfig();
    }

    public string BuildSelectedContext(bool includeSender)
    {
        var selected = this.messages.Where(m => m.Selected).ToList();
        if (selected.Count == 0)
            selected = this.messages.TakeLast(20).ToList();

        return BuildContext(
            this.configuration.CurrentSceneName,
            this.configuration.SceneNotes,
            this.configuration.CurrentSceneTags,
            this.configuration.Partners,
            this.configuration.FollowUpTasks,
            selected,
            includeSender);
    }

    public string BuildHistoryContext(SceneHistoryEntry entry, bool includeSender)
    {
        return BuildContext(
            entry.SceneName,
            entry.SceneNotes,
            entry.Tags,
            entry.Partners,
            entry.FollowUpTasks,
            entry.Messages,
            includeSender);
    }

    public string BuildCurrentMarkdown(bool selectedOnly)
    {
        var exportedMessages = selectedOnly
            ? this.messages.Where(m => m.Selected).ToList()
            : this.messages.ToList();

        if (selectedOnly && exportedMessages.Count == 0)
            exportedMessages = this.messages.ToList();

        return BuildMarkdown(
            this.configuration.CurrentSceneName,
            DateTime.Now,
            this.configuration.SceneNotes,
            this.configuration.CurrentSceneTags,
            this.configuration.Partners,
            this.configuration.FollowUpTasks,
            exportedMessages);
    }

    public string BuildHistoryMarkdown(SceneHistoryEntry entry)
    {
        return BuildMarkdown(
            entry.SceneName,
            entry.SavedAtLocal,
            entry.SceneNotes,
            entry.Tags,
            entry.Partners,
            entry.FollowUpTasks,
            entry.Messages);
    }

    public string BuildCurrentSummary()
    {
        return BuildSummary(
            this.configuration.CurrentSceneName,
            DateTime.Now,
            this.configuration.SceneNotes,
            this.configuration.CurrentSceneTags,
            this.configuration.Partners,
            this.configuration.FollowUpTasks,
            this.messages);
    }

    public string BuildHistorySummary(SceneHistoryEntry entry)
    {
        return BuildSummary(
            entry.SceneName,
            entry.SavedAtLocal,
            entry.SceneNotes,
            entry.Tags,
            entry.Partners,
            entry.FollowUpTasks,
            entry.Messages);
    }

    public bool HistoryEntryMatches(SceneHistoryEntry entry, string query)
    {
        query = CleanText(query);
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return Contains(entry.SceneName, query) ||
               Contains(entry.SceneNotes, query) ||
               Contains(entry.Tags, query) ||
               entry.Partners.Any(p => Contains(p.DisplayName, query) || Contains(p.Name, query) || Contains(p.World, query) || Contains(p.Notes, query) || Contains(p.Relationship, query) || Contains(p.Tags, query)) ||
               entry.FollowUpTasks.Any(t => Contains(t.Text, query)) ||
               entry.Messages.Any(m => Contains(m.Sender, query) || Contains(m.Body, query) || Contains(m.ChatKind, query));
    }

    private static string BuildContext(
        string sceneName,
        string sceneNotes,
        string sceneTags,
        IEnumerable<ScenePartner> partners,
        IEnumerable<SceneTask> tasks,
        IEnumerable<SceneMessage> sceneMessages,
        bool includeSender)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Scene: {sceneName}");

        if (!string.IsNullOrWhiteSpace(sceneTags))
            builder.AppendLine($"Tags: {sceneTags}");

        var partnerList = partners.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        if (partnerList.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Scene Partners:");
            foreach (var partner in partnerList)
            {
                var detail = new List<string>();
                if (!string.IsNullOrWhiteSpace(partner.Relationship)) detail.Add(partner.Relationship.Trim());
                if (!string.IsNullOrWhiteSpace(partner.Tags)) detail.Add(partner.Tags.Trim());
                if (!string.IsNullOrWhiteSpace(partner.Notes)) detail.Add(partner.Notes.Trim());
                builder.AppendLine(detail.Count > 0 ? $"- {partner.DisplayName} ({string.Join("; ", detail)})" : $"- {partner.DisplayName}");
            }
        }

        if (!string.IsNullOrWhiteSpace(sceneNotes))
        {
            builder.AppendLine();
            builder.AppendLine("Scene Notes:");
            builder.AppendLine(sceneNotes.Trim());
        }

        var taskList = tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList();
        if (taskList.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Follow-Up Tasks:");
            foreach (var task in taskList)
                builder.AppendLine($"- [{(task.IsDone ? "x" : " ")}] {task.Text.Trim()}");
        }

        var pinned = sceneMessages.Where(m => m.IsPinned).ToList();
        if (pinned.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Pinned Scene Lines:");
            foreach (var message in pinned)
                builder.AppendLine(message.ToPromptLine(includeSender));
        }

        builder.AppendLine();
        builder.AppendLine("Captured Scene Lines:");
        foreach (var message in sceneMessages)
            builder.AppendLine(message.ToPromptLine(includeSender));

        return builder.ToString().Trim();
    }

    private static string BuildMarkdown(
        string sceneName,
        DateTime savedAt,
        string sceneNotes,
        string sceneTags,
        IEnumerable<ScenePartner> partners,
        IEnumerable<SceneTask> tasks,
        IEnumerable<SceneMessage> sceneMessages)
    {
        var messages = sceneMessages.ToList();
        var pinned = messages.Where(m => m.IsPinned).ToList();
        var builder = new StringBuilder();

        builder.AppendLine($"# {EscapeMarkdown(string.IsNullOrWhiteSpace(sceneName) ? "Untitled Scene" : sceneName.Trim())}");
        builder.AppendLine();
        builder.AppendLine($"**Saved:** {savedAt:yyyy-MM-dd HH:mm}");
        if (!string.IsNullOrWhiteSpace(sceneTags))
            builder.AppendLine($"**Tags:** {EscapeMarkdown(sceneTags.Trim())}");
        builder.AppendLine();

        var partnerList = partners.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
        if (partnerList.Count > 0)
        {
            builder.AppendLine("## Scene Partners");
            builder.AppendLine();
            foreach (var partner in partnerList)
            {
                builder.AppendLine($"- **{EscapeMarkdown(partner.DisplayName)}**");
                if (!string.IsNullOrWhiteSpace(partner.Relationship)) builder.AppendLine($"  - Relationship: {EscapeMarkdown(partner.Relationship)}");
                if (!string.IsNullOrWhiteSpace(partner.Tags)) builder.AppendLine($"  - Tags: {EscapeMarkdown(partner.Tags)}");
                if (!string.IsNullOrWhiteSpace(partner.Notes)) builder.AppendLine($"  - Notes: {EscapeMarkdown(partner.Notes)}");
            }
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(sceneNotes))
        {
            builder.AppendLine("## Scene Notes");
            builder.AppendLine();
            builder.AppendLine(EscapeMarkdown(sceneNotes.Trim()));
            builder.AppendLine();
        }

        var taskList = tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToList();
        if (taskList.Count > 0)
        {
            builder.AppendLine("## Follow-Up Tasks");
            builder.AppendLine();
            foreach (var task in taskList)
                builder.AppendLine($"- [{(task.IsDone ? "x" : " ")}] {EscapeMarkdown(task.Text)}");
            builder.AppendLine();
        }

        if (pinned.Count > 0)
        {
            builder.AppendLine("## Pinned Lines");
            builder.AppendLine();
            foreach (var message in pinned)
                builder.AppendLine(message.ToMarkdownLine(includeSender: true));
            builder.AppendLine();
        }

        builder.AppendLine("## Captured Chat");
        builder.AppendLine();
        if (messages.Count == 0)
        {
            builder.AppendLine("_No captured chat lines._");
        }
        else
        {
            foreach (var message in messages)
                builder.AppendLine(message.ToMarkdownLine(includeSender: true));
        }

        return builder.ToString().Trim();
    }

    private static string BuildSummary(
        string sceneName,
        DateTime savedAt,
        string sceneNotes,
        string sceneTags,
        IEnumerable<ScenePartner> partners,
        IEnumerable<SceneTask> tasks,
        IEnumerable<SceneMessage> sceneMessages)
    {
        var messages = sceneMessages.ToList();
        var pinned = messages.Where(m => m.IsPinned).ToList();
        var openTasks = tasks.Where(t => !t.IsDone && !string.IsNullOrWhiteSpace(t.Text)).ToList();
        var builder = new StringBuilder();

        builder.AppendLine($"Scene: {sceneName}");
        builder.AppendLine($"Date: {savedAt:yyyy-MM-dd HH:mm}");
        if (!string.IsNullOrWhiteSpace(sceneTags)) builder.AppendLine($"Tags: {sceneTags.Trim()}");
        builder.AppendLine($"Partners: {string.Join(", ", partners.Select(p => p.DisplayName).Where(n => !string.IsNullOrWhiteSpace(n)))}");
        builder.AppendLine($"Captured Lines: {messages.Count}");
        builder.AppendLine($"Pinned Lines: {pinned.Count}");
        builder.AppendLine($"Open Follow-Ups: {openTasks.Count}");

        if (!string.IsNullOrWhiteSpace(sceneNotes))
        {
            builder.AppendLine();
            builder.AppendLine("Notes:");
            builder.AppendLine(sceneNotes.Trim());
        }

        if (pinned.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Pinned Lines:");
            foreach (var message in pinned)
                builder.AppendLine(message.ToPromptLine(includeSender: true));
        }

        if (openTasks.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Follow-Ups:");
            foreach (var task in openTasks)
                builder.AppendLine($"- {task.Text.Trim()}");
        }

        return builder.ToString().Trim();
    }


    public void RefreshTrackedMessageFlags()
    {
        foreach (var message in this.messages)
        {
            message.FromTrackedPartner = this.IsTracked(message.Sender);
            if (message.FromTrackedPartner)
                message.Selected = true;
        }
    }

    private static string CleanText(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    private static bool Contains(string? value, string query) => !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);

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

    private static bool NamesMatch(string? left, string? right) => NameWorldParser.NamesMatch(left, right);
}
