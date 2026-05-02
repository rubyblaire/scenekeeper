namespace SceneKeeper.Services;

public readonly record struct ParsedCharacterName(string Name, string World)
{
    public string DisplayName => string.IsNullOrWhiteSpace(this.World) ? this.Name : $"{this.Name}@{this.World}";
}

public static class NameWorldParser
{
    private static readonly string[] KnownWorlds =
    {
        "Adamantoise", "Cactuar", "Faerie", "Gilgamesh", "Jenova", "Midgardsormr", "Sargatanas", "Siren",
        "Balmung", "Brynhildr", "Coeurl", "Diabolos", "Goblin", "Malboro", "Mateus", "Zalera",
        "Behemoth", "Excalibur", "Exodus", "Famfrit", "Hyperion", "Lamia", "Leviathan", "Ultros",
        "Halicarnassus", "Maduin", "Marilith", "Seraph", "Cuchulainn", "Golem", "Kraken", "Rafflesia",
        "Ravana", "Bismarck", "Sephirot", "Sophia", "Zurvan",
        "Cerberus", "Louisoix", "Moogle", "Omega", "Phantom", "Ragnarok", "Sagittarius", "Spriggan",
        "Alpha", "Lich", "Odin", "Phoenix", "Raiden", "Shiva", "Twintania", "Zodiark",
        "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Ramuh", "Tonberry", "Typhon",
        "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima", "Valefor", "Yojimbo", "Zeromus"
    };

    public static ParsedCharacterName Parse(string rawName, string? explicitWorld = null)
    {
        var value = Clean(rawName);
        var world = Clean(explicitWorld ?? string.Empty);

        if (string.IsNullOrWhiteSpace(value))
            return new ParsedCharacterName(string.Empty, world);

        value = value.Replace("", string.Empty).Replace("", string.Empty).Trim();

        // Prefer explicit @World format if present.
        var atIndex = value.LastIndexOf('@');
        if (atIndex > 0 && atIndex < value.Length - 1)
        {
            var namePart = Clean(value[..atIndex]);
            var worldPart = Clean(value[(atIndex + 1)..]);
            return new ParsedCharacterName(namePart, string.IsNullOrWhiteSpace(world) ? worldPart : world);
        }

        // Dalamud chat sender TextValue can sometimes flatten "Firstname Lastname" + "World" into "Firstname LastnameWorld".
        // If a known world is glued to the end, split it back out so nearby object matching still works.
        foreach (var knownWorld in KnownWorlds.OrderByDescending(w => w.Length))
        {
            if (value.Length <= knownWorld.Length)
                continue;

            if (!value.EndsWith(knownWorld, StringComparison.OrdinalIgnoreCase))
                continue;

            var possibleName = Clean(value[..^knownWorld.Length]);
            if (possibleName.Contains(' ', StringComparison.Ordinal) && possibleName.Length >= 3)
                return new ParsedCharacterName(possibleName, string.IsNullOrWhiteSpace(world) ? knownWorld : world);
        }

        return new ParsedCharacterName(value, world);
    }

    public static bool NamesMatch(string? leftName, string? rightName)
    {
        var left = Parse(leftName ?? string.Empty);
        var right = Parse(rightName ?? string.Empty);

        if (string.IsNullOrWhiteSpace(left.Name) || string.IsNullOrWhiteSpace(right.Name))
            return false;

        if (!string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(left.World) || string.IsNullOrWhiteSpace(right.World))
            return true;

        return string.Equals(left.World, right.World, StringComparison.OrdinalIgnoreCase);
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Replace("\u00a0", " ");
}
