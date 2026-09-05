using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Calculates an exact, side-effect-free equipment upgrade quote.</summary>
public sealed class EquipmentUpgradePolicy(
    EquipmentCatalog catalog,
    EquipmentUpgradePrices prices,
    EquipmentBlueprintCatalog? blueprints = null)
{
    public EquipmentUpgradeQuote Quote(
        EquipmentUpgradeContext? context,
        EquipmentUpgradeRequest request,
        Guid operationId,
        DateTimeOffset now)
    {
        var expires = DateTimeOffset.FromUnixTimeSeconds(
            (now.ToUnixTimeSeconds() / 300 + 1) * 300);
        var before = context?.Equipment?.ProgressionData;
        EquipmentData? after = null;
        long partsCost = 0;
        long cinderCost = 0;
        long partsReturned = 0;
        string? error = null;
        string? blueprintItemId = null;
        long availableBlueprints = 0;

        try
        {
            if (!Enum.IsDefined(request.Kind)
                || operationId == Guid.Empty
                || request.ItemInstanceId == Guid.Empty)
                throw new InvalidOperationException("Invalid equipment upgrade request.");
            if (context is null)
                throw new InvalidOperationException("Character was not found.");
            if (context.UnavailableReason is not null)
                throw new InvalidOperationException(context.UnavailableReason);
            if (before is null)
                throw new InvalidOperationException("Only current equipment can be reinforced or dismantled.");
            if (before.State.Ownership.OwnerId != context.Character.Id
                || before.State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned)
                throw new InvalidOperationException("This equipment is not personally owned by the character.");
            if (context.InventoryItem is { Quantity: not 1 })
                throw new InvalidOperationException("Equipment must be an individual inventory item.");

            if (request.Kind == EquipmentUpgradeOperationKind.Dismantle)
            {
                if (context.IsEquipped || context.InventoryItem?.Quantity != 1)
                    throw new InvalidOperationException("Unequip this item before dismantling it.");

                // Rank value is intrinsic to the awarded item. It is deliberately
                // calculated before confirmation checks so previews can disclose it.
                partsReturned = prices.GetDismantleParts(before.State.Tier, before.State.Rank);
                after = null;
                if ((context.InventoryItem.IsFavorite || context.Equipment!.IsFavorite)
                    && !request.AllowFavoriteDismantle)
                    throw new InvalidOperationException("Confirm dismantling this favorite item explicitly.");

                if (partsReturned > int.MaxValue - context.PartStacks.Sum(stack => (long)stack.Quantity))
                    throw new InvalidOperationException("The Reinforcement Parts balance cannot hold this return.");
            }
            else if (request.Kind == EquipmentUpgradeOperationKind.ApplyVariant)
            {
                var blueprint = blueprints?.Find(request.BlueprintStyleId)
                    ?? throw new InvalidOperationException("Select a valid blueprint.");
                blueprintItemId = blueprint.ItemId;
                availableBlueprints = context.BlueprintStacks?.Where(x => x.ItemInstance.ItemBaseId == blueprintItemId)
                    .Sum(x => (long)x.Quantity) ?? 0;
                var evaluated = EquipmentData.Create(before.EquipmentState, catalog.Evaluator);
                if (evaluated.Serialize() != before.Serialize())
                    throw new InvalidOperationException("This equipment needs its original content version before conversion.");
                cinderCost = checked(blueprints!.CindersPerTier * before.State.Tier);
                after = EquipmentData.Create(before.EquipmentState.ApplyVariant(catalog.Evaluator, blueprint.StyleId), catalog.Evaluator);
                if (availableBlueprints < 1)
                    throw new InvalidOperationException("You need one matching blueprint.");
            }
            else
            {
                // Current descriptors use authored allocation. Older frozen descriptors
                // retain and scale their existing stats instead of being silently rerolled.
                var evaluated = EquipmentData.Create(before.EquipmentState, catalog.Evaluator);
                if (before.State.Rank >= EquipmentBalance.MaximumRank)
                    throw new InvalidOperationException(
                        $"Equipment is already at rank {EquipmentBalance.MaximumRank}.");

                var tierPrices = prices.ForTier(before.State.Tier);
                partsCost = tierPrices.RankPartCosts[before.State.Rank];
                cinderCost = tierPrices.RankCinderCosts[before.State.Rank];
                after = evaluated.Serialize() == before.Serialize()
                    ? EquipmentData.Create(before.EquipmentState.Reinforce(catalog.Evaluator), catalog.Evaluator)
                    : before.ReinforceFrozen(catalog.Evaluator.Balance);
            }

            if (context.Character.Cinders < cinderCost)
                error = "Not enough Cinders.";
            if (context.PartStacks.Sum(stack => (long)stack.Quantity) < partsCost)
                error = "Not enough Reinforcement Parts.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OverflowException)
        {
            error = ex.Message;
        }

        var availableParts = context?.PartStacks.Sum(stack => (long)stack.Quantity) ?? 0;
        var availableCinders = context?.Character.Cinders ?? 0;
        var token = Fingerprint(new
        {
            characterId = context?.Character.Id,
            operationId,
            request,
            expires,
            before = before?.Serialize(),
            after = after?.Serialize(),
            itemVersion = context?.Equipment?.Version ?? 0,
            prices.Version,
            partsCost,
            cinderCost,
            partsReturned,
            availableParts,
            availableCinders,
            blueprintItemId,
            availableBlueprints,
            blueprintVersion = blueprints?.Version,
            equipped = context?.IsEquipped,
            favorite = context?.InventoryItem?.IsFavorite ?? context?.Equipment?.IsFavorite
        });

        return new EquipmentUpgradeQuote(
            operationId,
            request,
            token,
            expires,
            error is null,
            error,
            before,
            after,
            partsCost,
            cinderCost,
            partsReturned,
            availableParts,
            availableCinders,
            context?.Equipment?.Version ?? 0,
            prices.Version,
            blueprintItemId,
            availableBlueprints);
    }

    public static string Fingerprint<T>(T data) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data))));
}
