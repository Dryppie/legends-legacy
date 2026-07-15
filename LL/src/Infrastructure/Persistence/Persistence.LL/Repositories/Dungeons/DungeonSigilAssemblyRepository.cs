using Application.Common.Interfaces;
using Domain.Models.Dungeons;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Dungeons;

public sealed class DungeonSigilAssemblyRepository(IDbContext context) : IDungeonSigilAssemblyRepository
{
    public async Task<long?> TrySpendFragmentsAsync(
        Guid characterId,
        long amount,
        CancellationToken cancellationToken)
    {
        var updated = await context.Characters
            .Where(x => x.Id == characterId && x.SigilFragments >= amount)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.SigilFragments, x => x.SigilFragments - amount),
                cancellationToken);

        return updated == 1
            ? await context.Characters
                .AsNoTracking()
                .Where(x => x.Id == characterId)
                .Select(x => x.SigilFragments)
                .SingleAsync(cancellationToken)
            : null;
    }
}
