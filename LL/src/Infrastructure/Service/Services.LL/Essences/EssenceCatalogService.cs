using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using Domain.Models.Items;
using Services.LL.Combat.Engine;

namespace Services.LL.Essences;

public sealed class EssenceCatalogService : IEssenceCatalogService
{
    private readonly IItemBaseRepository _itemBases;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IAbilityCatalogProvider _catalogProvider;

    public EssenceCatalogService(
        IItemBaseRepository itemBases,
        IEssenceDefinitionRepository essenceDefinitions,
        IAbilityCatalogProvider catalogProvider)
    {
        _itemBases = itemBases;
        _essenceDefinitions = essenceDefinitions;
        _catalogProvider = catalogProvider;
    }

    public async Task<EssenceCatalogReport> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var catalog = _catalogProvider.GetCatalog();
        var itemIdByEssenceId = await _itemBases.GetEssenceItemBaseIdsByDefinitionIdAsync(cancellationToken);
        var essenceByMonsterId = _essenceDefinitions.GetAll()
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceMonsterId))
            .GroupBy(x => x.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.First(),
                StringComparer.OrdinalIgnoreCase);

        var areas = BuildRegionOneSources()
            .GroupBy(x => new { x.AreaId, x.AreaName, x.SourceType, x.Tier })
            .Select(group => new EssenceCatalogArea(
                group.Key.AreaId,
                group.Key.AreaName,
                group.Key.SourceType,
                group.Key.Tier,
                group
                    .Select(entry => BuildMonster(entry, essenceByMonsterId, itemIdByEssenceId, catalog))
                    .OrderBy(x => x.Name)
                    .ToList()))
            .OrderBy(x => GetSourceSortOrder(x.SourceType))
            .ThenBy(x => GetAreaSortOrder(x.Id))
            .ThenBy(x => x.Name)
            .ToList();

        return new EssenceCatalogReport(
        [
            new EssenceCatalogRegion("region_01", "Shenic", areas)
        ]);
    }

    private static EssenceCatalogMonster BuildMonster(
        EssenceCatalogSourceEntry entry,
        IReadOnlyDictionary<string, EssenceDefinition> essenceByMonsterId,
        IReadOnlyDictionary<string, string> itemIdByEssenceId,
        AbilityCatalog catalog)
    {
        var monsterId = $"monster.{entry.CreatureKey}";
        essenceByMonsterId.TryGetValue(monsterId, out var essence);

        return new EssenceCatalogMonster(
            monsterId,
            entry.MonsterName,
            entry.CreatureKey,
            entry.SourceType,
            entry.AreaName,
            entry.Tier,
            essence is null
                ? null
                : BuildEssence(essence, itemIdByEssenceId, catalog));
    }

    private static EssenceCatalogEssence BuildEssence(
        EssenceDefinition essence,
        IReadOnlyDictionary<string, string> itemIdByEssenceId,
        AbilityCatalog catalog)
    {
        itemIdByEssenceId.TryGetValue(essence.Id, out var itemId);
        catalog.AbilitiesById.TryGetValue(essence.ActiveAbilityId, out var activeAbility);
        catalog.AbilitiesById.TryGetValue(essence.PassiveAbilityId, out var passiveAbility);

        return new EssenceCatalogEssence(
            essence.Id,
            essence.Name,
            essence.Description,
            essence.Rarity.ToString(),
            itemId,
            essence.Tags,
            essence.AttributeBonuses
                .Select(x => new EssenceCatalogAttributeBonus(x.Attribute.ToString(), x.BaseValue))
                .ToList(),
            new EssenceCatalogDrop(
                essence.Drop.BaseDropChance,
                essence.Drop.ResonanceGainPerFailedEligibleKill,
                essence.Drop.DropChanceBonusPerResonance,
                essence.Drop.MaxResonanceBonus),
            activeAbility is null ? null : BuildAbility(activeAbility),
            passiveAbility is null ? null : BuildAbility(passiveAbility));
    }

    private static EssenceCatalogAbility BuildAbility(AbilitySpec ability) =>
        new(
            ability.Id,
            ability.Name,
            ability.Kind.ToString(),
            ability.Description,
            ability.CooldownTicks,
            ability.Tags,
            ability.Triggers.Select(BuildTrigger).ToList(),
            ability.Effects.Select(BuildEffect).ToList());

    private static EssenceCatalogTrigger BuildTrigger(AbilityTriggerSpec trigger) =>
        new(
            trigger.Event.ToString(),
            trigger.InternalCooldownTicks,
            trigger.EffectIds,
            trigger.Conditions.Select(BuildCondition).ToList());

    private static EssenceCatalogEffect BuildEffect(AbilityEffectSpec effect) =>
        new(
            effect.Id,
            effect.Operation.ToString(),
            effect.Target.ToString(),
            effect.BaseValue,
            effect.ScalingAttribute?.ToString(),
            effect.ScalingCoefficient,
            effect.Attribute?.ToString(),
            effect.StatusId,
            effect.SummonId,
            effect.Resource.ToString(),
            effect.DurationTicks,
            effect.IntervalTicks,
            effect.Uses,
            effect.AttackType.ToString(),
            effect.DamageType.ToString(),
            effect.LifeStealPercentage,
            effect.Tags,
            effect.Conditions.Select(BuildCondition).ToList());

    private static EssenceCatalogCondition BuildCondition(AbilityConditionSpec condition) =>
        new(
            condition.Type.ToString(),
            condition.Subject.ToString(),
            condition.StatusId,
            condition.Tag,
            condition.Value);

    private static int GetSourceSortOrder(string sourceType) =>
        sourceType switch
        {
            "Idle Area" => 0,
            "Dungeon" => 1,
            "Rift" => 2,
            "Future Dungeon" => 3,
            _ => 4
        };

    private static int GetAreaSortOrder(string areaId) =>
        areaId switch
        {
            "region_01_area_01" => 1,
            "region_01_area_02" => 2,
            "region_01_area_03" => 3,
            "region_01_area_04" => 4,
            "region_01_area_06" => 5,
            "region_01_area_08" => 6,
            "region_01_area_09" => 7,
            "region_01_area_10" => 8,
            "region_01_area_11" => 9,
            "region_01_area_07" => 10,
            _ => int.MaxValue
        };

    private static IReadOnlyList<EssenceCatalogSourceEntry> BuildRegionOneSources() =>
    [
        new("region_01_area_01", "Lumo Ruins", "Idle Area", "T1", "Large Rat", "large_rat"),
        new("region_01_area_01", "Lumo Ruins", "Idle Area", "T1", "Goblin", "goblin"),
        new("region_01_area_01", "Lumo Ruins", "Idle Area", "T1", "Goblin Archer", "goblin_archer"),
        new("region_01_area_01", "Lumo Ruins", "Idle Area", "T1", "Goblin Warrior", "goblin_warrior"),
        new("region_01_area_02", "Blood Grove", "Idle Area", "T1", "Flame Imp", "flame_imp"),
        new("region_01_area_02", "Blood Grove", "Idle Area", "T1", "Frost Imp", "frost_imp"),
        new("region_01_area_02", "Blood Grove", "Idle Area", "T1", "Shadow Imp", "shadow_imp"),
        new("region_01_area_02", "Blood Grove", "Idle Area", "T1", "Vampire Bat", "vampire_bat"),
        new("region_01_area_03", "Crystal Creek", "Idle Area", "T1", "Blue Slime", "blue_slime"),
        new("region_01_area_03", "Crystal Creek", "Idle Area", "T1", "Brown Slime", "brown_slime"),
        new("region_01_area_03", "Crystal Creek", "Idle Area", "T1", "Green Slime", "green_slime"),
        new("region_01_area_03", "Crystal Creek", "Idle Area", "T1", "Rainbow Slime", "rainbow_slime"),
        new("region_01_area_03", "Crystal Creek", "Idle Area", "T1", "Red Slime", "red_slime"),
        new("region_01_area_03", "Crystal Creek", "Idle Area", "T1", "Transparent Slime", "transparent_slime"),
        new("region_01_area_04", "Twilight Clearing", "Idle Area", "T1", "Enchanted Fairy", "enchanted_fairy"),
        new("region_01_area_04", "Twilight Clearing", "Idle Area", "T1", "Glade Panther", "glade_panther"),
        new("region_01_area_04", "Twilight Clearing", "Idle Area", "T1", "Illusion Fox", "illusion_fox"),
        new("region_01_area_04", "Twilight Clearing", "Idle Area", "T1", "Nightshade Blossom", "nightshade_blossom"),
        new("region_01_area_04", "Twilight Clearing", "Idle Area", "T1", "Pixie", "pixie"),
        new("region_01_area_06", "Oak Thicket", "Idle Area", "T1", "Moss Lizard", "moss_lizard"),
        new("region_01_area_06", "Oak Thicket", "Idle Area", "T1", "Spider", "spider"),
        new("region_01_area_06", "Oak Thicket", "Idle Area", "T1", "Treant Sapling", "treant_sapling"),
        new("region_01_area_06", "Oak Thicket", "Idle Area", "T1", "Venomous Snake", "venomous_snake"),
        new("region_01_area_06", "Oak Thicket", "Idle Area", "T1", "Viper", "viper"),
        new("region_01_area_08", "Old Forest", "Idle Area", "T1", "Giant Spider", "giant_spider"),
        new("region_01_area_08", "Old Forest", "Idle Area", "T1", "Venomous Spiderling", "venomous_spiderling"),
        new("region_01_area_08", "Old Forest", "Idle Area", "T1", "Blackjaw Spider", "blackjaw_spider"),
        new("region_01_area_08", "Old Forest", "Idle Area", "T1", "Raven", "raven"),
        new("region_01_area_08", "Old Forest", "Idle Area", "T1", "Widow Stalker", "widow_stalker"),
        new("region_01_area_09", "Bleak Orchard", "Idle Area", "T1", "Scarecrow", "scarecrow"),
        new("region_01_area_09", "Bleak Orchard", "Idle Area", "T1", "Lost Soul", "lost_soul"),
        new("region_01_area_09", "Bleak Orchard", "Idle Area", "T1", "Apparition", "apparition"),
        new("region_01_area_09", "Bleak Orchard", "Idle Area", "T1", "Specter", "specter"),
        new("region_01_area_10", "Rotting Hamlet", "Idle Area", "T1", "Zombie", "zombie"),
        new("region_01_area_10", "Rotting Hamlet", "Idle Area", "T1", "Half Zombie", "half_zombie"),
        new("region_01_area_10", "Rotting Hamlet", "Idle Area", "T1", "Undead", "undead"),
        new("region_01_area_10", "Rotting Hamlet", "Idle Area", "T1", "Blood Zombie", "blood_zombie"),
        new("region_01_area_11", "Wormburrow Depths", "Idle Area", "T1", "Giant Worm", "giant_worm"),
        new("region_01_area_11", "Wormburrow Depths", "Idle Area", "T1", "Burrowed Horror", "burrowed_horror"),
        new("region_01_area_11", "Wormburrow Depths", "Idle Area", "T1", "Cave Leech", "cave_leech"),
        new("region_01_area_11", "Wormburrow Depths", "Idle Area", "T1", "Stonejaw Grub", "stonejaw_grub"),
        new("region_01_area_11", "Wormburrow Depths", "Idle Area", "T1", "Deep Burrower", "deep_burrower"),
        new("region_01_area_07", "Forgotten Ruins", "Idle Area", "T1", "Feral Ghoul", "feral_ghoul"),
        new("region_01_area_07", "Forgotten Ruins", "Idle Area", "T1", "Plague Ghoul", "plague_ghoul"),
        new("region_01_area_07", "Forgotten Ruins", "Idle Area", "T1", "Ravenous Ghoul", "ravenous_ghoul"),
        new("region_01_area_07", "Forgotten Ruins", "Idle Area", "T1", "Skeleton Archer", "skeleton_archer"),
        new("region_01_area_07", "Forgotten Ruins", "Idle Area", "T1", "Skeleton Mage", "skeleton_mage"),
        new("region_01_area_07", "Forgotten Ruins", "Idle Area", "T1", "Skeleton Warrior", "skeleton_warrior"),
        new("region_01_dungeon_goblin_mines", "Goblin Mines", "Dungeon", "T1-T2", "Goblin", "goblin"),
        new("region_01_dungeon_goblin_mines", "Goblin Mines", "Dungeon", "T1-T2", "Goblin Archer", "goblin_archer"),
        new("region_01_dungeon_goblin_mines", "Goblin Mines", "Dungeon", "T1-T2", "Goblin Warrior", "goblin_warrior"),
        new("region_01_dungeon_goblin_mines", "Goblin Mines", "Dungeon", "T1-T2", "Goblin Shaman", "goblin_shaman"),
        new("region_01_dungeon_goblin_mines", "Goblin Mines", "Dungeon", "T1-T2", "Hobgoblin", "hobgoblin"),
        new("region_01_dungeon_forgotten_catacombs", "Forgotten Catacombs", "Dungeon", "T1-T2", "Skeleton", "skeleton"),
        new("region_01_dungeon_forgotten_catacombs", "Forgotten Catacombs", "Dungeon", "T1-T2", "Poisonous Rat", "poisonous_rat"),
        new("region_01_dungeon_forgotten_catacombs", "Forgotten Catacombs", "Dungeon", "T1-T2", "Cave Bat", "cave_bat"),
        new("region_01_dungeon_forgotten_catacombs", "Forgotten Catacombs", "Dungeon", "T1-T2", "Giant Bat", "giant_bat"),
        new("region_01_dungeon_forgotten_catacombs", "Forgotten Catacombs", "Dungeon", "T1-T2", "Necroshade Wraith", "necroshade_wraith"),
        new("region_01_rift_hives_abyss", "Hive's Abyss", "Rift", "T1", "Ant Worker", "ant_worker"),
        new("region_01_rift_hives_abyss", "Hive's Abyss", "Rift", "T1", "Fire Ant", "fire_ant"),
        new("region_01_future_dungeon_great_tree", "The Great Tree", "Future Dungeon", "T2", "Forest Spirit", "forest_spirit"),
        new("region_01_future_dungeon_great_tree", "The Great Tree", "Future Dungeon", "T2", "Wood Nymph", "wood_nymph"),
        new("region_01_future_dungeon_tangled_cave", "Tangled Cave", "Future Dungeon", "T2", "Giant Spider", "giant_spider"),
        new("region_01_future_dungeon_tangled_cave", "Tangled Cave", "Future Dungeon", "T2", "Venomous Spiderling", "venomous_spiderling")
    ];

    private sealed record EssenceCatalogSourceEntry(
        string AreaId,
        string AreaName,
        string SourceType,
        string Tier,
        string MonsterName,
        string CreatureKey);
}
