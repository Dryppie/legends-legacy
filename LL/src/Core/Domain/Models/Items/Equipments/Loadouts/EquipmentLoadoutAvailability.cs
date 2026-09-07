using Domain.Models.Items.Equipments.Progression;

namespace Domain.Models.Items.Equipments.Loadouts;

public static class EquipmentLoadoutAvailability
{
    public static HashSet<Guid> GetAvailableItemIds(int characterLevel, Guid characterId,
        IEnumerable<EquipmentInstance> items, IReadOnlyCollection<Guid> borrowedItemIds) =>
        items.Where(item => item.ProgressionData is { } data &&
                characterLevel >= EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(item.Tier) &&
                (data.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned
                    ? borrowedItemIds.Contains(item.Id)
                    : data.State.Ownership.OwnerId == characterId))
            .Select(x => x.Id).ToHashSet();
}
