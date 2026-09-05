using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Essences;
using Domain.Helpers;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;

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
    int EquipmentBalanceVersion);

/// <summary>
/// Compatibility facade for callers that still use canonical profiles. Builds are
/// now produced by the progression equipment reference factory and never consult
/// recipes, crafting rolls, Potential, or tempering.
/// </summary>
public sealed class CanonicalEquipmentBuildFactory
{
    public const string TutorialStarterBuildId = "tutorial-starter";
    public const int MaximumCanonicalEssenceCount = 10;

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
        IReadOnlyDictionary<EquipmentType, string>> ProfileArchetypeIds =
        new Dictionary<CanonicalPartyProfile, IReadOnlyDictionary<EquipmentType, string>>
        {
            [CanonicalPartyProfile.Balanced] = Archetypes(
                "plain.medium_mail", "plain.greatsword", "plain.cloth_cowl", "plain.light_leggings"),
            [CanonicalPartyProfile.Offense] = Archetypes(
                "plain.light_vest", "plain.gauntlets", "plain.light_hood", "plain.light_leggings"),
            [CanonicalPartyProfile.Sustain] = Archetypes(
                "plain.cloth_robe", "plain.staff", "plain.cloth_cowl", "plain.cloth_pants"),
            [CanonicalPartyProfile.Defensive] = Archetypes(
                "plain.heavy_breastplate", "plain.maul", "plain.heavy_helm", "plain.heavy_legplates"),
            [CanonicalPartyProfile.Area] = Archetypes(
                "plain.cloth_robe", "plain.staff", "plain.cloth_cowl", "plain.cloth_pants")
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

    private readonly EquipmentCatalog _equipment;
    private readonly EquipmentReferenceBuildFactory _referenceBuilds;
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IReadOnlyList<CanonicalEquipmentProgressionRung> _ladder;

    public CanonicalEquipmentBuildFactory(
        EquipmentCatalog equipment,
        EquipmentReferenceBuildFactory referenceBuilds,
        IEssenceCombatLoadoutResolver essenceLoadouts,
        IEssenceDefinitionRepository essenceDefinitions)
    {
        _equipment = equipment;
        _referenceBuilds = referenceBuilds;
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
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung,
        IReadOnlyList<string> essenceIds)
    {
        var normalizedEssenceIds = ValidateExplicitEssences(essenceIds);
        var build = CreateBuildCore(profile, rung, normalizedEssenceIds.Count);
        var identity = $"{profile}:explicit:{string.Join(':', normalizedEssenceIds)}";
        var essences = CreateEssences(
            normalizedEssenceIds,
            identity,
            build.Character.Id,
            normalizedEssenceIds.Count);
        return build with
        {
            EquippedEssences = essences,
            Rating = CalculateRating(build.Character, build.Equipment, essences)
        };
    }

    public CanonicalEquipmentBuild CreateBuild(
        CanonicalCooperativeRole role,
        CanonicalEquipmentProgressionRung rung,
        int essenceCount) =>
        CreateRoleBuildCore(role, rung, essenceCount);

    public CanonicalEquipmentBuild CreateBuild(
        CanonicalCooperativeRole role,
        CanonicalEquipmentProgressionRung rung,
        IReadOnlyList<string> essenceIds)
    {
        var profile = CanonicalCooperativeRosterCatalog.EquipmentProfileFor(role);
        var build = CreateBuild(profile, rung, essenceIds);
        build.Character.Name = $"Canonical {role} - {rung.Id}";
        return build;
    }

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
        var itemId = CreateDeterministicGuid("region-one-tutorial-starter-equipment");
        var state = EquipmentState.Award(
            itemId,
            _equipment.Evaluator,
            GetDefinitionId("plain.mace", Rarity.Common),
            tier: 1,
            rank: 0,
            new(EquipmentAwardKind.Administrative, "offline-reference-build", TutorialStarterBuildId),
            new(EquipmentOwnershipKind.BoundPersonal, character.Id),
            ItemQuality.Standard);
        var data = EquipmentData.Create(state, _equipment.Evaluator);
        var equipment = new EquipmentInstance
        {
            Id = itemId,
            ItemBaseId = data.ItemBaseId,
            ItemBase = _equipment.GetEquipmentBase(data.ItemBaseId)
        };
        equipment.ApplyProgressionData(data);
        var essences = CreateEssences(CanonicalPartyProfile.Balanced, character.Id, essenceCount: 1);

        return new CanonicalEquipmentBuild(
            rung,
            CanonicalPartyProfile.Balanced,
            character,
            [equipment],
            essences,
            CalculateRating(character, [equipment], essences),
            _equipment.Evaluator.Balance.Version);
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
        if (essenceCount is < 0 or > MaximumCanonicalEssenceCount)
            throw new ArgumentOutOfRangeException(nameof(essenceCount));

        var characterLevel = Math.Max(
            Math.Max(
                EquipmentTierBudgetCurve.GetFirstCharacterLevelForTier(rung.Tier),
                ProfileCharacterLevels[profile]),
            (essenceCount - 1) * 10);
        var selections = CanonicalSlots
            .Take(rung.EquippedSlotCount)
            .Select(slot => new EquipmentReferenceEquipmentSelection(
                ToSlot(slot),
                GetDefinitionId(ProfileArchetypeIds[profile][slot], rung.Rarity),
                ActiveStyleId: null,
                UseNativeStyle: false))
            .ToArray();
        var reference = _referenceBuilds.Create(new EquipmentReferenceBuildDefinition(
            $"canonical-{profile.ToString().ToLowerInvariant()}-{rung.Id}",
            characterLevel,
            rung.Tier,
            Rank: 0,
            selections,
            ProfileEssenceIds[profile].Take(essenceCount).ToArray(),
            rung.Quality));

        reference.Character.Name = $"Canonical {profile} - {rung.Id}";
        return new CanonicalEquipmentBuild(
            rung,
            profile,
            reference.Character,
            reference.Equipment,
            reference.EquippedEssences,
            reference.Rating,
            reference.EquipmentBalanceVersion);
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

    private IReadOnlyList<string> ValidateExplicitEssences(IReadOnlyList<string> essenceIds)
    {
        ArgumentNullException.ThrowIfNull(essenceIds);
        if (essenceIds.Count > MaximumCanonicalEssenceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(essenceIds),
                essenceIds.Count,
                $"Canonical builds require between zero and {MaximumCanonicalEssenceCount} Essences.");
        }

        var normalized = essenceIds
            .Select(id => id?.Trim() ?? string.Empty)
            .ToArray();
        if (normalized.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Canonical Essence IDs cannot be empty.", nameof(essenceIds));
        if (normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
            throw new ArgumentException("Canonical builds cannot contain duplicate Essences.", nameof(essenceIds));

        var definitions = normalized.Select(id => _essenceDefinitions.GetById(id)
            ?? throw new InvalidOperationException($"Canonical Essence '{id}' was not found.")).ToArray();
        var duplicateSource = definitions
            .GroupBy(definition => definition.SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSource is not null)
        {
            throw new InvalidOperationException(
                $"Canonical builds cannot equip multiple Essences from '{duplicateSource.Key}'.");
        }

        return normalized;
    }

    private IReadOnlyList<CanonicalEquipmentProgressionRung> CreateLadder()
    {
        const int minimumTier = EquipmentStatBudgetCatalog.MinimumTier;
        var maximumTier = ProfileArchetypeIds.Values
            .SelectMany(x => x.Values)
            .Distinct(StringComparer.Ordinal)
            .Select(id => _equipment.Evaluator.GetArchetype(id).MaximumTier)
            .Min();

        var candidates = new List<(
            int Tier,
            ItemQuality Quality,
            Rarity Rarity,
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
                candidate.SlotCount,
                candidate.Id))
            .ToList()
            .AsReadOnly();
    }

    private void ValidateProfileContent()
    {
        foreach (var (profile, archetypes) in ProfileArchetypeIds)
        {
            if (!CanonicalSlots.All(archetypes.ContainsKey))
                throw new InvalidOperationException(
                    $"Canonical {profile} does not define every equipment slot.");

            foreach (var (slot, archetypeId) in archetypes)
            {
                var archetype = _equipment.Evaluator.GetArchetype(archetypeId);
                if (archetype.EquipmentType != slot)
                {
                    throw new InvalidOperationException(
                        $"Canonical {profile} archetype '{archetypeId}' is not {slot} equipment.");
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

    private static IReadOnlyDictionary<EquipmentType, string> Archetypes(
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
            [EquipmentType.Ring] = "plain.band",
            [EquipmentType.Necklace] = "plain.amulet",
            [EquipmentType.Relic] = "plain.vial"
        };

    private string GetDefinitionId(string archetypeId, Rarity rarity) =>
        _equipment.Evaluator.Definitions.Single(definition =>
            definition.ArchetypeId == archetypeId
            && definition.NativeStyleId is null
            && definition.Rarity == (EquipmentRarity)rarity).Id;

    private static EquipmentSlotType ToSlot(EquipmentType type) => type switch
    {
        EquipmentType.Chest => EquipmentSlotType.Chest,
        EquipmentType.TwoHanded => EquipmentSlotType.MainHand,
        EquipmentType.Head => EquipmentSlotType.Head,
        EquipmentType.Legs => EquipmentSlotType.Legs,
        EquipmentType.Ring => EquipmentSlotType.Ring,
        EquipmentType.Necklace => EquipmentSlotType.Necklace,
        EquipmentType.Relic => EquipmentSlotType.Relic,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
