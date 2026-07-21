using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;

namespace Services.LL.JsonDefinitions.Dungeons;

public sealed class DungeonCatalogValidator
{
    private const int SupportedSchemaVersion = 3;

    public IReadOnlyList<string> Validate(DungeonCatalogDocument document)
    {
        var errors = new List<string>();

        if (document.SchemaVersion != SupportedSchemaVersion)
            errors.Add($"Unsupported dungeon catalog schemaVersion '{document.SchemaVersion}'. Expected '{SupportedSchemaVersion}'.");

        if (document.Families.Count == 0)
            errors.Add("At least one dungeon family is required.");

        AddDuplicateErrors(document.Families.Select(x => x.Id), "dungeon family", errors);

        var difficultyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in document.Families)
        {
            ValidateFamily(family, difficultyIds, errors);
        }

        return errors;
    }

    public void ThrowIfInvalid(DungeonCatalogDocument document)
    {
        var errors = Validate(document);
        if (errors.Count > 0)
            throw new InvalidOperationException("Dungeon catalog validation failed: " + string.Join(" | ", errors));
    }

    private static void ValidateFamily(
        DungeonFamilyDefinition family,
        HashSet<string> difficultyIds,
        List<string> errors)
    {
        var familyLabel = string.IsNullOrWhiteSpace(family.Id) ? "<missing family id>" : family.Id;

        if (string.IsNullOrWhiteSpace(family.Id))
            errors.Add("Dungeon family id is required.");

        if (string.IsNullOrWhiteSpace(family.Name))
            errors.Add($"{familyLabel}: name is required.");

        if (string.IsNullOrWhiteSpace(family.SigilItemId))
            errors.Add($"{familyLabel}: sigilItemId is required.");

        if (family.EntryCosts.Count == 0)
            errors.Add($"{familyLabel}: at least one entry cost is required.");

        if (family.RoomTemplates.Count == 0)
            errors.Add($"{familyLabel}: at least one room template is required.");

        if (family.Difficulties.Count == 0)
            errors.Add($"{familyLabel}: at least one difficulty is required.");

        if (family.RestSiteCount < 0)
            errors.Add($"{familyLabel}: restSiteCount must be specified and cannot be negative.");

        AddDuplicateErrors(
            family.RoomTemplates.Select(x => x.Id),
            $"room template in family '{familyLabel}'",
            errors);

        foreach (var room in family.RoomTemplates)
        {
            ValidateRoom(familyLabel, room, errors);
        }

        var orderedDifficulties = family.Difficulties.OrderBy(x => x.Difficulty).ToList();
        AddDuplicateDifficultyNumberErrors(familyLabel, orderedDifficulties, errors);

        for (var index = 0; index < orderedDifficulties.Count; index++)
        {
            var difficulty = orderedDifficulties[index];
            var difficultyLabel = string.IsNullOrWhiteSpace(difficulty.Id) ? "<missing difficulty id>" : difficulty.Id;

            if (difficulty.Difficulty != index + 1)
                errors.Add($"{familyLabel}: difficulties must be contiguous and start at 1; found '{difficulty.Difficulty}' at position '{index + 1}'.");

            if (!Enum.IsDefined(typeof(DungeonGrade), difficulty.Difficulty))
                errors.Add($"{difficultyLabel}: difficulty '{difficulty.Difficulty}' does not map to a supported dungeon grade.");

            if (string.IsNullOrWhiteSpace(difficulty.Id))
            {
                errors.Add($"{familyLabel}: difficulty id is required.");
            }
            else
            {
                if (!difficultyIds.Add(difficulty.Id))
                    errors.Add($"Duplicate dungeon difficulty id '{difficulty.Id}'.");

                var expectedFamilyId = DungeonDefinitionIdentity.GetFamilyId(difficulty.Id);
                if (!expectedFamilyId.Equals(family.Id, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{difficultyLabel}: id does not belong to family '{familyLabel}'.");
            }

            if (difficulty.RecommendedCombatRating < 0)
                errors.Add($"{difficultyLabel}: recommendedCombatRating cannot be negative.");

            if (difficulty.MinRooms <= 0)
                errors.Add($"{difficultyLabel}: minRooms must be greater than zero.");

            if (difficulty.MaxRooms < difficulty.MinRooms)
                errors.Add($"{difficultyLabel}: maxRooms must be greater than or equal to minRooms.");
        }
    }

    private static void ValidateRoom(
        string familyId,
        DungeonRoomTemplateDefinition room,
        List<string> errors)
    {
        var roomLabel = string.IsNullOrWhiteSpace(room.Id) ? "<missing room template id>" : room.Id;

        if (string.IsNullOrWhiteSpace(room.Id))
            errors.Add($"{familyId}: room template id is required.");

        if (room.Weight < 0)
            errors.Add($"{familyId}/{roomLabel}: weight cannot be negative.");

        if (room.Type is not (RoomType.Combat or RoomType.MiniBoss or RoomType.Boss))
            return;

        var encounterIds = room.EncounterIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (encounterIds.Count == 0)
            errors.Add($"{familyId}/{roomLabel}: at least one encounter id is required.");

        if (room.Type == RoomType.Combat)
            AddDuplicateErrors(encounterIds, $"encounter in room '{familyId}/{roomLabel}'", errors);

        if (room.Type is RoomType.MiniBoss or RoomType.Boss)
        {
            if (string.IsNullOrWhiteSpace(room.FeaturedEncounterId))
            {
                errors.Add($"{familyId}/{roomLabel}: featuredEncounterId is required for {room.Type} rooms.");
            }
            else if (!encounterIds.Contains(room.FeaturedEncounterId.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"{familyId}/{roomLabel}: featuredEncounterId '{room.FeaturedEncounterId}' is not in encounterIds.");
            }
        }
    }

    private static void AddDuplicateDifficultyNumberErrors(
        string familyId,
        IReadOnlyCollection<DungeonDifficultyDefinition> difficulties,
        List<string> errors)
    {
        errors.AddRange(difficulties
            .GroupBy(x => x.Difficulty)
            .Where(x => x.Count() > 1)
            .Select(x => $"{familyId}: duplicate difficulty number '{x.Key}'."));
    }

    private static void AddDuplicateErrors(
        IEnumerable<string> values,
        string label,
        List<string> errors)
    {
        errors.AddRange(values
            .GroupBy(x => x?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => string.IsNullOrWhiteSpace(x.Key)
                ? $"A {label} id is required."
                : $"Duplicate {label} id '{x.Key}'."));
    }
}
