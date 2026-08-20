using Domain.Models.Raids;

namespace Services.LL.PowerRatings;

/// <summary>
/// Behavioral roles used only by deterministic cooperative balance rosters.
/// These are not player classes and do not force a role onto a live character.
/// </summary>
public enum CanonicalCooperativeRole
{
    Guardian,
    Restorer,
    Striker,
    Controller,
    AreaSpecialist,
    DefensiveHybrid
}

public sealed record CanonicalCooperativeRosterSlot(
    CanonicalCooperativeRole Role,
    int PartyNumber,
    int SlotIndex);

/// <summary>
/// Versioned, deterministic compositions used to calibrate cooperative content.
/// One Guardian and one Restorer form the stable core of every five-character cell.
/// </summary>
public static class CanonicalCooperativeRosterCatalog
{
    public const int Version = 2;

    private static readonly CanonicalCooperativeRole[] CellRoles =
    [
        CanonicalCooperativeRole.Guardian,
        CanonicalCooperativeRole.Restorer,
        CanonicalCooperativeRole.Striker,
        CanonicalCooperativeRole.Striker,
        CanonicalCooperativeRole.Controller
    ];

    public static IReadOnlyList<CanonicalCooperativeRosterSlot> CreateParty(int partySize)
    {
        if (partySize <= 0 || partySize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(partySize),
                partySize,
                "Canonical cooperative rosters must contain between one and 100 characters.");
        }

        return Enumerable.Range(0, partySize)
            .Select(slotIndex => new CanonicalCooperativeRosterSlot(
                CellRoles[slotIndex % CellRoles.Length],
                slotIndex / CellRoles.Length + 1,
                slotIndex))
            .ToArray();
    }

    public static CanonicalCooperativeRole ResolveRaidRole(
        RaidLane lane,
        int slotIndex,
        int laneSlots)
    {
        if (!RaidParties.IsAssignable(lane))
            throw new ArgumentOutOfRangeException(nameof(lane), lane, "Only raid parties have canonical roles.");
        if (laneSlots <= 0)
            throw new ArgumentOutOfRangeException(nameof(laneSlots));
        if (slotIndex < 0 || slotIndex >= laneSlots)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        if (slotIndex == 0)
            return CanonicalCooperativeRole.Guardian;
        if (slotIndex == 1 && laneSlots >= 2)
            return CanonicalCooperativeRole.Restorer;

        return lane switch
        {
            RaidLane.Rearguard => CanonicalCooperativeRole.AreaSpecialist,
            RaidLane.Vanguard => CanonicalCooperativeRole.Striker,
            RaidLane.MainGuard => CanonicalCooperativeRole.DefensiveHybrid,
            _ => throw new ArgumentOutOfRangeException(nameof(lane), lane, null)
        };
    }

    public static CanonicalPartyProfile EquipmentProfileFor(CanonicalCooperativeRole role) =>
        role switch
        {
            CanonicalCooperativeRole.Guardian => CanonicalPartyProfile.Defensive,
            CanonicalCooperativeRole.Restorer => CanonicalPartyProfile.Sustain,
            CanonicalCooperativeRole.Striker => CanonicalPartyProfile.Offense,
            CanonicalCooperativeRole.Controller => CanonicalPartyProfile.Balanced,
            CanonicalCooperativeRole.AreaSpecialist => CanonicalPartyProfile.Area,
            CanonicalCooperativeRole.DefensiveHybrid => CanonicalPartyProfile.Defensive,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

}
