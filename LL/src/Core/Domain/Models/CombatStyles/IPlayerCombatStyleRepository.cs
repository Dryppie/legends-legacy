namespace Domain.Models.CombatStyles;

public interface IPlayerCombatStyleRepository
{
    Task<List<PlayerCombatStyle>> GetByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<List<PlayerCombatStyleNode>> GetNodesByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task AddAsync(PlayerCombatStyle combatStyle, CancellationToken cancellationToken);
    Task AddNodeAsync(PlayerCombatStyleNode node, CancellationToken cancellationToken);
    void RemoveNodes(IReadOnlyCollection<PlayerCombatStyleNode> nodes);
    Task DeactivateActiveStylesAsync(Guid characterId, DateTimeOffset updatedAt, CancellationToken cancellationToken);
}
