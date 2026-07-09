namespace Domain.Models.Essences;

public sealed record SoulArchive(
    IReadOnlyList<PlayerEssenceArchiveEntry> Essences,
    int EssenceDust);

public sealed record CreatureArchive(
    IReadOnlyList<CreatureArchiveEntry> Creatures);

public sealed record CreatureArchiveEntry(
    string CreatureId,
    string Name,
    int KillCount,
    DateTimeOffset FirstDefeatedAtUtc,
    DateTimeOffset LastDefeatedAtUtc,
    string? EssenceDefinitionId,
    string? EssenceName,
    bool IsEssenceAbsorbed,
    IReadOnlyList<string> Tags);

public sealed record EssenceCodex(
    IReadOnlyList<EssenceCodexEntry> Entries);

public sealed record EssenceCodexEntry(
    string Id,
    string Title,
    string Description,
    string BenefitText,
    int Current,
    int Required,
    bool IsUnlocked,
    string Category);

public sealed record PlayerEssenceArchiveEntry(
    PlayerEssence Essence,
    int? AttunedSlot);

public sealed record EssenceLoadouts(
    IReadOnlyList<EssenceLoadout> Loadouts,
    int Limit,
    int UnlockedSlots);

public sealed record SaveEssenceLoadoutRequest(
    Guid? Id,
    string Name,
    IReadOnlyList<SaveEssenceLoadoutSlotRequest> Slots);

public sealed record SaveEssenceLoadoutSlotRequest(
    int SlotIndex,
    Guid? PlayerEssenceId);

public sealed record EssenceOperationResult(
    bool Succeeded,
    string Message);

public sealed record DismantleEssenceResult(
    bool Succeeded,
    string Message,
    int DustGained);

public sealed record SpendEssenceDustResult(
    bool Succeeded,
    string Message,
    int DustSpent,
    int XpGained,
    int LevelsGained,
    bool ReachedTierCap);
