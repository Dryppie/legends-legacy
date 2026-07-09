using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Bonuses;
using Domain.Models.Dungeons.Runs;
using Services.LL.Extensions;
using Services.LL.Interfaces;

namespace Services.LL.Dungeons;

public sealed class DungeonCheckpointService : IDungeonCheckpointService
{
    private readonly IDungeonPressureService _pressure;
    private readonly IDungeonBoonService _boons;
    private readonly IBonusService? _bonusService;

    public DungeonCheckpointService(
        IDungeonPressureService pressure,
        IDungeonBoonService boons,
        IBonusService? bonusService = null)
    {
        _pressure = pressure;
        _boons = boons;
        _bonusService = bonusService;
    }

    public DungeonCheckpointChoiceResult ApplyChoice(DungeonRun run, RoomInstance room, string choiceId)
    {
        return ApplyChoiceAsync(run, room, choiceId, CancellationToken.None).GetAwaiter().GetResult();
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

    public async Task<DungeonCheckpointChoiceResult> ApplyChoiceAsync(
        DungeonRun run,
        RoomInstance room,
        string choiceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(room);

        var choice = EnsureChoices(run)
            .FirstOrDefault(x => string.Equals(x.Id, choiceId, StringComparison.OrdinalIgnoreCase));

        if (choice is null)
        {
            throw new InvalidOperationException("The selected checkpoint choice is not available.");
        }

        var rewardRetentionBps = 0d;
        if (_bonusService is not null && string.Equals(choice.Id, "rest", StringComparison.OrdinalIgnoreCase))
        {
            var factors = await _bonusService.GetAggregatedAsync(run.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
            rewardRetentionBps = factors.Get(BonusKind.DungeonRewardRetentionBps);
        }

        var outcome = choice.Id switch
        {
            "withdraw" => ApplyWithdraw(run, room),
            "focus" => ApplyFocus(run),
            "push_deeper" => ApplyPushDeeper(run, choice),
            "rest" => ApplyRest(run, choice, rewardRetentionBps),
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

    private DungeonCheckpointChoiceOutcome ApplyRest(DungeonRun run, DungeonCheckpointChoiceOption choice, double rewardRetentionBps)
    {
        _pressure.ApplyPressureDelta(run, choice.PressureDelta);
        ReduceUnsecuredLoot(run, 0.10m, rewardRetentionBps);

        return DungeonCheckpointChoiceOutcome.Rest;
    }

    private static void AddFlag(DungeonRun run, string flag, int amount)
    {
        run.State.Flags[flag] = run.State.Flags.GetValueOrDefault(flag) + amount;
    }

    private static void ReduceUnsecuredLoot(DungeonRun run, decimal percent, double rewardRetentionBps)
    {
        var baseRetention = 1m - percent;
        var factor = Math.Clamp(baseRetention * rewardRetentionBps.ToPositiveMultiplierDecimal(), 0m, 1m);
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
