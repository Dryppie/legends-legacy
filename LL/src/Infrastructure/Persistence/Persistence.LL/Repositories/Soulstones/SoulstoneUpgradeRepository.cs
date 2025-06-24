using Application.Common.Interfaces;
using Domain.Models.Soulstones;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Soulstones;
public class SoulstoneUpgradeRepository : ISoulstoneUpgradeRepository
{
    private readonly IDbContext _context;

    public SoulstoneUpgradeRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<List<CharacterSoulstoneUpgrade>> GetSoulstoneUpgradesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken)
    {
        return await _context.CharacterSoulstoneUpgrades
            .Where(csu => csu.CharacterId == characterId)
            .ToListAsync(cancellationToken);
    }
}
