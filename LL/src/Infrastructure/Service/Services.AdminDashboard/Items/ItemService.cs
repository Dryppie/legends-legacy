using Application.Common.Interfaces;
using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Items.Dtos;
using Domain.Models.Items;
using Services.AdminDashboard.JsonReaders;

namespace Services.AdminDashboard.Items;
public class ItemService : IItemService
{
    //private readonly IItemRepository _itemRepository;
    private readonly ItemBaseJsonReader _itemReader;
    public ItemService(/*IItemRepository itemRepository*/)
    {
        //_itemRepository = itemRepository;
        _itemReader = new();
    }
    public async Task<List<ItemBaseDto>> GetItemBasesAsync(CancellationToken cancellationToken)
    {
        return _itemReader.GetItemsFromJson();
    }
    public async Task UpdateItemBaseAsync(ItemBaseDto itemBase, CancellationToken cancellationToken)
    {
        _itemReader.UpdateItemFromItemBase(itemBase);
    }

}
