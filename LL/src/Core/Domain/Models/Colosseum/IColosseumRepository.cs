using Domain.Models.Entities.Characters;

namespace Domain.Models.Colosseum;
public interface IColosseumRepository
{
    Task<List<Character>> GetArenaOpponents(Guid characterId, CancellationToken cancellationToken);
}