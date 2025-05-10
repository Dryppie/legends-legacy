using Application.UseCases._AdminDashboard.Items.Dtos;

namespace Application.Interfaces.Services.AdminDashboard;
public interface IItemService
{
    Task<List<ItemBaseDto>> GetItemBasesAsync(CancellationToken cancellationToken);
    Task UpdateItemBaseAsync(ItemBaseDto itemToUpdate, CancellationToken cancellationToken);
}