using Application.Interfaces.Services.LL.PowerRatings;
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
        int slotIndex);
}

/// <summary>
/// Creates detached snapshots for the development-only raid roster shortcut.
/// The seeded guest's real level, equipment, inventory, and Essences are never changed.
/// </summary>
public sealed class RaidDevelopmentRosterFactory(
    CanonicalEquipmentBuildFactory canonicalBuilds)
    : IRaidDevelopmentRosterFactory
{
    public const int PowerMultiplier = 3;

    public RaidDevelopmentBuild Create(
        Guid characterId,
        string characterName,
        RaidBossDefinition boss,
        RaidBossTierDefinition tier,
        RaidLane lane,
        int slotIndex)
    {
        ArgumentNullException.ThrowIfNull(boss);
        ArgumentNullException.ThrowIfNull(tier);

        var rung = canonicalBuilds.GetProgressionLadder()
            .Where(candidate => candidate.Tier == tier.Tier)
            .OrderByDescending(candidate => candidate.Index)
            .First();
        var profile = lane switch
        {
            RaidLane.Flank => CanonicalPartyProfile.Area,
            RaidLane.Ward => CanonicalPartyProfile.Sustain,
            _ => slotIndex % 2 == 0
                ? CanonicalPartyProfile.Offense
                : CanonicalPartyProfile.Defensive
        };
        var build = canonicalBuilds.CreateBuildForArea(
            profile,
            rung,
            boss.LevelRequirement,
            CanonicalEquipmentBuildFactory.GetEssenceCountForDungeonTier(tier.Tier));
        var powerCarrier = build.Equipment.First();
        powerCarrier.InstanceModifiers.Add(new InstanceAttributeModifier(
            AttributeType.Power,
            (PowerMultiplier - 1) * 100f,
            ModifierType.Multiplicative));

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
            checked(CombatRatingDisplay.FromRaw(build.Rating.Overall) * PowerMultiplier),
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
