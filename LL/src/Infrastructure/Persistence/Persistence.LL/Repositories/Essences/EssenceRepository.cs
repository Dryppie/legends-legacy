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

    public async Task<Character?> GetCharacterWithEssenceLoadoutsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .Include(x => x.EssenceLoadouts)
                .ThenInclude(x => x.Slots)
                    .ThenInclude(x => x.PlayerEssence)
            .AsSingleQuery()
            .FirstOrDefaultAsync(x => x.Id == characterId, cancellationToken);

    public async Task<int> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.Characters
            .Where(x => x.Id == characterId)
            .Select(x => x.Level)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<PlayerEssence>> GetPlayerEssencesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var persistedEssences = await _context.PlayerEssences
            .Where(x => x.CharacterId == characterId)
            .OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.EssenceDefinitionId)
            .ToListAsync(cancellationToken);

        var trackedEssences = _context.PlayerEssences.Local
            .Where(x => x.CharacterId == characterId && !persistedEssences.Any(persisted => persisted.Id == x.Id));

        return persistedEssences
            .Concat(trackedEssences)
            .OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.EssenceDefinitionId)
            .ToList();
    }

    public async Task<PlayerEssence?> GetPlayerEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.FirstOrDefaultAsync(x => x.Id == playerEssenceId && x.CharacterId == characterId, cancellationToken);

    public async Task<bool> HasPlayerEssenceAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.AnyAsync(x => x.CharacterId == characterId && x.EssenceDefinitionId == essenceDefinitionId, cancellationToken);

    public async Task<int> CountOwnedPlayerEssencesAsync(Guid characterId, IReadOnlyCollection<Guid> playerEssenceIds, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.CountAsync(x => x.CharacterId == characterId && playerEssenceIds.Contains(x.Id), cancellationToken);

    public async Task AddPlayerEssenceAsync(PlayerEssence essence, CancellationToken cancellationToken) =>
        await _context.PlayerEssences.AddAsync(essence, cancellationToken);

    public async Task<CreatureResonance?> GetCreatureResonanceAsync(Guid characterId, string creatureId, CancellationToken cancellationToken)
    {
        var trackedResonance = _context.CreatureResonances.Local.FirstOrDefault(x =>
            x.CharacterId == characterId &&
            x.CreatureId == creatureId);

        return trackedResonance ?? await _context.CreatureResonances
            .FirstOrDefaultAsync(x => x.CharacterId == characterId && x.CreatureId == creatureId, cancellationToken);
    }

    public async Task<IReadOnlyList<CreatureResonance>> GetCreatureResonancesAsync(
        Guid characterId,
        IReadOnlyCollection<string> creatureIds,
        CancellationToken cancellationToken)
    {
        if (creatureIds.Count == 0)
        {
            return [];
        }

        var persisted = await _context.CreatureResonances
            .Where(x => x.CharacterId == characterId && creatureIds.Contains(x.CreatureId))
            .ToListAsync(cancellationToken);
        var persistedIds = persisted
            .Select(x => x.CreatureId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tracked = _context.CreatureResonances.Local
            .Where(x => x.CharacterId == characterId &&
                        creatureIds.Contains(x.CreatureId, StringComparer.OrdinalIgnoreCase) &&
                        !persistedIds.Contains(x.CreatureId));

        return [.. persisted, .. tracked];
    }

    public async Task AddCreatureResonanceAsync(CreatureResonance resonance, CancellationToken cancellationToken)
    {
        var alreadyTracked = _context.CreatureResonances.Local.Any(x =>
            x.CharacterId == resonance.CharacterId &&
            x.CreatureId == resonance.CreatureId);

        if (!alreadyTracked)
            await _context.CreatureResonances.AddAsync(resonance, cancellationToken);
    }

    public async Task<EssenceLoadout?> GetLoadoutWithSlotsAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts
            .Include(x => x.Slots)
            .FirstOrDefaultAsync(x => x.Id == loadoutId && x.CharacterId == characterId, cancellationToken);

    public async Task<List<EssenceLoadout>> GetLoadoutsWithSlotsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts
            .Include(x => x.Slots)
                .ThenInclude(x => x.PlayerEssence)
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

    public async Task<int> CountLoadoutsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.CountAsync(x => x.CharacterId == characterId, cancellationToken);

    public async Task<bool> HasLoadoutNameAsync(
        Guid characterId,
        string name,
        Guid? excludingLoadoutId,
        CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.AnyAsync(
            x => x.CharacterId == characterId
                 && x.Name == name
                 && (!excludingLoadoutId.HasValue || x.Id != excludingLoadoutId.Value),
            cancellationToken);

    public async Task AddLoadoutAsync(EssenceLoadout loadout, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.AddAsync(loadout, cancellationToken);

    public async Task<EssenceLoadout?> GetLoadoutAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts.FirstOrDefaultAsync(x => x.Id == loadoutId && x.CharacterId == characterId, cancellationToken);

    public void RemoveLoadout(EssenceLoadout loadout) =>
        _context.EssenceLoadouts.Remove(loadout);

    public async Task ReplaceLoadoutSlotsAsync(Guid loadoutId, IReadOnlyCollection<EssenceLoadoutSlot> slots, CancellationToken cancellationToken)
    {
        var existingSlots = await _context.EssenceLoadoutSlots
            .Where(x => x.EssenceLoadoutId == loadoutId)
            .ToListAsync(cancellationToken);
        _context.EssenceLoadoutSlots.RemoveRange(existingSlots);

        if (slots.Count > 0)
            await _context.EssenceLoadoutSlots.AddRangeAsync(slots, cancellationToken);
    }
}
