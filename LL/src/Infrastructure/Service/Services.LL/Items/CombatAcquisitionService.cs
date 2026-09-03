using Services.LL.WorldTower;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Items;
using Application.WebSockets.Contracts;
using Domain.Models.CharacterActions;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.Extensions.Options;

namespace Services.LL.Items;

public sealed class CombatAcquisitionService(CombatAcquisitionCatalog catalog, ICombatAcquisitionRepository repository,
    IQuestRepository quests, ICharacterActionService actions, IStateSyncService sync, IOptions<EquipmentProgressionOptions> options, IOptions<WorldTowerOptions> towerOptions)
    : ICombatAcquisitionService
{
    public async Task<IReadOnlyList<CombatAcquisitionView>> GetAsync(Guid id, CancellationToken ct)
    {
        if (!options.Value.OrdinaryAcquisitionEnabled) return [];
        var views = new List<CombatAcquisitionView>();
        foreach (var pool in catalog.Pools)
            if (await GetPoolAsync(id, pool, ct) is { } view) views.Add(view);
        return views;
    }

    private async Task<CombatAcquisitionView?> GetPoolAsync(Guid id, CombatAcquisitionRules rules, CancellationToken ct)
    {
        if (!options.Value.OrdinaryAcquisitionEnabled) return null;
        var level = await repository.GetLevelAsync(id, ct);
        if (level == null) return null;
        var progress = await repository.GetAsync(id, rules.PoolId, ct);
        var sigils = new List<CombatAcquisitionSigilOption>();
        foreach (var source in rules.Sigils)
        {
            var error = level < source.MinimumLevel ? $"Requires level {source.MinimumLevel}."
                : source.RequiredQuestId != null && (await quests.GetProgressAsync(id, source.RequiredQuestId, ct))?.Status != QuestStatus.Completed
                    ? "Complete this dungeon's prerequisite quest first."
                : source.RequiredTowerFloor is { } floor && !await repository.HasClearedTowerFloorAsync(towerOptions.Value.ServerId, floor, ct)
                    ? $"Requires the server to clear World Tower floor {floor}." : null;
            sigils.Add(new(source.FamilyId, source.ItemBaseId, error == null, error));
        }
        return new(rules.PoolId, rules.Version, rules.RegionName, rules.EquipmentTier, level >= rules.MinimumLevel && (progress?.HasEnteredRegion ?? false),
            progress?.Plain?.Equipment.State.DefinitionId, progress?.PlainVictories ?? 0,
            progress?.Plain?.RequiredVictories ?? rules.PlainTargetVictories,
            progress?.Sigil?.FamilyId, progress?.SigilVictories ?? 0,
            progress?.Sigil?.RequiredVictories ?? rules.SigilVictories,
            rules.DiscoveryChance, catalog.Equipment.GetOptions(rules.EquipmentTier), sigils);
    }

    public async Task<CombatAcquisitionSelectionResult> SelectAsync(Guid id, Guid operationId, string poolId, string? definitionId, string? sigilFamilyId, CancellationToken ct)
    {
        if (!options.Value.OrdinaryAcquisitionEnabled) return new(null, "Ordinary equipment acquisition is not available for this character.");
        if (operationId == Guid.Empty || id == Guid.Empty) return new(null, "A selection operation ID is required.");
        var rules = catalog.Pools.SingleOrDefault(p => p.PoolId == poolId);
        if (rules == null) return new(null, "Select an eligible regional reward pool.");
        await repository.LockAsync(id, ct);
        var existing = await repository.GetSelectionAsync(id, operationId, ct);
        if (existing != null)
            return existing.PoolId == poolId && existing.DefinitionId == definitionId && existing.SigilFamilyId == sigilFamilyId
                ? new(await GetPoolAsync(id, rules, ct), null) : new(null, "Operation ID belongs to another selection request.");
        var view = await GetPoolAsync(id, rules, ct);
        if (view == null) return new(null, "Character was not found.");
        if (definitionId != null && !view.Targets.Any(x => x.DefinitionId == definitionId))
            return new(view, "Select an eligible plain equipment target.");
        if (sigilFamilyId != null && !view.Sigils.Any(x => x.FamilyId == sigilFamilyId && x.CanSelect))
            return new(view, "Select an unlocked dungeon family.");

        // The idle scheduler resolves encounters at their start boundary. Settle all due boundaries before changing choices.
        var action = await actions.PeekCharacterActionAsync(id, ct);
        if (action is { IsDeleted: false, CharacterActionType: CharacterActionType.Combat })
        {
            var resolved = await actions.GetCharacterActionAsync(id, ct);
            if (resolved?.ProcessedCount > 0)
                await sync.InvalidateCharacterScopesAsync(id, StateSyncScopes.CharacterResources, EquipmentKeys.TargetSelectionSettlementReason, ct);
            if (resolved is { HasMoreDueWork: true })
                return new(await GetPoolAsync(id, rules, ct), "Earned combat is still being resolved. Retry the selection after catching up.");
        }
        var progress = await repository.GetAsync(id, rules.PoolId, ct);
        if (progress is not { HasEnteredRegion: true } || await repository.GetLevelAsync(id, ct) < rules.MinimumLevel)
            return new(view, $"Reach level {rules.MinimumLevel} and fight in {rules.RegionName} to unlock these choices.");
        PlainEquipmentCommitment? plain = null;
        if (definitionId != null)
        {
            var data = EquipmentData.Create(EquipmentState.Award(Guid.NewGuid(), catalog.Equipment.Evaluator,
                definitionId, rules.EquipmentTier, 0, new(EquipmentAwardKind.ProtectedReward, rules.PoolId, operationId.ToString("N")),
                new(EquipmentOwnershipKind.BoundPersonal, id)), catalog.Equipment.Evaluator);
            plain = new(operationId, data, rules.PlainTargetVictories);
        }
        var sigil = rules.Sigils.SingleOrDefault(x => x.FamilyId == sigilFamilyId);
        progress.Select(plain, sigil == null ? null : new(sigil.FamilyId, sigil.ItemBaseId, rules.SigilVictories));
        repository.AddSelection(new() { CharacterId = id, OperationId = operationId, PoolId = poolId, DefinitionId = definitionId, SigilFamilyId = sigilFamilyId });
        return new(await GetPoolAsync(id, rules, ct), null);
    }
}
