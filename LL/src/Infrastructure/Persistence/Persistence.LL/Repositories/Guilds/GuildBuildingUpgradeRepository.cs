using Application.Common.Interfaces;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Guilds;
public class GuildBuildingUpgradeRepository : IGuildBuildingUpgradeRepository
{
    private readonly IDbContext _context;

    public GuildBuildingUpgradeRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<List<GuildBuildingUpgrade>> GetGuildBuildingUpgradesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken)
    {
        var guild = await _context.Guilds
            .Include(g => g.Members)
            .Include(g => g.GuildBuildingUpgrades)
            .FirstOrDefaultAsync(g =>
                g.Members.Any(m => m.CharacterId == characterId),
                cancellationToken);

        if (guild == null)
            return [];

        return [.. guild.GuildBuildingUpgrades];
    }
}
