namespace Domain.Models.Items;

public interface IItemBaseRepository
{
    Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> itemIds, CancellationToken cancellationToken);
}
