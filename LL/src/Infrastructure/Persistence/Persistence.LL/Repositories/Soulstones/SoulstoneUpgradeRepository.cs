using Application.Common.Interfaces;
using Domain.Models.Soulstones;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Soulstones;
public class SoulstoneUpgradeRepository : ISoulstoneUpgradeRepository
{
    private readonly IDbContext _context;

    public SoulstoneUpgradeRepository(IDbContext context)
    {
        _context = context;
    }

    public Task<Character?> GetCharacterAsync(Guid characterId, CancellationToken cancellationToken) =>
        _context.Characters.Include(x => x.CharacterSoulstoneUpgrades)
            .SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);

    public void Remove(Character character, IReadOnlyCollection<CharacterSoulstoneUpgrade> upgrades)
    {
        _context.CharacterSoulstoneUpgrades.RemoveRange(upgrades);
        foreach (var upgrade in upgrades) character.CharacterSoulstoneUpgrades.Remove(upgrade);
    }

    public async Task<List<CharacterSoulstoneUpgrade>> GetSoulstoneUpgradesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken)
    {
        return await _context.CharacterSoulstoneUpgrades
            .Where(csu => csu.CharacterId == characterId)
            .ToListAsync(cancellationToken);
    }
}
