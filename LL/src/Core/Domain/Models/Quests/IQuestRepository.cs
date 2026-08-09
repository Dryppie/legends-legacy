namespace Domain.Models.Quests;

public interface IQuestRepository
{
    Task<IReadOnlyList<CharacterQuestProgress>> GetProgressesAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<CharacterQuestProgress?> GetProgressAsync(
        Guid characterId,
        string questId,
        CancellationToken cancellationToken);

    Task<int?> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken);

    Task<bool> HasProcessedEventAsync(Guid outboxMessageId, CancellationToken cancellationToken);

    Task<bool> HasEssenceInActiveLoadoutAsync(
        Guid characterId,
        string essenceDefinitionId,
        CancellationToken cancellationToken);

    Task<bool> HasQualifyingEquipmentEquippedAsync(
        Guid characterId,
        IReadOnlyCollection<string> itemBaseIds,
        int? tier,
        bool mustBeCrafted,
        bool toolSlotOnly,
        CancellationToken cancellationToken);

    void AddProgress(CharacterQuestProgress progress);
    void AddEventLedger(QuestEventLedger ledger);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
