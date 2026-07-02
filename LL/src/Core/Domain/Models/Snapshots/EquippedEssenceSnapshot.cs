using Domain.Models.Essences;

namespace Domain.Models.Snapshots;

public sealed class EquippedEssenceSnapshot
{
    public Guid Id { get; init; }
    public Guid CharacterSnapshotId { get; init; }
    public int SlotIndex { get; init; }
    public Guid PlayerEssenceId { get; init; }
    public string EssenceDefinitionId { get; init; } = string.Empty;
    public int NativeRegion { get; init; } = 1;
    public int PotentialTier { get; init; } = 1;
    public int Level { get; init; }
    public int CurrentXp { get; init; }
    public int AscensionTier { get; init; }
    public bool IsEvolved { get; init; }

    private EquippedEssenceSnapshot() { }

    public static EquippedEssenceSnapshot From(Guid characterSnapshotId, int slotIndex, PlayerEssence essence) =>
        new()
        {
            Id = Guid.NewGuid(),
            CharacterSnapshotId = characterSnapshotId,
            SlotIndex = slotIndex,
            PlayerEssenceId = essence.Id,
            EssenceDefinitionId = essence.EssenceDefinitionId,
            NativeRegion = essence.NativeRegion,
            PotentialTier = essence.PotentialTier,
            Level = essence.Level,
            CurrentXp = essence.CurrentXp,
            AscensionTier = essence.AscensionTier,
            IsEvolved = essence.IsEvolved
        };

    public PlayerEssence ToPlayerEssence(Guid characterId) =>
        new()
        {
            Id = PlayerEssenceId,
            CharacterId = characterId,
            EssenceDefinitionId = EssenceDefinitionId,
            NativeRegion = NativeRegion,
            PotentialTier = PotentialTier,
            Level = Level,
            CurrentXp = CurrentXp,
            AscensionTier = AscensionTier,
            IsEvolved = IsEvolved
        };
}
