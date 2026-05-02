using System.Text.RegularExpressions;
using SceneKeeper.Models;

namespace SceneKeeper.Services;

public sealed class SpellCheckService
{
    private static readonly Regex WordRegex = new("[A-Za-z][A-Za-z']{2,}", RegexOptions.Compiled);

    private static readonly string[] BuiltInWords =
    {
        "aiden", "aetheryte", "alliance", "another", "around", "asked", "beautiful", "because", "before", "between",
        "blade", "blood", "breathe", "breathed", "bright", "brought", "candle", "candlelight", "carefully", "castle",
        "character", "chest", "clear", "close", "closer", "cold", "conversation", "crimson", "dark", "darkness", "delicate",
        "distance", "door", "echo", "emote", "enough", "even", "evening", "every", "eyes", "face", "faint", "familiar",
        "feel", "felt", "figure", "finally", "fingers", "fiona", "floor", "follow", "found", "gaze", "gentle", "glance",
        "glanced", "glass", "gloved", "grief", "hand", "hands", "heart", "heavy", "himself", "herself", "hope", "house",
        "inside", "instead", "journal", "kept", "kind", "knew", "know", "lantern", "light", "little", "look", "looked",
        "lost", "low", "made", "moment", "moved", "near", "never", "night", "nothing", "once", "other", "parlor",
        "partner", "perhaps", "place", "quiet", "quietly", "rain", "rainy", "reach", "remember", "replied", "response",
        "right", "room", "scene", "seemed", "shadow", "shadows", "shook", "silence", "silent", "slow", "slowly",
        "smile", "soft", "softly", "someone", "something", "speak", "spoke", "still", "story", "stood", "strange",
        "table", "through", "toward", "turned", "until", "voice", "walked", "wanted", "warm", "watch", "watched",
        "while", "whisper", "whispered", "window", "with", "without", "world", "would", "writing", "yes", "you", "your"
    };

    public List<SpellCheckIssue> FindIssues(
        IReadOnlyList<string> paragraphs,
        IEnumerable<string> customWords,
        IEnumerable<string> ignoredWords,
        bool skipCapitalizedWords)
    {
        var dictionary = BuildDictionary(customWords);
        var ignored = new HashSet<string>(ignoredWords.Select(NormalizeWord), StringComparer.OrdinalIgnoreCase);
        var issues = new List<SpellCheckIssue>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var paragraphIndex = 0; paragraphIndex < paragraphs.Count; paragraphIndex++)
        {
            var paragraph = paragraphs[paragraphIndex] ?? string.Empty;
            foreach (Match match in WordRegex.Matches(paragraph))
            {
                var word = match.Value.Trim('\'', '’');
                if (string.IsNullOrWhiteSpace(word) || word.Length <= 2)
                    continue;

                if (skipCapitalizedWords && char.IsUpper(word[0]))
                    continue;

                var normalized = NormalizeWord(word);
                if (ignored.Contains(normalized) || dictionary.Contains(normalized))
                    continue;

                var key = $"{paragraphIndex}:{normalized}";
                if (!seen.Add(key))
                    continue;

                issues.Add(new SpellCheckIssue
                {
                    ParagraphIndex = paragraphIndex,
                    Word = word,
                    Suggestions = GetSuggestions(normalized, dictionary).Take(5).ToList(),
                });
            }
        }

        return issues;
    }

    public bool IsKnownWord(string word, IEnumerable<string> customWords, IEnumerable<string> ignoredWords)
    {
        var normalized = NormalizeWord(word);
        return BuildDictionary(customWords).Contains(normalized) || ignoredWords.Any(w => string.Equals(NormalizeWord(w), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> BuildDictionary(IEnumerable<string> customWords)
    {
        var words = new HashSet<string>(BuiltInWords.Select(NormalizeWord), StringComparer.OrdinalIgnoreCase);
        foreach (var word in customWords)
        {
            var normalized = NormalizeWord(word);
            if (!string.IsNullOrWhiteSpace(normalized))
                words.Add(normalized);
        }
        return words;
    }

    private static IEnumerable<string> GetSuggestions(string word, HashSet<string> dictionary)
    {
        return dictionary
            .Select(candidate => new { Word = candidate, Distance = EditDistance(word, candidate) })
            .Where(x => x.Distance <= 2 || (word.Length > 6 && x.Distance <= 3))
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Word.Length)
            .ThenBy(x => x.Word)
            .Select(x => x.Word);
    }

    private static string NormalizeWord(string value)
    {
        return new string((value ?? string.Empty).Trim().ToLowerInvariant().Where(c => char.IsLetter(c) || c == '\'').ToArray());
    }

    private static int EditDistance(string source, string target)
    {
        if (source == target) return 0;
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        var d = new int[source.Length + 1, target.Length + 1];
        for (var i = 0; i <= source.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= target.Length; j++) d[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[source.Length, target.Length];
    }
}
