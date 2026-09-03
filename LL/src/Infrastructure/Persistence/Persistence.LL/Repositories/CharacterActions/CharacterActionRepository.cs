using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CharacterActions;
public class CharacterActionRepository : ICharacterActionRepository
{
    private readonly IDbContext _context;

    public CharacterActionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)
            .FirstOrDefaultAsync(a => a.CharacterId == characterAction.CharacterId, cancellationToken);

        if (existingAction == null)
        {
            characterAction.IsDeleted = false; // Ensure it's not marked as deleted on creation
            characterAction.BlockedUntilUtc = GetSwitchLock(characterAction.ActionDetails, now);
            await _context.CharacterActions.AddAsync(characterAction, cancellationToken);
            return characterAction;
        }

        if (!existingAction.IsDeleted &&
            existingAction.ActionDetails is CombatActionDetails existingCombatDetails &&
            characterAction.ActionDetails is CombatActionDetails requestedCombatDetails)
        {
            existingCombatDetails.AreaId = requestedCombatDetails.AreaId;
            existingCombatDetails.Area = requestedCombatDetails.Area;
            existingAction.UpdatedAt = now;
            existingAction.RowVersion++;
            _context.CharacterActions.Update(existingAction);
            return existingAction;
        }

        if (existingAction.BlockedUntilUtc > now)
        {
            return null;
        }

        existingAction.NextResolutionAtUtc = characterAction.NextResolutionAtUtc;
        existingAction.UpdatedAt = now;
        existingAction.BlockedUntilUtc = GetSwitchLock(characterAction.ActionDetails, now);
        existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
        existingAction.IsDeleted = false;
        existingAction.RowVersion++;

        if (existingAction.ActionDetails != null)
        {
            _context.ActionDetails.Remove(existingAction.ActionDetails);
        }

        existingAction.ActionDetails = characterAction.ActionDetails!;
        existingAction.ActionDetails.CharacterActionId = existingAction.CharacterId;
        _context.ActionDetails.Add(existingAction.ActionDetails);
        _context.CharacterActions.Update(existingAction);
        return existingAction;
    }

    public async Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var stoppedCombat = characterAction.ActionDetails as CombatActionDetails;
        var combatRestartBlockedUntil = stoppedCombat == null
            ? null
            : GetCombatRestartLock(characterAction, now);
        if (characterAction.ActionDetails != null)
            _context.ActionDetails.Remove(characterAction.ActionDetails);  // Explicitly remove the related entity

        characterAction.IsDeleted = true;
        characterAction.ActionDetails = null;
        characterAction.BlockedUntilUtc = combatRestartBlockedUntil;
        characterAction.NextResolutionAtUtc = null;
        characterAction.UpdatedAt = now;
        characterAction.RowVersion++;
        _context.CharacterActions.Update(characterAction);
        return true;
    }

    public Task<CharacterAction?> GetActionScheduleAsync(Guid characterId, CancellationToken cancellationToken) =>
        _context.CharacterActions
            .Include(ca => ca.ActionDetails)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId, cancellationToken);

    public async Task<CharacterAction?> GetCombatActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CombatActionDetails).Area)
                    .ThenInclude(a => a.Creatures)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
        return characterAction;
    }

    public void UpdateCharacterAction(CharacterAction characterAction)
    {
        var entry = _context.GetEntry(characterAction);

        // A newly started combat action is resolved immediately, before the unit of
        // work is saved. Calling Update here would change the new parent from Added
        // to Modified while leaving its ActionDetails Added. Relational providers
        // would then insert the details before the missing parent and violate the FK.
        if (entry.State == EntityState.Added)
            return;

        characterAction.RowVersion++;

        if (entry.State == EntityState.Detached)
            _context.CharacterActions.Update(characterAction);
    }

    private static DateTimeOffset? GetSwitchLock(
        ActionDetails? actionDetails,
        DateTimeOffset now) =>
        actionDetails is CombatActionDetails
            ? now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds)
            : null;

    private static DateTimeOffset? GetCombatRestartLock(
        CharacterAction characterAction,
        DateTimeOffset now)
    {
        var deadline = characterAction.BlockedUntilUtc;
        if (characterAction.NextResolutionAtUtc > deadline)
        {
            deadline = characterAction.NextResolutionAtUtc;
        }

        return deadline > now ? deadline : null;
    }

    public async Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
    }
}
