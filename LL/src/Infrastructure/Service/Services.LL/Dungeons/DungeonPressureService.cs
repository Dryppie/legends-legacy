using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonPressureService : IDungeonPressureService
{
    private static readonly IReadOnlyList<DungeonMechanicThreshold> DefaultThresholds =
    [
        new() { Id = "minor", Value = 25, Description = "Minor danger", RewardMultiplierBonusPercent = 10 },
        new() { Id = "moderate", Value = 50, Description = "Moderate danger", RewardMultiplierBonusPercent = 25 },
        new() { Id = "high", Value = 75, Description = "High danger", RewardMultiplierBonusPercent = 45 },
        new() { Id = "maximum", Value = 100, Description = "Maximum danger", RewardMultiplierBonusPercent = 75 }
    ];

    private readonly IDungeonDefinitions _dungeons;

    public DungeonPressureService(IDungeonDefinitions dungeons)
    {
        _dungeons = dungeons;
    }

    public DungeonPressureResult ApplyPressureDelta(DungeonRun run, int delta)
    {
        ArgumentNullException.ThrowIfNull(run);

        EnsureState(run);
        var mechanic = GetMechanic(run);
        HydrateMechanicState(run, mechanic);
        var previous = run.State.Pressure;

        run.State.Pressure = Math.Clamp(previous + delta, 0, Math.Max(1, mechanic.MaxValue));
        run.State.RewardMultiplierPercent =
            CalculateRewardMultiplierPercent(run.State.Pressure, mechanic) +
            run.State.Flags.GetValueOrDefault("reward_multiplier_bonus_pct");
        run.State.CurrentMechanicThresholds = GetActiveThresholds(run.State.Pressure, mechanic)
            .Select(x => new DungeonMechanicThresholdState
            {
                Id = x.Id,
                Value = x.Value,
                Description = x.Description,
                RewardMultiplierBonusPercent = x.RewardMultiplierBonusPercent
            })
            .ToList();

        return new DungeonPressureResult
        {
            PreviousPressure = previous,
            Pressure = run.State.Pressure,
            RewardMultiplierPercent = run.State.RewardMultiplierPercent,
            ActiveThresholdIds = run.State.CurrentMechanicThresholds
                .Select(x => x.Id)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()
        };
    }

    public int CalculateRewardMultiplierPercent(int pressure)
    {
        var thresholds = GetThresholds(null);
        var active = thresholds
            .Where(x => pressure >= x.Value)
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return 100 + (active?.RewardMultiplierBonusPercent ?? 0);
    }

    public IReadOnlyList<string> GetActivePressureThresholdIds(int pressure) =>
        GetThresholds(null)
            .Where(x => pressure >= x.Value)
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

    private static int CalculateRewardMultiplierPercent(int pressure, DungeonMechanicDefinition mechanic)
    {
        var active = GetActiveThresholds(pressure, mechanic)
            .OrderByDescending(x => x.Value)
            .FirstOrDefault();

        return 100 + (active?.RewardMultiplierBonusPercent ?? 0);
    }

    private static IReadOnlyList<DungeonMechanicThreshold> GetActiveThresholds(
        int pressure,
        DungeonMechanicDefinition mechanic) =>
        GetThresholds(mechanic)
            .Where(x => pressure >= x.Value)
            .ToList();

    private DungeonMechanicDefinition GetMechanic(DungeonRun run)
    {
        var mechanic = _dungeons.GetByKey(run.DungeonDefinitionId).Mechanic ?? new DungeonMechanicDefinition();
        if (mechanic.Thresholds.Count == 0)
        {
            mechanic.Thresholds = DefaultThresholds.ToList();
        }

        return mechanic;
    }

    private static void HydrateMechanicState(DungeonRun run, DungeonMechanicDefinition mechanic)
    {
        run.State.MechanicId = string.IsNullOrWhiteSpace(mechanic.Id)
            ? "pressure"
            : mechanic.Id;
        run.State.MechanicDisplayName = string.IsNullOrWhiteSpace(mechanic.DisplayName)
            ? "Pressure"
            : mechanic.DisplayName;
        run.State.MechanicMaxValue = Math.Max(1, mechanic.MaxValue);
    }

    private static IReadOnlyList<DungeonMechanicThreshold> GetThresholds(DungeonMechanicDefinition? mechanic)
    {
        if (mechanic?.Thresholds.Count > 0)
        {
            return mechanic.Thresholds;
        }

        return DefaultThresholds;
    }

    private static void EnsureState(DungeonRun run)
    {
        run.State ??= new DungeonRunState();
        run.State.RunId = run.Id;
        if (run.State.RewardMultiplierPercent <= 0)
        {
            run.State.RewardMultiplierPercent = 100;
        }
    }
}
