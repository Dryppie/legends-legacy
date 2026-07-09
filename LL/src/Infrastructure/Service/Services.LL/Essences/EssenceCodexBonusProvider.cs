using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
using Services.LL.Interfaces;

namespace Services.LL.Essences;

public sealed class EssenceCodexBonusProvider : IBonusProvider
{
    private readonly IEssenceCodexCollectionService _codexCollections;

    public EssenceCodexBonusProvider(IEssenceCodexCollectionService codexCollections)
    {
        _codexCollections = codexCollections;
    }

    public async ValueTask<IReadOnlyCollection<Bonus>> GetBonusesAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default)
    {
        var entries = await _codexCollections.GetVisibleEntriesAsync(characterId, ct);

        return entries
            .Where(entry => entry.IsUnlocked)
            .Select(entry => new Bonus(entry.BonusKind, entry.BonusValue))
            .ToList();
    }
}
