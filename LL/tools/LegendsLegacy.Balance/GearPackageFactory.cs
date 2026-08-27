using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Components.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Services.LL.PowerRatings;

namespace LegendsLegacy.Balance;

public enum GearPackageArchetype
{
    Balanced,
    Offensive,
    Defensive
}

public sealed record GearPackageDefinition(
    string Id,
    string ProgressionAnchor,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    GearPackageArchetype Archetype);

public sealed record GearPackageSnapshot(
    GearPackageDefinition Definition,
    int CharacterLevel,
    int EquipmentBalanceVersion,
    GearPackageCombatRatingSnapshot CombatRating,
    IReadOnlyDictionary<string, float> ProjectedAttributes,
    IReadOnlyList<GearPackageItemSnapshot> Equipment);

public sealed record GearPackageCombatRatingSnapshot(
    int AlgorithmVersion,
    int DefinitionVersion,
    int DisplayOverall,
    int RawOverall,
    int SingleTargetOffense,
    int MultiTargetOffense,
    int PhysicalDurability,
    int MagicalDurability,
    int Sustain,
    int ControlUtility);

public sealed record GearPackageItemSnapshot(
    string Slot,
    string ItemBaseId,
    string DisplayName,
    string? RecipeId,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    int StatModelVersion,
    IReadOnlyList<GearPackageModifierSnapshot> Modifiers);

public sealed record GearPackageModifierSnapshot(
    string Attribute,
    float Amount,
    string ModifierType);

public sealed class GearPackageFactory(CanonicalEquipmentBuildFactory canonicalBuilds)
{
    public static IReadOnlyList<GearPackageDefinition> RegionOneDefinitions { get; } =
        Array.AsReadOnly<GearPackageDefinition>(
        [
            new(
                "T1_Rare_Exceptional_Balanced",
                "WorldTower.Region1.Floor1",
                1,
                Rarity.Rare,
                ItemQuality.Exceptional,
                GearPackageArchetype.Balanced),
            new(
                "T1_Epic_Exceptional_Balanced",
                "WorldTower.Region1.Floor10",
                1,
                Rarity.Epic,
                ItemQuality.Exceptional,
                GearPackageArchetype.Balanced)
        ]);

    public IReadOnlyList<GearPackageSnapshot> CreateRegionOneAnchors() =>
        RegionOneDefinitions.Select(Create).ToArray();

    public GearPackageSnapshot Create(GearPackageDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var rung = canonicalBuilds.GetProgressionLadder().SingleOrDefault(candidate =>
            candidate.Tier == definition.Tier
            && candidate.Rarity == definition.Rarity
            && candidate.Quality == definition.Quality)
            ?? throw new InvalidOperationException(
                $"No canonical equipment rung matches Gear Package '{definition.Id}'.");
        var profile = definition.Archetype switch
        {
            GearPackageArchetype.Balanced => CanonicalPartyProfile.Balanced,
            GearPackageArchetype.Offensive => CanonicalPartyProfile.Offense,
            GearPackageArchetype.Defensive => CanonicalPartyProfile.Defensive,
            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.Archetype,
                "Unsupported Gear Package archetype.")
        };

        // Gear Packages intentionally exclude Essences so equipment and Essence
        // progression remain independently measurable inputs.
        var build = canonicalBuilds.CreateBuild(profile, rung, Array.Empty<string>());
        ValidateBuild(definition, build);
        var projectedAttributes = CombatRatingCalculator.ProjectDirectAttributes(
            build.Character.BaseAttributes,
            AttributeCalculator.ProjectEquipmentModifiers(
                build.Equipment,
                build.Character.Level));
        var rating = build.Rating;

        return new GearPackageSnapshot(
            definition,
            build.Character.Level,
            build.EquipmentBalanceVersion,
            new GearPackageCombatRatingSnapshot(
                PowerRatingAlgorithm.Version,
                CombatRatingCalculator.DefinitionVersion,
                CombatRatingDisplay.FromRaw(rating.Overall),
                rating.Overall,
                rating.SingleTargetOffense,
                rating.MultiTargetOffense,
                rating.PhysicalDurability,
                rating.MagicalDurability,
                rating.Sustain,
                rating.ControlUtility),
            projectedAttributes
                .OrderBy(attribute => attribute.Key)
                .ToDictionary(
                    attribute => attribute.Key.ToString(),
                    attribute => attribute.Value,
                    StringComparer.Ordinal),
            build.Equipment
                .OrderBy(item => item.EquipmentBase.EquipmentType)
                .Select(item => new GearPackageItemSnapshot(
                    item.EquipmentBase.EquipmentType.ToString(),
                    item.ItemBaseId,
                    item.DisplayName,
                    item.BaseRecipeId,
                    item.Tier,
                    item.Rarity,
                    item.Quality,
                    item.StatModelVersion,
                    item.AttributeModifiers
                        .OrderBy(modifier => modifier.AttributeType)
                        .ThenBy(modifier => modifier.ModifierType)
                        .Select(modifier => new GearPackageModifierSnapshot(
                            modifier.AttributeType.ToString(),
                            modifier.Amount,
                            modifier.ModifierType.ToString()))
                        .ToArray()))
                .ToArray());
    }

    private static void ValidateBuild(
        GearPackageDefinition definition,
        CanonicalEquipmentBuild build)
    {
        if (build.EquippedEssences.Count != 0)
            throw new InvalidOperationException($"Gear Package '{definition.Id}' unexpectedly contains Essences.");
        if (build.Equipment.Count == 0)
            throw new InvalidOperationException($"Gear Package '{definition.Id}' contains no equipment.");
        if (build.Equipment.Any(item =>
                item.Tier != definition.Tier
                || item.Rarity != definition.Rarity
                || item.Quality != definition.Quality))
        {
            throw new InvalidOperationException(
                $"Gear Package '{definition.Id}' does not match its requested tier, rarity, and quality.");
        }
    }
}

public sealed class EquipmentOnlyEssenceLoadoutResolver : IEssenceCombatLoadoutResolver
{
    public Task<EssenceCombatLoadout> ResolveAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        Task.FromResult(CreateEmpty(characterId));

    public EssenceCombatLoadout Resolve(
        Guid characterId,
        IEnumerable<PlayerEssence> equippedEssences)
    {
        if (equippedEssences.Any())
        {
            throw new InvalidOperationException(
                "The equipment-only Gear Package boundary cannot resolve Essences.");
        }

        return CreateEmpty(characterId);
    }

    private static EssenceCombatLoadout CreateEmpty(Guid characterId) =>
        new(
            characterId,
            Array.Empty<PlayerEssence>(),
            Array.Empty<AttributeModifierBase>(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}
