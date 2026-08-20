using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Raids;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Raids;
using Domain.Models.Snapshots;
using Services.LL.PowerRatings;

namespace Services.LL.Raids;

public sealed record RaidDevelopmentBuild(
    int PowerRating,
    CharacterSnapshot Snapshot);

public interface IRaidDevelopmentRosterFactory
{
    RaidDevelopmentBuild Create(
        Guid characterId,
        string characterName,
        RaidBossDefinition boss,
        RaidBossTierDefinition tier,
        RaidLane lane,
        int slotIndex,
        double powerMultiplier);
}

/// <summary>
/// Creates detached snapshots for the development-only raid roster shortcut.
/// The seeded guest's real level, equipment, inventory, and Essences are never changed.
/// </summary>
public sealed class RaidDevelopmentRosterFactory(
    CanonicalEquipmentBuildFactory canonicalBuilds,
    IRaidPowerRecommendationStore powerRecommendations)
    : IRaidDevelopmentRosterFactory
{
    public const double DefaultPowerMultiplier = 1d;
    public const double MinimumPowerMultiplier = 0.5d;
    public const double MaximumPowerMultiplier = 2d;

    public static bool IsSupportedPowerMultiplier(double powerMultiplier) =>
        double.IsFinite(powerMultiplier)
        && powerMultiplier >= MinimumPowerMultiplier
        && powerMultiplier <= MaximumPowerMultiplier;

    public RaidDevelopmentBuild Create(
        Guid characterId,
        string characterName,
        RaidBossDefinition boss,
        RaidBossTierDefinition tier,
        RaidLane lane,
        int slotIndex,
        double powerMultiplier)
    {
        ArgumentNullException.ThrowIfNull(boss);
        ArgumentNullException.ThrowIfNull(tier);
        if (!IsSupportedPowerMultiplier(powerMultiplier))
        {
            throw new ArgumentOutOfRangeException(
                nameof(powerMultiplier),
                powerMultiplier,
                $"Development roster power must be between {MinimumPowerMultiplier:0.##}x and {MaximumPowerMultiplier:0.##}x.");
        }

        var ladder = canonicalBuilds.GetProgressionLadder();
        var rung = powerRecommendations.TryGet(boss.Id, 0, out var recommendation)
            ? ladder.SingleOrDefault(candidate => candidate.Id.Equals(
                recommendation.CanonicalRungId,
                StringComparison.OrdinalIgnoreCase))
            : null;
        rung ??= ladder
            .Where(candidate => candidate.Tier == Math.Clamp(tier.Tier + 1, 1, 100))
            .OrderByDescending(candidate => candidate.Index)
            .FirstOrDefault();
        rung ??= ladder[^1];
        var role = CanonicalCooperativeRosterCatalog.ResolveRaidRole(
            lane,
            slotIndex,
            tier.LaneSlots);
        var build = canonicalBuilds.CreateBuildForArea(
            role,
            rung,
            boss.LevelRequirement,
            CanonicalEquipmentBuildFactory.GetEssenceCountForDungeonTier(Math.Clamp(tier.Tier + 1, 1, 3)));
        if (Math.Abs(powerMultiplier - DefaultPowerMultiplier) > double.Epsilon)
        {
            var powerCarrier = build.Equipment.First();
            powerCarrier.InstanceModifiers.Add(new InstanceAttributeModifier(
                AttributeType.Power,
                (float)((powerMultiplier - 1d) * 100d),
                ModifierType.Multiplicative));
        }

        var snapshotId = Guid.NewGuid();
        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = characterId,
            Name = characterName,
            Level = boss.LevelRequirement,
            BaseAttributes = build.Character.BaseAttributes
                .Select(attribute => new EntityAttributeSnapshot
                {
                    CharacterSnapshotId = snapshotId,
                    AttributeType = attribute.AttributeType,
                    Value = attribute.Value
                })
                .ToList(),
            Equipment = build.Equipment
                .Select(equipment => EquipmentSnapshot.From(
                    ToSlot(equipment.EquipmentBase.EquipmentType),
                    equipment))
                .ToList(),
            EquippedEssences = build.EquippedEssences
                .Select((essence, index) =>
                    EquippedEssenceSnapshot.From(snapshotId, index, essence))
                .ToList()
        };

        return new RaidDevelopmentBuild(
            checked((int)Math.Round(
                CombatRatingDisplay.FromRaw(build.Rating.Overall) * powerMultiplier,
                MidpointRounding.AwayFromZero)),
            snapshot);
    }

    private static EquipmentSlotType ToSlot(EquipmentType type) => type switch
    {
        EquipmentType.Head => EquipmentSlotType.Head,
        EquipmentType.Relic => EquipmentSlotType.Relic,
        EquipmentType.Chest => EquipmentSlotType.Chest,
        EquipmentType.Necklace => EquipmentSlotType.Necklace,
        EquipmentType.Legs => EquipmentSlotType.Legs,
        EquipmentType.Ring => EquipmentSlotType.Ring,
        EquipmentType.OneHanded or EquipmentType.TwoHanded => EquipmentSlotType.MainHand,
        EquipmentType.OffHand => EquipmentSlotType.OffHand,
        EquipmentType.Tool => EquipmentSlotType.Tool,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported equipment slot.")
    };
}
