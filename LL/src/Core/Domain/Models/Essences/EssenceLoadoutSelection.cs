namespace Domain.Models.Essences;

public static class EssenceLoadoutSelection
{
    public static void SetAvailability(IEnumerable<EssenceLoadout> loadouts, int limit)
    {
        var legacySlot = 0;
        foreach (var loadout in loadouts.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            loadout.IsUsable = (loadout.PresetSlot > 0 ? loadout.PresetSlot : ++legacySlot) <= limit;
    }
    public const EssenceCombatActivity AllActivities =
        EssenceCombatActivity.IdleCombat |
        EssenceCombatActivity.Dungeon |
        EssenceCombatActivity.Raid |
        EssenceCombatActivity.WorldTower |
        EssenceCombatActivity.Arena |
        EssenceCombatActivity.Tournament |
        EssenceCombatActivity.RegionBoss;

    public static IOrderedEnumerable<EssenceLoadout> InArchiveOrder(IEnumerable<EssenceLoadout> loadouts) =>
        loadouts
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id);

    public static EssenceLoadout? Select(
        IEnumerable<EssenceLoadout> loadouts,
        EssenceCombatActivity activity)
    {
        var ordered = InArchiveOrder(loadouts.Where(x => x.IsUsable)).ToList();
        if (activity != EssenceCombatActivity.None)
        {
            var assigned = ordered.FirstOrDefault(loadout =>
                (loadout.AutoUseActivities & activity) == activity);
            if (assigned is not null)
            {
                return assigned;
            }
        }

        return ordered.FirstOrDefault();
    }

    public static bool IsValidSingleActivity(EssenceCombatActivity activity) =>
        activity != EssenceCombatActivity.None &&
        (activity & ~AllActivities) == 0 &&
        (((int)activity & ((int)activity - 1)) == 0);
}
