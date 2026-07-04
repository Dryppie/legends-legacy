using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.WebSockets;
using Application.UseCases.Achievements.Dtos;
using Application.UseCases.Colosseum.EventHandlers;
using Application.UseCases.Colosseum.Events;
using Application.WebSockets.Contracts;
using Domain.Models.Achievements;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments;

namespace EssenceSystem.Tests;

public sealed class ArenaBattleCompletedEventHandlerTests
{
    [Fact]
    public async Task Handle_records_achievement_and_publishes_completion_to_both_characters()
    {
        var characterId = Guid.NewGuid();
        var enemyId = Guid.NewGuid();
        var publisher = new RecordingGameEventPublisher();
        var achievements = new RecordingAchievementService();
        var handler = new ArenaBattleCompletedEventHandler(publisher, achievements);

        await handler.Handle(
            new ArenaBattleCompletedEvent(
                characterId,
                enemyId,
                BattleOutcome.Victory,
                CharacterRatingBefore: 1000,
                CharacterRatingAfter: 1024,
                EnemyRatingBefore: 980,
                EnemyRatingAfter: 956),
            CancellationToken.None);

        var achievementCall = Assert.Single(achievements.ColosseumBattleCalls);
        Assert.Equal(characterId, achievementCall.CharacterId);
        Assert.Equal(enemyId, achievementCall.OpponentCharacterId);
        Assert.Equal(BattleOutcome.Victory, achievementCall.Outcome);
        Assert.Equal(1000, achievementCall.CharacterRatingBefore);
        Assert.Equal(980, achievementCall.OpponentRatingBefore);

        Assert.Collection(
            publisher.Published,
            published =>
            {
                var audience = Assert.IsType<Audience.Character>(published.Audience);
                Assert.Equal(characterId, audience.CharacterId);
                AssertArenaMessage(published.Message, characterId, enemyId);
            },
            published =>
            {
                var audience = Assert.IsType<Audience.Character>(published.Audience);
                Assert.Equal(enemyId, audience.CharacterId);
                AssertArenaMessage(published.Message, characterId, enemyId);
            });
    }

    private static void AssertArenaMessage(
        GameEventMsg message,
        Guid characterId,
        Guid enemyId)
    {
        var arenaMessage = Assert.IsType<ArenaBattleCompletedMsg>(message);
        Assert.Equal(characterId, arenaMessage.CharacterId);
        Assert.Equal(enemyId, arenaMessage.EnemyId);
        Assert.Equal("Victory", arenaMessage.Outcome);
        Assert.Equal(1000, arenaMessage.CharacterRatingBefore);
        Assert.Equal(1024, arenaMessage.CharacterRatingAfter);
        Assert.Equal(980, arenaMessage.EnemyRatingBefore);
        Assert.Equal(956, arenaMessage.EnemyRatingAfter);
    }

    private sealed class RecordingGameEventPublisher : IGameEventPublisher
    {
        public List<(Audience Audience, GameEventMsg Message)> Published { get; } = [];

        public Task PublishAsync(Audience audience, GameEventMsg message)
        {
            Published.Add((audience, message));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAchievementService : IAchievementService
    {
        public List<ColosseumBattleCall> ColosseumBattleCalls { get; } = [];

        public Task RecordColosseumBattleAsync(
            Guid characterId,
            Guid opponentCharacterId,
            BattleOutcome outcome,
            int characterRatingBefore,
            int opponentRatingBefore,
            CancellationToken cancellationToken)
        {
            ColosseumBattleCalls.Add(new ColosseumBattleCall(
                characterId,
                opponentCharacterId,
                outcome,
                characterRatingBefore,
                opponentRatingBefore));

            return Task.CompletedTask;
        }

        public Task<AchievementOverviewDto> GetOverviewAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new AchievementOverviewDto());

        public Task<IReadOnlyList<AchievementDto>> GetAchievementsAsync(
            Guid accountId,
            Guid characterId,
            AchievementFilters filters,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AchievementDto>>([]);

        public Task<IReadOnlyList<TitleDto>> GetTitlesAsync(
            Guid accountId,
            Guid characterId,
            TitleFilters filters,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TitleDto>>([]);

        public Task<EquippedTitleDto?> EquipTitleAsync(
            Guid accountId,
            Guid characterId,
            string titleKey,
            TitleDisplayPosition displayPosition,
            CancellationToken cancellationToken) =>
            Task.FromResult<EquippedTitleDto?>(null);

        public Task UnequipTitleAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AchievementUnlockDto>> AddProgressAsync(
            Guid accountId,
            Guid? characterId,
            AchievementRequirementType requirementType,
            long amount = 1,
            string? requirementTarget = null,
            bool setToMax = false,
            int? seasonId = null,
            string? metadataJson = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AchievementUnlockDto>>([]);

        public Task RecordDungeonRunStartedAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordDungeonRunCompletedAsync(
            Guid characterId,
            string dungeonDefinitionId,
            bool completedWithoutDefeat,
            bool completedWithoutCheckpointRetreat,
            IReadOnlyCollection<string> defeatedBossKeys,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordIdleCombatAsync(
            Guid characterId,
            int monstersDefeated,
            IReadOnlyCollection<string> defeatedCreatureFamilyKeys,
            int playerDefeats,
            int? lowestWinningHealthPercent,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordEssenceAbsorbedAsync(
            Guid characterId,
            int uniqueEssenceCount,
            IReadOnlyCollection<string> completedCollectionKeys,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordEssenceLoadoutSavedAsync(Guid characterId, int equippedEssenceCount, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordEssenceAscendedAsync(Guid characterId, int ascensionTier, int ascendedToTierCount, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordItemsCraftedAsync(
            Guid characterId,
            IReadOnlyCollection<EquipmentInstance> craftedItems,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordItemsTemperedAsync(
            Guid characterId,
            TemperingSummary summary,
            IReadOnlyCollection<EquipmentInstance> completedItems,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordBlueprintUnlockedAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordCharacterCreatedAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordCharacterLevelReachedAsync(Guid characterId, int level, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<AchievementRecalculationResultDto?> RecalculateProgressAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<AchievementRecalculationResultDto?>(null);
    }

    private sealed record ColosseumBattleCall(
        Guid CharacterId,
        Guid OpponentCharacterId,
        BattleOutcome Outcome,
        int CharacterRatingBefore,
        int OpponentRatingBefore);
}
