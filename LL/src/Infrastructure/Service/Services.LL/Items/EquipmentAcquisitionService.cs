using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Outbox;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.Extensions.Options;

namespace Services.LL.Items;

public sealed class EquipmentAcquisitionEligibility(EquipmentAcquisitionCatalog catalog, IEquipmentAcquisitionRepository repository,
    IQuestRepository quests, IOptions<EquipmentProgressionOptions> options) : IEquipmentAcquisitionEligibility
{
    public async Task<string?> GetErrorAsync(Guid id, string dungeonId, CancellationToken ct)
    {
        if (!options.Value.ProtectedAcquisitionEnabled || catalog.FindDungeon(dungeonId) is not { } pool) return null;
        var level = await repository.GetLevelAsync(id, ct);
        if (level is null) return "Character was not found.";
        if (level < pool.MinimumLevel) return $"Requires level {pool.MinimumLevel}.";
        if (pool.RequiredQuestId != null && (await quests.GetProgressAsync(id, pool.RequiredQuestId, ct))?.Status != QuestStatus.Completed)
            return "Complete this dungeon's prerequisite quest first.";
        return null;
    }
}

public sealed class EquipmentAcquisitionService(EquipmentAcquisitionCatalog catalog, IEquipmentAcquisitionRepository repository,
    IStarterEquipmentRepository starters, IDungeonDefinitions dungeons, IDungeonAccessPolicy access,
    IDungeonRunRepository runs, IGameEventOutbox outbox, IOptions<EquipmentProgressionOptions> options,
    TimeProvider clock, IEquipmentAcquisitionEligibility eligibility) : IEquipmentAcquisitionService
{
    public async Task<IReadOnlyList<EquipmentProtectionPoolView>> GetPoolsAsync(Guid id, CancellationToken ct)
    {
        if (!options.Value.ProtectedAcquisitionEnabled) return [];
        var result = new List<EquipmentProtectionPoolView>();
        foreach (var pool in catalog.Pools) result.Add(await ViewAsync(id, pool, ct));
        return result;
    }

    public async Task<EquipmentProgressionTargetSelectionResult> SelectAsync(Guid id, string poolId, string? definitionId, CancellationToken ct)
    {
        if (!options.Value.ProtectedAcquisitionEnabled) return new(null, "Protected equipment acquisition is not available yet.");
        var pool = catalog.Pools.SingleOrDefault(x => x.Id == poolId);
        if (pool == null || definitionId != null && !pool.TargetDefinitionIds.Contains(definitionId, StringComparer.Ordinal))
            return new(null, "Select an eligible target from this source.");
        await repository.LockAsync(id, ct);
        var view = await ViewAsync(id, pool, ct);
        if (!view.CanSelect) return new(view, string.Join(" ", view.MissingRequirements));
        var progress = await ProgressAsync(id, pool.Id, ct);
        progress.Select(definitionId);
        return new(view with { SelectedDefinitionId = progress.SelectedDefinitionId, Progress = progress.CompletionsWithoutMatch }, null);
    }

    private async Task<EquipmentProtectionPoolView> ViewAsync(Guid id, EquipmentProtectionPool pool, CancellationToken ct)
    {
        var eligibility = await access.EvaluateForSigilAssemblyAsync(id, dungeons.GetByKey(pool.DungeonId), ct);
        var progress = await repository.GetProgressAsync(id, pool.Id, ct);
        // Stable preview IDs are not award IDs; actual descriptors are frozen with new IDs at commitment.
        var previews = pool.TargetDefinitionIds.Select(definition => EquipmentData.Create(EquipmentState.Award(
            PreviewId(definition), catalog.Evaluator, definition, pool.EquipmentTier, 1,
            new(EquipmentAwardKind.ProtectedReward, pool.DungeonId, "preview"),
            new(EquipmentOwnershipKind.BoundPersonal, id)), catalog.Evaluator)).ToArray();
        return new(pool, progress?.SelectedDefinitionId, progress?.CompletionsWithoutMatch ?? 0,
            eligibility.CanEnter, eligibility.MissingRequirements, previews,
            pool.Difficulty == 1 && !await runs.HasCompletedDungeonAsync(id, pool.DungeonId, ct));
    }

    public async Task FreezeAsync(DungeonRun run, DungeonDefinition dungeon, CancellationToken ct)
    {
        if (!options.Value.ProtectedAcquisitionEnabled || catalog.FindDungeon(dungeon.Id) is not { } pool) return;
        if (run.EquipmentCommitment != null) return;
        if (run.DungeonDefinitionId != dungeon.Id || run.Status != DungeonRunStatus.Active || run.Id == Guid.Empty
            || run.CharacterId == Guid.Empty || (int)dungeon.Grade != pool.Difficulty || dungeon.Region != pool.Region)
            throw new InvalidOperationException("Dungeon does not match its equipment protection pool.");
        var error = await eligibility.GetErrorAsync(run.CharacterId, dungeon.Id, ct);
        if (error != null) throw new InvalidOperationException(error);
        var progress = await repository.GetProgressAsync(run.CharacterId, pool.Id, ct);
        EquipmentData? target = null;
        if (progress?.SelectedDefinitionId is { } selected)
        {
            if (!pool.TargetDefinitionIds.Contains(selected, StringComparer.Ordinal))
                throw new InvalidOperationException("Select a current eligible equipment target before starting this dungeon.");
            target = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), catalog.Evaluator, selected,
                pool.EquipmentTier, 1, new(EquipmentAwardKind.RandomDiscovery, dungeon.Id, run.Id.ToString("N")),
                new(EquipmentOwnershipKind.UnboundPersonal, run.CharacterId)), catalog.Evaluator);
        }
        run.EquipmentCommitment = new(run.CharacterId, run.Id, pool.Id, dungeon.Id, pool.Difficulty,
            pool.MatchingChance, pool.GuaranteeCompletions, pool.CompletionScrap, target);
    }

    public async Task CompleteAsync(DungeonRun run, bool firstCompletion, CancellationToken ct)
    {
        // Honor committed runs even when the acquisition flag or content changes during the run.
        if (run.EquipmentCommitment is not { } commitment || run.Status != DungeonRunStatus.Completed) return;
        if (commitment.CharacterId != run.CharacterId || commitment.RunId != run.Id || commitment.DungeonId != run.DungeonDefinitionId)
            throw new InvalidOperationException("The protected reward does not belong to this run.");
        await repository.LockAsync(run.CharacterId, ct);
        if (await repository.GetCompletionAsync(run.CharacterId, run.Id, ct) != null) return;
        var progress = await ProgressAsync(run.CharacterId, commitment.PoolId, ct);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(EquipmentKeys.SourcePrefix + $"{run.Id:N}:{run.Seed}:{commitment.PoolId}"));
        var roll = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(digest) / 4294967296d;
        var outcome = progress.Complete(commitment, firstCompletion, roll, clock.GetUtcNow());
        if (outcome.Equipment is { } equipment)
            await runs.AddPendingRewardAsync(run, new RunReward { Id = equipment.State.Id, ItemId = equipment.ItemBaseId,
                Name = equipment.DisplayName, ItemType = ItemType.Equipment, Quantity = 1, Source = EquipmentKeys.ProtectedDungeonSource,
                ProgressionData = equipment }, ct);
        if (outcome.Scrap > 0)
            await runs.AddPendingRewardAsync(run, new RunReward { ItemId = "tempered_scrap", Name = "Tempered Scrap",
                ItemType = ItemType.Resource, Quantity = outcome.Scrap, Source = EquipmentKeys.DungeonCompletionSource }, ct);
        repository.AddCompletion(new() { CharacterId = run.CharacterId, RunId = run.Id, Outcome = outcome });
        await outbox.EnqueueAsync(GameEventTypes.EquipmentSecured, outcome, run.CharacterId, null, ct);
    }

    public async Task MarkClaimedAsync(DungeonRun run, CancellationToken ct)
    {
        if (run.EquipmentCommitment is null) return;
        var receipt = await repository.GetCompletionAsync(run.CharacterId, run.Id, ct);
        if (receipt != null) receipt.ClaimedAtUtc ??= clock.GetUtcNow();
    }

    public async Task<IReadOnlyList<BaselineEquipmentRecoveryOption>> GetRecoveryOptionsAsync(Guid id, CancellationToken ct)
    {
        if (!options.Value.BaselineRecoveryEnabled) return [];
        var owned = await repository.GetOwnedAndPendingAsync(id, ct);
        var result = new List<BaselineEquipmentRecoveryOption>();
        foreach (var kind in Enum.GetValues<StarterEquipmentGrantKind>())
            if (await starters.GetGrantAsync(id, kind, ct) is { } grant) result.AddRange(BaselineEquipmentRecoveryPolicy.Options(grant, owned));
        return result;
    }

    public async Task<BaselineEquipmentRecoveryResult> RecoverAsync(Guid id, Guid operationId, StarterEquipmentGrantKind kind, CancellationToken ct)
    {
        if (!options.Value.BaselineRecoveryEnabled) return new(null, "Baseline equipment recovery is not available yet.");
        if (id == Guid.Empty || operationId == Guid.Empty || !Enum.IsDefined(kind)) return new(null, "Invalid recovery request.");
        await repository.LockAsync(id, ct);
        var existing = await repository.GetRecoveryAsync(id, operationId, ct);
        if (existing != null) return existing.Outcome.Kind == kind ? new(existing.Outcome, null) : new(null, "Operation ID belongs to another recovery request.");
        var grant = await starters.GetGrantAsync(id, kind, ct);
        if (grant == null) return new(null, "Claim the original baseline equipment reward first.");
        var owned = await repository.GetOwnedAndPendingAsync(id, ct);
        var recovery = BaselineEquipmentRecoveryPolicy.Recover(grant, owned, operationId, clock.GetUtcNow());
        await repository.AwardRecoveryAsync(id, recovery, ct);
        repository.AddRecovery(new() { CharacterId = id, OperationId = operationId, Outcome = recovery });
        // A dedicated event cannot accidentally grant generic loot/quest/profession rewards.
        if (recovery.Equipment.Count > 0)
            await outbox.EnqueueAsync(GameEventTypes.BaselineEquipmentRecovered, recovery, id, null, ct);
        return new(recovery, null);
    }

    private async Task<EquipmentProtectionProgress> ProgressAsync(Guid id, string poolId, CancellationToken ct)
    {
        var progress = await repository.GetProgressAsync(id, poolId, ct);
        if (progress != null) return progress;
        progress = new() { CharacterId = id, PoolId = poolId };
        repository.AddProgress(progress);
        return progress;
    }
    private static Guid PreviewId(string definition) => new(SHA256.HashData(Encoding.UTF8.GetBytes(definition)).AsSpan(0, 16));
}
