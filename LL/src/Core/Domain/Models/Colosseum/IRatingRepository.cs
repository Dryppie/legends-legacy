namespace Domain.Models.Colosseum;
public interface IRatingRepository
{
    Task<int> GetColosseumRatingAsync(Guid characterId, CancellationToken cancellationToken);
    Task SetColosseumRatingAsync(Guid characterId, int newA, CancellationToken cancellationToken);
}
