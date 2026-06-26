namespace Domain.Models.Achievements;

public static class TitleDisplayFormatter
{
    public static string Format(string characterName, string titleName, TitleDisplayPosition position)
    {
        var normalizedCharacterName = Normalize(characterName, "Character");
        var normalizedTitleName = Normalize(titleName, "Title");

        return position == TitleDisplayPosition.Prefix
            ? $"{normalizedTitleName} {normalizedCharacterName}"
            : $"{normalizedCharacterName}, the {normalizedTitleName}";
    }

    private static string Normalize(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
}
