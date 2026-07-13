using Domain.Models.Colosseum;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Achievements;

public interface IAchievementRepository
{
    Task<IReadOnlyList<AchievementDefinition>> GetActiveDefinitionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AchievementDefinition>> GetActiveDefinitionsAsync(AchievementRequirementType requirementType, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerAchievementProgress>> GetProgressesAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
    Task<PlayerAchievementProgress?> GetProgressAsync(Guid accountId, Guid? characterId, Guid achievementDefinitionId, int? seasonId, CancellationToken cancellationToken);
    Task AddProgressAsync(PlayerAchievementProgress progress, CancellationToken cancellationToken);
    Task<int> CountCompletedAchievementsAsync(Guid accountId, CancellationToken cancellationToken);
    Task<int> GetTotalAchievementPointsAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TitleDefinition>> GetActiveTitlesAsync(CancellationToken cancellationToken);
    Task<TitleDefinition?> GetActiveTitleByKeyAsync(string titleKey, CancellationToken cancellationToken);
    Task<TitleDefinition?> GetActiveTitleBySourceAchievementKeyAsync(string achievementKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerTitleUnlock>> GetTitleUnlocksAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
    Task<int> CountTitleUnlocksAsync(Guid accountId, CancellationToken cancellationToken);
    Task<bool> HasTitleUnlockAsync(Guid accountId, Guid? characterId, Guid titleDefinitionId, int? seasonId, CancellationToken cancellationToken);
    Task AddTitleUnlockAsync(PlayerTitleUnlock unlock, CancellationToken cancellationToken);

    Task<Character?> GetCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
    Task<Guid> GetAccountIdForCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<string?> GetCharacterNameAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, Guid>> GetAccountIdsForCharactersAsync(IReadOnlyCollection<Guid> characterIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlayerEssence>> GetPlayerEssencesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<int> GetEquippedEssenceCountAsync(Guid characterId, CancellationToken cancellationToken);
    Task<int> GetBlueprintUnlockCountAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EquipmentInstance>> GetOwnedEquipmentAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DungeonCompletionRecord>> GetDungeonCompletionsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ColosseumMatchResult>> GetColosseumMatchesAsync(Guid characterId, CancellationToken cancellationToken);
}
