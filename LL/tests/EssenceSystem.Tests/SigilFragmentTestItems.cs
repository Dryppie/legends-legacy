using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Persistence.LL;

namespace EssenceSystem.Tests;

internal static class SigilFragmentTestItems
{
    public static void Seed(LLDbContext db, Guid characterId, int quantity = 0)
    {
        var definition = new ItemBase { Id = SigilFragmentItem.ItemBaseId, Name = "Sigil Fragments", ItemType = ItemType.Resource, Stackable = true, IsBound = true };
        db.ItemBases.Add(definition);
        var character = db.Characters.Local.Single(x => x.Id == characterId);
        character.Inventory ??= new Inventory { CharacterId = characterId, Character = character };
        if (quantity > 0)
        {
            var item = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = definition.Id, ItemBase = definition };
            character.Inventory.InventoryItems.Add(new InventoryItem { InventoryId = characterId, ItemInstanceId = item.Id, ItemInstance = item, Quantity = quantity });
        }
    }
}
