namespace Domain.Models.WorldTower;

public static class WorldTowerPartyRules
{
    public const int MaximumPartySize = 5;

    public static int GetPartyCount(int requiredSlots)
    {
        if (requiredSlots <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredSlots));

        return checked((requiredSlots + MaximumPartySize - 1) / MaximumPartySize);
    }

    public static int GetPartyNumber(int partySlot)
    {
        if (partySlot <= 0)
            throw new ArgumentOutOfRangeException(nameof(partySlot));

        return checked((partySlot - 1) / MaximumPartySize + 1);
    }

    public static bool IsValidSlot(int? partySlot, int requiredSlots) =>
        !partySlot.HasValue || partySlot.Value >= 1 && partySlot.Value <= requiredSlots;

    public static bool HasCompletePartyLayout(TowerRally rally) =>
        rally.Participants.Count == rally.RequiredSlots
        && rally.Participants.All(participant => participant.PartySlot.HasValue)
        && rally.Participants.Select(participant => participant.PartySlot!.Value).Distinct().Count()
            == rally.RequiredSlots
        && rally.Participants.All(participant =>
            IsValidSlot(participant.PartySlot, rally.RequiredSlots));
}

public sealed record TowerPartyAssignment(Guid CharacterId, int? PartySlot);
