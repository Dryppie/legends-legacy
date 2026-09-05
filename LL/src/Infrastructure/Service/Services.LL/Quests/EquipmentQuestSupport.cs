using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Items.Equipments.Progression;

namespace Services.LL.Quests;

public sealed class EquipmentQuestSupport(IQuestEquipmentRewardRepository equipment, IStarterEquipmentRepository starters,
    IPlainEquipmentRepository plain) : IEquipmentQuestSupport
{
    public async Task<bool> IsEquippedAsync(Guid characterId, string objectiveType, string? starterKind, CancellationToken ct)
    {
        var equipped = (await equipment.GetEquippedAsync(characterId, ct)).Where(x =>
            x.State.Ownership.OwnerId == characterId && x.State.Ownership.Kind != EquipmentOwnershipKind.GuildOwned)
            .DistinctBy(x => x.State.Id).ToArray();
        if (objectiveType == EquipmentKeys.AreaDropObjective)
        {
            var earned = await plain.GetAsync(characterId, ct);
            return earned.Any(x => x.Copies > 0 && equipped.Any(e => e.State.DefinitionId == x.DefinitionId && e.State.Tier == x.Tier));
        }
        if (objectiveType != EquipmentKeys.StarterLoadoutObjective || !Enum.TryParse<StarterEquipmentGrantKind>(starterKind, out var kind))
            throw new InvalidOperationException("Unknown Equipment progression quest equipment objective.");
        var grant = await starters.GetGrantAsync(characterId, kind, ct);
        return grant != null && grant.Equipment.GroupBy(x => (x.State.DefinitionId, x.State.Tier)).All(required =>
            equipped.Count(x => x.State.DefinitionId == required.Key.DefinitionId && x.State.Tier == required.Key.Tier) >= required.Count());
    }
}
