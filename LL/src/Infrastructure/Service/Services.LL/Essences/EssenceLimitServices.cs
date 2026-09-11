using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;

namespace Services.LL.Essences;

public sealed class EssenceSlotUnlockService : IEssenceSlotUnlockService
{
    public int GetUnlockedSlotCount(int characterLevel)
        => EssenceSlotProgression.GetUnlockedSlotCount(characterLevel);
}

public sealed class EssenceLoadoutLimitService(Application.Interfaces.Services.LL.Nobility.INobilityService? nobility = null,
    TimeProvider? time = null) : IEssenceLoadoutLimitService
{
    public async Task<int> GetLoadoutLimitAsync(Guid characterId, CancellationToken ct) => nobility is null ? 3 :
        (await nobility.GetBenefitsAsync(characterId, nobility.CombatEvaluationTime ?? (time ?? TimeProvider.System).GetUtcNow(), ct)).EssenceLoadouts;
}

public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random = new();
    public double NextDouble() => _random.NextDouble();
}
