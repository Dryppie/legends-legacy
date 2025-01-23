using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CharacterActions;
public class CharacterActionRepository : ICharacterActionRepository
{
    private readonly IDbContext _context;

    public CharacterActionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<bool> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        bool alreadyExists = await _context.CharacterActions
        .AnyAsync(a => a.CharacterId == characterAction.CharacterId, cancellationToken);

        // If it doesn't exist, we add it
        if (!alreadyExists)
        {
            await _context.CharacterActions.AddAsync(characterAction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
        return characterAction;
    }

    public async Task UpdateCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        _context.CharacterActions.Update(characterAction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _context.CharacterActions.FindAsync([characterId], cancellationToken);
        _context.CharacterActions.Remove(characterAction!);
        await _context.SaveChangesAsync(cancellationToken);
    }
}