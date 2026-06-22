using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonCheckpointService : IDungeonCheckpointService
{
    private readonly IDungeonPressureService _pressure;
    private readonly IDungeonBoonService _boons;

    public DungeonCheckpointService(
        IDungeonPressureService pressure,
        IDungeonBoonService boons)
    {
        _pressure = pressure;
        _boons = boons;
    }

    public IReadOnlyList<DungeonCheckpointChoiceOption> EnsureChoices(DungeonRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        run.State.CurrentCheckpointChoices =
        [
            new()
            {
                Id = "withdraw",
                Label = "Withdraw",
                Description = "End the dungeon safely and keep your pending rewards."
            },
            new()
            {
                Id = "focus",
                Label = "Focus",
                Description = "Choose one temporary boon for the rest of this run."
            },
            new()
            {
                Id = "push_deeper",
                Label = "Push Deeper",
                Description = "Increase danger and improve the final reward multiplier.",
                PressureDelta = 15,
                RewardMultiplierDeltaPercent = 20
            },
            new()
            {
                Id = "rest",
                Label = "Rest",
                Description = "Reduce pressure, losing a small amount of unsecured loot.",
                PressureDelta = -10
            }
        ];

        return run.State.CurrentCheckpointChoices;
    }

    public DungeonCheckpointChoiceResult ApplyChoice(DungeonRun run, RoomInstance room, string choiceId)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(room);

        var choice = EnsureChoices(run)
            .FirstOrDefault(x => string.Equals(x.Id, choiceId, StringComparison.OrdinalIgnoreCase));

        if (choice is null)
        {
            throw new InvalidOperationException("The selected checkpoint choice is not available.");
        }

        var outcome = choice.Id switch
        {
            "withdraw" => ApplyWithdraw(run, room),
            "focus" => ApplyFocus(run),
            "push_deeper" => ApplyPushDeeper(run, choice),
            "rest" => ApplyRest(run, choice),
            _ => throw new InvalidOperationException("The selected checkpoint choice is not supported.")
        };

        return new DungeonCheckpointChoiceResult
        {
            Choice = choice,
            Outcome = outcome
        };
    }

    private static DungeonCheckpointChoiceOutcome ApplyWithdraw(DungeonRun run, RoomInstance room)
    {
        run.Status = DungeonRunStatus.Withdrawn;
        run.CompletedAt = DateTimeOffset.UtcNow;
        room.Status = RoomInstanceStatus.Completed;
        run.State.CurrentCheckpointChoices.Clear();
        run.State.SecuredLoot = CreateLootBagFromRun(run);
        run.State.UnsecuredLoot = new DungeonLootBag();

        return DungeonCheckpointChoiceOutcome.Withdraw;
    }

    private DungeonCheckpointChoiceOutcome ApplyFocus(DungeonRun run)
    {
        run.State.CurrentCheckpointChoices.Clear();
        if (_boons.GenerateBoonChoices(run).Count > 0)
        {
            AddFlag(run, "pending_boon_completes_room", 1);
        }

        return DungeonCheckpointChoiceOutcome.Focus;
    }

    private DungeonCheckpointChoiceOutcome ApplyPushDeeper(DungeonRun run, DungeonCheckpointChoiceOption choice)
    {
        AddFlag(run, "checkpoint_pushes", 1);
        AddFlag(run, "reward_multiplier_bonus_pct", choice.RewardMultiplierDeltaPercent);
        _pressure.ApplyPressureDelta(run, choice.PressureDelta);

        return DungeonCheckpointChoiceOutcome.PushDeeper;
    }

    private DungeonCheckpointChoiceOutcome ApplyRest(DungeonRun run, DungeonCheckpointChoiceOption choice)
    {
        _pressure.ApplyPressureDelta(run, choice.PressureDelta);
        ReduceUnsecuredLoot(run, 0.10m);

        return DungeonCheckpointChoiceOutcome.Rest;
    }

    private static void AddFlag(DungeonRun run, string flag, int amount)
    {
        run.State.Flags[flag] = run.State.Flags.GetValueOrDefault(flag) + amount;
    }

    private static void ReduceUnsecuredLoot(DungeonRun run, decimal percent)
    {
        var factor = Math.Clamp(1m - percent, 0m, 1m);
        run.PendingExperience = (int)Math.Floor(run.PendingExperience * factor);
        run.PendingCinders = (int)Math.Floor(run.PendingCinders * factor);
        run.PendingSoulstones = (int)Math.Floor(run.PendingSoulstones * factor);

        foreach (var reward in run.PendingRewards)
        {
            reward.Quantity = (int)Math.Floor(reward.Quantity * factor);
        }

        run.State.UnsecuredLoot = CreateLootBagFromRun(run);
    }

    private static DungeonLootBag CreateLootBagFromRun(DungeonRun run)
    {
        var bag = new DungeonLootBag
        {
            Experience = run.PendingExperience,
            Cinders = run.PendingCinders,
            Soulstones = run.PendingSoulstones
        };

        foreach (var reward in run.PendingRewards)
        {
            if (!string.IsNullOrWhiteSpace(reward.ItemId) && reward.Quantity > 0)
            {
                bag.Items[reward.ItemId] = bag.Items.GetValueOrDefault(reward.ItemId) + reward.Quantity;
            }
        }

        return bag;
    }
}
