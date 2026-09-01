using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;

namespace Services.LL.Essences;

public sealed class EssenceSlotUnlockService : IEssenceSlotUnlockService
{
    public int GetUnlockedSlotCount(int characterLevel)
        => EssenceSlotProgression.GetUnlockedSlotCount(characterLevel);
}

public sealed class EssenceLoadoutLimitService : IEssenceLoadoutLimitService
{
    public int GetLoadoutLimit(Guid characterId) => 3;
}

public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random = new();
    public double NextDouble() => _random.NextDouble();
}
