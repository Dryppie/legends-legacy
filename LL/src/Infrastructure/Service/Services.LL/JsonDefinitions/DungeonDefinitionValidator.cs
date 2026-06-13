using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;

namespace Services.LL.JsonDefinitions;

public sealed class DungeonDefinitionValidator : IDungeonDefinitionValidator
{
    public IReadOnlyList<string> Validate(IReadOnlyList<DungeonDefinition> definitions)
    {
        var errors = new List<string>();
        var byId = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        errors.AddRange(definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => string.IsNullOrWhiteSpace(x.Key) || x.Count() > 1)
            .Select(x => string.IsNullOrWhiteSpace(x.Key)
                ? "Dungeon id is required."
                : $"Duplicate dungeon id '{x.Key}'."));

        foreach (var dungeon in definitions)
        {
            ValidateDefinition(dungeon, byId, errors);
        }

        ValidateGradeProgression(definitions, errors);

        return errors;
    }

    public void ThrowIfInvalid(IReadOnlyList<DungeonDefinition> definitions)
    {
        var errors = Validate(definitions);
        if (errors.Count > 0)
            throw new InvalidOperationException("Dungeon definition validation failed: " + string.Join(" | ", errors));
    }

    private static void ValidateDefinition(
        DungeonDefinition dungeon,
        IReadOnlyDictionary<string, DungeonDefinition> definitionsById,
        List<string> errors)
    {
        var label = string.IsNullOrWhiteSpace(dungeon.Id) ? "<missing id>" : dungeon.Id;

        if (string.IsNullOrWhiteSpace(dungeon.Name))
            errors.Add($"{label}: name is required.");

        if (string.IsNullOrWhiteSpace(dungeon.SigilItemId))
            errors.Add($"{label}: sigilItemId is required.");

        if (dungeon.RecommendedCombatRating < 0)
            errors.Add($"{label}: recommendedCombatRating cannot be negative.");

        if (dungeon.MinRooms <= 0)
            errors.Add($"{label}: minRooms must be greater than zero.");

        if (dungeon.MaxRooms < dungeon.MinRooms)
            errors.Add($"{label}: maxRooms must be greater than or equal to minRooms.");

        if (!string.IsNullOrWhiteSpace(dungeon.RequiredPreviousDungeonId)
            && !definitionsById.ContainsKey(dungeon.RequiredPreviousDungeonId))
        {
            errors.Add($"{label}: requiredPreviousDungeonId '{dungeon.RequiredPreviousDungeonId}' does not exist.");
        }

        if (dungeon.Rooms.Count == 0)
            errors.Add($"{label}: at least one room definition is required.");

        if (dungeon.Rooms.All(x => x.Type != RoomType.Boss))
            errors.Add($"{label}: at least one Boss room definition is required.");

        foreach (var room in dungeon.Rooms)
        {
            ValidateRoom(label, room, errors);
        }

        foreach (var cost in dungeon.EntryCosts)
        {
            if (string.IsNullOrWhiteSpace(cost.ItemId))
                errors.Add($"{label}: entry cost itemId is required.");

            if (cost.Amount <= 0)
                errors.Add($"{label}: entry cost '{cost.ItemId}' amount must be greater than zero.");
        }

        ValidateRewards(label, "completionRewards", dungeon.RewardTable.CompletionRewards, errors);
        ValidateRewards(label, "bonusRewards", dungeon.RewardTable.BonusRewards, errors);
        ValidateRewards(label, "firstClearRewards", dungeon.RewardTable.FirstClearRewards, errors);
    }

    private static void ValidateRoom(string dungeonId, RoomDefinition room, List<string> errors)
    {
        if (room.Weight < 0)
            errors.Add($"{dungeonId}: room '{room.Type}' weight cannot be negative.");

        if (room.Type is RoomType.Combat or RoomType.MiniBoss or RoomType.Boss)
        {
            var encounterIds = room.EncounterIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (encounterIds.Count == 0)
                errors.Add($"{dungeonId}: room '{room.Type}' requires at least one encounter id.");

            var duplicates = encounterIds
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);

            errors.AddRange(duplicates.Select(id => $"{dungeonId}: room '{room.Type}' has duplicate encounter id '{id}'."));
        }
    }

    private static void ValidateRewards(
        string dungeonId,
        string rewardSection,
        IReadOnlyCollection<DungeonRewardGrant> rewards,
        List<string> errors)
    {
        foreach (var reward in rewards)
        {
            if (string.IsNullOrWhiteSpace(reward.ItemId))
                errors.Add($"{dungeonId}: {rewardSection} itemId is required.");

            if (reward.MinAmount <= 0)
                errors.Add($"{dungeonId}: {rewardSection} '{reward.ItemId}' minAmount must be greater than zero.");

            if (reward.MaxAmount < reward.MinAmount)
                errors.Add($"{dungeonId}: {rewardSection} '{reward.ItemId}' maxAmount must be greater than or equal to minAmount.");

            if (reward.Chance is < 0 or > 1)
                errors.Add($"{dungeonId}: {rewardSection} '{reward.ItemId}' chance must be between 0 and 1.");
        }
    }

    private static void ValidateGradeProgression(IReadOnlyList<DungeonDefinition> definitions, List<string> errors)
    {
        foreach (var family in definitions.GroupBy(x => DungeonDefinitionIdentity.GetFamilyId(x.Id ?? string.Empty), StringComparer.OrdinalIgnoreCase))
        {
            var ordered = family.OrderBy(x => (int)x.Grade).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var previous = ordered[i - 1];
                var current = ordered[i];

                if (current.RecommendedCombatRating < previous.RecommendedCombatRating)
                    errors.Add($"{current.Id}: recommendedCombatRating must not be lower than previous grade '{previous.Id}'.");

                if (string.IsNullOrWhiteSpace(current.RequiredPreviousDungeonId))
                    continue;

                if (!current.RequiredPreviousDungeonId.Equals(previous.Id, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{current.Id}: requiredPreviousDungeonId should point to previous grade '{previous.Id}'.");
            }
        }
    }

}
