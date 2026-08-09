using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.WebSockets;
using Application.UseCases.Achievements.Dtos;
using Application.WebSockets.Contracts;
using Domain.Models.Achievements;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using System.Globalization;
using System.Text.Json;
using Services.LL.Providers;

namespace Services.LL.Achievements;

public sealed class AchievementService : IAchievementService
{
    private static readonly (int Rank, string Name, int RequiredPoints)[] RenownThresholds =
    [
        (0, "Unknown", 0),
        (1, "Noticed", 100),
        (2, "Recognized", 250),
        (3, "Respected", 500),
        (4, "Celebrated", 1000),
        (5, "Renowned", 2000),
        (6, "Exalted", 3500),
        (7, "Famed", 5000),
        (8, "Illustrious", 7500),
        (9, "Mythic", 10000),
        (10, "Living Legend", 15000)
    ];

    private readonly IAchievementRepository _repository;
    private readonly IGameEventPublisher? _eventPublisher;
    private readonly IAchievementSystemChatPublisher? _systemChatPublisher;
    private readonly SoulstoneUpgradeDefinitionProvider? _soulstoneUpgrades;

    public AchievementService(
        IAchievementRepository repository,
        IGameEventPublisher? eventPublisher = null,
        IAchievementSystemChatPublisher? systemChatPublisher = null,
        SoulstoneUpgradeDefinitionProvider? soulstoneUpgrades = null)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _systemChatPublisher = systemChatPublisher;
        _soulstoneUpgrades = soulstoneUpgrades;
    }

    public async Task<AchievementOverviewDto> GetOverviewAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken)
    {
        var achievements = await GetAchievementsAsync(accountId, characterId, new AchievementFilters(), cancellationToken);
        var totalPoints = await GetTotalAchievementPointsAsync(accountId, cancellationToken);
        var renown = CalculateLegacyRenown(totalPoints);
        var totalTitles = await _repository.CountTitleUnlocksAsync(accountId, cancellationToken);

        var available = achievements.Where(x => x.Visibility != AchievementVisibility.Hidden || x.IsCompleted).ToList();

        return new AchievementOverviewDto
        {
            TotalAchievementPoints = totalPoints,
            LegacyRenownRank = renown.Rank,
            LegacyRenownName = renown.Name,
            TotalAchievementsUnlocked = achievements.Count(x => x.IsCompleted),
            TotalAchievementsAvailable = available.Count,
            TotalTitlesUnlocked = totalTitles,
            RecentlyUnlockedAchievements = achievements
                .Where(x => x.IsCompleted)
                .OrderByDescending(x => x.CompletedAt)
                .Take(5)
                .ToList(),
            NearlyCompletedAchievements = achievements
                .Where(x => !x.IsCompleted && x.RequiredAmount > 0 && x.CurrentAmount > 0)
                .OrderByDescending(x => decimal.Divide(x.CurrentAmount, x.RequiredAmount))
                .Take(5)
                .ToList(),
            CategorySummaries = available
                .GroupBy(x => x.Category)
                .Select(group => new AchievementCategorySummaryDto
                {
                    Category = group.Key,
                    Unlocked = group.Count(x => x.IsCompleted),
                    Available = group.Count(),
                    CurrentProgress = group.Sum(x => Math.Min(x.CurrentAmount, x.RequiredAmount)),
                    RequiredProgress = group.Sum(x => x.RequiredAmount)
                })
                .OrderBy(x => x.Category)
                .ToList()
        };
    }

    public async Task<IReadOnlyList<AchievementDto>> GetAchievementsAsync(
        Guid accountId,
        Guid characterId,
        AchievementFilters filters,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetActiveDefinitionsAsync(cancellationToken);
        var progress = await _repository.GetProgressesAsync(accountId, characterId, cancellationToken);
        var titlesByAchievement = (await _repository.GetActiveTitlesAsync(cancellationToken))
            .Where(x => x.SourceAchievementKey != null)
            .ToDictionary(x => x.SourceAchievementKey!, StringComparer.OrdinalIgnoreCase);

        var progressByDefinition = progress
            .GroupBy(x => x.AchievementDefinitionId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.IsCompleted).ThenByDescending(p => p.CurrentAmount).First());

        var mapped = definitions
            .Select(definition =>
            {
                progressByDefinition.TryGetValue(definition.Id, out var current);
                titlesByAchievement.TryGetValue(definition.Key, out var title);
                return MapAchievement(definition, current, title);
            })
            .Where(x => filters.Category is null || x.Category == filters.Category)
            .Where(x => filters.Visibility is null || x.Visibility == filters.Visibility)
            .Where(x => filters.Completed is null || x.IsCompleted == filters.Completed)
            .Where(x => MatchesSearch(x, filters.Search))
            .ToList();

        return mapped;
    }

    public async Task<IReadOnlyList<TitleDto>> GetTitlesAsync(
        Guid accountId,
        Guid characterId,
        TitleFilters filters,
        CancellationToken cancellationToken)
    {
        var character = await _repository.GetCharacterAsync(accountId, characterId, cancellationToken);

        var characterName = character?.Name ?? "Character";
        var titles = await _repository.GetActiveTitlesAsync(cancellationToken);
        var requirementAmountsByAchievement = (await _repository.GetActiveDefinitionsAsync(cancellationToken))
            .ToDictionary(x => x.Key, x => x.RequirementAmount, StringComparer.OrdinalIgnoreCase);
        var unlocks = await _repository.GetTitleUnlocksAsync(accountId, characterId, cancellationToken);

        var unlockByTitle = unlocks
            .GroupBy(x => x.TitleDefinitionId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(u => u.UnlockedAt).First());

        return titles
            .Select(title =>
            {
                unlockByTitle.TryGetValue(title.Id, out var unlock);
                var isEquipped = character?.EquippedTitleDefinitionId == title.Id;
                var displayPosition = isEquipped
                    ? character!.EquippedTitleDisplayPosition
                    : TitleDisplayPosition.Prefix;
                requirementAmountsByAchievement.TryGetValue(title.SourceAchievementKey ?? string.Empty, out var requirementAmount);
                return MapTitle(title, unlock, isEquipped, characterName, displayPosition, requirementAmount);
            })
            .Where(x => filters.Category is null || x.Category == filters.Category)
            .Where(x => filters.Rarity is null || x.Rarity == filters.Rarity)
            .Where(x => filters.Unlocked is null || x.IsUnlocked == filters.Unlocked)
            .Where(x => MatchesSearch(x, filters.Search))
            .ToList();
    }

    public async Task<EquippedTitleDto?> EquipTitleAsync(
        Guid accountId,
        Guid characterId,
        string titleKey,
        TitleDisplayPosition displayPosition,
        CancellationToken cancellationToken)
    {
        titleKey = titleKey.Trim();
        var title = await _repository.GetActiveTitleByKeyAsync(titleKey, cancellationToken);
        if (title is null)
        {
            return null;
        }

        var character = await _repository.GetCharacterAsync(accountId, characterId, cancellationToken);
        if (character is null)
        {
            return null;
        }

        var unlocks = await _repository.GetTitleUnlocksAsync(accountId, characterId, cancellationToken);
        var isUnlocked = unlocks.Any(x => x.TitleDefinitionId == title.Id);
        if (!isUnlocked)
        {
            return null;
        }

        character.EquippedTitleDefinitionId = title.Id;
        character.EquippedTitleDisplayPosition = displayPosition;
        return MapEquippedTitle(title, character.Name, displayPosition);
    }

    public async Task UnequipTitleAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _repository.GetCharacterAsync(accountId, characterId, cancellationToken);
        if (character is not null)
        {
            character.EquippedTitleDefinitionId = null;
            character.EquippedTitleDisplayPosition = TitleDisplayPosition.Prefix;
        }
    }

    public async Task<IReadOnlyList<AchievementUnlockDto>> AddProgressAsync(
        Guid accountId,
        Guid? characterId,
        AchievementRequirementType requirementType,
        long amount = 1,
        string? requirementTarget = null,
        bool setToMax = false,
        int? seasonId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var unlocks = await AddProgressCoreAsync(
            accountId,
            characterId,
            requirementType,
            amount,
            requirementTarget,
            setToMax,
            seasonId,
            metadataJson,
            syncLegacyProgress: true,
            cancellationToken);

        await PublishUnlockAnnouncementsAsync(characterId, unlocks, cancellationToken);
        return unlocks;
    }

    private async Task<IReadOnlyList<AchievementUnlockDto>> AddProgressCoreAsync(
        Guid accountId,
        Guid? characterId,
        AchievementRequirementType requirementType,
        long amount = 1,
        string? requirementTarget = null,
        bool setToMax = false,
        int? seasonId = null,
        string? metadataJson = null,
        bool syncLegacyProgress = true,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return [];
        }

        var definitions = (await _repository.GetActiveDefinitionsAsync(requirementType, cancellationToken)).ToList();

        definitions = definitions
            .Where(x => TargetMatches(x.RequirementTarget, requirementTarget))
            .Where(x => x.Scope != AchievementScope.Character || characterId.HasValue)
            .ToList();

        if (definitions.Count == 0)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        var unlocks = new List<AchievementUnlockDto>();
        foreach (var definition in definitions)
        {
            var scopedCharacterId = definition.Scope == AchievementScope.Character ? (Guid?)characterId : null;
            var scopedSeasonId = definition.Scope == AchievementScope.Seasonal ? seasonId : null;
            var progress = await GetOrCreateProgressAsync(
                accountId,
                scopedCharacterId,
                definition,
                scopedSeasonId,
                now,
                cancellationToken);

            if (progress.IsCompleted && !definition.IsRepeatable)
            {
                continue;
            }

            progress.CurrentAmount = setToMax
                ? Math.Max(progress.CurrentAmount, amount)
                : progress.CurrentAmount + amount;
            progress.MetadataJson = metadataJson ?? progress.MetadataJson;
            progress.UpdatedAt = now;

            if (progress.CurrentAmount < definition.RequirementAmount)
            {
                continue;
            }

            var unlock = await CompleteAchievementAsync(progress, definition, characterId, now, cancellationToken);
            if (unlock is not null)
            {
                unlocks.Add(unlock);
            }
        }

        if (syncLegacyProgress && unlocks.Count > 0)
        {
            unlocks.AddRange(await SyncDependentAchievementProgressAsync(accountId, characterId, cancellationToken));
        }

        return unlocks;
    }

    public async Task RecordColosseumBattleAsync(
        Guid characterId,
        Guid opponentCharacterId,
        BattleOutcome outcome,
        int characterRatingBefore,
        int opponentRatingBefore,
        CancellationToken cancellationToken)
    {
        var accountIds = await _repository.GetAccountIdsForCharactersAsync(
            [characterId, opponentCharacterId],
            cancellationToken);

        if (!accountIds.TryGetValue(characterId, out var actorAccountId) ||
            !accountIds.TryGetValue(opponentCharacterId, out var opponentAccountId) ||
            actorAccountId == opponentAccountId)
        {
            return;
        }

        await AddProgressAsync(actorAccountId, characterId, AchievementRequirementType.ColosseumBattlesCompleted, cancellationToken: cancellationToken);
        if (outcome == BattleOutcome.Victory)
        {
            var losingStreak = await GetProgressAmountAsync(
                actorAccountId,
                characterId,
                AchievementRequirementType.WinColosseumAfterLosingStreak,
                null,
                null,
                cancellationToken);

            if (losingStreak > 0)
            {
                await AddProgressAsync(
                    actorAccountId,
                    characterId,
                    AchievementRequirementType.WinColosseumAfterLosingStreak,
                    losingStreak,
                    setToMax: true,
                    cancellationToken: cancellationToken);

                await SetProgressAmountAsync(
                    actorAccountId,
                    characterId,
                    AchievementRequirementType.WinColosseumAfterLosingStreak,
                    0,
                    null,
                    null,
                    cancellationToken);
            }

            await AddProgressAsync(actorAccountId, characterId, AchievementRequirementType.ColosseumBattlesWon, cancellationToken: cancellationToken);
            await AddProgressAsync(actorAccountId, characterId, AchievementRequirementType.ColosseumWinStreak, cancellationToken: cancellationToken);

            var ratingDifference = opponentRatingBefore - characterRatingBefore;
            if (ratingDifference > 0)
            {
                await AddProgressAsync(
                    actorAccountId,
                    characterId,
                    AchievementRequirementType.DefeatColosseumOpponentRatingAbove,
                    ratingDifference,
                    setToMax: true,
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await SetProgressAmountAsync(
                actorAccountId,
                characterId,
                AchievementRequirementType.ColosseumWinStreak,
                0,
                null,
                null,
                cancellationToken);

            if (outcome == BattleOutcome.Defeat)
            {
                var currentLosingStreak = await GetProgressAmountAsync(
                    actorAccountId,
                    characterId,
                    AchievementRequirementType.WinColosseumAfterLosingStreak,
                    null,
                    null,
                    cancellationToken: cancellationToken);

                await SetProgressAmountAsync(
                    actorAccountId,
                    characterId,
                    AchievementRequirementType.WinColosseumAfterLosingStreak,
                    currentLosingStreak + 1,
                    null,
                    null,
                    cancellationToken);
            }
        }
    }

    public async Task RecordDungeonRunStartedAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var accountId = await _repository.GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.DungeonRunsStarted,
            cancellationToken: cancellationToken);
    }

    public async Task RecordDungeonRunCompletedAsync(
        Guid characterId,
        string dungeonDefinitionId,
        bool completedWithoutDefeat,
        bool completedWithoutRetreat,
        bool completedWithoutWeapon,
        IReadOnlyCollection<string> defeatedBossKeys,
        CancellationToken cancellationToken)
    {
        var accountId = await _repository.GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(accountId, characterId, AchievementRequirementType.DungeonRunsCompleted, cancellationToken: cancellationToken);
        await AddProgressAsync(accountId, characterId, AchievementRequirementType.SpecificDungeonCompleted, requirementTarget: dungeonDefinitionId, cancellationToken: cancellationToken);
        if (completedWithoutDefeat)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.DungeonCompletedWithoutDefeat, cancellationToken: cancellationToken);
        }

        if (completedWithoutRetreat)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.DungeonCompletedWithoutRetreat, cancellationToken: cancellationToken);
        }

        if (completedWithoutWeapon)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.DungeonCompletedWithoutWeapon, cancellationToken: cancellationToken);
        }

        foreach (var bossKey in defeatedBossKeys)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.SpecificDungeonBossDefeated, requirementTarget: bossKey, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordIdleCombatAsync(
        Guid characterId,
        int monstersDefeated,
        IReadOnlyCollection<string> defeatedCreatureFamilyKeys,
        int playerDefeats,
        int? lowestWinningHealthPercent,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        if (monstersDefeated > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.MonstersDefeated,
                monstersDefeated,
                cancellationToken: cancellationToken);
        }

        foreach (var family in defeatedCreatureFamilyKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.CreatureFamilyDefeated,
                family.Count(),
                family.Key,
                cancellationToken: cancellationToken);
        }

        if (playerDefeats > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.PlayerDefeats,
                playerDefeats,
                cancellationToken: cancellationToken);
        }

        if (lowestWinningHealthPercent.HasValue)
        {
            var unlocks = await CompleteCombatWinBelowHealthThresholdsAsync(
                accountId,
                characterId,
                lowestWinningHealthPercent.Value,
                cancellationToken);
            await PublishUnlockAnnouncementsAsync(characterId, unlocks, cancellationToken);
        }
    }

    public async Task RecordEssenceAbsorbedAsync(
        Guid characterId,
        int uniqueEssenceCount,
        IReadOnlyCollection<string> completedCollectionKeys,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.EssencesAbsorbed,
            cancellationToken: cancellationToken);

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.UniqueEssencesArchived,
            uniqueEssenceCount,
            setToMax: true,
            cancellationToken: cancellationToken);

        foreach (var collectionKey in completedCollectionKeys.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.EssenceCollectionCompleted,
                1,
                collectionKey,
                setToMax: true,
                cancellationToken: cancellationToken);
        }
    }

    public async Task RecordEssenceLoadoutSavedAsync(Guid characterId, int equippedEssenceCount, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.EquippedEssenceCountReached,
            equippedEssenceCount,
            setToMax: true,
            cancellationToken: cancellationToken);
    }

    public async Task RecordEssenceAscendedAsync(Guid characterId, int ascensionTier, int ascendedToTierCount, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.EssencesAscended,
            cancellationToken: cancellationToken);

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.EssencesAscendedToTier,
            ascendedToTierCount,
            ascensionTier.ToString(),
            setToMax: true,
            cancellationToken: cancellationToken);
    }

    public async Task RecordItemsCraftedAsync(
        Guid characterId,
        IReadOnlyCollection<EquipmentInstance> craftedItems,
        int? craftingMasteryLevel,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty || craftedItems.Count == 0)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.ItemsCrafted,
            craftedItems.Count,
            cancellationToken: cancellationToken);

        var setItemCount = craftedItems.Count(IsSetItem);
        if (setItemCount > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.SetItemsCrafted,
                setItemCount,
                cancellationToken: cancellationToken);
        }

        await RecordUniqueItemVariantsAsync(accountId, characterId, craftedItems, cancellationToken);

        if (craftingMasteryLevel is > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.CraftingMasteryLevelReached,
                craftingMasteryLevel.Value,
                setToMax: true,
                cancellationToken: cancellationToken);
        }
    }

    public async Task RecordItemsTemperedAsync(
        Guid characterId,
        TemperingSummary summary,
        IReadOnlyCollection<EquipmentInstance> completedItems,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        if (summary.TotalActions > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.ItemsTempered,
                summary.TotalActions,
                cancellationToken: cancellationToken);
        }

        if (summary.Masterpieces > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.MasterpiecesCrafted,
                summary.Masterpieces,
                cancellationToken: cancellationToken);
        }

        if (summary.CursedOutcomes > 0)
        {
            await AddProgressAsync(
                accountId,
                characterId,
                AchievementRequirementType.CursedCraftingOutcomes,
                summary.CursedOutcomes,
                cancellationToken: cancellationToken);
        }

        var highQualityUnlocks = await CompleteHighQualityLowPotentialAchievementsAsync(
            accountId,
            characterId,
            completedItems,
            cancellationToken);
        await PublishUnlockAnnouncementsAsync(characterId, highQualityUnlocks, cancellationToken);
    }

    public async Task RecordBlueprintUnlockedAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.BlueprintsUnlocked,
            cancellationToken: cancellationToken);
    }

    public async Task RecordCharacterCreatedAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.AccountCreatedOrFirstCharacterCreated,
            cancellationToken: cancellationToken);
    }

    public async Task RecordCharacterLevelReachedAsync(Guid characterId, int level, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty || level <= 0)
        {
            return;
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.CharacterLevelReached,
            level,
            setToMax: true,
            cancellationToken: cancellationToken);
    }

    public async Task RecordProphecyCompletedAsync(Guid characterId, bool completedWeeklyCycle, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(accountId, characterId, AchievementRequirementType.PropheciesCompleted, cancellationToken: cancellationToken);
        if (completedWeeklyCycle)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.WeeklyProphecyCycleCompleted, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordGuildJoinedAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId != Guid.Empty)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.GuildJoined, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordGuildProgressAsync(
        Guid characterId,
        int ordersCompleted,
        bool missionCompleted,
        long suppliesGenerated,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        if (ordersCompleted > 0)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.GuildOrdersCompleted, ordersCompleted, cancellationToken: cancellationToken);
        }

        if (missionCompleted)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.GuildMissionsCompleted, cancellationToken: cancellationToken);
        }

        if (suppliesGenerated > 0)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.GuildSuppliesGenerated, suppliesGenerated, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordMarketplaceSaleAsync(Guid characterId, CancellationToken cancellationToken) =>
        await RecordSingleProgressAsync(characterId, AchievementRequirementType.MarketplaceSalesCompleted, cancellationToken);

    public async Task RecordSoulstoneUpgradePurchasedAsync(Guid characterId, bool allUpgradesMaxed, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(accountId, characterId, AchievementRequirementType.SoulstoneUpgradesPurchased, cancellationToken: cancellationToken);
        if (allUpgradesMaxed)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.AllSoulstoneUpgradesMaxed, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordDungeonMasteryLevelReachedAsync(Guid characterId, int level, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId != Guid.Empty && level > 0)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.DungeonMasteryLevelReached, level, setToMax: true, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordColosseumTournamentAsync(Guid characterId, bool won, CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId == Guid.Empty)
        {
            return;
        }

        await AddProgressAsync(accountId, characterId, AchievementRequirementType.ColosseumTournamentsCompleted, cancellationToken: cancellationToken);
        if (won)
        {
            await AddProgressAsync(accountId, characterId, AchievementRequirementType.ColosseumTournamentsWon, cancellationToken: cancellationToken);
        }
    }

    public async Task RecordChampionMarketPurchaseAsync(Guid characterId, CancellationToken cancellationToken) =>
        await RecordSingleProgressAsync(characterId, AchievementRequirementType.ChampionMarketPurchases, cancellationToken);

    public async Task<AchievementRecalculationResultDto?> RecalculateProgressAsync(
        Guid accountId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var character = await _repository.GetCharacterAsync(accountId, characterId, cancellationToken);
        if (character is null)
        {
            return null;
        }

        var completedBefore = await CountCompletedAchievementsAsync(accountId, cancellationToken);
        var unlocks = new List<AchievementUnlockDto>();

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.AccountCreatedOrFirstCharacterCreated,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.CharacterLevelReached,
            character.Level,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        await RecalculateEssenceProgressAsync(accountId, characterId, unlocks, cancellationToken);
        await RecalculateCraftingProgressAsync(accountId, characterId, unlocks, cancellationToken);
        await RecalculateDungeonProgressAsync(accountId, characterId, unlocks, cancellationToken);
        await RecalculateColosseumProgressAsync(accountId, characterId, unlocks, cancellationToken);
        await RecalculateAdditionalProgressAsync(accountId, characterId, unlocks, cancellationToken);

        unlocks.AddRange(await SyncDependentAchievementProgressAsync(accountId, characterId, cancellationToken));

        await PublishUnlockAnnouncementsAsync(characterId, unlocks, cancellationToken);
        var completedAfter = completedBefore + unlocks.Select(x => x.AchievementKey).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        return new AchievementRecalculationResultDto
        {
            AccountId = accountId,
            CharacterId = characterId,
            CompletedBefore = completedBefore,
            CompletedAfter = Math.Max(completedBefore, completedAfter),
            Unlocks = unlocks
        };
    }

    public static (int Rank, string Name) CalculateLegacyRenown(int achievementPoints)
    {
        var threshold = RenownThresholds
            .Where(x => achievementPoints >= x.RequiredPoints)
            .OrderByDescending(x => x.RequiredPoints)
            .First();

        return (threshold.Rank, threshold.Name);
    }

    private async Task<Guid> GetAccountIdForCharacterAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _repository.GetAccountIdForCharacterAsync(characterId, cancellationToken);

    private async Task RecordSingleProgressAsync(
        Guid characterId,
        AchievementRequirementType requirementType,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdForCharacterAsync(characterId, cancellationToken);
        if (accountId != Guid.Empty)
        {
            await AddProgressAsync(accountId, characterId, requirementType, cancellationToken: cancellationToken);
        }
    }

    private async Task<int> CountCompletedAchievementsAsync(Guid accountId, CancellationToken cancellationToken) =>
        await _repository.CountCompletedAchievementsAsync(accountId, cancellationToken);

    private async Task RecalculateEssenceProgressAsync(
        Guid accountId,
        Guid characterId,
        List<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        var essences = await _repository.GetPlayerEssencesAsync(characterId, cancellationToken);

        var uniqueEssenceCount = essences
            .Select(x => x.EssenceDefinitionId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.UniqueEssencesArchived,
            uniqueEssenceCount,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        var ascendedCount = essences.Count(x => x.AscensionTier > 0);
        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.EssencesAscended,
            ascendedCount,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        foreach (var tierGroup in essences
            .Where(x => x.AscensionTier > 0)
            .GroupBy(x => x.AscensionTier))
        {
            unlocks.AddRange(await AddProgressCoreAsync(
                accountId,
                characterId,
                AchievementRequirementType.EssencesAscendedToTier,
                tierGroup.Count(),
                tierGroup.Key.ToString(),
                setToMax: true,
                syncLegacyProgress: false,
                cancellationToken: cancellationToken));
        }

        var equippedEssenceCount = await _repository.GetEquippedEssenceCountAsync(characterId, cancellationToken);

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.EquippedEssenceCountReached,
            equippedEssenceCount,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));
    }

    private async Task RecalculateCraftingProgressAsync(
        Guid accountId,
        Guid characterId,
        List<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        var blueprintUnlocks = await _repository.GetBlueprintUnlockCountAsync(characterId, cancellationToken);

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.BlueprintsUnlocked,
            blueprintUnlocks,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        var equipment = await _repository.GetOwnedEquipmentAsync(characterId, cancellationToken);

        var craftedItems = equipment
            .Where(x => !string.IsNullOrWhiteSpace(x.BaseRecipeId))
            .ToList();

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.ItemsCrafted,
            craftedItems.Count,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.MasterpiecesCrafted,
            craftedItems.Count(x => x.IsMasterpiece),
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.SetItemsCrafted,
            craftedItems.Count(IsSetItem),
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        var uniqueVariantCount = craftedItems
            .Select(x => $"{x.BaseRecipeId}\u001f{x.BlueprintId}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.UniqueItemVariantsCrafted,
            uniqueVariantCount,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.CraftingMasteryLevelReached,
            await _repository.GetMaxCraftingMasteryLevelAsync(characterId, cancellationToken),
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        unlocks.AddRange(await CompleteHighQualityLowPotentialAchievementsAsync(
            accountId,
            characterId,
            craftedItems,
            cancellationToken));
    }

    private async Task RecordUniqueItemVariantsAsync(
        Guid accountId,
        Guid characterId,
        IReadOnlyCollection<EquipmentInstance> craftedItems,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetActiveDefinitionsAsync(
            AchievementRequirementType.UniqueItemVariantsCrafted,
            cancellationToken);
        var definition = definitions.FirstOrDefault();
        if (definition is null)
        {
            return;
        }

        var progress = await _repository.GetProgressAsync(
            accountId,
            definition.Scope == AchievementScope.Character ? characterId : null,
            definition.Id,
            null,
            cancellationToken);
        var variants = new HashSet<string>(
            string.IsNullOrWhiteSpace(progress?.MetadataJson)
                ? []
                : JsonSerializer.Deserialize<HashSet<string>>(progress.MetadataJson) ?? [],
            StringComparer.OrdinalIgnoreCase);

        // Preserve progress created before variant identities were recorded.
        for (var index = variants.Count; index < (progress?.CurrentAmount ?? 0); index++)
        {
            variants.Add($"legacy:{index}");
        }

        foreach (var item in craftedItems.Where(x => !string.IsNullOrWhiteSpace(x.BaseRecipeId)))
        {
            variants.Add($"{item.BaseRecipeId}\u001f{item.BlueprintId}");
        }

        await AddProgressAsync(
            accountId,
            characterId,
            AchievementRequirementType.UniqueItemVariantsCrafted,
            variants.Count,
            setToMax: true,
            metadataJson: JsonSerializer.Serialize(variants),
            cancellationToken: cancellationToken);
    }

    private async Task RecalculateDungeonProgressAsync(
        Guid accountId,
        Guid characterId,
        List<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        var completions = await _repository.GetDungeonCompletionsAsync(characterId, cancellationToken);

        var totalCompletions = completions.Sum(x => x.CompletionCount);
        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.DungeonRunsCompleted,
            totalCompletions,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        foreach (var completion in completions)
        {
            unlocks.AddRange(await AddProgressCoreAsync(
                accountId,
                characterId,
                AchievementRequirementType.SpecificDungeonCompleted,
                completion.CompletionCount,
                completion.DungeonDefinitionId,
                setToMax: true,
                syncLegacyProgress: false,
                cancellationToken: cancellationToken));
        }
    }

    private async Task RecalculateColosseumProgressAsync(
        Guid accountId,
        Guid characterId,
        List<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        var matches = await _repository.GetColosseumMatchesAsync(characterId, cancellationToken);

        if (matches.Count == 0)
        {
            return;
        }

        var opponentIds = matches
            .Select(match => match.CharacterAId == characterId ? match.CharacterBId : match.CharacterAId)
            .Distinct()
            .ToList();
        var opponentAccountIds = await _repository.GetAccountIdsForCharactersAsync(opponentIds, cancellationToken);

        var validMatches = matches
            .Where(match =>
            {
                var opponentId = match.CharacterAId == characterId ? match.CharacterBId : match.CharacterAId;
                return opponentAccountIds.TryGetValue(opponentId, out var opponentAccountId) &&
                    opponentAccountId != accountId;
            })
            .ToList();

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.ColosseumBattlesCompleted,
            validMatches.Count,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        var wins = validMatches.Where(x => x.WinnerId == characterId).ToList();
        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.ColosseumBattlesWon,
            wins.Count,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        var bestWinStreak = 0;
        var currentWinStreak = 0;
        var bestRatingUpset = 0;
        foreach (var match in validMatches)
        {
            if (match.WinnerId == characterId)
            {
                currentWinStreak++;
                bestWinStreak = Math.Max(bestWinStreak, currentWinStreak);
                bestRatingUpset = Math.Max(bestRatingUpset, GetRatingUpset(match, characterId));
            }
            else
            {
                currentWinStreak = 0;
            }
        }

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.ColosseumWinStreak,
            bestWinStreak,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));

        unlocks.AddRange(await AddProgressCoreAsync(
            accountId,
            characterId,
            AchievementRequirementType.DefeatColosseumOpponentRatingAbove,
            bestRatingUpset,
            setToMax: true,
            syncLegacyProgress: false,
            cancellationToken: cancellationToken));
    }

    private async Task RecalculateAdditionalProgressAsync(
        Guid accountId,
        Guid characterId,
        List<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.PropheciesCompleted,
            await _repository.GetCompletedProphecyCountAsync(accountId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        if (await _repository.HasCompletedWeeklyProphecyCycleAsync(accountId, cancellationToken))
        {
            unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.WeeklyProphecyCycleCompleted,
                setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        }

        if (await _repository.IsGuildMemberAsync(characterId, cancellationToken))
        {
            unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.GuildJoined,
                setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        }
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.GuildOrdersCompleted,
            await _repository.GetCompletedGuildOrderCountAsync(accountId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.GuildMissionsCompleted,
            await _repository.GetCompletedGuildMissionCountAsync(characterId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.GuildSuppliesGenerated,
            await _repository.GetGuildSuppliesGeneratedAsync(accountId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));

        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.MarketplaceSalesCompleted,
            await _repository.GetMarketplaceSaleCountAsync(accountId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.SoulstoneUpgradesPurchased,
            await _repository.GetSoulstoneUpgradeRankCountAsync(accountId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));

        if (_soulstoneUpgrades is not null)
        {
            var ranks = await _repository.GetSoulstoneUpgradeRanksAsync(characterId, cancellationToken);
            var enabled = _soulstoneUpgrades.All.Values.Where(x => x.Enabled).ToList();
            if (enabled.Count > 0 && enabled.All(x => ranks.GetValueOrDefault(x.Id) >= x.MaxRank))
            {
                unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.AllSoulstoneUpgradesMaxed,
                    setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
            }
        }

        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.DungeonMasteryLevelReached,
            await _repository.GetMaxDungeonMasteryLevelAsync(characterId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));

        var tournaments = await _repository.GetTournamentSummaryAsync(characterId, cancellationToken);
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.ColosseumTournamentsCompleted,
            tournaments.Completed, setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.ColosseumTournamentsWon,
            tournaments.Won, setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
        unlocks.AddRange(await AddProgressCoreAsync(accountId, characterId, AchievementRequirementType.ChampionMarketPurchases,
            await _repository.GetChampionMarketPurchaseCountAsync(characterId, cancellationToken), setToMax: true, syncLegacyProgress: false, cancellationToken: cancellationToken));
    }

    private async Task<long> GetProgressAmountAsync(
        Guid accountId,
        Guid? characterId,
        AchievementRequirementType requirementType,
        string? requirementTarget,
        int? seasonId,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetActiveDefinitionsAsync(requirementType, cancellationToken);

        var definition = definitions.FirstOrDefault(x => TargetMatches(x.RequirementTarget, requirementTarget));
        if (definition is null)
        {
            return 0;
        }

        var scopedCharacterId = definition.Scope == AchievementScope.Character ? characterId : null;
        var scopedSeasonId = definition.Scope == AchievementScope.Seasonal ? seasonId : null;
        var progress = await _repository.GetProgressAsync(
            accountId,
            scopedCharacterId,
            definition.Id,
            scopedSeasonId,
            cancellationToken);
        return progress?.CurrentAmount ?? 0;
    }

    private async Task SetProgressAmountAsync(
        Guid accountId,
        Guid? characterId,
        AchievementRequirementType requirementType,
        long amount,
        string? requirementTarget,
        int? seasonId,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetActiveDefinitionsAsync(requirementType, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var definition in definitions.Where(x => TargetMatches(x.RequirementTarget, requirementTarget)))
        {
            var scopedCharacterId = definition.Scope == AchievementScope.Character ? (Guid?)characterId : null;
            var scopedSeasonId = definition.Scope == AchievementScope.Seasonal ? seasonId : null;
            var progress = await GetOrCreateProgressAsync(
                accountId,
                scopedCharacterId,
                definition,
                scopedSeasonId,
                now,
                cancellationToken);

            if (progress.IsCompleted && !definition.IsRepeatable)
            {
                continue;
            }

            progress.CurrentAmount = Math.Max(0, amount);
            progress.UpdatedAt = now;
        }
    }

    private async Task<IReadOnlyList<AchievementUnlockDto>> CompleteCombatWinBelowHealthThresholdsAsync(
        Guid accountId,
        Guid characterId,
        int lowestWinningHealthPercent,
        CancellationToken cancellationToken)
    {
        if (lowestWinningHealthPercent <= 0)
        {
            return [];
        }

        var definitions = await _repository.GetActiveDefinitionsAsync(
            AchievementRequirementType.WinCombatBelowHealthPercent,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var unlocks = new List<AchievementUnlockDto>();
        foreach (var definition in definitions.Where(x => lowestWinningHealthPercent <= x.RequirementAmount))
        {
            var scopedCharacterId = definition.Scope == AchievementScope.Character ? (Guid?)characterId : null;
            var progress = await GetOrCreateProgressAsync(accountId, scopedCharacterId, definition, null, now, cancellationToken);
            if (progress.IsCompleted && !definition.IsRepeatable)
            {
                continue;
            }

            progress.CurrentAmount = definition.RequirementAmount;
            progress.UpdatedAt = now;
            var unlock = await CompleteAchievementAsync(progress, definition, characterId, now, cancellationToken);
            if (unlock is not null)
            {
                unlocks.Add(unlock);
            }
        }

        if (unlocks.Count > 0)
        {
            unlocks.AddRange(await SyncDependentAchievementProgressAsync(accountId, characterId, cancellationToken));
        }

        return unlocks;
    }

    private async Task<PlayerAchievementProgress> GetOrCreateProgressAsync(
        Guid accountId,
        Guid? characterId,
        AchievementDefinition definition,
        int? seasonId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var progress = await _repository.GetProgressAsync(
            accountId,
            characterId,
            definition.Id,
            seasonId,
            cancellationToken);

        if (progress is not null)
        {
            return progress;
        }

        progress = new PlayerAchievementProgress
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CharacterId = characterId,
            AchievementDefinitionId = definition.Id,
            SeasonId = seasonId,
            RequiredAmount = definition.RequirementAmount,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddProgressAsync(progress, cancellationToken);
        return progress;
    }

    private async Task<AchievementUnlockDto?> CompleteAchievementAsync(
        PlayerAchievementProgress progress,
        AchievementDefinition definition,
        Guid? completedByCharacterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (progress.IsCompleted && !definition.IsRepeatable)
        {
            return null;
        }

        progress.IsCompleted = true;
        progress.CompletedAt = now;
        progress.CompletedByCharacterId = completedByCharacterId;
        progress.CurrentAmount = Math.Max(progress.CurrentAmount, definition.RequirementAmount);
        progress.UpdatedAt = now;

        var titleUnlock = await UnlockTitleRewardAsync(progress, definition, completedByCharacterId, now, cancellationToken);
        var characterName = completedByCharacterId.HasValue
            ? await _repository.GetCharacterNameAsync(completedByCharacterId.Value, cancellationToken)
            : null;
        var titleName = titleUnlock?.TitleDefinition.Name;

        return new AchievementUnlockDto
        {
            AchievementKey = definition.Key,
            AchievementName = definition.Name,
            Points = definition.Points,
            TitleKey = titleUnlock?.TitleDefinition.Key,
            TitleName = titleName,
            ShouldAnnounce = !string.IsNullOrWhiteSpace(definition.GlobalSystemMessageTemplate),
            PlayerSystemMessage = FormatAchievementSystemMessage(
                definition.PlayerSystemMessageTemplate,
                definition,
                titleName,
                characterName,
                isGlobal: false),
            GlobalSystemMessage = FormatAchievementSystemMessage(
                definition.GlobalSystemMessageTemplate,
                definition,
                titleName,
                characterName,
                isGlobal: true)
        };
    }

    private async Task<PlayerTitleUnlock?> UnlockTitleRewardAsync(
        PlayerAchievementProgress progress,
        AchievementDefinition definition,
        Guid? completedByCharacterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var title = await _repository.GetActiveTitleBySourceAchievementKeyAsync(definition.Key, cancellationToken);
        if (title is null)
        {
            return null;
        }

        var characterId = title.Scope == TitleScope.Character ? completedByCharacterId : null;
        var exists = await _repository.HasTitleUnlockAsync(
            progress.AccountId,
            characterId,
            title.Id,
            progress.SeasonId,
            cancellationToken);
        if (exists)
        {
            return null;
        }

        var unlock = new PlayerTitleUnlock
        {
            Id = Guid.NewGuid(),
            AccountId = progress.AccountId,
            CharacterId = characterId,
            TitleDefinitionId = title.Id,
            TitleDefinition = title,
            UnlockedAt = now,
            UnlockedByAchievementDefinitionId = definition.Id,
            SeasonId = progress.SeasonId
        };
        await _repository.AddTitleUnlockAsync(unlock, cancellationToken);
        return unlock;
    }

    private async Task<IReadOnlyList<AchievementUnlockDto>> SyncDependentAchievementProgressAsync(
        Guid accountId,
        Guid? characterId,
        CancellationToken cancellationToken)
    {
        var unlocks = new List<AchievementUnlockDto>();

        // Meta-achievements can unlock one another, so repeat until the account state stabilizes.
        for (var pass = 0; pass < 8; pass++)
        {
            var passUnlocks = new List<AchievementUnlockDto>();
            passUnlocks.AddRange(await AddProgressCoreAsync(
                accountId,
                characterId,
                AchievementRequirementType.AchievementPointsReached,
                await GetTotalAchievementPointsAsync(accountId, cancellationToken),
                setToMax: true,
                syncLegacyProgress: false,
                cancellationToken: cancellationToken));
            passUnlocks.AddRange(await AddProgressCoreAsync(
                accountId,
                characterId,
                AchievementRequirementType.AchievementsUnlocked,
                await _repository.CountCompletedAchievementsAsync(accountId, cancellationToken),
                setToMax: true,
                syncLegacyProgress: false,
                cancellationToken: cancellationToken));
            passUnlocks.AddRange(await AddProgressCoreAsync(
                accountId,
                characterId,
                AchievementRequirementType.NonHiddenAchievementsCompleted,
                await _repository.CountCompletedNonHiddenAchievementsAsync(accountId, cancellationToken),
                setToMax: true,
                syncLegacyProgress: false,
                cancellationToken: cancellationToken));
            passUnlocks.AddRange(await AddProgressCoreAsync(
                accountId,
                characterId,
                AchievementRequirementType.TitlesUnlocked,
                await _repository.CountTitleUnlocksAsync(accountId, cancellationToken),
                setToMax: true,
                syncLegacyProgress: false,
                cancellationToken: cancellationToken));

            if (passUnlocks.Count == 0)
            {
                break;
            }

            unlocks.AddRange(passUnlocks);
        }

        return unlocks;
    }

    private async Task<IReadOnlyList<AchievementUnlockDto>> CompleteHighQualityLowPotentialAchievementsAsync(
        Guid accountId,
        Guid characterId,
        IReadOnlyCollection<EquipmentInstance> items,
        CancellationToken cancellationToken)
    {
        var bestMatch = items
            .Where(x => x.Quality >= ItemQuality.Exceptional && x.Potential.HasValue)
            .OrderBy(x => x.Potential!.Value)
            .FirstOrDefault();
        if (bestMatch is null)
        {
            return [];
        }

        var definitions = await _repository.GetActiveDefinitionsAsync(
            AchievementRequirementType.HighQualityItemCraftedBelowPotential,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var unlocks = new List<AchievementUnlockDto>();
        foreach (var definition in definitions.Where(x => bestMatch.Potential!.Value < x.RequirementAmount))
        {
            var scopedCharacterId = definition.Scope == AchievementScope.Character ? (Guid?)characterId : null;
            var progress = await GetOrCreateProgressAsync(accountId, scopedCharacterId, definition, null, now, cancellationToken);
            if (progress.IsCompleted && !definition.IsRepeatable)
            {
                continue;
            }

            progress.CurrentAmount = definition.RequirementAmount;
            progress.UpdatedAt = now;
            var unlock = await CompleteAchievementAsync(progress, definition, characterId, now, cancellationToken);
            if (unlock is not null)
            {
                unlocks.Add(unlock);
            }
        }

        if (unlocks.Count > 0)
        {
            unlocks.AddRange(await SyncDependentAchievementProgressAsync(accountId, characterId, cancellationToken));
        }

        return unlocks;
    }

    private async Task<int> GetTotalAchievementPointsAsync(Guid accountId, CancellationToken cancellationToken) =>
        await _repository.GetTotalAchievementPointsAsync(accountId, cancellationToken);

    private async Task PublishUnlockAnnouncementsAsync(
        Guid? characterId,
        IReadOnlyCollection<AchievementUnlockDto> unlocks,
        CancellationToken cancellationToken)
    {
        if (_eventPublisher is null)
        {
            if (_systemChatPublisher is not null)
            {
                await _systemChatPublisher.PublishAsync(characterId, unlocks, cancellationToken);
            }

            return;
        }

        foreach (var unlock in unlocks)
        {
            if (characterId.HasValue && !string.IsNullOrWhiteSpace(unlock.PlayerSystemMessage))
            {
                await _eventPublisher.PublishAsync(
                    new Audience.Character(characterId.Value),
                    new AchievementUnlockedMsg(
                        characterId,
                        unlock.AchievementKey,
                        unlock.AchievementName,
                        unlock.Points,
                        unlock.TitleKey,
                        unlock.TitleName,
                        unlock.PlayerSystemMessage,
                        IsGlobal: false));
            }

            if (!string.IsNullOrWhiteSpace(unlock.GlobalSystemMessage))
            {
                await _eventPublisher.PublishAsync(
                    new Audience.World(),
                    new AchievementUnlockedMsg(
                        characterId,
                        unlock.AchievementKey,
                        unlock.AchievementName,
                        unlock.Points,
                        unlock.TitleKey,
                        unlock.TitleName,
                        unlock.GlobalSystemMessage,
                        IsGlobal: true));
            }
        }

        if (_systemChatPublisher is not null)
        {
            await _systemChatPublisher.PublishAsync(characterId, unlocks, cancellationToken);
        }
    }

    private static AchievementDto MapAchievement(
        AchievementDefinition definition,
        PlayerAchievementProgress? progress,
        TitleDefinition? rewardTitle)
    {
        var completed = progress?.IsCompleted == true;
        if (!completed && definition.Visibility == AchievementVisibility.Hidden)
        {
            return new AchievementDto
            {
                Key = definition.Key,
                Name = "Hidden Achievement",
                Description = "This achievement has not been discovered yet.",
                Category = definition.Category,
                Type = definition.Type,
                Scope = definition.Scope,
                Visibility = definition.Visibility,
                Rarity = definition.Rarity,
                RequirementType = definition.RequirementType,
                RequiredAmount = definition.RequirementAmount,
                CurrentAmount = 0,
                Points = definition.Points
            };
        }

        var obscured = !completed && definition.Visibility == AchievementVisibility.Obscured;
        return new AchievementDto
        {
            Key = definition.Key,
            Name = definition.Name,
            Description = obscured
                ? definition.Hint ?? "The exact requirement is unknown."
                : FormatDescription(definition.Description, definition.RequirementAmount),
            Hint = definition.Hint,
            Category = definition.Category,
            Type = definition.Type,
            Scope = definition.Scope,
            Visibility = definition.Visibility,
            Rarity = definition.Rarity,
            RequirementType = definition.RequirementType,
            RequirementTarget = obscured ? null : definition.RequirementTarget,
            RequiredAmount = obscured ? 0 : definition.RequirementAmount,
            CurrentAmount = obscured ? 0 : progress?.CurrentAmount ?? 0,
            Points = definition.Points,
            IsCompleted = completed,
            CompletedAt = progress?.CompletedAt,
            CompletedByCharacterId = progress?.CompletedByCharacterId,
            RewardTitleKey = rewardTitle?.Key,
            RewardTitleName = rewardTitle?.Name
        };
    }

    private static TitleDto MapTitle(
        TitleDefinition title,
        PlayerTitleUnlock? unlock,
        bool isEquipped,
        string characterName,
        TitleDisplayPosition displayPosition,
        long requirementAmount)
    {
        var unlocked = unlock is not null;
        var hidden = title.IsHiddenUntilUnlocked && !unlocked;
        var name = hidden ? "Hidden Title" : title.Name;

        return new TitleDto
        {
            Key = title.Key,
            Name = name,
            Description = hidden
                ? "Unlock this title to reveal its source."
                : FormatDescription(title.Description, requirementAmount),
            Category = title.Category,
            Rarity = title.Rarity,
            DisplayPosition = displayPosition,
            Scope = title.Scope,
            IsUnlocked = unlocked,
            IsEquipped = isEquipped,
            SourceAchievementKey = hidden ? null : title.SourceAchievementKey,
            UnlockedByCharacterId = unlock?.CharacterId,
            UnlockedAt = unlock?.UnlockedAt,
            Preview = TitleDisplayFormatter.Format(characterName, name, displayPosition),
            PrefixPreview = TitleDisplayFormatter.Format(characterName, name, TitleDisplayPosition.Prefix),
            SuffixPreview = TitleDisplayFormatter.Format(characterName, name, TitleDisplayPosition.Suffix)
        };
    }

    private static EquippedTitleDto MapEquippedTitle(
        TitleDefinition title,
        string characterName,
        TitleDisplayPosition displayPosition) => new()
    {
        Key = title.Key,
        Name = title.Name,
        DisplayPosition = displayPosition,
        DisplayName = TitleDisplayFormatter.Format(characterName, title.Name, displayPosition)
    };

    private static string FormatDescription(string description, long requirementAmount) =>
        description.Replace(
            "{number}",
            requirementAmount.ToString("N0", CultureInfo.InvariantCulture),
            StringComparison.Ordinal);

    private static bool MatchesSearch(AchievementDto achievement, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return achievement.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            achievement.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            achievement.Key.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(TitleDto title, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return title.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            title.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            title.Key.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TargetMatches(string? definitionTarget, string? eventTarget)
    {
        if (string.IsNullOrWhiteSpace(definitionTarget))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(eventTarget))
        {
            return false;
        }

        return eventTarget.Equals(definitionTarget, StringComparison.OrdinalIgnoreCase) ||
            eventTarget.StartsWith(definitionTarget + "_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSetItem(EquipmentInstance item) =>
        item.AffinityTags.Any(IsSetTag) ||
        item.BlueprintId?.Contains("set", StringComparison.OrdinalIgnoreCase) == true ||
        item.BaseRecipeId?.Contains("set", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsSetTag(string value) =>
        value.Equals("set", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("set:", StringComparison.OrdinalIgnoreCase) ||
        value.EndsWith("_set", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("_set_", StringComparison.OrdinalIgnoreCase);

    private static int GetRatingUpset(Domain.Models.Colosseum.ColosseumMatchResult match, Guid characterId)
    {
        var characterRating = match.CharacterAId == characterId
            ? match.CharacterARatingBefore
            : match.CharacterBRatingBefore;
        var opponentRating = match.CharacterAId == characterId
            ? match.CharacterBRatingBefore
            : match.CharacterARatingBefore;

        return Math.Max(0, opponentRating - characterRating);
    }

    private static string FormatAchievementSystemMessage(
        string? template,
        AchievementDefinition definition,
        string? titleName,
        string? characterName,
        bool isGlobal)
    {
        var message = string.IsNullOrWhiteSpace(template)
            ? isGlobal
                ? null
                : "Achievement unlocked: {achievementName} (+{points} points)."
            : template;
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return message
            .Replace("{achievementKey}", definition.Key, StringComparison.OrdinalIgnoreCase)
            .Replace("{achievementName}", definition.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{points}", definition.Points.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{titleName}", titleName ?? "no title", StringComparison.OrdinalIgnoreCase)
            .Replace("{characterName}", characterName ?? "A hero", StringComparison.OrdinalIgnoreCase);
    }

}
