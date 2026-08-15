using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
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
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Outbox;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Regions.Areas;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;
using Services.LL.Outbox;

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
                Assert.Equal(GameEventOutboxConsumerNames.EventQuests, delivery.Consumer);
                Assert.Equal(GameEventOutboxDeliveryStatus.Pending, delivery.Status);
                Assert.Equal(message.Id, delivery.MessageId);
            },
            delivery =>
            {
                Assert.Equal(GameEventOutboxConsumerNames.Quests, delivery.Consumer);
                Assert.Equal(GameEventOutboxDeliveryStatus.Pending, delivery.Status);
                Assert.Equal(message.Id, delivery.MessageId);
            });
    }

    [Fact]
    public void Consumer_registry_routes_new_activity_events_to_quests()
    {
        var registry = new GameEventOutboxConsumerRegistry();

        Assert.Contains(GameEventOutboxConsumerNames.Quests, registry.GetConsumers(GameEventTypes.EssenceFocusSet));
        Assert.Contains(GameEventOutboxConsumerNames.EventQuests, registry.GetConsumers(GameEventTypes.EssenceFocusSet));
        Assert.Contains(GameEventOutboxConsumerNames.Quests, registry.GetConsumers(GameEventTypes.FocusedCreatureEssenceReceived));
        Assert.Contains(GameEventOutboxConsumerNames.EventQuests, registry.GetConsumers(GameEventTypes.FocusedCreatureEssenceReceived));
        Assert.Contains(
            GameEventOutboxConsumerNames.Quests,
            registry.GetConsumers(GameEventTypes.ColosseumBattleCompleted));
        Assert.Contains(GameEventOutboxConsumerNames.Quests, registry.GetConsumers(GameEventTypes.ProphecyCompleted));
        Assert.Contains(GameEventOutboxConsumerNames.EventQuests, registry.GetConsumers(GameEventTypes.ProphecyCompleted));
        Assert.Contains(
            GameEventOutboxConsumerNames.Quests,
            registry.GetConsumers(GameEventTypes.EssenceAscended));
        Assert.Contains(
            GameEventOutboxConsumerNames.Quests,
            registry.GetConsumers(GameEventTypes.EquipmentTempered));
        Assert.Contains(
            GameEventOutboxConsumerNames.Quests,
            registry.GetConsumers(GameEventTypes.DungeonRunStarted));
        Assert.Contains(
            GameEventOutboxConsumerNames.Quests,
            registry.GetConsumers(GameEventTypes.DungeonRunCompleted));
        Assert.Contains(GameEventOutboxConsumerNames.Quests, registry.GetConsumers(GameEventTypes.TournamentBattleCompleted));
        Assert.Contains(GameEventOutboxConsumerNames.EventQuests, registry.GetConsumers(GameEventTypes.TournamentBattleCompleted));
        Assert.Equal(
            [GameEventOutboxConsumerNames.RealtimeInventory],
            registry.GetConsumers(GameEventTypes.InventoryItemsGranted));
        Assert.Equal(
            [GameEventOutboxConsumerNames.RealtimeTournamentGrounds],
            registry.GetConsumers(GameEventTypes.TournamentGroundsUpdated));
        Assert.Equal(
            [GameEventOutboxConsumerNames.RealtimeWorldTower],
            registry.GetConsumers(GameEventTypes.WorldTowerRallyUpdated));
    }

    [Fact]
    public void Consumer_registry_routes_transfer_chat_messages_to_durable_chat_delivery()
    {
        var registry = new GameEventOutboxConsumerRegistry();

        var consumer = Assert.Single(
            registry.GetConsumers(GameEventTypes.PlayerTransferChatMessage));

        Assert.Equal(GameEventOutboxConsumerNames.TransferChat, consumer);
    }

    [Fact]
    public void Consumer_registry_routes_tournament_announcements_to_durable_chat_delivery()
    {
        var registry = new GameEventOutboxConsumerRegistry();

        var consumer = Assert.Single(
            registry.GetConsumers(GameEventTypes.TournamentChatAnnouncement));

        Assert.Equal(GameEventOutboxConsumerNames.TournamentChat, consumer);
    }

    [Fact]
    public async Task Event_quest_consumer_maps_combat_and_daily_prophecy_events()
    {
        var characterId = Guid.NewGuid();
        var progression = new RecordingEventQuestProgressionService();
        var consumer = new EventQuestGameEventOutboxConsumer(progression, CreateJsonOptions());

        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.IdleCombatEncounterCompleted,
                new IdleCombatEncounterCompletedPayload(
                    characterId,
                    "region_01_area_01",
                    true,
                    1,
                    [],
                    0,
                    100,
                    3,
                    "Mining",
                    2)),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.ProphecyCompleted,
                new ProphecyCompletedPayload(characterId, Guid.NewGuid(), "Daily")),
            CancellationToken.None);

        Assert.Equal(["CombatEncounterCompleted", "DailyProphecyCompleted"], progression.Triggers.Select(x => x.Type));
        Assert.Equal(3, progression.Triggers[0].ActionCount);
        Assert.Equal("Mining", progression.Triggers[0].EquippedGatheringType);
        Assert.Equal(2, progression.Triggers[0].WinningEncounterCount);
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
    public async Task Quest_consumer_maps_focused_drop_colosseum_and_daily_prophecy_events()
    {
        var characterId = Guid.NewGuid();
        var progression = new RecordingQuestProgressionService();
        var consumer = new QuestGameEventOutboxConsumer(progression, CreateJsonOptions());

        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.EssenceFocusSet,
                new EssenceFocusSetPayload(characterId, "monster.goblin")),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.FocusedCreatureEssenceReceived,
                new FocusedCreatureEssenceReceivedPayload(
                    characterId,
                    "monster.goblin",
                    "essence.goblin")),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.ColosseumBattleCompleted,
                new ColosseumBattleCompletedPayload(
                    characterId,
                    Guid.NewGuid(),
                    BattleOutcome.Defeat,
                    1000,
                    1000)),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.ProphecyCompleted,
                new ProphecyCompletedPayload(characterId, Guid.NewGuid(), "Daily")),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.ProphecyCompleted,
                new ProphecyCompletedPayload(characterId, Guid.NewGuid(), "Weekly")),
            CancellationToken.None);

        Assert.Equal(
            [
                "EssenceFocusSet",
                "FocusedCreatureEssenceReceived",
                "ColosseumBattleStarted",
                "DailyProphecyCompleted"
            ],
            progression.Triggers.Select(trigger => trigger.Type));
        var focusedDrop = progression.Triggers[1];
        Assert.Equal("monster.goblin", focusedDrop.CreatureDefinitionId);
        Assert.Equal("essence.goblin", focusedDrop.EssenceDefinitionId);
    }

    [Fact]
    public async Task Quest_consumer_preserves_the_crafted_base_recipe_id()
    {
        var characterId = Guid.NewGuid();
        var progression = new RecordingQuestProgressionService();
        var consumer = new QuestGameEventOutboxConsumer(progression, CreateJsonOptions());
        var payload = new EquipmentCraftedPayload(
            characterId,
            [
                new OutboxEquipmentItemPayload(
                    "hatchet",
                    1,
                    Rarity.Common,
                    ItemQuality.Standard,
                    10,
                    "recipe.weapon.one_handed.hand_axe",
                    null,
                    [],
                    false)
            ],
            1);

        await consumer.HandleAsync(
            CreateOutboxMessage(characterId, GameEventTypes.EquipmentCrafted, payload),
            CancellationToken.None);

        var trigger = Assert.Single(progression.Triggers);
        Assert.Equal(
            ["recipe.weapon.one_handed.hand_axe"],
            trigger.CraftedBaseRecipeIds);
        Assert.Equal([ItemQuality.Standard], trigger.CraftedItemQualities);
        Assert.Equal([10], trigger.CraftedItemPotentials);
    }

    [Fact]
    public async Task Quest_consumer_maps_new_quest_activity_events()
    {
        var characterId = Guid.NewGuid();
        var progression = new RecordingQuestProgressionService();
        var consumer = new QuestGameEventOutboxConsumer(progression, CreateJsonOptions());
        var temperedItem = new OutboxEquipmentItemPayload(
            "shortsword",
            2,
            Rarity.Common,
            ItemQuality.Fine,
            0,
            "recipe.weapon.one_handed.shortsword",
            null,
            [],
            false);

        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.EssenceLoadoutChanged,
                new EssenceLoadoutChangedPayload(characterId, [], 3, true)),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.EssenceAscended,
                new EssenceAscendedPayload(characterId, 1, 1)),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.EquipmentTempered,
                new EquipmentTemperedPayload(characterId, new TemperingSummary(), [temperedItem])),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.DungeonRunStarted,
                new DungeonRunStartedPayload(characterId)),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.DungeonRunCompleted,
                new DungeonRunCompletedPayload(characterId, "goblin_mines", false, false, false, [])),
            CancellationToken.None);
        await consumer.HandleAsync(
            CreateOutboxMessage(
                characterId,
                GameEventTypes.TournamentBattleCompleted,
                new TournamentBattleCompletedPayload(characterId, Guid.NewGuid(), Guid.NewGuid())),
            CancellationToken.None);

        Assert.Equal(
            [
                "EssenceLoadoutChanged",
                "EssenceAscended",
                "EquipmentTempered",
                "DungeonRunStarted",
                "DungeonRunCompleted",
                "TournamentBattleCompleted"
            ],
            progression.Triggers.Select(trigger => trigger.Type));
        Assert.True(progression.Triggers[0].HasCompatibleEssenceTrio);
        Assert.Equal([0], progression.Triggers[2].CraftedItemPotentials);
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
            new EquippedGatheringTool { GatheringType = GatheringType.Mining },
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
            [],
            [],
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
        Assert.Equal(3, payload.ActionCount);
        Assert.Equal("Mining", payload.EquippedGatheringType);
        Assert.Equal(2, payload.WinningEncounterCount);

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

    private static GameEventOutboxMessage CreateOutboxMessage<TPayload>(
        Guid characterId,
        string eventType,
        TPayload payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, CreateJsonOptions()),
            CreatedAt = Now,
            AvailableAt = Now
        };

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

    private sealed class RecordingQuestProgressionService : IQuestProgressionService
    {
        public List<QuestTrigger> Triggers { get; } = [];

        public Task<QuestProgressionResult> ProcessAsync(
            Guid characterId,
            QuestTrigger trigger,
            Guid? outboxMessageId,
            string eventType,
            CancellationToken cancellationToken)
        {
            Triggers.Add(trigger);
            return Task.FromResult(new QuestProgressionResult(
                new QuestJournal([], null),
                [],
                []));
        }
    }

    private sealed class RecordingEventQuestProgressionService : IEventQuestProgressionService
    {
        public List<QuestTrigger> Triggers { get; } = [];

        public Task ProcessAsync(
            Guid characterId,
            QuestTrigger trigger,
            Guid outboxMessageId,
            string eventType,
            CancellationToken cancellationToken)
        {
            Triggers.Add(trigger);
            return Task.CompletedTask;
        }
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

        public Task<string?> GetEssenceFocusCreatureIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

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
            bool completedWithoutRetreat,
            bool completedWithoutWeapon,
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

        public Task RecordItemsCraftedAsync(Guid characterId, IReadOnlyCollection<EquipmentInstance> craftedItems, int? craftingMasteryLevel, CancellationToken cancellationToken) =>
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

        public Task RecordProphecyCompletedAsync(Guid characterId, bool completedWeeklyCycle, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordGuildJoinedAsync(Guid characterId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordGuildProgressAsync(Guid characterId, int ordersCompleted, bool missionCompleted, long suppliesGenerated, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordMarketplaceSaleAsync(Guid characterId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordSoulstoneUpgradePurchasedAsync(Guid characterId, bool allUpgradesMaxed, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordDungeonMasteryLevelReachedAsync(Guid characterId, int level, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordColosseumTournamentAsync(Guid characterId, bool won, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RecordChampionMarketPurchaseAsync(Guid characterId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<AchievementRecalculationResultDto?> RecalculateProgressAsync(Guid accountId, Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<AchievementRecalculationResultDto?>(null);
    }
}
