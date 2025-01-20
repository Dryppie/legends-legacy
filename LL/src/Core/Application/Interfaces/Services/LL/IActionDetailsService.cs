using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Application.Interfaces.Services.LL;
public interface IActionDetailsService 
{
    Task<CombatActionDetails> CreateCombatActionDetailsFromAreaNameAsync(string areaName, Guid characterId, CancellationToken cancellationToken);
}