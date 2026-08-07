using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Definitions.Rooms;

namespace Services.LL.JsonDefinitions.Dungeons;

public sealed class DungeonDefinitionMaterializer(DungeonCatalogValidator catalogValidator)
{
    public IReadOnlyList<DungeonDefinition> Materialize(DungeonCatalogDocument document)
    {
        catalogValidator.ThrowIfInvalid(document);

        return document.Families
            .SelectMany(MaterializeFamily)
            .ToList();
    }

    private static IEnumerable<DungeonDefinition> MaterializeFamily(DungeonFamilyDefinition family)
    {
        var orderedDifficulties = family.Difficulties
            .OrderBy(x => x.Difficulty)
            .ToList();

        for (var index = 0; index < orderedDifficulties.Count; index++)
        {
            var difficulty = orderedDifficulties[index];
            var previous = index == 0 ? null : orderedDifficulties[index - 1];

            yield return new DungeonDefinition
            {
                Id = difficulty.Id,
                Name = $"{family.Name} {ToRomanNumeral(difficulty.Difficulty)}",
                SigilItemId = family.SigilItemId,
                Region = family.Region,
                Grade = (DungeonGrade)difficulty.Difficulty,
                Tier = difficulty.Difficulty,
                EnemyStrengthMultiplier = difficulty.EnemyStrengthMultiplier,
                RequiredAreaId = family.RequiredAreaId,
                RequiredQuestId = family.RequiredQuestId,
                RequiredPreviousDungeonId = previous?.Id,
                RequiredPreviousDungeonGrade = previous is null ? null : (DungeonGrade)previous.Difficulty,
                EntryCosts = family.EntryCosts.Select(Clone).ToList(),
                RewardTable = Clone(difficulty.RewardTable),
                CompletionRewardTableIds = [$"reward.dungeon.{difficulty.Id}.completion"],
                TierRewardTableIds = [$"reward.dungeon.tier.{difficulty.Difficulty}"],
                MonsterLootModifiers = family.MonsterLootModifiers.ToDictionary(x => x.Key, x => x.Value),
                GatheringNodes = difficulty.GatheringNodes.Select(Clone).ToList(),
                RestSiteCount = family.RestSiteCount,
                MinRooms = difficulty.MinRooms,
                MaxRooms = difficulty.MaxRooms,
                Rooms = family.RoomTemplates.Select(MaterializeRoom).ToList()
            };
        }
    }

    private static RoomDefinition MaterializeRoom(DungeonRoomTemplateDefinition template)
    {
        var encounterIds = template.EncounterIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (template.Type == RoomType.Combat)
            encounterIds = encounterIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (!string.IsNullOrWhiteSpace(template.FeaturedEncounterId))
        {
            var featuredEncounterId = template.FeaturedEncounterId.Trim();
            var featuredIndex = encounterIds.FindIndex(x =>
                x.Equals(featuredEncounterId, StringComparison.OrdinalIgnoreCase));
            if (featuredIndex > 0)
            {
                encounterIds.RemoveAt(featuredIndex);
                encounterIds.Insert(0, featuredEncounterId);
            }
        }

        return new RoomDefinition
        {
            Type = template.Type,
            Weight = template.Weight,
            EncounterIds = encounterIds
        };
    }

    private static DungeonEntryCost Clone(DungeonEntryCost cost) => new()
    {
        ItemId = cost.ItemId,
        Amount = cost.Amount
    };

    private static DungeonRewardTable Clone(DungeonRewardTable table) => new()
    {
        FirstClearRewards = table.FirstClearRewards.Select(Clone).ToList(),
        CompletionRewards = table.CompletionRewards.Select(Clone).ToList(),
        BonusRewards = table.BonusRewards.Select(Clone).ToList()
    };

    private static DungeonRewardGrant Clone(DungeonRewardGrant reward) => new()
    {
        ItemId = reward.ItemId,
        MinAmount = reward.MinAmount,
        MaxAmount = reward.MaxAmount,
        Chance = reward.Chance
    };

    private static DungeonGatheringNodeDefinition Clone(DungeonGatheringNodeDefinition node) => new()
    {
        Id = node.Id,
        Name = node.Name,
        Type = node.Type,
        LevelRequirement = node.LevelRequirement,
        ProcChance = node.ProcChance,
        RewardTableId = node.RewardTableId,
        Loot = node.Loot.Select(Clone).ToList()
    };

    private static DungeonGatheringLootEntryDefinition Clone(DungeonGatheringLootEntryDefinition loot) => new()
    {
        ItemId = loot.ItemId,
        Weight = loot.Weight,
        MinQuantity = loot.MinQuantity,
        MaxQuantity = loot.MaxQuantity,
        IsRare = loot.IsRare
    };

    private static string ToRomanNumeral(int difficulty) => difficulty switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => throw new InvalidOperationException($"Unsupported dungeon difficulty '{difficulty}'.")
    };
}
