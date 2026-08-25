using Domain.Models.Bonuses;
using Domain.Models.Essences.Definitions;

namespace Domain.Models.Essences;

public sealed record SoulArchive(
    IReadOnlyList<PlayerEssenceArchiveEntry> Essences,
    int EssenceDust);

public sealed record CreatureArchive(
    IReadOnlyList<CreatureArchiveEntry> Creatures,
    bool CanChangeEssenceFocus,
    DateTimeOffset? EssenceFocusAvailableAtUtc,
    DateTimeOffset? EssenceFocusSetAtUtc);

public sealed record CreatureArchiveEntry(
    string CreatureId,
    string Name,
    int KillCount,
    DateTimeOffset FirstDefeatedAtUtc,
    DateTimeOffset LastDefeatedAtUtc,
    bool IsEssenceFocus,
    DateTimeOffset? EssenceFocusSetAtUtc,
    long EssenceFocusTotalDurationSeconds,
    long CurrentEssenceFocusDurationSeconds,
    IReadOnlyList<CreatureArchiveEssenceEntry> Essences,
    IReadOnlyList<CreatureArchiveLocation> Locations,
    IReadOnlyList<string> Tags);

public sealed record CreatureArchiveLocation(
    int RegionId,
    string RegionName,
    string SourceType,
    string SourceId,
    string SourceName);

public sealed record CreatureArchiveEssenceEntry(
    string EssenceDefinitionId,
    string Name,
    bool IsAbsorbed,
    IReadOnlyList<string> Tags,
    EssenceDefinition Definition);

public sealed record EssenceCodex(
    IReadOnlyList<EssenceCodexEntry> Entries);

public sealed record EssenceCodexEntry(
    string Id,
    string Title,
    string Description,
    string BenefitText,
    BonusKind BonusKind,
    double BaseBonusValue,
    double BonusValue,
    double BonusValuePerCollectionAscensionTier,
    int CollectionAscensionTier,
    int MaxCollectionAscensionTier,
    int Current,
    int Required,
    bool IsUnlocked,
    string Category,
    IReadOnlyList<EssenceCodexMember> Essences);

public sealed record EssenceCodexMember(
    string? EssenceDefinitionId,
    string Name,
    bool IsDiscovered,
    bool IsAbsorbed,
    int AscensionTier,
    EssenceDefinition? Definition);

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

public enum SaveEssenceLoadoutFailure
{
    Validation,
    NameConflict
}

public sealed record SaveEssenceLoadoutResult(
    bool Succeeded,
    string Message,
    EssenceLoadout? Loadout,
    SaveEssenceLoadoutFailure? Failure = null);

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
