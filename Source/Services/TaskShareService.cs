using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SceneKeeper.Models;

namespace SceneKeeper.Services;

public sealed class TaskShareService
{
    public const string SharePrefix = "SKTASK:1:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions LegacyJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string BuildPayload(string sceneCode, string sceneName, string updatedBy, IEnumerable<SceneTask> tasks)
    {
        var payload = this.BuildPayloadObject(sceneCode, sceneName, updatedBy, tasks);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return SharePrefix + ToBase64Url(Compress(json));
    }

    public bool TryImportPayload(
        string shareText,
        string expectedSceneCode,
        bool requireMatchingSceneCode,
        IList<SceneTask> targetTasks,
        out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(shareText))
        {
            message = "No task share string was provided.";
            return false;
        }

        TaskSyncPayload? payload;
        try
        {
            payload = this.ReadPayload(shareText.Trim());
        }
        catch (Exception ex)
        {
            message = $"Could not read task share string: {ex.Message}";
            return false;
        }

        if (payload is null || !string.Equals(payload.Source, "SceneKeeper", StringComparison.OrdinalIgnoreCase))
        {
            message = "That does not look like a SceneKeeper task share string.";
            return false;
        }

        if (payload.Version > 1)
        {
            message = $"This task share string uses a newer format version ({payload.Version}) than this SceneKeeper build supports.";
            return false;
        }

        var expected = Clean(expectedSceneCode);
        var incoming = Clean(payload.SceneCode);
        if (requireMatchingSceneCode && !string.IsNullOrWhiteSpace(expected) && !string.Equals(expected, incoming, StringComparison.OrdinalIgnoreCase))
        {
            message = "Incoming task share string did not match this scene's share code.";
            return false;
        }

        var added = 0;
        var updated = 0;
        var skippedOlder = 0;
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
            else
            {
                skippedOlder++;
            }
        }

        message = $"Imported task share string. Added: {added}, updated: {updated}, skipped older: {skippedOlder}.";
        return true;
    }

    private TaskSyncPayload BuildPayloadObject(string sceneCode, string sceneName, string updatedBy, IEnumerable<SceneTask> tasks)
    {
        var payload = new TaskSyncPayload
        {
            Source = "SceneKeeper",
            Version = 1,
            SceneCode = Clean(sceneCode),
            SceneName = Clean(sceneName),
            UpdatedBy = Clean(updatedBy),
            ExportedAtUtc = DateTime.UtcNow,
            Tasks = tasks.Where(t => !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Clone()).ToList(),
        };

        foreach (var task in payload.Tasks)
        {
            task.IsShared = true;
            if (task.Id == Guid.Empty)
                task.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(task.UpdatedBy))
                task.UpdatedBy = payload.UpdatedBy;
            if (task.UpdatedAtUtc == default)
                task.UpdatedAtUtc = DateTime.UtcNow;
        }

        return payload;
    }

    private TaskSyncPayload? ReadPayload(string shareText)
    {
        if (shareText.StartsWith(SharePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var encoded = shareText[SharePrefix.Length..].Trim();
            var json = Decompress(FromBase64Url(encoded));
            return JsonSerializer.Deserialize<TaskSyncPayload>(json, JsonOptions);
        }

        // Backward compatibility for older SceneKeeper builds that copied raw JSON payloads.
        return JsonSerializer.Deserialize<TaskSyncPayload>(shareText, LegacyJsonOptions);
    }

    private static byte[] Compress(string text)
    {
        var input = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(input, 0, input.Length);
        }

        return output.ToArray();
    }

    private static string Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            0 => string.Empty,
            _ => throw new FormatException("Invalid Base64 task share string."),
        };

        return Convert.FromBase64String(padded);
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
