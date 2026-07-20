using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonVigorService : IDungeonVigorService
{
    public int ApplyCombatToll(DungeonRun run, RoomInstance room, CombatResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var playerIds = result.PlayerTeam
            .Select(entity => entity.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var maxHealth = Math.Max(1, result.PlayerTeam.Sum(entity => Math.Max(0, entity.MaxHealth)));
        var damageTaken = result.EntityStats
            .Where(stats => playerIds.Contains(stats.EntityId))
            .Sum(stats => Math.Max(0, stats.DamageTaken));
        var damagePercent = damageTaken * 100d / maxHealth;
        var downedMembers = result.PlayerTeam.Count(entity => entity.Health <= 0);
        var omenModifier = run.State.ActiveOmens.Sum(omen => omen.CombatTollModifier);
        var preparedReduction = run.State.Flags.Remove("wardstone_prepared") ? 3 : 0;
        var toll = 3
            + (int)Math.Round(0.15d * damagePercent, MidpointRounding.AwayFromZero)
            + (downedMembers * 8)
            + omenModifier
            - preparedReduction;

        return Apply(run, room, -Math.Clamp(toll, 0, 25), "Combat toll");
    }

    public int ApplyHazardToll(DungeonRun run, RoomInstance room, int baseToll)
    {
        var tier = GetTier(run.DungeonDefinitionId);
        var tierOneReduction = tier == 1 ? (int)Math.Round(baseToll * .25d, MidpointRounding.AwayFromZero) : 0;
        var omenModifier = run.State.ActiveOmens.Sum(omen => omen.HazardTollModifier);
        var toll = Math.Clamp(baseToll - tierOneReduction + omenModifier, 0, 25);
        return Apply(run, room, -toll, "Hazard toll");
    }

    public int ApplyEventChange(DungeonRun run, RoomInstance room, int amount, string reason) =>
        Apply(
            run,
            room,
            Math.Clamp(amount, -25, 25),
            string.IsNullOrWhiteSpace(reason) ? "Event consequence" : reason);

    public int RecoverAtWardstone(DungeonRun run, RoomInstance room)
    {
        var recovery = GetTier(run.DungeonDefinitionId) switch
        {
            1 => 20,
            3 => 10,
            _ => 15
        };
        return Apply(run, room, recovery, "Wardstone recovery");
    }

    public void RefreshState(DungeonRun run)
    {
        run.State.Vigor = Math.Clamp(run.State.Vigor, 0, 100);
        var exhaustedAt = GetTier(run.DungeonDefinitionId) == 3 ? 30 : 25;
        run.State.VigorState = run.State.Vigor == 0
            ? "Spent"
            : run.State.Vigor <= exhaustedAt
                ? "Exhausted"
                : run.State.Vigor <= 40
                    ? "Strained"
                    : "Steady";
        run.State.VigorThresholds =
        [
            new()
            {
                State = "Steady",
                MinimumVigor = 41,
                MaximumVigor = 100,
                Summary = "The expedition is operating at full capability.",
                Effects = ["No Vigor penalties."],
                IsCurrent = run.State.VigorState == "Steady"
            },
            new()
            {
                State = "Strained",
                MinimumVigor = exhaustedAt + 1,
                MaximumVigor = 40,
                Summary = "Fatigue makes the route ahead harder to judge.",
                Effects = ["Displayed route Vigor forecasts widen by 2."],
                IsCurrent = run.State.VigorState == "Strained"
            },
            new()
            {
                State = "Exhausted",
                MinimumVigor = 1,
                MaximumVigor = exhaustedAt,
                Summary = "The party enters combat in a weakened state.",
                Effects =
                [
                    "Party members enter combat at 90% maximum health.",
                    "Displayed route Vigor forecasts widen by 2."
                ],
                IsCurrent = run.State.VigorState == "Exhausted"
            },
            new()
            {
                State = "Spent",
                MinimumVigor = 0,
                MaximumVigor = 0,
                Summary = "The expedition can no longer continue.",
                Effects =
                [
                    "The run fails at the current breakpoint.",
                    "Pending Loot is lost."
                ],
                IsCurrent = run.State.VigorState == "Spent"
            }
        ];
    }

    private int Apply(DungeonRun run, RoomInstance room, int amount, string reason)
    {
        var before = run.State.Vigor;
        run.State.Vigor = Math.Clamp(before + amount, 0, 100);
        RefreshState(run);
        var actual = run.State.Vigor - before;
        run.State.VigorHistory.Add(new DungeonVigorChange
        {
            RoomIndex = room.RoomIndex,
            Amount = actual,
            VigorAfter = run.State.Vigor,
            Reason = reason
        });
        run.State.LastConsequence = actual >= 0
            ? $"{reason}: +{actual} Vigor ({run.State.VigorState})."
            : $"{reason}: {actual} Vigor ({run.State.VigorState}).";
        return actual;
    }

    private static int GetTier(string dungeonDefinitionId) =>
        dungeonDefinitionId.EndsWith("_iii", StringComparison.OrdinalIgnoreCase)
            ? 3
            : dungeonDefinitionId.EndsWith("_ii", StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;
}
