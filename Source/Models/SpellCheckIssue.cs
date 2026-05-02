namespace SceneKeeper.Models;

public sealed class SpellCheckIssue
{
    public int ParagraphIndex { get; set; }
    public string Word { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
}
