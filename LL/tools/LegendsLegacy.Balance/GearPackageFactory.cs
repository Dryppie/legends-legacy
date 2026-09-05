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
    string? DefinitionId,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    int BalanceVersion,
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
        // Gear Packages intentionally exclude Essences so equipment and Essence
        // progression remain independently measurable inputs.
        var build = CreateCanonicalBuild(definition, Array.Empty<string>());
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
            CreateRatingSnapshot(rating),
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
                    item.ProgressionData?.State.DefinitionId,
                    item.Tier,
                    item.Rarity,
                    item.Quality,
                    item.ProgressionData?.State.BalanceVersion ?? 0,
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

    internal CanonicalEquipmentBuild CreateCanonicalBuild(
        GearPackageDefinition definition,
        IReadOnlyList<string> essenceIds)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(essenceIds);
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
        var build = canonicalBuilds.CreateBuild(profile, rung, essenceIds);
        ValidateBuild(definition, build, essenceIds.Count);
        return build;
    }

    internal static GearPackageCombatRatingSnapshot CreateRatingSnapshot(CombatRatingBreakdown rating) =>
        new(
            PowerRatingAlgorithm.Version,
            CombatRatingCalculator.DefinitionVersion,
            CombatRatingDisplay.FromRaw(rating.Overall),
            rating.Overall,
            rating.SingleTargetOffense,
            rating.MultiTargetOffense,
            rating.PhysicalDurability,
            rating.MagicalDurability,
            rating.Sustain,
            rating.ControlUtility);

    internal static IReadOnlyDictionary<Domain.Models.Attributes.AttributeType, float> ProjectAttributes(
        CanonicalEquipmentBuild build) =>
        AttributeCalculator.CalculateProjectedAttributes(
            build.Character.BaseAttributes.ToDictionary(
                attribute => attribute.AttributeType,
                attribute => attribute.Value),
            AttributeCalculator.ProjectEquipmentModifiers(
                build.Equipment,
                build.Character.Level));

    private static void ValidateBuild(
        GearPackageDefinition definition,
        CanonicalEquipmentBuild build,
        int expectedEssenceCount)
    {
        if (build.EquippedEssences.Count != expectedEssenceCount)
        {
            throw new InvalidOperationException(
                $"Gear Package '{definition.Id}' materialized an unexpected Essence count.");
        }
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

public sealed class CatalogEssenceLoadoutResolver(
    IEssenceDefinitionRepository essenceDefinitions) : IEssenceCombatLoadoutResolver
{
    public Task<EssenceCombatLoadout> ResolveAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Resolve(characterId, Array.Empty<PlayerEssence>()));
    }

    public EssenceCombatLoadout Resolve(
        Guid characterId,
        IEnumerable<PlayerEssence> equippedEssences)
    {
        var essences = equippedEssences.ToArray();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var essence in essences)
        {
            var definition = essenceDefinitions.GetById(essence.EssenceDefinitionId)
                ?? throw new InvalidOperationException(
                    $"Essence '{essence.EssenceDefinitionId}' was not found while materializing a balance character.");
            tags.UnionWith(definition.Tags);
            if (essence.IsEvolved)
                tags.UnionWith(definition.Evolution.AddsTags);
        }

        // The production CR contract currently excludes Essence abilities and
        // Essence definitions no longer contribute direct attribute modifiers.
        return new EssenceCombatLoadout(
            characterId,
            essences,
            Array.Empty<AttributeModifierBase>(),
            tags);
    }
}
