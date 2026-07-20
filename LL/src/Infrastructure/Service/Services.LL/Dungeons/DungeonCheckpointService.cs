using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonCheckpointService : IDungeonCheckpointService
{
    private readonly IDungeonVigorService _vigor;

    public DungeonCheckpointService(IDungeonVigorService vigor)
    {
        _vigor = vigor;
    }

    public IReadOnlyList<DungeonCheckpointChoiceOption> EnsureChoices(DungeonRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        run.State ??= new DungeonRunState { RunId = run.Id };

        if (run.State.WardstoneBoonChosen)
        {
            run.State.CurrentCheckpointChoices =
            [
                new()
                {
                    Id = "continue",
                    Label = "Continue",
                    Description = run.State.CurrentSection >= run.State.TotalSections
                        ? "Lock extraction at this Wardstone and approach the boss."
                        : "Lock extraction at this Wardstone and enter the next Section.",
                    Kind = "Decision"
                },
                new()
                {
                    Id = "extract",
                    Label = "Extract",
                    Description = "End the delve safely and keep all Pending Loot.",
                    Kind = "Decision"
                }
            ];
            return run.State.CurrentCheckpointChoices;
        }

        run.State.CurrentCheckpointChoices =
        [
            new()
            {
                Id = "recover",
                Label = "Recover",
                Description = "Restore Vigor based on delve tier.",
                VigorDelta = GetRecovery(run.DungeonDefinitionId),
                Kind = "Boon"
            }
        ];
        if (!run.State.WardstoneBoonIdsChosen.Contains("prepare", StringComparer.OrdinalIgnoreCase))
        {
            run.State.CurrentCheckpointChoices.Add(
                new()
                {
                    Id = "prepare",
                    Label = "Prepare",
                    Description = "Reduce the next combat Vigor toll by 3.",
                    Kind = "Boon"
                });
        }

        return run.State.CurrentCheckpointChoices;
    }

    public Task<DungeonCheckpointChoiceResult> ApplyChoiceAsync(
        DungeonRun run,
        RoomInstance room,
        string choiceId,
        CancellationToken cancellationToken)
    {
        var choice = EnsureChoices(run)
            .FirstOrDefault(candidate => string.Equals(candidate.Id, choiceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The selected Wardstone choice is not available.");

        var outcome = choice.Id switch
        {
            "recover" => ApplyRecover(run, room),
            "prepare" => ApplyPrepare(run),
            "continue" => ApplyContinue(run),
            "extract" => ApplyExtract(run, room),
            _ => throw new InvalidOperationException("The selected Wardstone choice is not supported.")
        };

        EnsureChoices(run);
        return Task.FromResult(new DungeonCheckpointChoiceResult { Choice = choice, Outcome = outcome });
    }

    private DungeonCheckpointChoiceOutcome ApplyRecover(DungeonRun run, RoomInstance room)
    {
        _vigor.RecoverAtWardstone(run, room);
        run.State.WardstoneBoonChosen = true;
        run.State.WardstoneBoonIdsChosen.Add("recover");
        return DungeonCheckpointChoiceOutcome.Recover;
    }

    private static DungeonCheckpointChoiceOutcome ApplyPrepare(DungeonRun run)
    {
        run.State.Flags["wardstone_prepared"] = 1;
        run.State.WardstoneBoonChosen = true;
        run.State.WardstoneBoonIdsChosen.Add("prepare");
        run.State.LastConsequence = "Prepared: the next combat Vigor toll is reduced by 3.";
        return DungeonCheckpointChoiceOutcome.Prepare;
    }

    private static DungeonCheckpointChoiceOutcome ApplyContinue(DungeonRun run)
    {
        run.State.ExtractionLocked = true;
        run.State.LastConsequence = "Extraction locked. The party continues deeper.";
        return DungeonCheckpointChoiceOutcome.Continue;
    }

    private static DungeonCheckpointChoiceOutcome ApplyExtract(DungeonRun run, RoomInstance room)
    {
        run.Status = DungeonRunStatus.Withdrawn;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.UsedCheckpointRetreat = true;
        room.Status = RoomInstanceStatus.Completed;
        run.State.SecuredLoot = CreateLootBagFromRun(run);
        run.State.UnsecuredLoot = new DungeonLootBag();
        run.State.LastConsequence = "Extracted safely. Pending Loot is secured.";
        return DungeonCheckpointChoiceOutcome.Extract;
    }

    private static int GetRecovery(string dungeonId) =>
        dungeonId.EndsWith("_iii", StringComparison.OrdinalIgnoreCase)
            ? 10
            : dungeonId.EndsWith("_ii", StringComparison.OrdinalIgnoreCase)
                ? 15
                : 20;

    private static DungeonLootBag CreateLootBagFromRun(DungeonRun run)
    {
        var bag = new DungeonLootBag
        {
            Experience = run.PendingExperience,
            Cinders = run.PendingCinders,
            Soulstones = run.PendingSoulstones
        };
        foreach (var reward in run.PendingRewards.Where(reward => reward.Quantity > 0))
        {
            bag.Items[reward.ItemId] = bag.Items.GetValueOrDefault(reward.ItemId) + reward.Quantity;
        }
        return bag;
    }
}
