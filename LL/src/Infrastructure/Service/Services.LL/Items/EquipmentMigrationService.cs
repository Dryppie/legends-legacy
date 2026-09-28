using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Analytics;

namespace Services.LL.Items;

public sealed class EquipmentMigrationService(IEquipmentMigrationRepository repository,
    EquipmentMigrationCatalog catalog, IGameEventOutbox outbox,
    TimeProvider time, IEquipmentUpgradeRepository upgrades, ICombatStyleMutationBoundary? boundary = null,
    Domain.Models.Attributes.AttributeRulesSelection? liveRules = null) : IEquipmentMigrationService
{
    public async Task<EquipmentMigrationChoice?> GetChoiceAsync(Guid characterId, Guid itemId, CancellationToken ct)
    {
        if (liveRules?.Version == Domain.Models.Attributes.AttributeRules.LegacyVersion) return null;
        var context = await upgrades.LoadAsync(characterId, itemId, false, ct);
        if (context?.UnavailableReason is not null || context?.Equipment?.ProgressionData is not { } current
            || current.State.Ownership.OwnerId != characterId || current.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned) return null;
        var receipt = await repository.GetActiveReceiptForItemAsync(itemId, ct);
        if (receipt is null || receipt.RespecializationAllowance == 0 || receipt.ChoiceUsedAtUtc.HasValue
            || current.State.ActiveStyleId != EquipmentData.Deserialize(receipt.AfterJson).State.ActiveStyleId) return null;
        var evaluator = catalog.Get(current.State.BalanceVersion).Evaluator;
        return new(receipt.OperationId, EquipmentMigrationPolicy.CompatibleDefinitions(current, evaluator)
            .Select(definition => ChangeSpecialization(current, definition.Id, evaluator)).ToArray());
    }

    public async Task<EquipmentMigrationPreview> PreviewAsync(EquipmentMigrationTarget target, string? definitionId, CancellationToken ct,
        int? targetBalanceVersion = null)
    {
        var proposal = await LoadPreviewAsync(target, false, definitionId, targetBalanceVersion, ct);
        var previous = await repository.GetActiveReceiptForItemAsync(target.ItemId, ct);
        return proposal with { RespecializationAllowance = RemainingAllowance(proposal, previous) };
    }

    private async Task<EquipmentMigrationPreview> LoadPreviewAsync(EquipmentMigrationTarget target, bool forMutation,
        string? definitionId, int? version, CancellationToken ct)
    {
        var before = await repository.LoadAsync(target, forMutation, ct);
        if (before is not null) return Preview(target, before, definitionId, version);
        var legacy = await repository.LoadLegacyAsync(target, ct)
            ?? throw new InvalidOperationException("Equipment was not found.");
        return legacy.Preview(target, catalog.Get(version).Evaluator,
            catalog.Versions?.Get(1).Evaluator.Balance ?? new EquipmentBalance(1), definitionId);
    }

    private EquipmentMigrationPreview Preview(EquipmentMigrationTarget target, EquipmentData before, string? definitionId, int? version) =>
        EquipmentMigrationPolicy.Preview(target, before, catalog.Get(version).Evaluator, definitionId,
            catalog.Versions?.Get(before.State.BalanceVersion).Evaluator.Balance);

    private static int RemainingAllowance(EquipmentMigrationPreview proposal, EquipmentMigrationReceipt? previous) =>
        proposal.RespecializationAllowance > 0
        || (previous is { RespecializationAllowance: > 0, ChoiceUsedAtUtc: null }
            && proposal.Before?.State.ActiveStyleId == EquipmentData.Deserialize(previous.AfterJson).State.ActiveStyleId) ? 1 : 0;

    public async Task<EquipmentMigrationReceipt> ApplyAsync(Guid operationId, EquipmentMigrationTarget target,
        string sourceHash, string definitionId, string actorId, CancellationToken ct,
        int? targetBalanceVersion = null, string? expectedResultHash = null)
    {
        if (operationId == Guid.Empty || string.IsNullOrWhiteSpace(actorId)) throw new InvalidOperationException("An operation and actor are required.");
        var completed = await repository.GetReceiptAsync(operationId, ct);
        if (completed is not null) return MatchReceipt(completed, target, sourceHash, definitionId, targetBalanceVersion, expectedResultHash);
        await PrepareAsync(target, ct);
        // Lock before rechecking the idempotency key, including unversioned imports.
        await repository.LoadAsync(target, true, ct);
        var existing = await repository.GetReceiptAsync(operationId, ct);
        if (existing is not null)
            return MatchReceipt(existing, target, sourceHash, definitionId, targetBalanceVersion, expectedResultHash);
        var proposal = await LoadPreviewAsync(target, false, definitionId, targetBalanceVersion, ct);
        if (proposal.SourceHash != sourceHash) throw new InvalidOperationException("Equipment changed after the preview. Preview it again.");
        if ((targetBalanceVersion.HasValue || proposal.SourceBalanceVersion > 1 || proposal.LegacyBefore is not null)
            && string.IsNullOrWhiteSpace(expectedResultHash))
            throw new InvalidOperationException("An explicit rebalance requires the result hash returned by preview.");
        if (expectedResultHash is not null && expectedResultHash != proposal.ResultHash)
            throw new InvalidOperationException("The target balance changed after preview. Preview it again.");
        var previous = await repository.GetActiveReceiptForItemAsync(target.ItemId, ct);
        var receipt = new EquipmentMigrationReceipt
        {
            OperationId = operationId, Revision = checked((previous?.Revision ?? -1) + 1),
            RespecializationAllowance = RemainingAllowance(proposal, previous), ItemId = target.ItemId, Location = target.Location, ContainerId = target.ContainerId,
            ActorId = actorId, SourceHash = sourceHash, ResultHash = EquipmentMigrationPolicy.Hash(proposal.After),
            BeforeJson = proposal.LegacyBefore is { } legacy ? LegacyEquipmentMigration.SerializeBefore(legacy) : proposal.Before!.Serialize(),
            AfterJson = proposal.After.Serialize(), MappingReason = proposal.MappingReason,
            AppliedAtUtc = time.GetUtcNow()
        };
        await repository.SaveAsync(target, proposal.After, receipt, true, ct);
        await NotifyChangedAsync(target, ct);
        await ObserveAsync("migration", operationId, proposal.After, ct);
        return receipt;
    }

    public async Task<EquipmentMigrationReceipt> RollbackAsync(Guid operationId, string actorId, CancellationToken ct)
    {
        var receipt = await repository.GetReceiptAsync(operationId, ct) ?? throw new InvalidOperationException("Migration receipt was not found.");
        if (string.IsNullOrWhiteSpace(actorId)) throw new InvalidOperationException("An actor is required.");
        if (receipt.RolledBackAtUtc.HasValue) return receipt;
        var target = new EquipmentMigrationTarget(receipt.ItemId, receipt.Location, receipt.ContainerId);
        await PrepareAsync(target, ct);
        var current = await repository.LoadAsync(target, true, ct) ?? throw new InvalidOperationException("Equipment moved or was consumed; manual reconciliation is required.");
        receipt = await repository.GetReceiptAsync(operationId, ct) ?? throw new InvalidOperationException("Receipt disappeared.");
        if (receipt.RolledBackAtUtc.HasValue) return receipt;
        if ((await repository.GetActiveReceiptForItemAsync(receipt.ItemId, ct))?.OperationId != operationId)
            throw new InvalidOperationException("Roll back the most recent equipment rebalance first.");
        if (receipt.ChoiceUsedAtUtc.HasValue || EquipmentMigrationPolicy.Hash(current) != receipt.ResultHash)
            throw new InvalidOperationException("Equipment changed or its respecialization credit was used. Rollback requires manual reconciliation.");
        var legacy = LegacyEquipmentMigration.ReadBefore(receipt.BeforeJson);
        var original = legacy is null ? EquipmentData.Deserialize(receipt.BeforeJson) : null;
        receipt.RolledBackAtUtc = time.GetUtcNow();
        receipt.MappingReason += $" Rollback by {actorId}.";
        if (legacy is not null) await repository.RestoreLegacyAsync(target, legacy, receipt, ct);
        else await repository.SaveAsync(target, original!, receipt, false, ct);
        await NotifyChangedAsync(target, ct);
        if (original is not null) await ObserveAsync("migration-rollback", operationId, original, ct);
        return receipt;
    }

    public async Task<EquipmentData> ChooseAsync(Guid characterId, Guid migrationId, Guid choiceOperationId, string definitionId, CancellationToken ct)
    {
        if (liveRules?.Version == Domain.Models.Attributes.AttributeRules.LegacyVersion)
            throw new InvalidOperationException("Specialization choices open when the new attribute rules are activated.");
        if (choiceOperationId == Guid.Empty) throw new InvalidOperationException("A choice operation ID is required.");
        var receipt = await repository.GetReceiptAsync(migrationId, ct) ?? throw new InvalidOperationException("Migration receipt was not found.");
        if (receipt.RolledBackAtUtc.HasValue) throw new InvalidOperationException("This conversion was rolled back.");
        var target = new EquipmentMigrationTarget(receipt.ItemId, EquipmentMigrationLocation.Instance);
        await PrepareAsync(target, ct);
        var available = await upgrades.LoadAsync(characterId, receipt.ItemId, true, ct);
        if (available?.UnavailableReason is not null || available?.Equipment is null)
            throw new InvalidOperationException(available?.UnavailableReason ?? "Equipment is not available in this character's inventory or slots.");
        var current = await repository.LoadAsync(target, true, ct) ?? throw new InvalidOperationException("Claim the equipment before choosing a specialization.");
        receipt = await repository.GetReceiptAsync(migrationId, ct) ?? throw new InvalidOperationException("Receipt disappeared.");
        if (receipt.RolledBackAtUtc.HasValue) throw new InvalidOperationException("This conversion was rolled back.");
        if (current.State.Ownership.OwnerId != characterId || current.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned)
            throw new InvalidOperationException("This character does not own the equipment.");
        if (receipt.ChoiceUsedAtUtc.HasValue)
            return receipt.ChoiceOperationId == choiceOperationId && receipt.ChosenDefinitionId == definitionId
                ? EquipmentData.Deserialize(receipt.ChoiceResultJson!) : throw new InvalidOperationException("The one-time choice has already been used.");
        if (receipt.RespecializationAllowance == 0
            || (await repository.GetActiveReceiptForItemAsync(receipt.ItemId, ct))?.OperationId != migrationId)
            throw new InvalidOperationException("No specialization choice is available on this receipt. Reload the item's current choice.");
        if (current.State.ActiveStyleId != EquipmentData.Deserialize(receipt.AfterJson).State.ActiveStyleId)
            throw new InvalidOperationException("Choose the specialization before changing the item's style.");
        var evaluator = catalog.Get(current.State.BalanceVersion).Evaluator;
        if (!EquipmentMigrationPolicy.CompatibleDefinitions(current, evaluator).Any(x => x.Id == definitionId))
            throw new InvalidOperationException("The chosen specialization is not compatible with this item.");
        // Later reinforcement and ownership binding survive this choice; use their current state.
        var after = ChangeSpecialization(current, definitionId, evaluator);
        receipt.ChoiceUsedAtUtc = time.GetUtcNow();
        receipt.ChoiceOperationId = choiceOperationId;
        receipt.ChosenDefinitionId = definitionId;
        receipt.ChoiceResultJson = after.Serialize();
        await repository.SaveAsync(target, after, receipt, false, ct);
        await NotifyChangedAsync(target, ct);
        await ObserveAsync("migration-choice", choiceOperationId, after, ct);
        return after;
    }

    private static EquipmentData ChangeSpecialization(EquipmentData current, string definitionId, EquipmentEvaluator evaluator)
    {
        var evaluated = EquipmentData.Create(EquipmentState.Restore(current.State with { DefinitionId = definitionId }), evaluator);
        return new(evaluated.State, current.ItemBaseId, current.DisplayName, current.Rarity, current.EquipmentType,
            current.Behavior, evaluated.Stats, evaluated.EquipmentSetId, evaluated.BaseStats, evaluated.Allocation);
    }

    private static EquipmentMigrationReceipt MatchReceipt(EquipmentMigrationReceipt existing,
        EquipmentMigrationTarget target, string sourceHash, string definitionId, int? targetBalanceVersion, string? expectedResultHash) =>
        existing.ItemId == target.ItemId && existing.Location == target.Location && existing.ContainerId == target.ContainerId
        && existing.SourceHash == sourceHash && EquipmentData.Deserialize(existing.AfterJson).State.DefinitionId == definitionId
        && (!targetBalanceVersion.HasValue || EquipmentData.Deserialize(existing.AfterJson).State.BalanceVersion == targetBalanceVersion.Value)
        && (expectedResultHash is null || existing.ResultHash == expectedResultHash)
            ? existing : throw new InvalidOperationException("Operation ID has already been used for a different conversion.");

    private async Task PrepareAsync(EquipmentMigrationTarget target, CancellationToken ct)
    {
        var characters = await repository.GetAffectedCharactersAsync(target, ct);
        if (boundary is null) await repository.AssertNoScheduledCombatAsync(characters, ct);
        else foreach (var characterId in characters.Order())
            if (await boundary.PrepareMutationAsync(characterId, ct) is { } blocked) throw new InvalidOperationException(blocked);
    }

    private async Task NotifyChangedAsync(EquipmentMigrationTarget target, CancellationToken ct)
    {
        foreach (var characterId in await repository.GetAffectedCharactersAsync(target, ct))
            await outbox.EnqueueAsync(Application.UseCases.Outbox.GameEventTypes.EquipmentChanged,
                new Application.UseCases.Outbox.EquipmentChangedPayload(characterId), characterId, null, ct);
    }

    private Task ObserveAsync(string kind, Guid operationId, EquipmentData data, CancellationToken ct) =>
        data.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned ? Task.CompletedTask
            : outbox.RecordEquipmentAsync(kind, operationId.ToString("N"), data.State.Ownership.OwnerId, data, "migration", ct);
}

/// <summary>Explicit candidate catalog, independent of the configured live award version.</summary>
public sealed record EquipmentMigrationCatalog(StarterEquipmentCatalog Current, IEquipmentCatalogProvider? Versions = null)
{
    public StarterEquipmentCatalog Get(int? version) => version is null || version == Current.Evaluator.Balance.Version
        ? Current : Versions?.Get(version.Value)
            ?? throw new InvalidOperationException($"Equipment release {version} is not available.");
}
