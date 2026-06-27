using Application.Common.Interfaces;
using Domain.Models.CombatStyles;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CombatStyles;

public sealed class PlayerCombatStyleRepository : IPlayerCombatStyleRepository
{
    private readonly IDbContext _context;

    public PlayerCombatStyleRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlayerCombatStyle>> GetByCharacterIdAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        return await _context.PlayerCombatStyles
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PlayerCombatStyleNode>> GetNodesByCharacterIdAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        return await _context.PlayerCombatStyleNodes
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlayerCombatStyle combatStyle, CancellationToken cancellationToken)
    {
        await _context.PlayerCombatStyles.AddAsync(combatStyle, cancellationToken);
    }

    public async Task AddNodeAsync(PlayerCombatStyleNode node, CancellationToken cancellationToken)
    {
        await _context.PlayerCombatStyleNodes.AddAsync(node, cancellationToken);
    }

    public void RemoveNodes(IReadOnlyCollection<PlayerCombatStyleNode> nodes)
    {
        _context.PlayerCombatStyleNodes.RemoveRange(nodes);
    }

    public async Task DeactivateActiveStylesAsync(
        Guid characterId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        foreach (var style in _context.PlayerCombatStyles.Local.Where(x => x.CharacterId == characterId && x.IsActive))
        {
            style.IsActive = false;
            style.UpdatedAt = updatedAt;
        }

        if (_context is not DbContext dbContext || !dbContext.Database.IsRelational())
            return;

        await _context.PlayerCombatStyles
            .Where(x => x.CharacterId == characterId && x.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.UpdatedAt, updatedAt), cancellationToken);
    }
}
