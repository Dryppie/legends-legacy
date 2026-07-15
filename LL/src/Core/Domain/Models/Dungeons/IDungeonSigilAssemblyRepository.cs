namespace Domain.Models.Dungeons;

public interface IDungeonSigilAssemblyRepository
{
    Task<long?> TrySpendFragmentsAsync(
        Guid characterId,
        long amount,
        CancellationToken cancellationToken);
}
