using Application.Common.Interfaces;
using Domain.Models.CombatStyles;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CombatStyles;

public sealed class CombatStyleRepository(IDbContext context) : ICombatStyleRepository
{
    public long TrackingGeneration => context.TrackingGeneration;
    public async Task<IReadOnlyList<CharacterCombatStyle>> GetOwnedAsync(Guid characterId, CancellationToken ct)
    {
        var persisted = await context.CharacterCombatStyles.Where(x => x.CharacterId == characterId).ToListAsync(ct);
        return persisted.Concat(context.CharacterCombatStyles.Local.Where(x => x.CharacterId == characterId
            && !persisted.Any(p => p.CombatStyleId == x.CombatStyleId))).ToList();
    }

    public async Task<CharacterCombatStyleSelection?> GetSelectionAsync(Guid characterId, CancellationToken ct) =>
        context.CharacterCombatStyleSelections.Local.SingleOrDefault(x => x.CharacterId == characterId)
        ?? await context.CharacterCombatStyleSelections.SingleOrDefaultAsync(x => x.CharacterId == characterId, ct);

    public void Add(CharacterCombatStyle style) => context.CharacterCombatStyles.Add(style);
    public void Add(CharacterCombatStyleSelection selection) => context.CharacterCombatStyleSelections.Add(selection);
}
