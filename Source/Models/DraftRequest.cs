namespace SceneKeeper.Models;

public sealed class DraftRequest
{
    public string Model { get; init; } = "gpt-5.1";
    public string CharacterContext { get; init; } = string.Empty;
    public string SceneContext { get; init; } = string.Empty;
    public string Intent { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
}
