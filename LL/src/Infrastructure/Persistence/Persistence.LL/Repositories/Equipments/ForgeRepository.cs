using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class ForgeRepository(IDbContext db) : IForgeRepository
{
    public async Task<ForgeReceipt?> GetReceiptAsync(Guid characterId, Guid operationId, CancellationToken ct) =>
        db.ForgeReceipts.Local.SingleOrDefault(x => x.CharacterId == characterId && x.OperationId == operationId)
        ?? await db.ForgeReceipts.SingleOrDefaultAsync(x => x.CharacterId == characterId && x.OperationId == operationId, ct);

    public async Task<ForgeContext?> LoadAsync(Guid characterId, Guid itemId, bool forMutation, CancellationToken ct)
    {
        // Also coordinate with transfers initiated by another character.
        if (forMutation) await db.AcquireCharacterRowsLockAsync([characterId], ct);
        var character = await db.Characters.Include(x => x.BaseAttributes)
            .Include(x => x.EquipmentSlots).ThenInclude(x => x.EquipmentInstance).ThenInclude(x => x!.InstanceModifiers)
            .Include(x => x.EquipmentSlots).ThenInclude(x => x.EquipmentInstance).ThenInclude(x => x!.ItemBase)
                .ThenInclude(x => (x as EquipmentBase)!.AttributeModifiers)
            .Include(x => x.EssenceLoadouts).ThenInclude(x => x.Slots).ThenInclude(x => x.PlayerEssence)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == characterId, ct);
        if (character is null) return null;
        var inventory = await db.InventoryItems.Include(x => x.ItemInstance).ThenInclude(x => x.ItemBase)
            .Include(x => (x.ItemInstance as EquipmentInstance)!.InstanceModifiers)
            .Where(x => x.InventoryId == characterId && (x.ItemInstanceId == itemId || x.ItemInstance.ItemBaseId == "tempered_scrap"))
            .ToListAsync(ct);
        inventory = inventory.Concat(db.InventoryItems.Local.Where(x => x.InventoryId == characterId
                && (x.ItemInstanceId == itemId || x.ItemInstance.ItemBaseId == "tempered_scrap")))
            .DistinctBy(x => x.ItemInstanceId).Where(x => db.GetEntry(x).State != EntityState.Deleted).ToList();
        var item = inventory.SingleOrDefault(x => x.ItemInstanceId == itemId);
        var slots = await db.EquipmentSlots.Include(x => x.EquipmentInstance).ThenInclude(x => x!.ItemBase)
            .Include(x => x.EquipmentInstance).ThenInclude(x => x!.InstanceModifiers)
            .Where(x => x.EquipmentInstanceId == itemId).ToListAsync(ct);
        var ownedSlots = slots.Where(x => x.EntityId == characterId).ToArray();
        var equipment = item?.ItemInstance as EquipmentInstance ?? ownedSlots.FirstOrDefault()?.EquipmentInstance;
        string? unavailable = null;
        if (item is null && ownedSlots.Length == 0) unavailable = "Item is not in your inventory or equipment slots.";
        if (slots.Any(x => x.EntityId != characterId) || item != null && ownedSlots.Length > 0)
            unavailable = "Item location is inconsistent; this item cannot be modified.";
        if (await db.MarketPlaceListings.AnyAsync(x => x.ItemInstanceId == itemId, ct)) unavailable = "Listed items cannot use the Forge.";
        if (await db.GuildVaultItems.AnyAsync(x => x.EquipmentInstanceId == itemId, ct)) unavailable = "Guild equipment cannot use the personal Forge.";
        var learned = await db.LearnedEquipmentStyles.Where(x => x.CharacterId == characterId).ToListAsync(ct);
        learned = learned.Concat(db.LearnedEquipmentStyles.Local.Where(x => x.CharacterId == characterId))
            .DistinctBy(x => x.StyleId).ToList();
        return new(character, item, equipment, ownedSlots.Length > 0, unavailable,
            inventory.Where(x => x.ItemInstance.ItemBaseId == "tempered_scrap").OrderBy(x => x.ItemInstanceId).ToArray(), learned);
    }

    public async Task ApplyAsync(ForgeContext context, ForgeQuote quote, ForgeReceipt receipt, CancellationToken ct)
    {
        if (!quote.CanExecute) throw new InvalidOperationException("An unavailable Forge quote cannot be committed.");
        // Resolve the refund base before mutating any tracked inventory/currency.
        ItemBase? scrapBase = null;
        if (quote.ScrapReturned > 0)
        {
            scrapBase = await db.ItemBases.SingleOrDefaultAsync(x => x.Id == "tempered_scrap", ct);
            if (scrapBase is not { Stackable: true }) throw new InvalidOperationException("Tempered Scrap is not configured.");
        }
        var character = context.Character;
        var outcome = receipt.Outcome;
        if (!quote.IsNoOp)
        {
            character.Cinders = checked(character.Cinders - quote.CinderCost);
            var remaining = quote.ScrapCost;
            foreach (var stack in context.ScrapStacks)
            {
                var paid = Math.Min(remaining, stack.Quantity);
                if (paid == 0) break;
                Consume(stack, (int)paid);
                remaining -= paid;
                Ledger("tempered_scrap", "Tempered Scrap", paid, outgoing: true, stack.ItemInstanceId);
            }
            if (remaining != 0) throw new InvalidOperationException("Scrap changed while applying the Forge operation.");
            if (quote.CinderCost > 0) Ledger("currency:cinders", "Cinders", quote.CinderCost, outgoing: true);
            switch (quote.Request.Kind)
            {
                case ForgeOperationKind.ImproveRank:
                case ForgeOperationKind.ChangeStyle:
                    context.Equipment!.ApplyProgressionData(quote.After!);
                    if (quote.UsesFreeApplication)
                        context.LearnedStyles.Single(x => x.StyleId == quote.Request.StyleId).UseFreeApplication(receipt.OperationId);
                    break;
                case ForgeOperationKind.LearnStyle:
                    var book = context.InventoryItem!;
                    Consume(book, 1);
                    db.LearnedEquipmentStyles.Add(new() { CharacterId = character.Id, StyleId = quote.Request.StyleId!, LearnedAtUtc = outcome.OccurredAtUtc });
                    Ledger(book.ItemInstance.ItemBaseId, book.ItemInstance.ItemBase.Name, 1, outgoing: true, book.ItemInstanceId);
                    break;
                case ForgeOperationKind.Salvage:
                    db.InventoryItems.Remove(context.InventoryItem!);
                    db.ItemInstances.Remove(context.Equipment!);
                    Ledger(context.Equipment!.ItemBaseId, context.Equipment.DisplayName, 1, outgoing: true, context.Equipment.Id);
                    if (quote.ScrapReturned > 0)
                    {
                        var stack = context.ScrapStacks.FirstOrDefault();
                        if (stack is null)
                        {
                            var instance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = scrapBase!.Id, ItemBase = scrapBase,
                                AcquisitionSource = EquipmentKeys.SalvageSource, AcquiredAtUtc = outcome.OccurredAtUtc };
                            stack = new InventoryItem { InventoryId = character.Id, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = 0 };
                            db.InventoryItems.Add(stack);
                        }
                        stack.Quantity = checked(stack.Quantity + (int)quote.ScrapReturned);
                        Ledger("tempered_scrap", "Tempered Scrap", quote.ScrapReturned, outgoing: false, stack.ItemInstanceId);
                    }
                    break;
            }
        }
        db.ForgeReceipts.Add(receipt);

        void Consume(InventoryItem stack, int quantity)
        {
            if (quantity <= 0 || stack.Quantity < quantity) throw new InvalidOperationException("Inventory changed during the Forge operation.");
            stack.Quantity -= quantity;
            if (stack.Quantity == 0) db.InventoryItems.Remove(stack);
        }

        void Ledger(string assetId, string name, long quantity, bool outgoing, Guid? instanceId = null) => db.EconomyLedger.Add(new()
        {
            EventType = EconomyEventType.EquipmentForge, AssetType = assetId == "currency:cinders" ? EconomyAssetType.Currency : EconomyAssetType.Item,
            ReferenceId = receipt.OperationId, AssetId = assetId, AssetName = name, Quantity = quantity,
            SenderCharacterId = outgoing ? character.Id : null, SenderAccountId = outgoing ? character.UserId : null,
            SenderCharacterLevel = outgoing ? character.Level : null, SourceItemInstanceId = outgoing ? instanceId : null,
            RecipientCharacterId = outgoing ? null : character.Id, RecipientAccountId = outgoing ? null : character.UserId,
            RecipientCharacterLevel = outgoing ? null : character.Level, DestinationItemInstanceId = outgoing ? null : instanceId,
            Source = EquipmentKeys.SourcePrefix + $"{quote.Request.Kind}", OccurredAt = outcome.OccurredAtUtc
        });
    }
}
