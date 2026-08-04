using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Professions;
using Domain.Helpers;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.PowerRatings;

public sealed record CanonicalEquipmentProgressionRung(
    int Index,
    int Tier,
    ItemQuality Quality,
    Rarity Rarity,
    int TemperingSteps,
    int EquippedSlotCount,
    string Id)
{
    public bool UsesProjectedTierScaling => Tier > EquipmentStatBudgetCatalog.MaximumTier;
}

public sealed record CanonicalEquipmentBuild(
    CanonicalEquipmentProgressionRung Rung,
    CanonicalPartyProfile Profile,
    Character Character,
    IReadOnlyList<EquipmentInstance> Equipment,
    IReadOnlyList<PlayerEssence> EquippedEssences,
    CombatRatingBreakdown Rating,
    int EquipmentBalanceVersion,
    string? MainHandRecipeId);

/// <summary>
/// Builds detached, deterministic canonical combatants from authored crafting
/// recipes, item bases, and Region 1 Essences. Nothing is persisted or granted
/// to a player. Equipment tiers beyond the live budget catalog are explicitly
/// projected for calibration while retaining those real content identities.
/// </summary>
public sealed class CanonicalEquipmentBuildFactory
{
    private const int PositiveTemperingAttemptsPerRarity = 10;
    private const int MaximumCanonicalEssenceCount = 6;
    private const int MaximumCalibrationEquipmentTier = 20;
    private const double ProjectedEquipmentPowerGrowthPerTier = 1.25d;

    // Stable slot order keeps every full-set matrix build deterministic.
    private static readonly EquipmentType[] CanonicalSlots =
    [
        EquipmentType.Chest,
        EquipmentType.TwoHanded,
        EquipmentType.Head,
        EquipmentType.Legs,
        EquipmentType.Ring,
        EquipmentType.Necklace,
        EquipmentType.Relic,
    ];

    private static readonly IReadOnlyDictionary<
        CanonicalPartyProfile,
        IReadOnlyDictionary<EquipmentType, string>> ProfileRecipeIds =
        new Dictionary<CanonicalPartyProfile, IReadOnlyDictionary<EquipmentType, string>>
        {
            [CanonicalPartyProfile.Balanced] = Recipes(
                "recipe.armor.chest.medium_mail",
                "recipe.weapon.two_handed.greatsword",
                "recipe.armor.head.medium_helm",
                "recipe.armor.legs.medium_greaves"),
            [CanonicalPartyProfile.Offense] = Recipes(
                "recipe.armor.chest.light_vest",
                "recipe.weapon.two_handed.gauntlets",
                "recipe.armor.head.light_hood",
                "recipe.armor.legs.light_legwraps"),
            [CanonicalPartyProfile.Sustain] = Recipes(
                "recipe.armor.chest.cloth_robe",
                "recipe.weapon.two_handed.staff",
                "recipe.armor.head.cloth_cowl",
                "recipe.armor.legs.cloth_pants"),
            [CanonicalPartyProfile.Defensive] = Recipes(
                "recipe.armor.chest.heavy_breastplate",
                "recipe.weapon.two_handed.maul",
                "recipe.armor.head.heavy_helm",
                "recipe.armor.legs.heavy_legplates"),
            [CanonicalPartyProfile.Area] = Recipes(
                "recipe.armor.chest.cloth_robe",
                "recipe.weapon.two_handed.staff",
                "recipe.armor.head.cloth_cowl",
                "recipe.armor.legs.cloth_pants")
        };

    private static readonly IReadOnlyDictionary<CanonicalPartyProfile, string[]> ProfileEssenceIds =
        new Dictionary<CanonicalPartyProfile, string[]>
        {
            [CanonicalPartyProfile.Balanced] =
            [
                "essence.goblin",
                "essence.vampire_bat",
                "essence.goblin_warrior",
                "essence.enchanted_fairy",
                "essence.goblin_archer",
                "essence.pixie"
            ],
            [CanonicalPartyProfile.Offense] =
            [
                "essence.goblin_archer",
                "essence.glade_panther",
                "essence.goblin_warrior",
                "essence.flame_imp",
                "essence.hobgoblin",
                "essence.vampire_bat"
            ],
            [CanonicalPartyProfile.Sustain] =
            [
                "essence.enchanted_fairy",
                "essence.pixie",
                "essence.treant_sapling",
                "essence.goblin_shaman",
                "essence.brown_slime",
                "essence.green_slime"
            ],
            [CanonicalPartyProfile.Defensive] =
            [
                "essence.brown_slime",
                "essence.goblin_warrior",
                "essence.treant_sapling",
                "essence.goblin_shaman",
                "essence.blue_slime",
                "essence.moss_lizard"
            ],
            [CanonicalPartyProfile.Area] =
            [
                "essence.flame_imp",
                "essence.pixie",
                "essence.frost_imp",
                "essence.shadow_imp",
                "essence.goblin_shaman",
                "essence.rainbow_slime"
            ]
        };

    private static readonly IReadOnlyDictionary<CanonicalPartyProfile, int> ProfileCharacterLevels =
        new Dictionary<CanonicalPartyProfile, int>
        {
            [CanonicalPartyProfile.Balanced] = 5,
            [CanonicalPartyProfile.Offense] = 15,
            [CanonicalPartyProfile.Sustain] = 15,
            [CanonicalPartyProfile.Defensive] = 10,
            [CanonicalPartyProfile.Area] = 15
        };

    private static readonly Rarity[] CalibrationRarities =
    [
        Rarity.Common,
        Rarity.Uncommon,
        Rarity.Rare,
        Rarity.Epic,
        Rarity.Unique,
        Rarity.Legendary
    ];

    private readonly ICraftingDefinitionProvider _craftingDefinitions;
    private readonly IItemStatRollService _statRolls;
    private readonly ITemperingMechanicsService _tempering;
    private readonly IItemPotentialService _potential;
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IReadOnlyList<CanonicalEquipmentProgressionRung> _ladder;

    public CanonicalEquipmentBuildFactory(
        ICraftingDefinitionProvider craftingDefinitions,
        IItemStatRollService statRolls,
        ITemperingMechanicsService tempering,
        IItemPotentialService potential,
        IEssenceCombatLoadoutResolver essenceLoadouts,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _craftingDefinitions = craftingDefinitions;
        _statRolls = statRolls;
        _tempering = tempering;
        _potential = potential;
        _essenceLoadouts = essenceLoadouts;
        _essenceDefinitions = essenceDefinitions;
        ValidateProfileContent();
        _ladder = CreateLadder();
    }

    public IReadOnlyList<CanonicalEquipmentProgressionRung> GetProgressionLadder() => _ladder;

    public CanonicalEquipmentBuild CreateBuild(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung) =>
        CreateBuild(profile, rung, essenceCount: 2);

    public CanonicalEquipmentBuild CreateBuildForDungeonTier(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        int dungeonTier) =>
        CreateBuild(profile, rung, GetEssenceCountForDungeonTier(dungeonTier));

    public static int GetEssenceCountForDungeonTier(int dungeonTier) => dungeonTier switch
    {
        1 => 2,
        2 => 4,
        3 => 6,
        _ => throw new ArgumentOutOfRangeException(
            nameof(dungeonTier),
            dungeonTier,
            "Canonical dungeon tiers must be between 1 and 3.")
    };

    private CanonicalEquipmentBuild CreateBuild(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        int essenceCount)
    {
        if (!_ladder.Any(candidate => candidate == rung))
            throw new ArgumentException(
                "The progression rung does not belong to the active canonical ladder.",
                nameof(rung));
        if (essenceCount is < 1 or > MaximumCanonicalEssenceCount)
            throw new ArgumentOutOfRangeException(nameof(essenceCount));

        var character = new Character
        {
            Id = CreateDeterministicGuid($"canonical-character:{profile}"),
            Name = $"Canonical {profile} - {rung.Id}",
            Level = Math.Max(
                Math.Max(rung.Tier, ProfileCharacterLevels[profile]),
                (essenceCount - 1) * 10),
            BaseAttributes = EntityBaseAttributeHelper
                .CreateEntityAttributes(CreateDeterministicGuid($"canonical-attributes:{profile}"))
                .OrderBy(attribute => attribute.AttributeType)
                .ToList()
        };
        var equipment = CanonicalSlots
            .Take(rung.EquippedSlotCount)
            .Select((slot, index) => CreateEquipment(profile, rung, slot, index))
            .ToList();
        var essences = CreateEssences(profile, character.Id, essenceCount);
        var essenceSources = essences
            .Select(essence =>
            {
                var loadout = _essenceLoadouts.Resolve(character.Id, [essence]);
                return new CombatRatingModifierSource(essence.PotentialTier, loadout.AttributeModifiers);
            })
            .ToList();

        return new CanonicalEquipmentBuild(
            rung,
            profile,
            character,
            equipment,
            essences,
            CombatRatingCalculator.Calculate(
                character.BaseAttributes,
                equipment,
                essenceSources),
            EquipmentStatBudgetCatalog.BalanceVersion,
            equipment
                .FirstOrDefault(item => item.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                ?.BaseRecipeId);
    }

    private EquipmentInstance CreateEquipment(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        EquipmentType slot,
        int slotIndex)
    {
        var recipeId = ProfileRecipeIds[profile][slot];
        var recipe = _craftingDefinitions.GetRecipe(recipeId)
            ?? throw new InvalidOperationException($"Canonical recipe '{recipeId}' was not found.");
        if (rung.Tier is < EquipmentStatBudgetCatalog.MinimumTier
            or > MaximumCalibrationEquipmentTier)
        {
            throw new InvalidOperationException(
                $"Canonical equipment Tier {rung.Tier} is outside the supported stat-budget range.");
        }

        if (!_craftingDefinitions.GetEquipmentBases().TryGetValue(
                recipe.OutputItemId,
                out var itemBase))
        {
            throw new InvalidOperationException(
                $"Canonical recipe '{recipe.Id}' output '{recipe.OutputItemId}' was not found.");
        }

        var design = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var raritySteps = TemperingConstants.GetRarityUpgradeCount(rung.Rarity);
        var requiredPotential =
            raritySteps
            * PositiveTemperingAttemptsPerRarity
            * TemperingConstants.PotentialCost;
        var startingPotential = Math.Max(
            requiredPotential,
            _potential.CalculateStartingPotential(
                itemBase,
                rung.Tier,
                rung.Quality,
                masteryLevel: 0,
                craftingLevel: rung.Tier));
        var equipment = new EquipmentInstance
        {
            Id = CreateDeterministicGuid($"canonical-equipment:{profile}:{rung.Id}:{slotIndex}"),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase,
            BaseRecipeId = recipe.Id,
            CraftedName = design.Name,
            Tier = rung.Tier,
            Rarity = Rarity.Common,
            Quality = rung.Quality,
            Potential = startingPotential,
            MaxPotential = startingPotential,
            TemperingProgress = 0,
            AffinityTags = [.. design.Tags],
            InstanceModifiers =
            [
                .. _statRolls.RollBaseStats(
                    itemBase,
                    design,
                    rung.Tier,
                    rung.Quality,
                    new Random(CreateDeterministicSeed(
                        $"canonical-stat-roll:{profile}:{rung.Id}:{slotIndex}")))
            ]
        };

        var temperingRandom = new PositiveTemperingRandom();
        for (var attempt = 0;
             attempt < raritySteps * PositiveTemperingAttemptsPerRarity;
             attempt++)
        {
            _tempering.ApplyTemperingAttempt(
                equipment,
                design.TemperingProfile,
                temperingRandom);
        }

        if (equipment.Rarity != rung.Rarity)
        {
            throw new InvalidOperationException(
                $"Canonical item '{equipment.DisplayName}' reached {equipment.Rarity} " +
                $"instead of {rung.Rarity}.");
        }

        ApplyProjectedTierScaling(equipment);
        return equipment;
    }

    private static void ApplyProjectedTierScaling(EquipmentInstance equipment)
    {
        if (equipment.Tier <= EquipmentStatBudgetCatalog.MaximumTier)
            return;

        // Tier 10 is the end of the currently authored equipment budget table.
        // Continue its late-game growth for calibration only so Mythic analysis
        // can report the equipment tier its present enemy scaling would require.
        var multiplier = Math.Pow(
            ProjectedEquipmentPowerGrowthPerTier,
            equipment.Tier - EquipmentStatBudgetCatalog.MaximumTier);
        foreach (var modifier in equipment.InstanceModifiers)
            modifier.Amount *= (float)multiplier;
    }

    private IReadOnlyList<PlayerEssence> CreateEssences(
        CanonicalPartyProfile profile,
        Guid characterId,
        int essenceCount) =>
        ProfileEssenceIds[profile]
            .Take(essenceCount)
            .Select((definitionId, index) => new PlayerEssence
            {
                Id = CreateDeterministicGuid($"canonical-essence:{profile}:{index}:{definitionId}"),
                CharacterId = characterId,
                EssenceDefinitionId = definitionId,
                Level = 1,
                NativeRegion = 1,
                PotentialTier = 1,
                AscensionTier = 0,
                IsEvolved = false,
                AbsorbedAt = DateTimeOffset.UnixEpoch,
                UpdatedAt = DateTimeOffset.UnixEpoch
            })
            .ToList();

    private IReadOnlyList<CanonicalEquipmentProgressionRung> CreateLadder()
    {
        // Calibration projects the real authored item bases and recipe designs
        // beyond the live Region 1 recipe cap. Tiers above the authored budget
        // table continue the late-game curve so the analyzer can identify and
        // expose requirements that are not yet attainable in live content.
        const int minimumTier = EquipmentStatBudgetCatalog.MinimumTier;
        const int maximumTier = MaximumCalibrationEquipmentTier;

        var candidates = new List<(
            int Tier,
            ItemQuality Quality,
            Rarity Rarity,
            int Steps,
            int SlotCount,
            string Id)>();

        foreach (var tier in Enumerable.Range(
                     minimumTier,
                     maximumTier - minimumTier + 1))
        {
            foreach (var rarity in CalibrationRarities)
            {
                candidates.Add((
                    tier,
                    ItemQuality.Standard,
                    rarity,
                    TemperingConstants.GetRarityUpgradeCount(rarity),
                    CanonicalSlots.Length,
                    $"t{tier}-standard-{rarity.ToString().ToLowerInvariant()}"));
            }
        }

        return candidates
            .Select((candidate, index) => new CanonicalEquipmentProgressionRung(
                index,
                candidate.Tier,
                candidate.Quality,
                candidate.Rarity,
                candidate.Steps,
                candidate.SlotCount,
                candidate.Id))
            .ToList()
            .AsReadOnly();
    }

    private void ValidateProfileContent()
    {
        foreach (var (profile, recipes) in ProfileRecipeIds)
        {
            if (!CanonicalSlots.All(recipes.ContainsKey))
                throw new InvalidOperationException(
                    $"Canonical {profile} does not define every equipment slot.");

            foreach (var (slot, recipeId) in recipes)
            {
                var recipe = _craftingDefinitions.GetRecipe(recipeId)
                    ?? throw new InvalidOperationException(
                        $"Canonical {profile} recipe '{recipeId}' was not found.");
                if (!recipe.Enabled || recipe.OutputItemType != slot)
                {
                    throw new InvalidOperationException(
                        $"Canonical {profile} recipe '{recipeId}' is not an enabled {slot} recipe.");
                }
            }
        }

        foreach (var (profile, essenceIds) in ProfileEssenceIds)
        {
            if (essenceIds.Length != MaximumCanonicalEssenceCount)
            {
                throw new InvalidOperationException(
                    $"Canonical {profile} must define exactly {MaximumCanonicalEssenceCount} Essences.");
            }
            if (essenceIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != essenceIds.Length)
                throw new InvalidOperationException($"Canonical {profile} contains duplicate Essences.");
            foreach (var essenceId in essenceIds)
            {
                if (_essenceDefinitions.GetById(essenceId) is null)
                {
                    throw new InvalidOperationException(
                        $"Canonical {profile} Essence '{essenceId}' was not found.");
                }
            }
        }
    }

    private static IReadOnlyDictionary<EquipmentType, string> Recipes(
        string chest,
        string twoHanded,
        string head,
        string legs) =>
        new Dictionary<EquipmentType, string>
        {
            [EquipmentType.Chest] = chest,
            [EquipmentType.TwoHanded] = twoHanded,
            [EquipmentType.Head] = head,
            [EquipmentType.Legs] = legs,
            [EquipmentType.Ring] = "recipe.jewelry.ring.band",
            [EquipmentType.Necklace] = "recipe.jewelry.necklace.amulet",
            [EquipmentType.Relic] = "recipe.jewelry.relic.vial"
        };

    private static int CreateDeterministicSeed(string value) =>
        BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed class PositiveTemperingRandom : Random
    {
        protected override double Sample() => 0.0005d;
    }
}
