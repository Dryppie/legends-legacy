using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Engine;

namespace Services.LL.Regions;

public sealed class RegionOneContentDiagnostics : IRegionOneContentDiagnostics
{
    private const string RetiredGoblinMinesIdleAreaId = "region_01_area_05";
    private readonly ICreatureRepository _creatures;
    private readonly IAreaRepository _areas;
    private readonly IItemBaseRepository _itemBases;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IAbilityCatalogBehaviorDiagnostics _behaviorDiagnostics;
    private readonly IDungeonDefinitions _dungeonDefinitions;

    public RegionOneContentDiagnostics(
        ICreatureRepository creatures,
        IAreaRepository areas,
        IItemBaseRepository itemBases,
        IEssenceDefinitionRepository essenceDefinitions,
        IAbilityCatalogProvider catalogProvider,
        IAbilityCatalogBehaviorDiagnostics behaviorDiagnostics,
        IDungeonDefinitions dungeonDefinitions)
    {
        _creatures = creatures;
        _areas = areas;
        _itemBases = itemBases;
        _essenceDefinitions = essenceDefinitions;
        _catalogProvider = catalogProvider;
        _behaviorDiagnostics = behaviorDiagnostics;
        _dungeonDefinitions = dungeonDefinitions;
    }

    public async Task<RegionOneContentDiagnosticReport> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var manifest = BuildManifest();
        var catalog = _catalogProvider.GetCatalog();
        var behaviorReport = _behaviorDiagnostics.Analyze();
        var coveredAbilityIds = behaviorReport.Scenarios
            .Select(x => x.AbilityId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var essenceItemIdSet = (await _itemBases.GetEssenceItemBaseIdsByDefinitionIdAsync(cancellationToken))
            .Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var creatures = (await _creatures.GetCreaturesAsync(cancellationToken))
            .Select(creature => new CreatureDiagnosticData(creature.Id, creature.Name, creature.ImagePath))
            .ToList();
        var creaturesByKey = creatures.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var areas = await _areas.GetAreasWithCreaturesAsync(cancellationToken);
        var staleAreaCount = await _areas.CountByIdAsync(RetiredGoblinMinesIdleAreaId, cancellationToken);
        var dungeons = _dungeonDefinitions.GetAll();
        var entries = new List<RegionOneContentEntryDiagnostic>(manifest.Count);

        foreach (var entry in manifest)
        {
            creaturesByKey.TryGetValue(entry.CreatureKey, out var creature);
            var essence = _essenceDefinitions.GetByMonsterId($"monster.{entry.CreatureKey}");
            var activeAbilityResolved = !string.IsNullOrWhiteSpace(essence?.ActiveAbilityId)
                && catalog.AbilitiesById.ContainsKey(essence.ActiveAbilityId);
            var passiveAbilityResolved = !string.IsNullOrWhiteSpace(essence?.PassiveAbilityId)
                && catalog.AbilitiesById.ContainsKey(essence.PassiveAbilityId);
            var behaviorCovered = activeAbilityResolved
                && passiveAbilityResolved
                && coveredAbilityIds.Contains(essence!.ActiveAbilityId)
                && coveredAbilityIds.Contains(essence.PassiveAbilityId);
            var sourcePlacementResolved = IsSourcePlacementResolved(entry, creature, areas, dungeons);
            var essenceItemResolved = essence is not null && essenceItemIdSet.Contains(essence.Id);
            var missing = BuildMissingList(
                creature is not null,
                essence is not null,
                activeAbilityResolved,
                passiveAbilityResolved,
                essenceItemResolved,
                sourcePlacementResolved,
                behaviorCovered,
                entry.RequiresMana);
            var isComplete = missing.Count == 0;

            entries.Add(new RegionOneContentEntryDiagnostic(
                entry.Name,
                entry.CreatureKey,
                entry.SourceType,
                entry.SourceName,
                entry.ExpectedTier,
                essence?.Id,
                essence?.ActiveAbilityId,
                essence?.PassiveAbilityId,
                entry.RequiresMana,
                creature is not null,
                essence is not null,
                activeAbilityResolved,
                passiveAbilityResolved,
                essenceItemResolved,
                sourcePlacementResolved,
                behaviorCovered,
                isComplete,
                missing));
        }

        var completeCount = entries.Count(x => x.IsComplete);
        var warnings = BuildWarnings(staleAreaCount, entries);

        return new RegionOneContentDiagnosticReport(
            entries.Count,
            completeCount,
            entries.Count - completeCount,
            entries.Count(x => x.RequiresMana),
            staleAreaCount,
            completeCount == entries.Count && staleAreaCount == 0,
            entries,
            warnings);
    }

    private static bool IsSourcePlacementResolved(
        RegionOneManifestEntry entry,
        CreatureDiagnosticData? creature,
        IReadOnlyList<Domain.Models.Regions.Areas.Area> areas,
        IReadOnlyList<Domain.Models.Dungeons.DungeonDefinition> dungeons)
    {
        if (entry.SourceType.Equals("Idle Area", StringComparison.OrdinalIgnoreCase))
        {
            return creature is not null
                && areas.Any(area =>
                    area.Name.Equals(entry.SourceName, StringComparison.OrdinalIgnoreCase)
                    && area.Creatures.Any(areaCreature => areaCreature.CreatureId == creature.Id));
        }

        if (entry.SourceType.Equals("Dungeon", StringComparison.OrdinalIgnoreCase))
        {
            return dungeons.Any(dungeon =>
                dungeon.Name.StartsWith(entry.SourceName, StringComparison.OrdinalIgnoreCase)
                && dungeon.Rooms.Any(room =>
                    room.EncounterIds.Any(id => id.Equals(entry.CreatureKey, StringComparison.OrdinalIgnoreCase))));
        }

        return false;
    }

    private static List<string> BuildMissingList(
        bool creatureResolved,
        bool essenceResolved,
        bool activeAbilityResolved,
        bool passiveAbilityResolved,
        bool essenceItemResolved,
        bool sourcePlacementResolved,
        bool behaviorCovered,
        bool requiresMana)
    {
        var missing = new List<string>();
        if (!creatureResolved) missing.Add("creature/entity data");
        if (!essenceResolved) missing.Add("essence definition");
        if (!activeAbilityResolved) missing.Add("active ability");
        if (!passiveAbilityResolved) missing.Add("passive ability");
        if (!essenceItemResolved) missing.Add("item essence base");
        if (!sourcePlacementResolved) missing.Add("source placement");
        if (!behaviorCovered) missing.Add("behavior test");
        if (requiresMana) missing.Add("mana runtime");
        return missing;
    }

    private static IReadOnlyList<string> BuildWarnings(
        int staleAreaCount,
        IReadOnlyList<RegionOneContentEntryDiagnostic> entries)
    {
        var warnings = new List<string>
        {
            "Hive's Abyss is intentionally hidden from dungeon availability and reserved for the future Rift model."
        };

        if (staleAreaCount > 0)
            warnings.Add("Retired idle Goblin Mines area still exists locally; restart/API seed cleanup should remove region_01_area_05 when possible.");

        var awaitingManaCount = entries.Count(x => x.RequiresMana);
        if (awaitingManaCount > 0)
            warnings.Add($"{awaitingManaCount} manifest entries reference MP or mana behavior and are marked awaiting mana runtime.");

        return warnings;
    }

    private static IReadOnlyList<RegionOneManifestEntry> BuildManifest() =>
    [
        new("Large Rat", "large_rat", "Idle Area", "Lumo Ruins", "T1", false),
        new("Goblin", "goblin", "Idle Area", "Lumo Ruins", "T1", false),
        new("Goblin Archer", "goblin_archer", "Idle Area", "Lumo Ruins", "T1", false),
        new("Goblin Warrior", "goblin_warrior", "Idle Area", "Lumo Ruins", "T1", false),
        new("Flame Imp", "flame_imp", "Idle Area", "Blood Grove", "T1", true),
        new("Frost Imp", "frost_imp", "Idle Area", "Blood Grove", "T1", true),
        new("Shadow Imp", "shadow_imp", "Idle Area", "Blood Grove", "T1", false),
        new("Vampire Bat", "vampire_bat", "Idle Area", "Blood Grove", "T1", false),
        new("Blue Slime", "blue_slime", "Idle Area", "Crystal Creek", "T1", true),
        new("Brown Slime", "brown_slime", "Idle Area", "Crystal Creek", "T1", false),
        new("Green Slime", "green_slime", "Idle Area", "Crystal Creek", "T1", false),
        new("Rainbow Slime", "rainbow_slime", "Idle Area", "Crystal Creek", "T1", false),
        new("Red Slime", "red_slime", "Idle Area", "Crystal Creek", "T1", false),
        new("Transparent Slime", "transparent_slime", "Idle Area", "Crystal Creek", "T1", false),
        new("Moss Lizard", "moss_lizard", "Idle Area", "Oak Thicket", "T1", false),
        new("Spider", "spider", "Idle Area", "Oak Thicket", "T1", true),
        new("Treant Sapling", "treant_sapling", "Idle Area", "Oak Thicket", "T1", false),
        new("Venomous Snake", "venomous_snake", "Idle Area", "Oak Thicket", "T1", true),
        new("Viper", "viper", "Idle Area", "Oak Thicket", "T1", false),
        new("Enchanted Fairy", "enchanted_fairy", "Idle Area", "Twilight Clearing", "T1", false),
        new("Glade Panther", "glade_panther", "Idle Area", "Twilight Clearing", "T1", false),
        new("Illusion Fox", "illusion_fox", "Idle Area", "Twilight Clearing", "T1", false),
        new("Nightshade Blossom", "nightshade_blossom", "Idle Area", "Twilight Clearing", "T1", false),
        new("Pixie", "pixie", "Idle Area", "Twilight Clearing", "T1", false),
        new("Giant Spider", "giant_spider", "Idle Area", "Old Forest", "T1", false),
        new("Venomous Spiderling", "venomous_spiderling", "Idle Area", "Old Forest", "T1", false),
        new("Blackjaw Spider", "blackjaw_spider", "Idle Area", "Old Forest", "T1", false),
        new("Raven", "raven", "Idle Area", "Old Forest", "T1", false),
        new("Widow Stalker", "widow_stalker", "Idle Area", "Old Forest", "T1", false),
        new("Scarecrow", "scarecrow", "Idle Area", "Bleak Orchard", "T1", false),
        new("Lost Soul", "lost_soul", "Idle Area", "Bleak Orchard", "T1", false),
        new("Apparition", "apparition", "Idle Area", "Bleak Orchard", "T1", false),
        new("Specter", "specter", "Idle Area", "Bleak Orchard", "T1", false),
        new("Zombie", "zombie", "Idle Area", "Rotting Hamlet", "T1", false),
        new("Half Zombie", "half_zombie", "Idle Area", "Rotting Hamlet", "T1", false),
        new("Undead", "undead", "Idle Area", "Rotting Hamlet", "T1", false),
        new("Blood Zombie", "blood_zombie", "Idle Area", "Rotting Hamlet", "T1", false),
        new("Giant Worm", "giant_worm", "Idle Area", "Wormburrow Depths", "T1", false),
        new("Burrowed Horror", "burrowed_horror", "Idle Area", "Wormburrow Depths", "T1", false),
        new("Cave Leech", "cave_leech", "Idle Area", "Wormburrow Depths", "T1", false),
        new("Stonejaw Grub", "stonejaw_grub", "Idle Area", "Wormburrow Depths", "T1", false),
        new("Deep Burrower", "deep_burrower", "Idle Area", "Wormburrow Depths", "T1", false),
        new("Feral Ghoul", "feral_ghoul", "Idle Area", "Forgotten Ruins", "T1", false),
        new("Plague Ghoul", "plague_ghoul", "Idle Area", "Forgotten Ruins", "T1", false),
        new("Ravenous Ghoul", "ravenous_ghoul", "Idle Area", "Forgotten Ruins", "T1", false),
        new("Skeleton Archer", "skeleton_archer", "Idle Area", "Forgotten Ruins", "T1", false),
        new("Skeleton Mage", "skeleton_mage", "Idle Area", "Forgotten Ruins", "T1", false),
        new("Skeleton Warrior", "skeleton_warrior", "Idle Area", "Forgotten Ruins", "T1", false),
        new("Goblin", "goblin", "Dungeon", "Goblin Mines", "T1", false),
        new("Goblin Archer", "goblin_archer", "Dungeon", "Goblin Mines", "T1", false),
        new("Goblin Warrior", "goblin_warrior", "Dungeon", "Goblin Mines", "T1", false),
        new("Goblin Shaman", "goblin_shaman", "Dungeon", "Goblin Mines", "T1", false),
        new("Hobgoblin", "hobgoblin", "Dungeon", "Goblin Mines", "T2", false),
        new("Skeleton", "skeleton", "Dungeon", "Forgotten Catacombs", "T1", true),
        new("Poisonous Rat", "poisonous_rat", "Dungeon", "Forgotten Catacombs", "T1", false),
        new("Cave Bat", "cave_bat", "Dungeon", "Forgotten Catacombs", "T1", false),
        new("Giant Bat", "giant_bat", "Dungeon", "Forgotten Catacombs", "T1", false),
        new("Necroshade Wraith", "necroshade_wraith", "Dungeon", "Forgotten Catacombs", "T2", false),
        new("Ant Worker", "ant_worker", "Rift", "Hive's Abyss", "T1", true),
        new("Fire Ant", "fire_ant", "Rift", "Hive's Abyss", "T1", false),
        new("Queen's Guard Ant", "queens_guard_ant", "Rift", "Hive's Abyss", "T1", false),
        new("Ant Queen", "ant_queen", "Rift", "Hive's Abyss", "T2", false),
        new("Ant King", "ant_king", "Rift", "Hive's Abyss", "T2", false),
        new("Forest Spirit", "forest_spirit", "Future Dungeon", "The Great Tree", "T2", false),
        new("Wood Nymph", "wood_nymph", "Future Dungeon", "The Great Tree", "T2", false),
        new("Giant Spider", "giant_spider", "Future Dungeon", "Tangled Cave", "T2", false),
        new("Venomous Spiderling", "venomous_spiderling", "Future Dungeon", "Tangled Cave", "T2", false)
    ];

    private sealed record RegionOneManifestEntry(
        string Name,
        string CreatureKey,
        string SourceType,
        string SourceName,
        string ExpectedTier,
        bool RequiresMana);

    private sealed record CreatureDiagnosticData(Guid Id, string Name, string Key);
}
