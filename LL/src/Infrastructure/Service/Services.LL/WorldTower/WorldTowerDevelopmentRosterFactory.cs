using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Services.LL.PowerRatings;

namespace Services.LL.WorldTower;

public sealed record WorldTowerDevelopmentBuild(
    int PowerRating,
    CharacterSnapshot Snapshot);

public interface IWorldTowerDevelopmentRosterFactory
{
    WorldTowerDevelopmentBuild Create(
        Guid characterId,
        string characterName,
        TowerFloorDefinition floor,
        int rosterIndex);
}

/// <summary>
/// Creates detached snapshots for the development-only roster shortcut.
/// The seeded guest's real level, equipment, inventory, and Essences are never changed.
/// </summary>
public sealed class WorldTowerDevelopmentRosterFactory(
    CanonicalEquipmentBuildFactory canonicalBuilds)
    : IWorldTowerDevelopmentRosterFactory
{
    private static readonly CanonicalPartyProfile[] MixedRosterProfiles =
    [
        CanonicalPartyProfile.Offense,
        CanonicalPartyProfile.Balanced,
        CanonicalPartyProfile.Sustain,
        CanonicalPartyProfile.Defensive,
        CanonicalPartyProfile.Area
    ];

    public WorldTowerDevelopmentBuild Create(
        Guid characterId,
        string characterName,
        TowerFloorDefinition floor,
        int rosterIndex)
    {
        ArgumentNullException.ThrowIfNull(floor);
        var benchmark = floor.BalanceBenchmark;
        var rung = canonicalBuilds.GetProgressionLadder().Single(candidate =>
            candidate.Id.Equals(benchmark.BuildId, StringComparison.OrdinalIgnoreCase));
        var profile = MixedRosterProfiles[
            Math.Abs(rosterIndex % MixedRosterProfiles.Length)];
        var build = canonicalBuilds.CreateBuildForArea(
            profile,
            rung,
            benchmark.CharacterLevel,
            benchmark.EssenceCount);
        var snapshotId = Guid.NewGuid();
        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = characterId,
            Name = characterName,
            Level = benchmark.CharacterLevel,
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

        return new WorldTowerDevelopmentBuild(
            CombatRatingDisplay.FromRaw(build.Rating.Overall),
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
