namespace SceneKeeper.Models;

public sealed class DraftResult
{
    public bool Success { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}
