using Application.Common.Interfaces;
using Domain.Models.Achievements;
using Domain.Models.Colosseum;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Achievements;

public sealed class AchievementRepository(IDbContext context) : IAchievementRepository
{
    public async Task<IReadOnlyList<AchievementDefinition>> GetActiveDefinitionsAsync(CancellationToken cancellationToken) =>
        await context.AchievementDefinitions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AchievementDefinition>> GetActiveDefinitionsAsync(
        AchievementRequirementType requirementType,
        CancellationToken cancellationToken) =>
        await context.AchievementDefinitions
            .AsNoTracking()
            .Where(x => x.IsActive && x.RequirementType == requirementType)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlayerAchievementProgress>> GetProgressesAsync(
        Guid accountId,
        Guid characterId,
        CancellationToken cancellationToken) =>
        await context.PlayerAchievementProgresses
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && (x.CharacterId == null || x.CharacterId == characterId))
            .ToListAsync(cancellationToken);

    public async Task<PlayerAchievementProgress?> GetProgressAsync(
        Guid accountId,
        Guid? characterId,
        Guid achievementDefinitionId,
        int? seasonId,
        CancellationToken cancellationToken)
    {
        var local = context.PlayerAchievementProgresses.Local.FirstOrDefault(x =>
            x.AccountId == accountId &&
            x.CharacterId == characterId &&
            x.AchievementDefinitionId == achievementDefinitionId &&
            x.SeasonId == seasonId);

        return local ?? await context.PlayerAchievementProgresses.FirstOrDefaultAsync(x =>
            x.AccountId == accountId &&
            x.CharacterId == characterId &&
            x.AchievementDefinitionId == achievementDefinitionId &&
            x.SeasonId == seasonId,
            cancellationToken);
    }

    public async Task AddProgressAsync(PlayerAchievementProgress progress, CancellationToken cancellationToken) =>
        await context.PlayerAchievementProgresses.AddAsync(progress, cancellationToken);

    public Task<int> CountCompletedAchievementsAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.PlayerAchievementProgresses.CountAsync(x => x.AccountId == accountId && x.IsCompleted, cancellationToken);

    public Task<int> GetTotalAchievementPointsAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.PlayerAchievementProgresses
            .Where(x => x.AccountId == accountId && x.IsCompleted)
            .Join(
                context.AchievementDefinitions,
                progress => progress.AchievementDefinitionId,
                definition => definition.Id,
                (_, definition) => definition.Points)
            .SumAsync(cancellationToken);

    public async Task<IReadOnlyList<TitleDefinition>> GetActiveTitlesAsync(CancellationToken cancellationToken) =>
        await context.TitleDefinitions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<TitleDefinition?> GetActiveTitleByKeyAsync(string titleKey, CancellationToken cancellationToken) =>
        context.TitleDefinitions.FirstOrDefaultAsync(x => x.Key == titleKey && x.IsActive, cancellationToken);

    public Task<TitleDefinition?> GetActiveTitleBySourceAchievementKeyAsync(string achievementKey, CancellationToken cancellationToken) =>
        context.TitleDefinitions.FirstOrDefaultAsync(x => x.IsActive && x.SourceAchievementKey == achievementKey, cancellationToken);

    public async Task<IReadOnlyList<PlayerTitleUnlock>> GetTitleUnlocksAsync(
        Guid accountId,
        Guid characterId,
        CancellationToken cancellationToken) =>
        await context.PlayerTitleUnlocks
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && (x.CharacterId == null || x.CharacterId == characterId))
            .ToListAsync(cancellationToken);

    public Task<int> CountTitleUnlocksAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.PlayerTitleUnlocks.CountAsync(x => x.AccountId == accountId, cancellationToken);

    public async Task<bool> HasTitleUnlockAsync(
        Guid accountId,
        Guid? characterId,
        Guid titleDefinitionId,
        int? seasonId,
        CancellationToken cancellationToken) =>
        context.PlayerTitleUnlocks.Local.Any(x =>
            x.AccountId == accountId &&
            x.CharacterId == characterId &&
            x.TitleDefinitionId == titleDefinitionId &&
            x.SeasonId == seasonId) ||
        await context.PlayerTitleUnlocks.AnyAsync(x =>
            x.AccountId == accountId &&
            x.CharacterId == characterId &&
            x.TitleDefinitionId == titleDefinitionId &&
            x.SeasonId == seasonId,
            cancellationToken);

    public async Task AddTitleUnlockAsync(PlayerTitleUnlock unlock, CancellationToken cancellationToken) =>
        await context.PlayerTitleUnlocks.AddAsync(unlock, cancellationToken);

    public Task<Character?> GetCharacterAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken) =>
        context.Characters.FirstOrDefaultAsync(x => x.Id == characterId && x.UserId == accountId, cancellationToken);

    public Task<Guid> GetAccountIdForCharacterAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.Characters
            .Where(x => x.Id == characterId)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<string?> GetCharacterNameAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetAccountIdsForCharactersAsync(
        IReadOnlyCollection<Guid> characterIds,
        CancellationToken cancellationToken) =>
        await context.Characters
            .AsNoTracking()
            .Where(x => characterIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UserId, cancellationToken);

    public async Task<IReadOnlyList<PlayerEssence>> GetPlayerEssencesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await context.PlayerEssences
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

    public Task<int> GetEquippedEssenceCountAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.EssenceLoadoutSlots
            .AsNoTracking()
            .Where(x => x.PlayerEssenceId != null && x.EssenceLoadout.CharacterId == characterId && x.EssenceLoadout.IsActive)
            .CountAsync(cancellationToken);

    public Task<int> GetBlueprintUnlockCountAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.CharacterRecipeUnlocks
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .Select(x => x.BlueprintId)
            .Where(x => x != string.Empty)
            .Distinct()
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<EquipmentInstance>> GetOwnedEquipmentAsync(Guid characterId, CancellationToken cancellationToken) =>
        await context.ItemInstances
            .AsNoTracking()
            .OfType<EquipmentInstance>()
            .Where(item => context.InventoryItems.Any(inventoryItem =>
                inventoryItem.InventoryId == characterId &&
                inventoryItem.ItemInstanceId == item.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DungeonCompletionRecord>> GetDungeonCompletionsAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await context.DungeonCompletionRecords
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ColosseumMatchResult>> GetColosseumMatchesAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await context.ColosseumMatches
            .AsNoTracking()
            .Where(x => x.CharacterAId == characterId || x.CharacterBId == characterId)
            .OrderBy(x => x.PlayedAt)
            .ToListAsync(cancellationToken);
}
