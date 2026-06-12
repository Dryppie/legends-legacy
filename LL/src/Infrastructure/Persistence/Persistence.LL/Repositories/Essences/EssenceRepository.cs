using Application.Common.Interfaces;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Essences;

public sealed class EssenceRepository : IEssenceRepository
{
    private readonly IDbContext _context;

    public EssenceRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<List<EssenceLoadoutSlot>> GetActiveSlotsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadoutSlots
            .Include(x => x.PlayerEssence)
            .Include(x => x.EssenceLoadout)
            .Where(x => x.EssenceLoadout.CharacterId == characterId && x.EssenceLoadout.IsActive && x.PlayerEssenceId != null)
            .ToListAsync(cancellationToken);

    public async Task<Character?> GetCharacterWithEssenceLoadoutsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .Include(x => x.EssenceLoadouts)
                .ThenInclude(x => x.Slots)
                    .ThenInclude(x => x.PlayerEssence)
            .AsSingleQuery()
            .FirstOrDefaultAsync(x => x.Id == characterId, cancellationToken);

    public async Task<EssenceLoadout?> GetActiveLoadoutAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts
            .Include(x => x.Slots)
                .ThenInclude(x => x.PlayerEssence)
            .FirstOrDefaultAsync(x => x.CharacterId == characterId && x.IsActive, cancellationToken);

    public async Task<int> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .Where(x => x.Id == characterId)
            .Select(x => x.Level)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<PlayerEssence>> GetPlayerEssencesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.PlayerEssences
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.EssenceDefinitionId)
            .ToListAsync(cancellationToken);

    public async Task<PlayerEssence?> GetPlayerEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.FirstOrDefaultAsync(x => x.Id == playerEssenceId && x.CharacterId == characterId, cancellationToken);

    public async Task<bool> HasPlayerEssenceAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.AnyAsync(x => x.CharacterId == characterId && x.EssenceDefinitionId == essenceDefinitionId, cancellationToken);

    public async Task<int> CountOwnedPlayerEssencesAsync(Guid characterId, IReadOnlyCollection<Guid> playerEssenceIds, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.CountAsync(x => x.CharacterId == characterId && playerEssenceIds.Contains(x.Id), cancellationToken);

    public async Task AddPlayerEssenceAsync(PlayerEssence essence, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.AddAsync(essence, cancellationToken);

    public async Task<CreatureResonance?> GetMonsterResonanceAsync(Guid characterId, string creatureId, CancellationToken cancellationToken) =>
        await _context.MonsterResonances.FirstOrDefaultAsync(x => x.CharacterId == characterId && x.CreatureId == creatureId, cancellationToken);

    public async Task AddMonsterResonanceAsync(CreatureResonance resonance, CancellationToken cancellationToken) =>
        await _context.MonsterResonances.AddAsync(resonance, cancellationToken);

    public async Task<EssenceLoadout?> GetLoadoutWithSlotsAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts
            .Include(x => x.Slots)
            .FirstOrDefaultAsync(x => x.Id == loadoutId && x.CharacterId == characterId, cancellationToken);

    public async Task<List<EssenceLoadout>> GetLoadoutsWithSlotsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts
            .Include(x => x.Slots)
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

    public async Task<int> CountLoadoutsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.CountAsync(x => x.CharacterId == characterId, cancellationToken);

    public async Task AddLoadoutAsync(EssenceLoadout loadout, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.AddAsync(loadout, cancellationToken);

    public async Task<EssenceLoadout?> GetLoadoutAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.FirstOrDefaultAsync(x => x.Id == loadoutId && x.CharacterId == characterId, cancellationToken);

    public void RemoveLoadout(EssenceLoadout loadout) =>
        _context.EssenceLoadouts.Remove(loadout);
}
