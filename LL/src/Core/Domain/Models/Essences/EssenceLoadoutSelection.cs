namespace Domain.Models.Essences;

public static class EssenceLoadoutSelection
{
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
        var ordered = InArchiveOrder(loadouts).ToList();
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
