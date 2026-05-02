using System.Text.Json;
using SceneKeeper.Models;

namespace SceneKeeper.Services;

public sealed class PlayerSyncTaskSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string BuildPayload(string sceneCode, string sceneName, string updatedBy, IEnumerable<SceneTask> tasks)
    {
        var payload = new TaskSyncPayload
        {
            SceneCode = Clean(sceneCode),
            SceneName = Clean(sceneName),
            UpdatedBy = Clean(updatedBy),
            ExportedAtUtc = DateTime.UtcNow,
            Tasks = tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Clone()).ToList(),
        };

        foreach (var task in payload.Tasks)
        {
            task.IsShared = true;
            if (string.IsNullOrWhiteSpace(task.UpdatedBy))
                task.UpdatedBy = payload.UpdatedBy;
            if (task.UpdatedAtUtc == default)
                task.UpdatedAtUtc = DateTime.UtcNow;
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public bool TryImportPayload(
        string json,
        string expectedSceneCode,
        bool requireMatchingSceneCode,
        IList<SceneTask> targetTasks,
        out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
        {
            message = "No PlayerSync task payload was provided.";
            return false;
        }

        TaskSyncPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TaskSyncPayload>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            message = $"Could not read PlayerSync task payload: {ex.Message}";
            return false;
        }

        if (payload is null || !string.Equals(payload.Source, "SceneKeeper", StringComparison.OrdinalIgnoreCase))
        {
            message = "That payload does not look like a SceneKeeper task sync payload.";
            return false;
        }

        var expected = Clean(expectedSceneCode);
        var incoming = Clean(payload.SceneCode);
        if (requireMatchingSceneCode && !string.IsNullOrWhiteSpace(expected) && !string.Equals(expected, incoming, StringComparison.OrdinalIgnoreCase))
        {
            message = "Incoming task payload did not match this scene's sync code.";
            return false;
        }

        var added = 0;
        var updated = 0;
        foreach (var incomingTask in payload.Tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)))
        {
            if (incomingTask.Id == Guid.Empty)
                incomingTask.Id = Guid.NewGuid();

            incomingTask.IsShared = true;
            if (incomingTask.UpdatedAtUtc == default)
                incomingTask.UpdatedAtUtc = payload.ExportedAtUtc == default ? DateTime.UtcNow : payload.ExportedAtUtc;
            if (string.IsNullOrWhiteSpace(incomingTask.UpdatedBy))
                incomingTask.UpdatedBy = payload.UpdatedBy;

            var existing = targetTasks.FirstOrDefault(t => t.Id == incomingTask.Id);
            if (existing is null)
            {
                targetTasks.Add(incomingTask.Clone());
                added++;
                continue;
            }

            if (incomingTask.UpdatedAtUtc >= existing.UpdatedAtUtc)
            {
                existing.Text = incomingTask.Text;
                existing.IsDone = incomingTask.IsDone;
                existing.IsShared = true;
                existing.UpdatedAtUtc = incomingTask.UpdatedAtUtc;
                existing.UpdatedBy = incomingTask.UpdatedBy;
                updated++;
            }
        }

        message = $"Imported PlayerSync tasks. Added: {added}, updated: {updated}.";
        return true;
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
