using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Economy;

public interface IEconomyLedgerRepository
{
    Task RecordItemAcquisitionsAsync(
        Guid characterId,
        IReadOnlyCollection<InventoryItem> items,
        string source,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    Task RecordGuildVaultMovementAsync(
        EconomyEventType eventType,
        Guid referenceId,
        Guid guildId,
        Character participant,
        EquipmentInstance equipment,
        bool participantIsSender,
        string source,
        CancellationToken cancellationToken);
}
