using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Dungeons.Mastery;

namespace Services.LL.Dungeons;

public sealed class DungeonVigorService : IDungeonVigorService
{
    public const int RestSiteRecovery = 15;
    public const double CombatTollMultiplier = 0.85d;

    public int ApplyCombatToll(DungeonRun run, RoomInstance room, CombatResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var maxHealth = Math.Max(1, result.PlayerTeam.Sum(entity => Math.Max(0, entity.MaxHealth)));
        var remainingHealth = result.PlayerTeam.Sum(entity =>
            Math.Clamp(entity.Health, 0, Math.Max(0, entity.MaxHealth)));
        var missingHealthPercent = 1d - remainingHealth / (double)maxHealth;
        var node = run.State.MapNodes
            .FirstOrDefault(candidate => candidate.RoomIndex == room.RoomIndex);
        var minimumToll = node is not null && node.VigorCostMin > 0
            ? node.VigorCostMin
            : 12;
        var maximumToll = node is not null && node.VigorCostMax >= minimumToll
            ? node.VigorCostMax
            : Math.Max(minimumToll, 22);
        var performanceToll = (int)Math.Round(
            (maximumToll - minimumToll) * Math.Clamp(missingHealthPercent, 0d, 1d),
            MidpointRounding.AwayFromZero);
        var combatToll = minimumToll + performanceToll;
        var masteryBenefits = DungeonMasteryBenefits.Resolve(run.State.MasteryLevelAtStart);
        var toll = Math.Max(0, ScaleCombatToll(combatToll) - masteryBenefits.CombatVigorCostReduction);

        return Apply(run, room, -Math.Clamp(toll, 0, 35), "Combat toll");
    }

    public static int ScaleCombatToll(int toll) =>
        (int)Math.Round(
            Math.Max(0, toll) * CombatTollMultiplier,
            MidpointRounding.AwayFromZero);

    public int RecoverAtRestSite(DungeonRun run, RoomInstance room)
    {
        var masteryBenefits = DungeonMasteryBenefits.Resolve(run.State.MasteryLevelAtStart);
        return Apply(
            run,
            room,
            RestSiteRecovery + masteryBenefits.RestSiteVigorBonus,
            "Rest Site recovery");
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
