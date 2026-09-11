using Application.Interfaces.Services.LL.Items;
using Domain.Models.Essences;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Loadouts;
using Domain.Models.Items.Equipments.Slots;

namespace Services.LL.Items;

public sealed class EquipmentLoadoutService(IEquipmentLoadoutRepository repository, IEquipmentSlotRepository equipment,
    ICombatStyleMutationBoundary? buildBoundary = null,
    Application.Interfaces.Services.LL.Nobility.INobilityService? nobility = null,
    TimeProvider? time = null) : IEquipmentLoadoutService
{
    public async Task<List<EquipmentLoadout>> GetAsync(Guid characterId, CancellationToken ct)
    {
        var loadouts = await repository.GetAsync(characterId, ct);
        var limit = await GetLimitAsync(characterId, ct);
        var legacySlot = 0;
        foreach (var loadout in loadouts.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            loadout.IsUsable = (loadout.PresetSlot > 0 ? loadout.PresetSlot : ++legacySlot) <= limit;
        return loadouts;
    }

    private async Task<int> GetLimitAsync(Guid characterId, CancellationToken ct) => nobility is null ? 3 :
        (await nobility.GetBenefitsAsync(characterId, nobility.CombatEvaluationTime ?? (time ?? TimeProvider.System).GetUtcNow(), ct)).EquipmentLoadouts;

    public async Task<EquipmentEquipResult> SaveAsync(Guid characterId, Guid? id, string name, CancellationToken ct)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 80) return EquipmentEquipResult.Fail("Choose a loadout name between 1 and 80 characters.");
        var loadouts = await GetAsync(characterId, ct);
        var loadout = loadouts.SingleOrDefault(x => x.Id == id);
        if (id.HasValue && loadout is null) return EquipmentEquipResult.Fail("Equipment loadout not found.");
        if (loadout is { IsUsable: false }) return EquipmentEquipResult.Fail("This saved preset requires Nobility. You can still view or copy it.");
        if (loadouts.Any(x => x.Id != id && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return EquipmentEquipResult.Fail("An equipment loadout with that name already exists.");
        var limit = await GetLimitAsync(characterId, ct);
        var freeSlot = Enumerable.Range(1, limit).FirstOrDefault(slot => !loadouts.Any(x => x.PresetSlot == slot));
        if (loadout is null && (loadouts.Count(x => x.IsUsable) >= limit || freeSlot == 0)) return EquipmentEquipResult.Fail("Equipment loadout limit reached.");
        var slots = await equipment.GetEquipmentSlotsByEntityIdAsync(characterId, ct);
        var available = await repository.GetAvailableItemIdsAsync(characterId, ct);
        if (slots.Any(x => x.EquipmentInstanceId.HasValue && !available.Contains(x.EquipmentInstanceId.Value)))
            return EquipmentEquipResult.Fail("Some equipped items are no longer available.");
        if (buildBoundary is not null && await buildBoundary.PrepareMutationAsync(characterId, ct) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        if (loadout is null)
        {
            loadout = new() { Id = Guid.NewGuid(), CharacterId = characterId, PresetSlot = freeSlot, CreatedAt = DateTimeOffset.UtcNow };
            await repository.AddAsync(loadout, ct);
        }
        loadout.Name = name;
        repository.ReplaceSlots(loadout, slots);
        return EquipmentEquipResult.Success();
    }

    public async Task<EquipmentEquipResult> DeleteAsync(Guid characterId, Guid id, CancellationToken ct)
    {
        var loadout = (await repository.GetAsync(characterId, ct)).SingleOrDefault(x => x.Id == id);
        if (loadout is null) return EquipmentEquipResult.Fail("Equipment loadout not found.");
        if (buildBoundary is not null && await buildBoundary.PrepareMutationAsync(characterId, ct) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        repository.Remove(loadout);
        return EquipmentEquipResult.Success();
    }

    public async Task<EquipmentEquipResult> CopyAsync(Guid characterId, Guid sourceId, Guid targetId, CancellationToken ct)
    {
        var loadouts = await GetAsync(characterId, ct);
        var source = loadouts.SingleOrDefault(x => x.Id == sourceId);
        var target = loadouts.SingleOrDefault(x => x.Id == targetId);
        if (source is null || target is null || sourceId == targetId) return EquipmentEquipResult.Fail("Choose two different saved presets.");
        if (!target.IsUsable) return EquipmentEquipResult.Fail("Choose an available destination preset.");
        if (buildBoundary is not null && await buildBoundary.PrepareMutationAsync(characterId, ct) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        repository.ReplaceSlots(target, source.Slots.Select(x => new EquipmentSlot
        {
            EntityId = characterId, EquipmentSlotType = x.SlotType,
            EquipmentInstanceId = x.EquipmentInstanceId, EquipmentInstance = x.EquipmentInstance
        }).ToList());
        return EquipmentEquipResult.Success();
    }

    public async Task<EquipmentEquipResult> SetActivitiesAsync(Guid characterId, Guid id, IReadOnlyList<EssenceCombatActivity> activities, CancellationToken ct)
    {
        if (activities.Any(x => !EssenceLoadoutSelection.IsValidSingleActivity(x))) return EquipmentEquipResult.Fail("Unsupported combat activity.");
        var loadouts = await GetAsync(characterId, ct);
        if (loadouts.All(x => x.Id != id)) return EquipmentEquipResult.Fail("Equipment loadout not found.");
        if (loadouts.Any(x => x.Id == id && !x.IsUsable)) return EquipmentEquipResult.Fail("This saved preset requires Nobility.");
        if (buildBoundary is not null && await buildBoundary.PrepareMutationAsync(characterId, ct) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        var mask = activities.Aggregate(EssenceCombatActivity.None, (a, b) => a | b);
        foreach (var loadout in loadouts)
            loadout.AutoUseActivities = loadout.Id == id ? mask : loadout.AutoUseActivities & ~mask;
        return EquipmentEquipResult.Success();
    }

    public async Task<List<EquipmentSlot>?> ResolveAsync(Guid characterId, EssenceCombatActivity activity, CancellationToken ct)
    {
        if (activity == EssenceCombatActivity.None) return null;
        var loadout = (await GetAsync(characterId, ct)).FirstOrDefault(x => x.IsUsable && (x.AutoUseActivities & activity) == activity);
        if (loadout is null || !await IsAvailableAsync(loadout, ct)) return null;
        return loadout.Slots.Select(x => new EquipmentSlot
        {
            EntityId = characterId, EquipmentSlotType = x.SlotType,
            EquipmentInstanceId = x.EquipmentInstanceId, EquipmentInstance = x.EquipmentInstance
        }).ToList();
    }

    public async Task<EquipmentEquipResult> ApplyAsync(Guid characterId, Guid id, CancellationToken ct)
    {
        var loadout = (await GetAsync(characterId, ct)).SingleOrDefault(x => x.Id == id);
        if (loadout is null) return EquipmentEquipResult.Fail("Equipment loadout not found.");
        if (!loadout.IsUsable) return EquipmentEquipResult.Fail("This saved preset requires Nobility.");
        if (!await IsAvailableAsync(loadout, ct)) return EquipmentEquipResult.Fail("Some saved equipment is no longer available. Update this loadout before equipping it.");
        if (buildBoundary is not null && await buildBoundary.PrepareMutationAsync(characterId, ct) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        var current = await equipment.GetEquipmentSlotsByEntityIdAsync(characterId, ct);
        foreach (var slot in current.Where(x => x.EquipmentInstanceId.HasValue).ToList())
            if (slot.EquipmentInstanceId.HasValue)
                await equipment.UnequipEquipmentAsync(characterId, slot.EquipmentSlotType, ct);
        foreach (var slot in loadout.Slots.OrderBy(x => x.SlotType).DistinctBy(x => x.EquipmentInstanceId))
        {
            var result = await equipment.EquipEquipmentAsync(characterId, slot.EquipmentInstanceId!.Value, slot.SlotType, ct);
            if (!result.Succeeded) throw new InvalidOperationException(result.ErrorMessage ?? "Equipment changed while applying the loadout.");
        }
        return EquipmentEquipResult.Success();
    }

    private async Task<bool> IsAvailableAsync(EquipmentLoadout loadout, CancellationToken ct)
    {
        var available = await repository.GetAvailableItemIdsAsync(loadout.CharacterId, ct);
        return loadout.Slots.All(x => x.EquipmentInstanceId.HasValue && x.EquipmentInstance is not null && available.Contains(x.EquipmentInstanceId.Value));
    }
}
