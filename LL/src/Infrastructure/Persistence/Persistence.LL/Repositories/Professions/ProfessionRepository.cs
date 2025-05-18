using Application.Common.Interfaces;
using Domain.Models.Professions;

namespace Persistence.LL.Repositories.Professions;
public class ProfessionRepository : IProfessionRepository
{
    private readonly IDbContext _context;
    public ProfessionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanPerformProfession(Guid characterId, ProfessionType professionType, int requiredLevel, CancellationToken cancellationToken)
    {
        var profession = await _context.Professions.FindAsync([characterId, professionType], cancellationToken);
        return profession != null && profession.Level >= requiredLevel;
    }
}
