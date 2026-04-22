namespace Services.LL.Interfaces.Combat.Reward;

public interface ICurrencyRewardWriter
{
    Task AddAsync(
        Guid characterId,
        int cinders,
        int soulstones,
        CancellationToken cancellationToken);
}