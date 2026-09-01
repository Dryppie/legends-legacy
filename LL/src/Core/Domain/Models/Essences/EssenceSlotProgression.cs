namespace Domain.Models.Essences;

public static class EssenceSlotProgression
{
    public static int GetUnlockedSlotCount(int characterLevel)
    {
        var unlocked = Math.Max(1, characterLevel / 10 + 1);
        return Math.Clamp(unlocked, 1, 10);
    }
}
