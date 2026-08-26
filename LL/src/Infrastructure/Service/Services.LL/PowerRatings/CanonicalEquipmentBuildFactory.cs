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

public enum CanonicalPartyProfile
{
    Balanced,
    Offense,
    Sustain,
    Defensive,
    Area
}

public sealed record CanonicalEquipmentProgressionRung(
    int Index,
    int Tier,
    ItemQuality Quality,
    Rarity Rarity,
    int TemperingSteps,
    int EquippedSlotCount,
    string Id)
{
    public bool UsesProjectedTierScaling => false;
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
    public const string TutorialStarterBuildId = "tutorial-starter";
    private const int PositiveTemperingAttemptsPerRarity = 10;
    public const int MaximumCanonicalEssenceCount = 10;
    private const int MaximumCalibrationEquipmentTier = 100;

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
                "recipe.armor.head.cloth_cowl",
                "recipe.armor.legs.light_legwraps"),
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
                "essence.goblin_archer",
                "essence.goblin_warrior",
                "essence.glade_panther",
                "essence.green_slime",
                "essence.flame_imp",
                "essence.raven",
                "essence.lumo_wisp",
                "essence.frost_imp",
                "essence.nightshade_blossom"
            ],
            [CanonicalPartyProfile.Offense] =
            [
                "essence.green_slime",
                "essence.cinder_beetle",
                "essence.pixie",
                "essence.giant_bat",
                "essence.rotfly_toad",
                "essence.poisonous_rat",
                "essence.venomous_snake",
                "essence.blood_harpy",
                "essence.flame_harpy",
                "essence.wind_harpy"
            ],
            [CanonicalPartyProfile.Sustain] =
            [
                "essence.goblin",
                "essence.goblin_shaman",
                "essence.brown_slime",
                "essence.flame_imp",
                "essence.vampire_bat",
                "essence.goblin_warrior",
                "essence.forest_spirit",
                "essence.blue_slime",
                "essence.lumo_wisp",
                "essence.treant_guardian"
            ],
            [CanonicalPartyProfile.Defensive] =
            [
                "essence.red_slime",
                "essence.goblin_warrior",
                "essence.goblin",
                "essence.flame_imp",
                "essence.green_slime",
                "essence.illusion_fox",
                "essence.hobgoblin",
                "essence.transparent_slime",
                "essence.wood_nymph",
                "essence.thornback_boar"
            ],
            [CanonicalPartyProfile.Area] =
            [
                "essence.flame_imp",
                "essence.pixie",
                "essence.frost_imp",
                "essence.shadow_imp",
                "essence.goblin_shaman",
                "essence.rainbow_slime",
                "essence.crystal_wisp",
                "essence.blood_harpy",
                "essence.flame_harpy",
                "essence.ice_harpy"
            ]
        };

    private static readonly IReadOnlyDictionary<CanonicalCooperativeRole, string[]> RoleEssenceIds =
        new Dictionary<CanonicalCooperativeRole, string[]>
        {
            [CanonicalCooperativeRole.Guardian] =
            [
                "essence.transparent_slime",
                "essence.brown_slime",
                "essence.wood_nymph",
                "essence.hobgoblin",
                "essence.cinder_beetle",
                "essence.red_slime",
                "essence.lumo_sentinel",
                "essence.bark_golem",
                "essence.treant_guardian",
                "essence.thornback_boar"
            ],
            [CanonicalCooperativeRole.Restorer] =
            [
                "essence.blue_slime",
                "essence.forest_spirit",
                "essence.goblin_shaman",
                "essence.lumo_wisp",
                "essence.treant_sapling",
                "essence.wood_nymph",
                "essence.crystal_wisp",
                "essence.nightshade_blossom",
                "essence.gnoll_shaman",
                "essence.elder_treant"
            ],
            [CanonicalCooperativeRole.Striker] =
            [
                .. ProfileEssenceIds[CanonicalPartyProfile.Offense]
            ],
            [CanonicalCooperativeRole.Controller] =
            [
                "essence.enchanted_fairy",
                "essence.frost_imp",
                "essence.goblin",
                "essence.giant_bat",
                "essence.hollow_stag",
                "essence.rainbow_slime",
                "essence.goblin_shaman",
                "essence.shadow_harpy",
                "essence.ice_harpy",
                "essence.wandering_ghost"
            ],
            [CanonicalCooperativeRole.AreaSpecialist] =
            [
                .. ProfileEssenceIds[CanonicalPartyProfile.Area]
            ],
            [CanonicalCooperativeRole.DefensiveHybrid] =
            [
                "essence.brown_slime",
                "essence.red_slime",
                "essence.vampire_bat",
                "essence.blood_zombie",
                "essence.lumo_sentinel",
                "essence.cinder_beetle",
                "essence.hobgoblin",
                "essence.bark_golem",
                "essence.thornback_boar",
                "essence.treant_guardian"
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

    private static readonly ItemQuality[] CalibrationQualities =
    [
        ItemQuality.Standard,
        ItemQuality.Fine,
        ItemQuality.Exceptional
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
        CreateBuildCore(profile, rung, essenceCount: 2);

    public CanonicalEquipmentBuild CreateBuild(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        int essenceCount) =>
        CreateBuildCore(profile, rung, essenceCount);

    public CanonicalEquipmentBuild CreateBuild(
        CanonicalCooperativeRole role,
        CanonicalEquipmentProgressionRung rung,
        int essenceCount) =>
        CreateRoleBuildCore(role, rung, essenceCount);

    public CanonicalEquipmentBuild CreateBuildForDungeonTier(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        int dungeonTier) =>
        CreateBuildCore(profile, rung, GetEssenceCountForDungeonTier(dungeonTier));

    public CanonicalEquipmentBuild CreateBuildForArea(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        int characterLevel,
        int essenceCount)
    {
        var build = CreateBuildCore(profile, rung, essenceCount);
        var resolvedLevel = Math.Max(1, characterLevel);
        build.Character.Level = resolvedLevel;
        build.Character.BaseAttributes = EntityBaseAttributeHelper
            .CreateEntityAttributesForLevel(build.Character.Id, resolvedLevel)
            .OrderBy(attribute => attribute.AttributeType)
            .ToList();
        return build with
        {
            Rating = CalculateRating(
                build.Character,
                build.Equipment,
                build.EquippedEssences)
        };
    }

    public CanonicalEquipmentBuild CreateBuildForArea(
        CanonicalCooperativeRole role,
        CanonicalEquipmentProgressionRung rung,
        int characterLevel,
        int essenceCount)
    {
        var build = CreateRoleBuildCore(role, rung, essenceCount);
        var resolvedLevel = Math.Max(1, characterLevel);
        build.Character.Level = resolvedLevel;
        build.Character.BaseAttributes = EntityBaseAttributeHelper
            .CreateEntityAttributesForLevel(build.Character.Id, resolvedLevel)
            .OrderBy(attribute => attribute.AttributeType)
            .ToList();
        return build with
        {
            Rating = CalculateRating(
                build.Character,
                build.Equipment,
                build.EquippedEssences)
        };
    }

    public CanonicalEquipmentBuild CreateTutorialStarterBuild()
    {
        var rung = _ladder.Single(candidate => candidate.Id == "t1-standard-common");
        var character = new Character
        {
            Id = CreateDeterministicGuid("region-one-tutorial-starter-character"),
            Name = "Region 1 Tutorial Starter",
            Level = 1,
            BaseAttributes = EntityBaseAttributeHelper
                .CreateEntityAttributesForLevel(
                    CreateDeterministicGuid("region-one-tutorial-starter-attributes"),
                    level: 1)
                .OrderBy(attribute => attribute.AttributeType)
                .ToList()
        };
        if (!_craftingDefinitions.GetEquipmentBases().TryGetValue("mace", out var maceBase))
            throw new InvalidOperationException("Tutorial starter mace item base was not found.");
        var equipment = new EquipmentInstance
        {
            Id = CreateDeterministicGuid("region-one-tutorial-starter-equipment"),
            ItemBaseId = maceBase.Id,
            ItemBase = maceBase,
            Tier = EquipmentStatBudgetCatalog.MinimumTier,
            Rarity = Rarity.Common,
            Quality = ItemQuality.Standard
        };
        var essences = CreateEssences(CanonicalPartyProfile.Balanced, character.Id, essenceCount: 1);

        return new CanonicalEquipmentBuild(
            rung,
            CanonicalPartyProfile.Balanced,
            character,
            [equipment],
            essences,
            CalculateRating(character, [equipment], essences),
            EquipmentStatBudgetCatalog.BalanceVersion,
            null);
    }

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

    private CanonicalEquipmentBuild CreateBuildCore(
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
                Math.Max(
                    EquipmentTierBudgetCurve.GetFirstCharacterLevelForTier(rung.Tier),
                    ProfileCharacterLevels[profile]),
                (essenceCount - 1) * 10),
            BaseAttributes = EntityBaseAttributeHelper
                .CreateEntityAttributesForLevel(
                    CreateDeterministicGuid($"canonical-attributes:{profile}"),
                    Math.Max(
                        Math.Max(
                            EquipmentTierBudgetCurve.GetFirstCharacterLevelForTier(rung.Tier),
                            ProfileCharacterLevels[profile]),
                        (essenceCount - 1) * 10))
                .OrderBy(attribute => attribute.AttributeType)
                .ToList()
        };
        var equipment = CanonicalSlots
            .Take(rung.EquippedSlotCount)
            .Select((slot, index) => CreateEquipment(profile, rung, slot, index))
            .ToList();
        var essences = CreateEssences(profile, character.Id, essenceCount);

        return new CanonicalEquipmentBuild(
            rung,
            profile,
            character,
            equipment,
            essences,
            CalculateRating(character, equipment, essences),
            EquipmentStatBudgetCatalog.BalanceVersion,
            equipment
                .FirstOrDefault(item => item.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
                ?.BaseRecipeId);
    }

    private CanonicalEquipmentBuild CreateRoleBuildCore(
        CanonicalCooperativeRole role,
        CanonicalEquipmentProgressionRung rung,
        int essenceCount)
    {
        var profile = CanonicalCooperativeRosterCatalog.EquipmentProfileFor(role);
        var build = CreateBuildCore(profile, rung, essenceCount);
        build.Character.Name = $"Canonical {role} - {rung.Id}";
        var essences = CreateEssences(role, build.Character.Id, essenceCount);
        return build with
        {
            EquippedEssences = essences,
            Rating = CalculateRating(build.Character, build.Equipment, essences)
        };
    }

    private CombatRatingBreakdown CalculateRating(
        Character character,
        IReadOnlyList<EquipmentInstance> equipment,
        IReadOnlyList<PlayerEssence> essences)
    {
        var essenceSources = essences
            .Select(essence =>
            {
                var loadout = _essenceLoadouts.Resolve(character.Id, [essence]);
                return new CombatRatingModifierSource(
                    EquipmentStatBudgetCatalog.MinimumTier,
                    loadout.AttributeModifiers);
            })
            .ToList();
        return CombatRatingCalculator.Calculate(
            character.BaseAttributes,
            equipment,
            essenceSources,
            character.Level);
    }

    private EquipmentInstance CreateEquipment(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        EquipmentType slot,
        int slotIndex)
    {
        var recipeId = ProfileRecipeIds[profile][slot];
        return CreateEquipmentFromRecipe(
            recipeId,
            rung,
            $"canonical-equipment:{profile}:{rung.Id}:{slotIndex}",
            $"canonical-stat-roll:{profile}:t1-" +
            $"{rung.Quality.ToString().ToLowerInvariant()}-" +
            $"{rung.Rarity.ToString().ToLowerInvariant()}:{slotIndex}");
    }

    private EquipmentInstance CreateEquipmentFromRecipe(
        string recipeId,
        CanonicalEquipmentProgressionRung rung,
        string equipmentIdentity,
        string statRollIdentity)
    {
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
            Id = CreateDeterministicGuid(equipmentIdentity),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase,
            BaseRecipeId = recipe.Id,
            CraftedName = design.Name,
            Tier = rung.Tier,
            StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
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
                    new Random(CreateDeterministicSeed(statRollIdentity)))
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

        return equipment;
    }

    private IReadOnlyList<PlayerEssence> CreateEssences(
        CanonicalPartyProfile profile,
        Guid characterId,
        int essenceCount) =>
        CreateEssences(ProfileEssenceIds[profile], profile.ToString(), characterId, essenceCount);

    private IReadOnlyList<PlayerEssence> CreateEssences(
        CanonicalCooperativeRole role,
        Guid characterId,
        int essenceCount) =>
        CreateEssences(RoleEssenceIds[role], role.ToString(), characterId, essenceCount);

    private static IReadOnlyList<PlayerEssence> CreateEssences(
        IReadOnlyList<string> definitionIds,
        string identity,
        Guid characterId,
        int essenceCount) =>
        definitionIds
            .Take(essenceCount)
            .Select((definitionId, index) => new PlayerEssence
            {
                Id = CreateDeterministicGuid($"canonical-essence:{identity}:{index}:{definitionId}"),
                CharacterId = characterId,
                EssenceDefinitionId = definitionId,
                Level = 1,
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
            foreach (var quality in CalibrationQualities)
            {
                foreach (var rarity in CalibrationRarities)
                {
                    candidates.Add((
                        tier,
                        quality,
                        rarity,
                        TemperingConstants.GetRarityUpgradeCount(rarity),
                        CanonicalSlots.Length,
                        $"t{tier}-{quality.ToString().ToLowerInvariant()}-" +
                        rarity.ToString().ToLowerInvariant()));
                }
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

        foreach (var (role, essenceIds) in RoleEssenceIds)
        {
            if (essenceIds.Length != MaximumCanonicalEssenceCount)
            {
                throw new InvalidOperationException(
                    $"Canonical {role} must define exactly {MaximumCanonicalEssenceCount} Essences.");
            }
            if (essenceIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != essenceIds.Length)
                throw new InvalidOperationException($"Canonical {role} contains duplicate Essences.");
            foreach (var essenceId in essenceIds)
            {
                if (_essenceDefinitions.GetById(essenceId) is null)
                {
                    throw new InvalidOperationException(
                        $"Canonical {role} Essence '{essenceId}' was not found.");
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
