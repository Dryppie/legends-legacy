using Application.Interfaces.Services.LL.Essences;

namespace Services.LL.Essences;

public sealed class EssenceSlotUnlockService : IEssenceSlotUnlockService
{
    public int GetUnlockedSlotCount(int characterLevel)
    {
        var unlocked = Math.Max(1, characterLevel / 10 + 1);
        return Math.Clamp(unlocked, 1, 10);
    }
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
