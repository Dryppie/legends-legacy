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
        CancellationToken cancellationToken)
    {
        var persisted = await context.PlayerAchievementProgresses
            .Where(x => x.AccountId == accountId && (x.CharacterId == null || x.CharacterId == characterId))
            .ToListAsync(cancellationToken);

        return persisted
            .Concat(context.PlayerAchievementProgresses.Local.Where(x =>
                x.AccountId == accountId && (x.CharacterId == null || x.CharacterId == characterId)))
            .DistinctBy(x => x.Id)
            .ToList();
    }

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

    public async Task<int> CountCompletedAchievementsAsync(Guid accountId, CancellationToken cancellationToken) =>
        (await GetAccountProgressesAsync(accountId, cancellationToken)).Count(x => x.IsCompleted);

    public async Task<int> CountCompletedNonHiddenAchievementsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var progresses = await GetAccountProgressesAsync(accountId, cancellationToken);
        var completedDefinitionIds = progresses
            .Where(x => x.IsCompleted)
            .Select(x => x.AchievementDefinitionId)
            .ToHashSet();

        return await context.AchievementDefinitions
            .AsNoTracking()
            .CountAsync(x =>
                x.IsActive &&
                x.Visibility != AchievementVisibility.Hidden &&
                x.RequirementType != AchievementRequirementType.NonHiddenAchievementsCompleted &&
                completedDefinitionIds.Contains(x.Id),
                cancellationToken);
    }

    public async Task<int> GetTotalAchievementPointsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var progresses = await GetAccountProgressesAsync(accountId, cancellationToken);
        var completedDefinitionIds = progresses
            .Where(x => x.IsCompleted)
            .Select(x => x.AchievementDefinitionId)
            .ToHashSet();

        return await context.AchievementDefinitions
            .AsNoTracking()
            .Where(x => completedDefinitionIds.Contains(x.Id))
            .SumAsync(x => x.Points, cancellationToken);
    }

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

    public async Task<int> CountTitleUnlocksAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var persisted = await context.PlayerTitleUnlocks
            .Where(x => x.AccountId == accountId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return persisted
            .Concat(context.PlayerTitleUnlocks.Local.Where(x => x.AccountId == accountId).Select(x => x.Id))
            .Distinct()
            .Count();
    }

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

    public Task<int> GetCompletedProphecyCountAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.PlayerProphecyInstances.CountAsync(x =>
            x.PlayerId == accountId &&
            (x.Status == Domain.Models.Prophecies.ProphecyStatus.Completed || x.Status == Domain.Models.Prophecies.ProphecyStatus.Claimed),
            cancellationToken);

    public async Task<bool> HasCompletedWeeklyProphecyCycleAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var weekly = await context.PlayerProphecyInstances
            .AsNoTracking()
            .Where(x => x.PlayerId == accountId && x.Scope == Domain.Models.Prophecies.ProphecyScope.Weekly)
            .Select(x => new { x.PeriodStart, x.PeriodEnd, x.Status })
            .ToListAsync(cancellationToken);

        return weekly
            .GroupBy(x => new { x.PeriodStart, x.PeriodEnd })
            .Any(group => group.All(x =>
                x.Status == Domain.Models.Prophecies.ProphecyStatus.Completed ||
                x.Status == Domain.Models.Prophecies.ProphecyStatus.Claimed));
    }

    public Task<bool> IsGuildMemberAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.GuildMembers.AnyAsync(x => x.CharacterId == characterId, cancellationToken);

    public Task<int> GetCompletedGuildOrderCountAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.PersonalGuildOrders.CountAsync(order =>
            context.Characters.Any(character => character.Id == order.CharacterId && character.UserId == accountId) &&
            (order.Status == Domain.Models.Guilds.Missions.PersonalGuildOrderStatus.Completed ||
             order.Status == Domain.Models.Guilds.Missions.PersonalGuildOrderStatus.RewardClaimed),
            cancellationToken);

    public Task<int> GetCompletedGuildMissionCountAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.GuildMissionInstances.CountAsync(instance =>
            instance.Status == Domain.Models.Guilds.Missions.GuildMissionStatus.Completed &&
            instance.Contributions.Any(contribution => contribution.CharacterId == characterId),
            cancellationToken);

    public Task<long> GetGuildSuppliesGeneratedAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.GuildMemberContributionPeriods
            .Where(period =>
                period.PeriodType == Domain.Models.Guilds.Missions.GuildMissionPeriodType.Weekly &&
                context.Characters.Any(character => character.Id == period.CharacterId && character.UserId == accountId))
            .SumAsync(period => period.GuildSuppliesGenerated, cancellationToken);

    public Task<int> GetMarketplaceSaleCountAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.MarketPlaceOrders.CountAsync(order =>
            context.Characters.Any(character => character.Id == order.SellerId && character.UserId == accountId),
            cancellationToken);

    public Task<int> GetSoulstoneUpgradeRankCountAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.CharacterSoulstoneUpgrades
            .Where(upgrade => context.Characters.Any(character => character.Id == upgrade.CharacterId && character.UserId == accountId))
            .SumAsync(upgrade => upgrade.Level, cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> GetSoulstoneUpgradeRanksAsync(Guid characterId, CancellationToken cancellationToken) =>
        await context.CharacterSoulstoneUpgrades
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .GroupBy(x => x.SoulstoneUpgradeDefinitionId)
            .ToDictionaryAsync(x => x.Key, x => x.Max(upgrade => upgrade.Level), cancellationToken);

    public Task<int> GetMaxDungeonMasteryLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.CharacterDungeonMasteries
            .Where(x => x.CharacterId == characterId)
            .Select(x => x.Level)
            .DefaultIfEmpty()
            .MaxAsync(cancellationToken);

    public Task<int> GetMaxCraftingMasteryLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.CharacterRecipeMasteries
            .Where(x => x.CharacterId == characterId)
            .Select(x => x.Level)
            .DefaultIfEmpty()
            .MaxAsync(cancellationToken);

    public async Task<(int Completed, int Won)> GetTournamentSummaryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var placements = await context.TournamentParticipants
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.FinalPlacement.HasValue)
            .Select(x => x.FinalPlacement!.Value)
            .ToListAsync(cancellationToken);
        return (placements.Count, placements.Count(x => x == 1));
    }

    public Task<int> GetChampionMarketPurchaseCountAsync(Guid characterId, CancellationToken cancellationToken) =>
        context.ChampionMarketPurchases.CountAsync(x => x.CharacterId == characterId, cancellationToken);

    private async Task<IReadOnlyList<PlayerAchievementProgress>> GetAccountProgressesAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var persisted = await context.PlayerAchievementProgresses
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return persisted
            .Concat(context.PlayerAchievementProgresses.Local.Where(x => x.AccountId == accountId))
            .DistinctBy(x => x.Id)
            .ToList();
    }
}
