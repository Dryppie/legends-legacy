using Application.Common.Interfaces;
using Domain.Models.Professions;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions;
public class ProfessionRepository : IProfessionRepository
{
    private readonly IDbContext _context;
    public ProfessionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetProfessionLevelAsync(Guid characterId, ProfessionType professionType, CancellationToken cancellationToken)
    {
        var profession = await _context.Professions.FindAsync([characterId, professionType], cancellationToken);

        return profession?.Level ?? 0;
    }

    public async Task<List<Profession>> GetProfessionsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.Professions
            .Where(p => p.CharacterId == characterId)
            .ToListAsync(cancellationToken);
    }

    public void UpdateProfessionLevels(List<Profession> professions)
    {
        _context.Professions.UpdateRange(professions);
    }
}
