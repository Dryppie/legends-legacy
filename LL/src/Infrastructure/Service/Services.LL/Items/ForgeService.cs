using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Professions;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Outbox;
using Domain.Models.CharacterActions;
using Domain.Models.Essences;
using Application.WebSockets.Contracts;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Options;

namespace Services.LL.Items;

public sealed class ForgeService(StarterEquipmentCatalog catalog, ForgePrices prices,
    IForgeRepository repository, ICharacterActionService actions, IGameEventOutbox outbox,
    IOptions<EquipmentProgressionOptions> options, TimeProvider timeProvider, IStateSyncService stateSync,
    IEssenceCombatLoadoutResolver essenceLoadouts, ICraftingDefinitionProvider setDefinitions) : IForgeService
{
    private readonly ForgePolicy _policy = new(catalog, prices);

    public async Task<ForgeQuote> PreviewAsync(Guid characterId, ForgeRequest request, CancellationToken ct)
    {
        if (!options.Value.ForgeEnabled) return DisabledQuote(request);
        return CreateQuote(await repository.LoadAsync(characterId, request.ItemInstanceId, false, ct), request, Guid.NewGuid());
    }

    public async Task<IReadOnlyList<ForgeStyleOption>> GetStylesAsync(Guid characterId, Guid itemInstanceId, CancellationToken ct)
    {
        if (!options.Value.ForgeEnabled) return [];
        var context = await repository.LoadAsync(characterId, itemInstanceId, false, ct);
        if (context is null) return [];
        var data = context.Equipment?.ProgressionData;
        return catalog.Styles.Select(style =>
        {
            var learned = context.LearnedStyles.SingleOrDefault(x => x.StyleId == style.Id);
            return new ForgeStyleOption(style.Id, style.Name, learned != null,
                learned is { FreeApplicationOperationId: null },
                data != null && style.Style.CompatibleArchetypeIds.Contains(data.State.ArchetypeId),
                data?.State.NativeStyleId == style.Id, data?.State.ActiveStyleId == style.Id) { ItemBaseId = style.ItemBaseId };
        }).ToArray();
    }

    public async Task<ForgeResult> ExecuteAsync(Guid characterId, Guid operationId, ForgeRequest request,
        string expectedQuote, CancellationToken ct)
    {
        if (!options.Value.ForgeEnabled) return new(null, "The Equipment progression Forge is not available yet.");
        if (operationId == Guid.Empty || characterId == Guid.Empty || string.IsNullOrWhiteSpace(expectedQuote))
            return new(null, "A current Forge quote and operation ID are required.");
        var fingerprint = ForgePolicy.Fingerprint(new { request, expectedQuote });
        var existing = await repository.GetReceiptAsync(characterId, operationId, ct);
        if (existing != null)
            return existing.RequestFingerprint == fingerprint ? new(existing.Outcome, null)
                : new(null, "This operation ID has already been used for a different request.");

        var context = await repository.LoadAsync(characterId, request.ItemInstanceId, true, ct);
        var quote = CreateQuote(context, request, operationId);
        if (quote.Token != expectedQuote) return new(null, "The Forge quote changed or expired. Review the fresh quote.", quote);
        if (!quote.CanExecute) return new(null, quote.UnavailableReason, quote);
        if (context!.IsEquipped && !quote.IsNoOp)
        {
            var action = await actions.PeekCharacterActionAsync(characterId, ct);
            if (action is { IsDeleted: false, CharacterActionType: CharacterActionType.Combat })
            {
                var resolved = await actions.GetCharacterActionAsync(characterId, ct);
                if (resolved?.ProcessedCount > 0)
                    await stateSync.InvalidateCharacterScopesAsync(characterId, StateSyncScopes.CharacterResources,
                        EquipmentKeys.ForgeSettlementReason, ct);
                if (resolved is { HasMoreDueWork: true })
                    return new(null, "Earned combat is still being resolved. Retry the Forge operation after catching up.");
                context = await repository.LoadAsync(characterId, request.ItemInstanceId, true, ct);
                quote = CreateQuote(context, request, operationId);
                if (quote.Token != expectedQuote) return new(null, "Equipment or price changed while resolving combat. Review the fresh quote.", quote);
                if (!quote.CanExecute) return new(null, quote.UnavailableReason, quote);
            }
        }
        var outcome = new ForgeOutcome(operationId, request.Kind, request.ItemInstanceId, request.StyleId,
            quote.Before, quote.After, quote.ScrapCost, quote.CinderCost, quote.ScrapReturned,
            quote.UsesFreeApplication, quote.IsNoOp, timeProvider.GetUtcNow());
        await repository.ApplyAsync(context!, quote, new() { CharacterId = characterId, OperationId = operationId,
            RequestFingerprint = fingerprint, Outcome = outcome }, ct);
        if (!quote.IsNoOp)
        {
            await outbox.EnqueueAsync(GameEventTypes.ForgeCompleted, outcome, characterId, context!.Character.UserId, ct);
            if (context.IsEquipped)
                await outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(characterId), characterId, null, ct);
        }
        return new(outcome, null);
    }

    private ForgeQuote DisabledQuote(ForgeRequest request) => _policy.Quote(null, request, Guid.NewGuid(), timeProvider.GetUtcNow())
        with { CanExecute = false, UnavailableReason = "The Equipment progression Forge is not available yet." };

    private ForgeQuote CreateQuote(ForgeContext? context, ForgeRequest request, Guid operationId)
    {
        var quote = _policy.Quote(context, request, operationId, timeProvider.GetUtcNow());
        if (context is not { IsEquipped: true } || quote.After == null) return quote;
        var essences = EssenceLoadoutSelection.Select(context.Character.EssenceLoadouts, EssenceCombatActivity.None)?
            .Slots.Select(x => x.PlayerEssence).Where(x => x != null).Cast<PlayerEssence>() ?? [];
        var extras = essenceLoadouts.Resolve(context.Character.Id, essences).AttributeModifiers;
        var impact = ForgeLoadoutImpact.Project(context, quote, extras, setDefinitions.GetEquipmentSets());
        return quote with { EquippedImpact = impact, Token = ForgePolicy.Fingerprint(new
        {
            quote.Token, context.Character.Level, impact,
            equippedIds = context.Character.EquipmentSlots.Where(x => x.EquipmentInstanceId != null)
                .Select(x => x.EquipmentInstanceId!.Value).Distinct().Order()
        }) };
    }
}
