using Application.Common.Interfaces;
using Common.Exceptions;
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
        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)  // Ensure ActionDetails is loaded
            .FirstOrDefaultAsync(a => a.CharacterId == characterAction.CharacterId, cancellationToken);

        if (existingAction == null)
        {
            characterAction.IsDeleted = false; // Ensure it's not marked as deleted on creation
            await _context.CharacterActions.AddAsync(characterAction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // If combat or any other action ends in the future, it is not possible to start a new action until that time has passed
        if (existingAction.UpdatedAt > DateTimeOffset.UtcNow)
        {
            return false;
        }

        existingAction.UpdatedAt = characterAction.UpdatedAt;
        existingAction.IsDeleted = false;

        if (existingAction.ActionDetails == null)
        {
            // If existing action had no details, add new details
            existingAction.ActionDetails = characterAction.ActionDetails!;
            _context.ActionDetails.Add(existingAction.ActionDetails);
        }
        else
        {
            // If existing action has details, update them
            _context.GetEntry(existingAction.ActionDetails).CurrentValues.SetValues(characterAction.ActionDetails!);
        }

        _context.CharacterActions.Update(existingAction);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);

        NotFoundException.ThrowIfNull(characterAction, nameof(characterAction), characterId);

        if (characterAction.ActionDetails != null)
            _context.ActionDetails.Remove(characterAction.ActionDetails);  // Explicitly remove the related entity

        characterAction.IsDeleted = true;
        characterAction.ActionDetails = null;
        _context.CharacterActions.Update(characterAction!);
        await _context.SaveChangesAsync(cancellationToken);
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
}