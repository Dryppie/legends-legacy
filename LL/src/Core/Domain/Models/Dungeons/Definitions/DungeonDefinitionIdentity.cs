namespace Domain.Models.Dungeons.Definitions;

public static class DungeonDefinitionIdentity
{
    public static string GetFamilyId(string dungeonId)
    {
        foreach (var suffix in new[] { "_iii", "_ii", "_i" })
        {
            if (dungeonId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return dungeonId[..^suffix.Length];
        }

        return dungeonId;
    }

    public static string GetFamilyTitle(string dungeonName)
    {
        foreach (var suffix in new[] { " III", " II", " I" })
        {
            if (dungeonName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return dungeonName[..^suffix.Length];
        }

        return dungeonName;
    }
}
