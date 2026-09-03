using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Economy;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Quests;

public sealed class QuestEquipmentRewardRepository(IDbContext db) : IQuestEquipmentRewardRepository
{
    public async Task<IReadOnlyList<EquipmentData>> GetEquippedAsync(Guid id, CancellationToken ct) =>
        (await db.EquipmentSlots.Include(x => x.EquipmentInstance).Where(x => x.EntityId == id).ToListAsync(ct))
        .Select(x => x.EquipmentInstance?.ProgressionData).Where(x => x != null).Cast<EquipmentData>().ToArray();

    public async Task AwardCindersAsync(Guid id, string questId, long amount, CancellationToken ct)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        await db.AcquireCharacterRowsLockAsync([id], ct);
        var character = await db.Characters.SingleAsync(x => x.Id == id, ct);
        character.Cinders = checked(character.Cinders + amount);
        db.EconomyLedger.Add(new() { EventType = EconomyEventType.QuestReward, AssetType = EconomyAssetType.Currency,
            RecipientCharacterId = id, RecipientAccountId = character.UserId, RecipientCharacterLevel = character.Level,
            AssetId = "cinders", AssetName = "Cinders", Quantity = amount, Source = questId });
    }
}
