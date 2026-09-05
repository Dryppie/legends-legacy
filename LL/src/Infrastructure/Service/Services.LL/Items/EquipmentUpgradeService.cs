using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.CharacterActions;
using Domain.Models.Items.Equipments.Progression;

namespace Services.LL.Items;

public sealed class EquipmentUpgradeService(
    StarterEquipmentCatalog catalog,
    EquipmentUpgradePrices prices,
    IEquipmentUpgradeRepository repository,
    ICharacterActionService actions,
    IGameEventOutbox outbox,
    TimeProvider timeProvider,
    IStateSyncService stateSync,
    EquipmentBlueprintCatalog? blueprints = null,
    IEquipmentBlueprintRepository? blueprintRepository = null) : IEquipmentUpgradeService
{
    private readonly EquipmentUpgradePolicy _policy = new(catalog, prices, blueprints);

    public async Task<IReadOnlyList<EquipmentBlueprintOption>> GetBlueprintsAsync(Guid characterId, Guid itemInstanceId, CancellationToken ct)
    {
        var context = await repository.LoadAsync(characterId, itemInstanceId, false, ct);
        var state = context?.Equipment?.ProgressionData?.State;
        if (state is null || context!.UnavailableReason is not null || blueprints is null) return [];
        var progress = blueprintRepository is null ? [] : await blueprintRepository.GetProgressAsync(characterId, ct);
        return blueprints.Blueprints.Where(x => catalog.Styles.Any(style => style.Id == x.StyleId
                && style.CompatibleArchetypeIds.Contains(state.ArchetypeId)))
            .Select(x => new EquipmentBlueprintOption(x.StyleId, x.Name, x.ItemId,
                context.BlueprintStacks?.Where(stack => stack.ItemInstance.ItemBaseId == x.ItemId).Sum(stack => (long)stack.Quantity) ?? 0,
                state.ActiveStyleId == x.StyleId,
                blueprints.Sources.Where(source => source.StyleIds.Contains(x.StyleId)).Select(source =>
                    new EquipmentBlueprintSourceProgress(source.Name, source.Region,
                        Math.Max(1, blueprints.GuaranteeCompletions - (progress.SingleOrDefault(p => p.FamilyId == source.FamilyId)?.Misses ?? 0)))).ToArray()))
            .OrderBy(x => x.Name).ToArray();
    }

    public async Task<EquipmentUpgradeQuote> PreviewAsync(
        Guid characterId,
        EquipmentUpgradeRequest request,
        CancellationToken cancellationToken) =>
        _policy.Quote(
            await repository.LoadAsync(characterId, request.ItemInstanceId, false, cancellationToken),
            request,
            Guid.NewGuid(),
            timeProvider.GetUtcNow());

    public async Task<EquipmentUpgradeResult> ExecuteAsync(
        Guid characterId,
        Guid operationId,
        EquipmentUpgradeRequest request,
        string expectedQuote,
        CancellationToken cancellationToken)
    {
        if (operationId == Guid.Empty || characterId == Guid.Empty || string.IsNullOrWhiteSpace(expectedQuote))
            return new(null, "A current upgrade quote and operation ID are required.");

        var requestFingerprint = EquipmentUpgradePolicy.Fingerprint(new { request, expectedQuote });
        var existing = await repository.GetReceiptAsync(characterId, operationId, cancellationToken);
        if (existing is not null)
            return existing.RequestFingerprint == requestFingerprint
                ? new(existing.Outcome, null)
                : new(null, "This operation ID has already been used for a different request.");

        var context = await repository.LoadAsync(
            characterId,
            request.ItemInstanceId,
            true,
            cancellationToken);

        // A concurrent retry can pass the optimistic receipt check before the
        // first request commits. Recheck after the character mutation lock.
        existing = await repository.GetReceiptAsync(characterId, operationId, cancellationToken);
        if (existing is not null)
            return existing.RequestFingerprint == requestFingerprint
                ? new(existing.Outcome, null)
                : new(null, "This operation ID has already been used for a different request.");

        var quote = CreateQuote(context, request, operationId);
        if (quote.Token != expectedQuote)
            return new(null, "The upgrade quote changed or expired. Review the fresh quote.", quote);
        if (!quote.CanExecute)
            return new(null, quote.UnavailableReason, quote);

        if (context!.IsEquipped && request.Kind != EquipmentUpgradeOperationKind.Dismantle)
        {
            var action = await actions.PeekCharacterActionAsync(characterId, cancellationToken);
            if (action is { IsDeleted: false, CharacterActionType: CharacterActionType.Combat })
            {
                var resolved = await actions.GetCharacterActionAsync(characterId, cancellationToken);
                if (resolved?.ProcessedCount > 0)
                {
                    await stateSync.InvalidateCharacterScopesAsync(
                        characterId,
                        StateSyncScopes.CharacterResources,
                        EquipmentKeys.ReinforcementSettlementReason,
                        cancellationToken);
                }
                if (resolved is { HasMoreDueWork: true })
                    return new(null,
                        "Earned combat is still being resolved. Retry after catching up.");

                context = await repository.LoadAsync(
                    characterId,
                    request.ItemInstanceId,
                    true,
                    cancellationToken);
                quote = CreateQuote(context, request, operationId);
                if (quote.Token != expectedQuote)
                    return new(null,
                        "Equipment or balances changed while resolving combat. Review the fresh quote.",
                        quote);
                if (!quote.CanExecute)
                    return new(null, quote.UnavailableReason, quote);
            }
        }

        var outcome = new EquipmentUpgradeOutcome(
            operationId,
            request.Kind,
            request.ItemInstanceId,
            quote.Before,
            quote.After,
            quote.PartsCost,
            quote.CinderCost,
            quote.PartsReturned,
            timeProvider.GetUtcNow(), quote.BlueprintItemId);
        await repository.ApplyAsync(
            context!,
            quote,
            new EquipmentUpgradeReceipt
            {
                CharacterId = characterId,
                OperationId = operationId,
                RequestFingerprint = requestFingerprint,
                Outcome = outcome
            },
            cancellationToken);

        if (context!.IsEquipped)
        {
            await outbox.EnqueueAsync(
                GameEventTypes.EquipmentChanged,
                new EquipmentChangedPayload(characterId),
                characterId,
                null,
                cancellationToken);
        }

        return new(outcome, null);
    }

    private EquipmentUpgradeQuote CreateQuote(
        EquipmentUpgradeContext? context,
        EquipmentUpgradeRequest request,
        Guid operationId) =>
        _policy.Quote(context, request, operationId, timeProvider.GetUtcNow());
}
