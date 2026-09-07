using Application.Common.Interfaces;
using Domain.Models.Dungeons.Mastery;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Dungeons;

public sealed class CharacterDungeonMasteryRepository : ICharacterDungeonMasteryRepository
{
    private readonly IDbContext _context;

    public CharacterDungeonMasteryRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CharacterDungeonMastery mastery, CancellationToken cancellationToken)
    {
        await _context.CharacterDungeonMasteries.AddAsync(mastery, cancellationToken);
    }

    public async Task<CharacterDungeonMastery?> GetAsync(
        Guid characterId,
        string dungeonDefinitionId,
        CancellationToken cancellationToken)
    {
        return _context.CharacterDungeonMasteries.Local.FirstOrDefault(
            x => x.CharacterId == characterId && x.DungeonDefinitionId == dungeonDefinitionId)
            ?? await _context.CharacterDungeonMasteries.FirstOrDefaultAsync(
            x => x.CharacterId == characterId && x.DungeonDefinitionId == dungeonDefinitionId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CharacterDungeonMastery>> GetForCharacterAsync(
        Guid characterId,
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken)
    {
        if (dungeonDefinitionIds.Count == 0)
        {
            return [];
        }

        var stored = await _context.CharacterDungeonMasteries
            .Where(x => x.CharacterId == characterId && dungeonDefinitionIds.Contains(x.DungeonDefinitionId))
            .ToListAsync(cancellationToken);
        return stored.Concat(_context.CharacterDungeonMasteries.Local.Where(
                x => x.CharacterId == characterId && dungeonDefinitionIds.Contains(x.DungeonDefinitionId)))
            .DistinctBy(x => x.DungeonDefinitionId)
            .ToArray();
    }
}
