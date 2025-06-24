using Domain.Models.Bonuses;

namespace Services.LL.Interfaces;
public interface IBonusProvider
{
    /// Called once per combat session (fast, pure, no DB context)
    ValueTask<IReadOnlyCollection<Bonus>> GetBonusesAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default);
}
