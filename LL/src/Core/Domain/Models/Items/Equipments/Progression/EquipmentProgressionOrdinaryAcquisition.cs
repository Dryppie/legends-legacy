namespace Domain.Models.Items.Equipments.Progression;

public sealed record CombatAcquisitionArea(string AreaId);
public sealed record CombatAcquisitionSigil(string FamilyId, string ItemBaseId, int MinimumLevel, string? RequiredQuestId, int? RequiredTowerFloor = null);
public sealed record CombatAcquisitionRules(string Version, string PoolId, int EquipmentTier, int VictoriesPerPerfectDay,
    int PlainTargetVictories, int SigilVictories, double DiscoveryChance,
    IReadOnlyList<CombatAcquisitionArea> Areas, IReadOnlyList<CombatAcquisitionSigil> Sigils,
    string RegionName, int MinimumLevel);

public sealed class CombatAcquisitionCatalog
{
    public CombatAcquisitionCatalog(StarterEquipmentCatalog equipment, IEnumerable<CombatAcquisitionRules> pools)
    {
        Equipment = equipment;
        Pools = Array.AsReadOnly(pools.Select(r => r with { Areas = Array.AsReadOnly(r.Areas.ToArray()), Sigils = Array.AsReadOnly(r.Sigils.ToArray()) }).ToArray());
        if (Pools.Count == 0 || Pools.Select(r => r.PoolId).Distinct().Count() != Pools.Count
            || Pools.SelectMany(r => r.Areas).Select(a => a.AreaId).Distinct().Count() != Pools.Sum(r => r.Areas.Count))
            throw new ArgumentException("Ordinary pools and their areas must be unique.");
        foreach (var rules in Pools)
        {
            EquipmentValidation.Id(rules.RegionName);
            equipment.GetOptions(rules.EquipmentTier);
            EquipmentValidation.Id(rules.Version);
            EquipmentValidation.Id(rules.PoolId);
            if (rules.MinimumLevel < 1 || rules.VictoriesPerPerfectDay != 8640 || rules.PlainTargetVictories < 1
                || rules.SigilVictories < 1 || !double.IsFinite(rules.DiscoveryChance) || rules.DiscoveryChance is < 0 or > 1
                || rules.Areas.Count == 0 || rules.Sigils.Count == 0
                || rules.Areas.Select(x => x.AreaId).Distinct().Count() != rules.Areas.Count
                || rules.Sigils.Select(x => x.FamilyId).Distinct().Count() != rules.Sigils.Count
                || rules.Sigils.Select(x => x.ItemBaseId).Distinct().Count() != rules.Sigils.Count)
                throw new ArgumentException("Invalid ordinary equipment acquisition rules.");
            foreach (var area in rules.Areas)
            {
                EquipmentValidation.Id(area.AreaId);
            }
            foreach (var sigil in rules.Sigils)
            {
                EquipmentValidation.Id(sigil.FamilyId);
                EquipmentValidation.Id(sigil.ItemBaseId);
                if (sigil.RequiredQuestId != null) EquipmentValidation.Id(sigil.RequiredQuestId);
                if (sigil.RequiredTowerFloor is < 1) throw new ArgumentException("Invalid Tower floor requirement.");
                if (sigil.MinimumLevel < 1) throw new ArgumentException("Invalid sigil level requirement.");
            }
        }
    }
    public StarterEquipmentCatalog Equipment { get; }
    public IReadOnlyList<CombatAcquisitionRules> Pools { get; }
}

public sealed record PlainEquipmentCommitment(Guid SelectionId, EquipmentData Equipment, int RequiredVictories);
public sealed record SigilTargetCommitment(string FamilyId, string ItemBaseId, int RequiredVictories);
public sealed record CombatAcquisitionVictoryResult(bool Applied, EquipmentData? Target, string? SigilItemBaseId);

/// <summary>One bounded checkpoint per character/pool; inventory settlement and checkpoint share the command transaction.</summary>
public sealed class CombatAcquisitionProgress
{
    public Guid CharacterId { get; init; }
    public string PoolId { get; init; } = string.Empty;
    public bool HasEnteredRegion { get; private set; }
    public int PlainVictories { get; private set; }
    public int SigilVictories { get; private set; }
    public long LastScheduleGeneration { get; private set; } = -1;
    public DateTimeOffset? LastEncounterAtUtc { get; private set; }
    public long Revision { get; private set; }
    public PlainEquipmentCommitment? Plain { get; private set; }
    public SigilTargetCommitment? Sigil { get; private set; }

    public void Select(PlainEquipmentCommitment? plain, SigilTargetCommitment? sigil)
    {
        if (plain != null && (plain.RequiredVictories < 1 || plain.SelectionId == Guid.Empty
            || plain.Equipment.State.Ownership.OwnerId != CharacterId
            || plain.Equipment.State.Provenance.Kind != EquipmentAwardKind.ProtectedReward))
            throw new ArgumentException("Invalid plain target commitment.");
        if (sigil != null && sigil.RequiredVictories < 1) throw new ArgumentException("Invalid sigil commitment.");
        // A same-target request preserves its frozen item and terms. A cleared/secured target can be explicitly selected again.
        if (Plain?.Equipment.State.DefinitionId != plain?.Equipment.State.DefinitionId) Plain = plain;
        if (Sigil?.FamilyId != sigil?.FamilyId) Sigil = sigil;
        Revision++;
    }

    public CombatAcquisitionVictoryResult Apply(long generation, DateTimeOffset startedAt, bool victory)
    {
        if (generation < 0) throw new ArgumentOutOfRangeException(nameof(generation));
        if (generation < LastScheduleGeneration || generation == LastScheduleGeneration && startedAt <= LastEncounterAtUtc)
            return new(false, null, null);
        LastScheduleGeneration = generation;
        LastEncounterAtUtc = startedAt;
        HasEnteredRegion = true;
        Revision++;
        if (!victory) return new(true, null, null);
        EquipmentData? target = null;
        string? sigil = null;
        if (Plain is { } plain && ++PlainVictories >= plain.RequiredVictories)
        {
            target = plain.Equipment;
            PlainVictories = 0;
            Plain = null;
        }
        if (Sigil is { } access && ++SigilVictories >= access.RequiredVictories)
        {
            sigil = access.ItemBaseId;
            SigilVictories = 0;
        }
        return new(true, target, sigil);
    }
}

public sealed class CombatAcquisitionSelectionReceipt
{
    public Guid CharacterId { get; init; }
    public Guid OperationId { get; init; }
    public string PoolId { get; init; } = string.Empty;
    public string? DefinitionId { get; init; }
    public string? SigilFamilyId { get; init; }
}

public sealed record CombatAcquisitionSigilOption(string FamilyId, string ItemBaseId, bool CanSelect, string? UnavailableReason);
public sealed record CombatAcquisitionView(string PoolId, string RulesVersion, string RegionName, int EquipmentTier, bool HasEnteredRegion,
    string? SelectedDefinitionId, int PlainVictories, int RequiredPlainVictories,
    string? SelectedSigilFamilyId, int SigilVictories, int RequiredSigilVictories,
    double DiscoveryChance, IReadOnlyList<StarterEquipmentOption> Targets, IReadOnlyList<CombatAcquisitionSigilOption> Sigils);
public sealed record CombatAcquisitionSelectionResult(CombatAcquisitionView? State, string? Error);

public interface ICombatAcquisitionRepository
{
    Task LockAsync(Guid characterId, CancellationToken ct);
    Task<int?> GetLevelAsync(Guid characterId, CancellationToken ct);
    Task<bool> HasClearedTowerFloorAsync(string serverId, int floor, CancellationToken ct);
    Task<CombatAcquisitionProgress?> GetAsync(Guid characterId, string poolId, CancellationToken ct);
    void Add(CombatAcquisitionProgress progress);
    Task<CombatAcquisitionSelectionReceipt?> GetSelectionAsync(Guid characterId, Guid operationId, CancellationToken ct);
    void AddSelection(CombatAcquisitionSelectionReceipt receipt);
}
