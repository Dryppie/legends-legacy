using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Items;
using Application.UseCases.Outbox;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Options;

namespace Services.LL.Items;

public sealed class PlainEquipmentRecoveryService(IPlainEquipmentRepository plain, IEquipmentAcquisitionRepository acquisition,
    IStarterEquipmentRepository starters, IOptions<EquipmentProgressionOptions> options, TimeProvider clock,
    IGameEventOutbox outbox) : IPlainEquipmentRecoveryService
{
    private async Task<IReadOnlyList<EquipmentData>> StartersAsync(Guid id, CancellationToken ct)
    {
        var result = new List<EquipmentData>();
        foreach (var kind in Enum.GetValues<StarterEquipmentGrantKind>())
            if (await starters.GetGrantAsync(id, kind, ct) is { } grant) result.AddRange(grant.Equipment);
        return result;
    }
    public async Task<IReadOnlyList<PlainEquipmentRecoveryOption>> GetOptionsAsync(Guid id, CancellationToken ct)
    {
        if (!options.Value.BaselineRecoveryEnabled) return [];
        var owned = await acquisition.GetOwnedAndPendingAsync(id, ct);
        var original = await StartersAsync(id, ct);
        return (await plain.GetAsync(id, ct)).Select(x => x.GetOption(owned, original)).ToArray();
    }
    public async Task<PlainEquipmentRecoveryResult> RecoverAsync(Guid id, Guid operationId, string definitionId, int tier, CancellationToken ct)
    {
        if (!options.Value.BaselineRecoveryEnabled) return new(null, "Equipment recovery is not available yet.");
        if (id == Guid.Empty || operationId == Guid.Empty || string.IsNullOrWhiteSpace(definitionId) || tier < 1)
            return new(null, "Invalid recovery request.");
        await acquisition.LockAsync(id, ct);
        var existing = await plain.GetRecoveryAsync(id, operationId, ct);
        if (existing != null) return existing.DefinitionId == definitionId && existing.Tier == tier
            ? new(existing, null) : new(null, "Operation ID belongs to another recovery request.");
        var entitlement = (await plain.GetAsync(id, ct)).SingleOrDefault(x => x.DefinitionId == definitionId && x.Tier == tier);
        if (entitlement == null) return new(null, "Earn this plain target before recovering it.");
        var recovery = entitlement.Recover(await acquisition.GetOwnedAndPendingAsync(id, ct), await StartersAsync(id, ct), operationId, clock.GetUtcNow());
        await plain.AwardRecoveryAsync(id, recovery, ct);
        if (recovery.Equipment.Count > 0) await outbox.EnqueueAsync(GameEventTypes.PlainEquipmentRecovered, recovery, id, null, ct);
        return new(recovery, null);
    }
}
