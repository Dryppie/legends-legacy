using Domain.Models.Items.Equipments.Progression;

namespace Application.Interfaces.Services.LL.Quests;

public interface IEquipmentQuestSupport
{
    Task<bool> IsEquippedAsync(Guid characterId, string objectiveType, string? starterKind, CancellationToken ct);
}

public interface IQuestEquipmentRewardRepository
{
    Task<IReadOnlyList<EquipmentData>> GetEquippedAsync(Guid characterId, CancellationToken ct);
    Task AwardCindersAsync(Guid characterId, string questId, long amount, CancellationToken ct);
}
