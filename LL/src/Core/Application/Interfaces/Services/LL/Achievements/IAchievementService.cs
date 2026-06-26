using Application.UseCases.Achievements.Dtos;
using Domain.Models.Achievements;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Achievements;

public interface IAchievementService
{
    Task<AchievementOverviewDto> GetOverviewAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AchievementDto>> GetAchievementsAsync(Guid accountId, Guid characterId, AchievementFilters filters, CancellationToken cancellationToken);
    Task<IReadOnlyList<TitleDto>> GetTitlesAsync(Guid accountId, Guid characterId, TitleFilters filters, CancellationToken cancellationToken);
    Task<EquippedTitleDto?> EquipTitleAsync(
        Guid accountId,
        Guid characterId,
        string titleKey,
        TitleDisplayPosition displayPosition,
        CancellationToken cancellationToken);
    Task UnequipTitleAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AchievementUnlockDto>> AddProgressAsync(
        Guid accountId,
        Guid? characterId,
        AchievementRequirementType requirementType,
        long amount = 1,
        string? requirementTarget = null,
        bool setToMax = false,
        int? seasonId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
    Task RecordColosseumBattleAsync(
        Guid characterId,
        Guid opponentCharacterId,
        BattleOutcome outcome,
        int characterRatingBefore,
        int opponentRatingBefore,
        CancellationToken cancellationToken);
    Task RecordDungeonRunStartedAsync(Guid characterId, CancellationToken cancellationToken);
    Task RecordDungeonRunCompletedAsync(
        Guid characterId,
        string dungeonDefinitionId,
        bool completedWithoutDefeat,
        bool completedWithoutCheckpointRetreat,
        IReadOnlyCollection<string> defeatedBossKeys,
        CancellationToken cancellationToken);
    Task RecordIdleCombatAsync(
        Guid characterId,
        int monstersDefeated,
        IReadOnlyCollection<string> defeatedCreatureFamilyKeys,
        int playerDefeats,
        int? lowestWinningHealthPercent,
        CancellationToken cancellationToken);
    Task RecordEssenceAbsorbedAsync(
        Guid characterId,
        int uniqueEssenceCount,
        IReadOnlyCollection<string> completedCollectionKeys,
        CancellationToken cancellationToken);
    Task RecordEssenceLoadoutSavedAsync(Guid characterId, int equippedEssenceCount, CancellationToken cancellationToken);
    Task RecordEssenceAscendedAsync(Guid characterId, int ascensionTier, int ascendedToTierCount, CancellationToken cancellationToken);
    Task RecordItemsCraftedAsync(Guid characterId, IReadOnlyCollection<EquipmentInstance> craftedItems, CancellationToken cancellationToken);
    Task RecordItemsTemperedAsync(
        Guid characterId,
        TemperingSummary summary,
        IReadOnlyCollection<EquipmentInstance> completedItems,
        CancellationToken cancellationToken);
    Task RecordBlueprintUnlockedAsync(Guid characterId, CancellationToken cancellationToken);
    Task RecordCharacterCreatedAsync(Guid characterId, CancellationToken cancellationToken);
    Task RecordCharacterLevelReachedAsync(Guid characterId, int level, CancellationToken cancellationToken);
    Task<AchievementRecalculationResultDto?> RecalculateProgressAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken);
}
