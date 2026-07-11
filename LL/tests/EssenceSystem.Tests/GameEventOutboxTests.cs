using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Tutorials;
using Application.UseCases.Achievements.Dtos;
using Application.UseCases.Outbox;
using Application.UseCases.Prophecies.Events;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Achievements;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Domain.Models.Outbox;
using Domain.Models.Regions.Areas;
using Domain.Models.Tutorials;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;
using Services.LL.Outbox;
using Services.LL.Tutorials;

namespace EssenceSystem.Tests;

public sealed class GameEventOutboxTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EnqueueAsync_creates_message_and_delivery_per_registered_consumer_without_saving()
    {
        await using var db = CreateDb();
        var outbox = new GameEventOutbox(
            db,
            new GameEventOutboxConsumerRegistry(),
            CreateJsonOptions(),
            new FixedTimeProvider(Now));

        var characterId = Guid.NewGuid();

        await outbox.EnqueueAsync(
            GameEventTypes.EssenceAbsorbed,
            new EssenceAbsorbedPayload(characterId, "essence.goblin", 1, []),
            characterId,
            accountId: null,
            CancellationToken.None);

        Assert.True(db.HasChanges);
        Assert.Empty(await db.GameEventOutboxMessages.ToListAsync());

        await db.SaveChangesAsync(CancellationToken.None);

        var message = Assert.Single(await db.GameEventOutboxMessages.ToListAsync());
        Assert.Equal(GameEventTypes.EssenceAbsorbed, message.EventType);
        Assert.Equal(characterId, message.CharacterId);
        Assert.Equal(Now, message.CreatedAt);
        Assert.Equal(Now, message.AvailableAt);

        var deliveries = await db.GameEventOutboxDeliveries
            .OrderBy(x => x.Consumer)
            .ToListAsync();

        Assert.Collection(
            deliveries,
            delivery =>
            {
                Assert.Equal(GameEventOutboxConsumerNames.Achievements, delivery.Consumer);
                Assert.Equal(GameEventOutboxDeliveryStatus.Pending, delivery.Status);
                Assert.Equal(message.Id, delivery.MessageId);
            },
            delivery =>
            {
                Assert.Equal(GameEventOutboxConsumerNames.Tutorial, delivery.Consumer);
                Assert.Equal(GameEventOutboxDeliveryStatus.Pending, delivery.Status);
                Assert.Equal(message.Id, delivery.MessageId);
            });
    }

    [Fact]
    public async Task Achievement_consumer_records_ledger_and_skips_duplicate_message()
    {
        await using var db = CreateDb();
        var achievements = new RecordingAchievementService();
        var consumer = new AchievementGameEventOutboxConsumer(
            db,
            achievements,
            CreateJsonOptions(),
            new FixedTimeProvider(Now));

        var characterId = Guid.NewGuid();
        var message = new GameEventOutboxMessage
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EventType = GameEventTypes.CharacterCreated,
            PayloadJson = JsonSerializer.Serialize(
                new CharacterCreatedPayload(characterId),
                CreateJsonOptions()),
            CreatedAt = Now,
            AvailableAt = Now
        };

        await consumer.HandleAsync(message, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        Assert.Equal([characterId], achievements.CharacterCreatedCalls);
        var ledger = Assert.Single(await db.AchievementEventLedgers.ToListAsync());
        Assert.Equal(message.Id, ledger.OutboxMessageId);
        Assert.Equal(characterId, ledger.CharacterId);
        Assert.Equal(GameEventTypes.CharacterCreated, ledger.EventType);
        Assert.Equal(Now, ledger.ProcessedAt);

        await consumer.HandleAsync(message, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        Assert.Equal([characterId], achievements.CharacterCreatedCalls);
        Assert.Single(await db.AchievementEventLedgers.ToListAsync());
    }

    [Fact]
    public async Task Tutorial_progression_uses_persisted_step_when_cache_is_stale()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();

        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepAbsorbEssence
        });
        await db.SaveChangesAsync(CancellationToken.None);

        var cache = new InMemoryTutorialProgressCache();
        cache.SetActive(
            characterId,
            TutorialConstants.FirstStepsTutorialId,
            TutorialConstants.StepDefeatTrainingCreature);

        var service = new TutorialService(
            db,
            itemBases: null!,
            inventory: null!,
            inventoryItemFactory: null!,
            lootRewardWriter: null!,
            new FirstStepsTutorialDefinitionProvider(),
            cache);

        var result = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.EssenceAbsorbed(TutorialConstants.TutorialEssenceDefinitionId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Progressed);
        Assert.Equal(TutorialConstants.StepEquipEssence, result.State?.CurrentStep);

        var progress = await db.CharacterTutorialProgresses.SingleAsync();
        Assert.Equal(TutorialConstants.StepEquipEssence, progress.CurrentStep);
        Assert.NotNull(progress.EssenceAbsorbedAt);
    }

    [Fact]
    public async Task Idle_combat_processor_enqueues_one_aggregate_outbox_message_for_many_encounters()
    {
        var characterId = Guid.NewGuid();
        var from = Now.AddMinutes(-3);
        var area = new Area { Id = "training-grounds", Name = "Training Grounds" };
        var facts = new IdleCombatRewardFacts(
            characterId,
            from,
            Now,
            Now,
            TimeSpan.FromMinutes(3),
            area,
            [],
            null,
            [
                CreateEncounter(
                    1,
                    BattleOutcome.Victory,
                    [
                        new Creature { Name = "Goblin Scout" },
                        new Creature { Name = "Wolf" }
                    ],
                    [new SimpleCombatEntity { MaxHealth = 100, Health = 25 }]),
                CreateEncounter(
                    2,
                    BattleOutcome.Defeat,
                    [new Creature { Name = "Goblin Brute" }],
                    []),
                CreateEncounter(
                    3,
                    BattleOutcome.Victory,
                    [new Creature { Name = "Goblin Archer" }],
                    [new SimpleCombatEntity { MaxHealth = 100, Health = 12 }])
            ]);

        var outcome = new IdleCombatCalculatedOutcome(
            characterId,
            from,
            Now,
            0,
            0,
            0,
            [],
            [],
            []);
        var outbox = new RecordingGameEventOutbox();
        var publisher = new RecordingPublisher();
        var processor = new IdleCombatOutcomeProcessor(
            new StubIdleCombatRewardFactBuilder(facts),
            new StubIdleCombatRewardCalculator(outcome),
            new StubIdleCombatRewardApplier(),
            new StubIdleCombatSessionFactory(),
            outbox,
            publisher,
            new RecordingCreatureArchiveService());
        var characterAction = new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = from,
            ActionDetails = new CombatActionDetails([], area)
        };
        var details = new IdleCombatOrchestrationDetails(
            from,
            Now,
            Now,
            PlannedEncounterCount: 3,
            EncounterCadence: TimeSpan.FromMinutes(1));

        await processor.ApplyAsync(
            new CombatOutcomeRequest(
                new IdleCombatOrchestrationRequest(characterAction, Now),
                new CombatOrchestrationResult(
                    Guid.NewGuid(),
                    CombatMode.Idle,
                    [],
                    details)),
            CancellationToken.None);

        var message = Assert.Single(outbox.Messages);
        Assert.Equal(GameEventTypes.IdleCombatEncounterCompleted, message.EventType);
        Assert.Equal(characterId, message.CharacterId);

        var payload = Assert.IsType<IdleCombatEncounterCompletedPayload>(message.Payload);
        Assert.Equal(characterId, payload.CharacterId);
        Assert.Equal(area.Id, payload.AreaId);
        Assert.True(payload.WonEncounter);
        Assert.Equal(3, payload.MonstersDefeated);
        Assert.Equal(["Goblin", "Wolf", "Goblin"], payload.DefeatedCreatureFamilyKeys);
        Assert.Equal(1, payload.PlayerDefeats);
        Assert.Equal(12, payload.LowestWinningHealthPercent);

        var prophecyBatch = Assert.Single(publisher.Notifications.OfType<ProphecyProgressBatchNotification>());
        Assert.Equal(6, prophecyBatch.ProgressEvents.Count);
        Assert.Empty(publisher.Notifications.OfType<ProphecyProgressNotification>());
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static JsonSerializerOptions CreateJsonOptions() =>
        new(JsonSerializerDefaults.Web);

    private static IdleEncounterRewardFacts CreateEncounter(
        int sequence,
        BattleOutcome outcome,
        IReadOnlyList<Creature> hostileCreatures,
        IReadOnlyList<SimpleCombatEntity> playerTeam) =>
        new(
            Guid.NewGuid(),
            sequence,
            Now.AddMinutes(sequence),
            outcome,
            [.. hostileCreatures.Select(x => x.Id)],
            hostileCreatures,
            new CombatResult
            {
                Outcome = outcome,
                PlayerTeam = [.. playerTeam]
            });

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<RecordedOutboxMessage> Messages { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Messages.Add(new RecordedOutboxMessage(eventType, payload!, characterId, accountId));
            return Task.CompletedTask;
        }
    }

    private sealed record RecordedOutboxMessage(
        string EventType,
        object Payload,
        Guid? CharacterId,
        Guid? AccountId);

    private sealed class StubIdleCombatRewardFactBuilder(IdleCombatRewardFacts facts) : IIdleCombatRewardFactBuilder
    {
        public Task<IdleCombatRewardFacts> BuildAsync(
            IdleCombatOutcomeContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(facts);
    }

    private sealed class StubIdleCombatRewardCalculator(IdleCombatCalculatedOutcome outcome) : IIdleCombatRewardCalculator
    {
        public Task<IdleCombatCalculatedOutcome> CalculateAsync(
            IdleCombatRewardFacts facts,
            CancellationToken cancellationToken) =>
            Task.FromResult(outcome);
    }

    private sealed class StubIdleCombatRewardApplier : IIdleCombatRewardApplier
    {
        public Task ApplyAsync(
            IdleCombatRewardFacts facts,
            IdleCombatCalculatedOutcome outcome,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class StubIdleCombatSessionFactory : IIdleCombatSessionFactory
    {
        public CombatSession Create(IdleCombatRewardFacts facts, IdleCombatCalculatedOutcome outcome) => new();
    }

    private sealed class RecordingCreatureArchiveService : ICreatureArchiveService
    {
        public List<Creature> RecordedCreatures { get; } = [];

        public Task RecordDefeatedCreaturesAsync(
            Guid characterId,
            IReadOnlyCollection<Creature> creatures,
            DateTimeOffset defeatedAtUtc,
            CancellationToken cancellationToken)
        {
            RecordedCreatures.AddRange(creatures);
            return Task.CompletedTask;
        }

        public Task<CreatureArchive> GetCreatureArchiveAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new CreatureArchive([], true, null, null));

        public Task<EssenceCodex> GetEssenceCodexAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new EssenceCodex([]));

        public Task<CreatureArchive> SetEssenceFocusAsync(Guid characterId, string? creatureId, CancellationToken cancellationToken) =>
            Task.FromResult(new CreatureArchive([], true, null, null));

        public Task<bool> IsEssenceFocusAsync(Guid characterId, string creatureId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<object> Notifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Publish((object)notification, cancellationToken);
    }

    private sealed class FirstStepsTutorialDefinitionProvider : ITutorialDefinitionProvider
    {
        private readonly TutorialDefinition _definition = new()
        {
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            Version = 1,
            Title = "First Steps",
            InitialStepKey = TutorialConstants.StepDefeatTrainingCreature,
            Steps =
            [
                new()
                {
                    Key = TutorialConstants.StepDefeatTrainingCreature,
                    Objective = "Defeat the creature in the Training Area.",
                    RequiredAmount = 1,
                    ActionLabel = "Go to Training Area",
                    DestinationRoute = "/game/world",
                    NextStepKey = TutorialConstants.StepAbsorbEssence,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "IdleCombatCompleted",
                        AreaId = TutorialConstants.TrainingGroundsAreaId,
                        RequiresVictory = true
                    }
                },
                new()
                {
                    Key = TutorialConstants.StepAbsorbEssence,
                    Objective = "Absorb the Unbound Goblin's Essence into your Soul Archive.",
                    RequiredAmount = 1,
                    ActionLabel = "Open Essences",
                    DestinationRoute = "/game/character/essences",
                    NextStepKey = TutorialConstants.StepEquipEssence,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EssenceAbsorbed",
                        EssenceDefinitionId = TutorialConstants.TutorialEssenceDefinitionId
                    }
                },
                new()
                {
                    Key = TutorialConstants.StepEquipEssence,
                    Objective = "Attune the Goblin's Essence in your active essence loadout.",
                    RequiredAmount = 1,
                    ActionLabel = "Open Essences",
                    DestinationRoute = "/game/character/essences",
                    NextStepKey = TutorialConstants.StepCraftEquipment,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EssenceLoadoutChanged",
                        EssenceDefinitionId = TutorialConstants.TutorialEssenceDefinitionId
                    }
                }
            ]
        };

        public TutorialDefinition Get(string tutorialId) => _definition;

        public TutorialStepDefinition? GetStep(string tutorialId, string stepKey) =>
            _definition.Steps.FirstOrDefault(step =>
                step.Key.Equals(stepKey, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingAchievementService : IAchievementService
    {
        public List<Guid> CharacterCreatedCalls { get; } = [];

        public Task RecordCharacterCreatedAsync(Guid characterId, CancellationToken cancellationToken)
        {
            CharacterCreatedCalls.Add(characterId);
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

        public Task RecordColosseumBattleAsync(
            Guid characterId,
            Guid opponentCharacterId,
            BattleOutcome outcome,
            int characterRatingBefore,
            int opponentRatingBefore,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

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

        public Task RecordItemsCraftedAsync(Guid characterId, IReadOnlyCollection<EquipmentInstance> craftedItems, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordItemsTemperedAsync(
            Guid characterId,
            TemperingSummary summary,
            IReadOnlyCollection<EquipmentInstance> completedItems,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordBlueprintUnlockedAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RecordCharacterLevelReachedAsync(Guid characterId, int level, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<AchievementRecalculationResultDto?> RecalculateProgressAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<AchievementRecalculationResultDto?>(null);
    }
}
