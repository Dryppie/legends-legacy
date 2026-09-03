using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Pure quote calculation: no state, currency or entitlement is changed.</summary>
public sealed class ForgePolicy(StarterEquipmentCatalog catalog, ForgePrices prices)
{
    public ForgeQuote Quote(ForgeContext? context, ForgeRequest request, Guid operationId, DateTimeOffset now)
    {
        var expires = DateTimeOffset.FromUnixTimeSeconds((now.ToUnixTimeSeconds() / 300 + 1) * 300);
        var before = context?.Equipment?.ProgressionData;
        EquipmentData? after = before;
        long scrap = 0, cinders = 0, returned = 0;
        var free = false;
        var noOp = false;
        string? error = null;
        try
        {
            if (!Enum.IsDefined(request.Kind) || operationId == Guid.Empty || request.ItemInstanceId == Guid.Empty)
                throw new InvalidOperationException("Invalid Forge request.");
            if (request.Kind is not (ForgeOperationKind.ChangeStyle or ForgeOperationKind.LearnStyle) && request.StyleId != null)
                throw new InvalidOperationException("This operation does not accept a style.");
            if (context is null) throw new InvalidOperationException("Character was not found.");
            var learned = context.LearnedStyles.SingleOrDefault(x => x.StyleId == request.StyleId);
            if (request.Kind == ForgeOperationKind.LearnStyle)
            {
                var style = catalog.Styles.SingleOrDefault(x => x.Id == request.StyleId)
                    ?? throw new InvalidOperationException("Unknown Blueprint style.");
                noOp = learned is not null;
                if (!noOp && (context.UnavailableReason != null || context.InventoryItem is not { Quantity: > 0 } book
                    || book.ItemInstance is EquipmentInstance || book.ItemInstance.ItemBaseId != style.ItemBaseId))
                    throw new InvalidOperationException("An available copy of this Blueprint book is required.");
                before = after = null;
            }
            else
            {
                if (context.UnavailableReason is not null) throw new InvalidOperationException(context.UnavailableReason);
                if (before is null) throw new InvalidOperationException("Only Equipment progression equipment can use this Forge.");
                if (context.InventoryItem is { Quantity: not 1 })
                    throw new InvalidOperationException("Equipment must be an individual inventory item.");
                if (before.State.Ownership.OwnerId != context.Character.Id || !before.State.Ownership.CanPersonallyModifyOrSalvage)
                    throw new InvalidOperationException("This equipment is not personally owned by the character.");
                if (request.Kind == ForgeOperationKind.Salvage)
                {
                    if (context.IsEquipped || context.InventoryItem?.Quantity != 1)
                        throw new InvalidOperationException("Unequip this item before salvaging it.");
                    if ((context.InventoryItem.IsFavorite || context.Equipment!.IsFavorite) && !request.AllowFavoriteSalvage)
                        throw new InvalidOperationException("Confirm salvage of this favorite item explicitly.");
                    returned = before.EquipmentState.GetSalvageScrap(prices.PaidScrapRecovery);
                    if (returned > int.MaxValue - context.ScrapStacks.Sum(x => (long)x.Quantity))
                        throw new InvalidOperationException("The Scrap balance cannot hold this refund.");
                    after = null;
                }
                else
                {
                    // Refuse silent rerolls if versioned content was edited in place.
                    var evaluated = EquipmentData.Create(before.EquipmentState, catalog.Evaluator);
                    if (evaluated.Serialize() != before.Serialize())
                        throw new InvalidOperationException("This equipment needs its original content version before it can be modified.");
                    var tierPrices = prices.ForTier(before.State.Tier);
                    if (request.Kind == ForgeOperationKind.ImproveRank)
                    {
                        if (before.State.Rank >= EquipmentBalance.MaximumRank)
                            throw new InvalidOperationException("Equipment is already at rank 5.");
                        scrap = tierPrices.RankScrapCosts[before.State.Rank];
                        cinders = tierPrices.RankCinderCosts[before.State.Rank];
                        after = EquipmentData.Create(before.EquipmentState.RecordPaidRankImprovement(
                            catalog.Evaluator, operationId, scrap, cinders), catalog.Evaluator);
                    }
                    else
                    {
                        noOp = request.StyleId == before.State.ActiveStyleId;
                        var proposed = before.EquipmentState.ChangeStyle(catalog.Evaluator, request.StyleId,
                            context.LearnedStyles.Select(x => x.StyleId).ToHashSet(StringComparer.Ordinal));
                        free = !noOp && request.StyleId != before.State.NativeStyleId
                            && learned is { FreeApplicationOperationId: null };
                        cinders = noOp || free ? 0 : tierPrices.StyleChangeCinders;
                        after = EquipmentData.Create(proposed, catalog.Evaluator);
                    }
                }
            }
            if (context.Character.Cinders < cinders) error = "Not enough Cinders.";
            if (context.ScrapStacks.Sum(x => (long)x.Quantity) < scrap) error = "Not enough Tempered Scrap.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        { error = ex.Message; }

        var token = Fingerprint(new { characterId = context?.Character.Id, operationId, request, expires, before = before?.Serialize(), after = after?.Serialize(),
            version = context?.Equipment?.Version ?? 0, prices.Version, scrap, cinders, returned, free, noOp,
            equipped = context?.IsEquipped, favorite = context?.InventoryItem?.IsFavorite,
            book = context?.InventoryItem?.ItemInstance.ItemBaseId,
            learned = context?.LearnedStyles.OrderBy(x => x.StyleId, StringComparer.Ordinal).Select(x => new { x.StyleId, x.FreeApplicationOperationId }) });
        return new(operationId, request, token, expires, error is null, error, before, after,
            scrap, cinders, returned, free, noOp, context?.Equipment?.Version ?? 0, prices.Version);
    }

    public static string Fingerprint<T>(T data) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data))));
}
