using Application.UseCases._AdminDashboard.Items.Dtos;
using Domain.Models.Items;

namespace Application.Interfaces.Services.AdminDashboard;
public interface IItemService
{

    Task<List<ItemBase>> GetItemBasesAsync(CancellationToken cancellationToken);
    Task UpdateItemBaseAsync(ItemBaseDto itemToUpdate, CancellationToken cancellationToken);
}