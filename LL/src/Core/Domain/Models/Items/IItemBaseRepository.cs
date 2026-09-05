namespace Domain.Models.Items;

public interface IItemBaseRepository
{
    Task<List<ItemBase>> GetTradableItemBasesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new List<ItemBase>());
    Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken cancellationToken);
    Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> itemBases, CancellationToken cancellationToken);
}
