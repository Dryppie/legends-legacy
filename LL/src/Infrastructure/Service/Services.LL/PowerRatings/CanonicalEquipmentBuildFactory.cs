using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace Services.LL.PowerRatings;

public sealed record CanonicalEquipmentProgressionRung(
    int Index,
    int Tier,
    ItemQuality Quality,
    Rarity Rarity,
    int TemperingSteps,
    int EquippedSlotCount,
    string Id);

public sealed record CanonicalEquipmentBuild(
    CanonicalEquipmentProgressionRung Rung,
    CanonicalPartyProfile Profile,
    Character Character,
    double AuthorizedBudget,
    double SpentBudget,
    IReadOnlyDictionary<AttributeType, double> EquipmentPoints,
    CombatRatingBreakdown Rating,
    IReadOnlyList<AttributeType> BindingCombatCaps,
    int EquipmentBalanceVersion,
    string? MainHandRecipeId);

/// <summary>
/// Builds detached, deterministic canonical combatants from the active equipment budget rules.
/// It deliberately creates no item instances and persists no player-owned state.
/// </summary>
public sealed class CanonicalEquipmentBuildFactory
{
    // The first acquired slot matches the authored tutorial reward. Remaining slots are
    // ordered deterministically, with a weapon early enough to represent ordinary combat.
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

    private static readonly IReadOnlyDictionary<CanonicalPartyProfile, string> MainHandRecipes =
        new Dictionary<CanonicalPartyProfile, string>
        {
            [CanonicalPartyProfile.Balanced] = "recipe.weapon.two_handed.greatsword",
            [CanonicalPartyProfile.Offense] = "recipe.weapon.two_handed.gauntlets",
            [CanonicalPartyProfile.Defensive] = "recipe.weapon.two_handed.maul",
            [CanonicalPartyProfile.Sustain] = "recipe.weapon.two_handed.staff",
            [CanonicalPartyProfile.Area] = "recipe.weapon.two_handed.staff"
        };

    private static readonly IReadOnlyDictionary<CanonicalPartyProfile, IReadOnlyDictionary<AttributeType, double>>
        ProfileWeights = new Dictionary<CanonicalPartyProfile, IReadOnlyDictionary<AttributeType, double>>
        {
            [CanonicalPartyProfile.Balanced] = Weights(
                (AttributeType.Power, 0.20),
                (AttributeType.Precision, 0.10),
                (AttributeType.Fortitude, 0.15),
                (AttributeType.Spirit, 0.10),
                (AttributeType.MaxHealth, 0.15),
                (AttributeType.WeaponDamage, 0.10),
                (AttributeType.Armor, 0.10),
                (AttributeType.Resistance, 0.10)),
            [CanonicalPartyProfile.Offense] = Weights(
                (AttributeType.Power, 0.30),
                (AttributeType.Precision, 0.20),
                (AttributeType.WeaponDamage, 0.20),
                (AttributeType.CritChance, 0.10),
                (AttributeType.CritDamage, 0.10),
                (AttributeType.AttackSpeed, 0.10)),
            [CanonicalPartyProfile.Defensive] = Weights(
                (AttributeType.Power, 0.10),
                (AttributeType.Fortitude, 0.25),
                (AttributeType.MaxHealth, 0.25),
                (AttributeType.Armor, 0.15),
                (AttributeType.Resistance, 0.15),
                (AttributeType.BlockChance, 0.10)),
            [CanonicalPartyProfile.Sustain] = Weights(
                (AttributeType.Power, 0.20),
                (AttributeType.Spirit, 0.20),
                (AttributeType.MaxHealth, 0.15),
                (AttributeType.Fortitude, 0.10),
                (AttributeType.Resistance, 0.10),
                (AttributeType.HealingPowerPercent, 0.10),
                (AttributeType.HealthRegeneration, 0.10),
                (AttributeType.Cooldown, 0.05)),
            [CanonicalPartyProfile.Area] = Weights(
                (AttributeType.Power, 0.30),
                (AttributeType.Precision, 0.15),
                (AttributeType.Spirit, 0.15),
                (AttributeType.WeaponDamage, 0.15),
                (AttributeType.CritChance, 0.10),
                (AttributeType.MagicPenetration, 0.10),
                (AttributeType.AttackSpeed, 0.05))
        };

    private static readonly (ItemQuality Quality, Rarity Rarity)[] TierMilestones =
    [
        (ItemQuality.Crude, Rarity.Common),
        (ItemQuality.Standard, Rarity.Uncommon),
        (ItemQuality.Fine, Rarity.Rare),
        (ItemQuality.Exceptional, Rarity.Legendary),
        (ItemQuality.Masterwork, Rarity.Legacy)
    ];

    private readonly CraftingBalanceOptions _balance;
    private readonly IReadOnlyList<double> _slotWeights;
    private readonly IReadOnlyList<CanonicalEquipmentProgressionRung> _ladder;

    public CanonicalEquipmentBuildFactory(IOptions<CraftingBalanceOptions> balance)
    {
        _balance = balance.Value;
        _slotWeights = CanonicalSlots.Select(_balance.GetSlotBudgetWeight).ToArray();
        _ladder = CreateLadder();
    }

    public IReadOnlyList<CanonicalEquipmentProgressionRung> GetProgressionLadder() => _ladder;

    public CanonicalEquipmentBuild CreateBuild(
        CanonicalPartyProfile profile,
        CanonicalEquipmentProgressionRung rung)
    {
        if (!_ladder.Any(candidate => candidate == rung))
            throw new ArgumentException("The progression rung does not belong to the active canonical ladder.", nameof(rung));

        var attributes = CreateCharacterBaseline();
        var directBaseAttributes = CombatRatingCalculator.RemovePrimaryContributions(attributes);
        var equipmentPoints = new Dictionary<AttributeType, double>();
        var bindingCaps = new HashSet<AttributeType>();
        var authorizedBudget = 0d;
        var spentBudget = 0d;
        var weights = ProfileWeights[profile];
        var qualityMultiplier = _balance.GetQualityStatMultiplier(rung.Quality);
        var temperingBudget =
            rung.TemperingSteps * TemperingConstants.GetDirectedImprovementBudget(rung.Tier);

        foreach (var slotWeight in _slotWeights.Take(rung.EquippedSlotCount))
        {
            var slotBudget =
                _balance.GetTierPowerBudget(rung.Tier) * slotWeight * qualityMultiplier
                + temperingBudget;
            authorizedBudget += slotBudget;
            var constraints = EquipmentConstraintProfile.CreateItemConstraints(
                attributes,
                slotWeight,
                _balance.GetMaximumCombatLoadoutBudgetWeight(),
                EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier);
            var allocation = EquipmentBudgetAllocator.AllocateConstrained(
                rung.Tier,
                slotBudget,
                weights,
                constraints,
                weights,
                perItemCapMultiplier: EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeight));
            spentBudget += allocation.SpentBudget;
            bindingCaps.UnionWith(allocation.BindingCombatCaps);

            foreach (var (attribute, points) in allocation.AddedPoints)
            {
                equipmentPoints[attribute] = equipmentPoints.GetValueOrDefault(attribute) + points;
                ApplyAttributeDelta(attributes, attribute, (float)points);
            }
        }

        var character = new Character
        {
            Id = Guid.Empty,
            Name = $"Canonical {profile} - {rung.Id}",
            Level = rung.Tier,
            BaseAttributes = attributes
                .OrderBy(entry => entry.Key)
                .Select(entry => new EntityAttribute
                {
                    AttributeType = entry.Key,
                    Value = entry.Value
                })
                .ToList()
        };

        return new CanonicalEquipmentBuild(
            rung,
            profile,
            character,
            authorizedBudget,
            spentBudget,
            equipmentPoints,
            CombatRatingCalculator.CalculateCanonical(
                directBaseAttributes,
                equipmentPoints,
                rung.Tier),
            bindingCaps.Order().ToList(),
            EquipmentStatBudgetCatalog.BalanceVersion,
            rung.EquippedSlotCount >= 2 ? MainHandRecipes[profile] : null);
    }

    private IReadOnlyList<CanonicalEquipmentProgressionRung> CreateLadder()
    {
        var candidates = new List<(
            int Tier,
            ItemQuality Quality,
            Rarity Rarity,
            int Steps,
            int SlotCount,
            string Id)>();

        // A real new character starts with base attributes and receives one tutorial chest,
        // not a complete seven-slot Tier-1 loadout. These acquisition states close the
        // low-end gap without inventing fractional item budgets.
        candidates.Add((1, ItemQuality.Crude, Rarity.Common, 0, 0, "t1-base"));
        for (var slotCount = 1; slotCount < CanonicalSlots.Length; slotCount++)
        {
            candidates.Add((
                1,
                ItemQuality.Crude,
                Rarity.Common,
                0,
                slotCount,
                $"t1-crude-common-{slotCount}-slots"));
        }

        foreach (var tier in Enumerable.Range(
                     EquipmentStatBudgetCatalog.MinimumTier,
                     EquipmentStatBudgetCatalog.MaximumTier))
        {
            foreach (var milestone in TierMilestones)
            {
                var steps = TemperingConstants.GetRarityUpgradeCount(milestone.Rarity);
                candidates.Add((
                    tier,
                    milestone.Quality,
                    milestone.Rarity,
                    steps,
                    CanonicalSlots.Length,
                    $"t{tier}-{milestone.Quality.ToString().ToLowerInvariant()}-" +
                    milestone.Rarity.ToString().ToLowerInvariant()));
            }
        }

        var result = candidates
            .OrderBy(candidate => CalculateExpectedCombatRating(
                candidate.Tier,
                candidate.Quality,
                candidate.Steps,
                candidate.SlotCount))
            .ThenBy(candidate => candidate.Tier)
            .ThenBy(candidate => candidate.Quality)
            .ThenBy(candidate => candidate.Rarity)
            .Select((candidate, index) => new CanonicalEquipmentProgressionRung(
                index,
                candidate.Tier,
                candidate.Quality,
                candidate.Rarity,
                candidate.Steps,
                candidate.SlotCount,
                candidate.Id))
            .ToList();
        return result.AsReadOnly();
    }

    private double CalculateExpectedCombatRating(
        int tier,
        ItemQuality quality,
        int temperingSteps,
        int equippedSlotCount)
    {
        var directBaseline = CombatRatingCalculator.RemovePrimaryContributions(
            CreateCharacterBaseline());
        var baseRating = CombatRatingCalculator.CalculateCanonical(
            directBaseline,
            new Dictionary<AttributeType, double>(),
            tier).Overall;
        return baseRating + CalculateAuthorizedBudget(
            tier,
            quality,
            temperingSteps,
            equippedSlotCount);
    }

    private double CalculateAuthorizedBudget(
        int tier,
        ItemQuality quality,
        int temperingSteps,
        int equippedSlotCount) =>
        _slotWeights.Take(equippedSlotCount).Sum(slotWeight =>
            _balance.GetTierPowerBudget(tier)
            * slotWeight
            * _balance.GetQualityStatMultiplier(quality)
            + temperingSteps * TemperingConstants.GetDirectedImprovementBudget(tier));

    private static IReadOnlyDictionary<AttributeType, double> Weights(
        params (AttributeType Attribute, double Weight)[] entries) =>
        entries.ToDictionary(entry => entry.Attribute, entry => entry.Weight);

    private static void ApplyAttributeDelta(
        IDictionary<AttributeType, float> attributes,
        AttributeType attribute,
        float amount)
    {
        attributes[attribute] =
            (attributes.TryGetValue(attribute, out var current) ? current : 0f) + amount;
        if (AttributeCombatRules.IsPrimary(attribute))
            AttributeCombatRules.ApplyPrimaryDelta(attributes, attribute, amount);
    }

    private static Dictionary<AttributeType, float> CreateCharacterBaseline() =>
        EntityBaseAttributeHelper.CreateEntityAttributes(Guid.Empty)
            .ToDictionary(attribute => attribute.AttributeType, attribute => attribute.Value);
}
