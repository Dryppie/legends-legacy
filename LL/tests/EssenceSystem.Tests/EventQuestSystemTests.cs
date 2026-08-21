using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Inventories;
using Domain.Models.Quests;
using Domain.Models.Quests.Events;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Persistence.LL.Repositories.Quests;
using Services.LL.Quests.Events;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace EssenceSystem.Tests;

public sealed class EventQuestSystemTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Event_quest_catalog_loads_and_validates_enabled_content()
    {
        var apiRoot = FindApiRoot();
        var provider = new JsonEventQuestDefinitionProvider(
            new ConfigurationBuilder().Build(),
            apiRoot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(2, provider.GetAll().Count);
        var definition = provider.Get("event.lumo_defense.example");
        Assert.Equal("event.lumo_defense.example", definition.Id);
        Assert.True(definition.Enabled);
        Assert.Equal("CombatEncounterCompleted", Assert.Single(definition.Objectives).Type);
        var communityReward = Assert.Single(definition.Rewards);
        Assert.Equal("Item", communityReward.Type);
        Assert.Equal(1, communityReward.Quantity);
        Assert.Equal("item.catalyst_selection_crate", communityReward.ItemBaseId);
        Assert.Equal(3, definition.PersonalMilestones.Count);
        Assert.Equal(250, definition.PersonalMilestones[0].RequiredContribution);
        Assert.Collection(
            definition.PersonalMilestones,
            milestone => Assert.Equal(
                ("soul_dust", 5),
                (Assert.Single(milestone.Rewards).ItemBaseId, Assert.Single(milestone.Rewards).Quantity)),
            milestone => Assert.Equal(
                ("item.monster_core.lesser", 6),
                (Assert.Single(milestone.Rewards).ItemBaseId, Assert.Single(milestone.Rewards).Quantity)),
            milestone => Assert.Equal(
                ["item.blueprint_selection_box"],
                milestone.Rewards.Select(reward => reward.ItemBaseId)));

        var tempering = provider.Get("event.a_broken_curse.2026_08");
        Assert.Equal(new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero), tempering.StartsAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero), tempering.EndsAtUtc);
        Assert.Equal("TemperingActionCompleted", Assert.Single(tempering.Objectives).Type);
        Assert.Equal(100_000, tempering.Objectives[0].RequiredAmount);
        Assert.Equal(50, Assert.Single(tempering.Rewards).Quantity);
        Assert.Equal("SigilFragments", tempering.Rewards[0].Type);
        Assert.Equal([500L, 1500L, 3000L], tempering.PersonalMilestones.Select(x => x.RequiredContribution));
        Assert.Equal(
            [("ore", 200), ("wood", 200), ("rawhide", 200)],
            tempering.PersonalMilestones[0].Rewards.Select(x => (x.ItemBaseId, x.Quantity)));
        Assert.Equal(
            "item.blueprint_selection_box",
            Assert.Single(tempering.PersonalMilestones[1].Rewards).ItemBaseId);
        Assert.Equal(
            "item.catalyst_selection_crate",
            Assert.Single(tempering.PersonalMilestones[2].Rewards).ItemBaseId);
    }

    [Fact]
    public async Task Tempering_action_batch_advances_global_and_personal_progress_by_action_count()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 100_000);
        definition.Objectives =
        [
            new QuestObjectiveDefinition
            {
                Key = "tempering-actions",
                Description = "Complete tempering actions.",
                Type = "TemperingActionCompleted",
                RequiredAmount = 100_000
            }
        ];
        var service = CreateService(db, definition, new RecordingPublisher());
        var characterId = Guid.NewGuid();
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.EquipmentTempered([], [], [], [], [], actionCount: 500),
            Guid.NewGuid(),
            "EquipmentTempered",
            CancellationToken.None);

        var state = Assert.Single((await service.GetJournalAsync(characterId, CancellationToken.None)).Events);
        Assert.Equal(500, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal(500, state.MyContribution);
        Assert.True(state.PersonalMilestones[0].IsUnlocked);
    }

    [Fact]
    public async Task Qualifying_outbox_event_advances_global_and_personal_progress_once()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 2);
        var publisher = new RecordingPublisher();
        var service = CreateService(db, definition, publisher);
        var characterId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "RealmTester",
            NormalizedName = "REALMTESTER"
        });
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            outboxId,
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            outboxId,
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        var journal = await service.GetJournalAsync(characterId, CancellationToken.None);
        var state = Assert.Single(journal.Events);
        Assert.Equal(1, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal(1, state.MyContribution);
        Assert.Equal(1, state.MyContributionRank);
        Assert.Equal(1, state.ContributorCount);
        Assert.Equal("RealmTester", Assert.Single(state.TopContributors).CharacterName);
        Assert.Single(await db.EventQuestEventLedgers.ToListAsync());
        Assert.Single(publisher.Messages.OfType<EventQuestChanged>());
    }

    [Fact]
    public async Task Starting_a_server_wide_event_announces_it_in_world_chat_once()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 5);
        var outbox = new RecordingGameEventOutbox();
        var service = CreateService(db, definition, new RecordingPublisher(), outbox: outbox);
        var characterId = Guid.NewGuid();
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.GetJournalAsync(characterId, CancellationToken.None);
        await service.GetJournalAsync(characterId, CancellationToken.None);

        var announcement = Assert.Single(outbox.ChatAnnouncements);
        Assert.Equal(definition.Id, announcement.EventQuestId);
        Assert.Contains(definition.Title, announcement.Body);
        Assert.Contains("underway", announcement.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/game/quests", announcement.TargetUrl);
        Assert.NotEqual(Guid.Empty, announcement.MessageId);
    }

    [Fact]
    public async Task Completing_a_server_wide_event_announces_it_in_world_chat_once()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 1);
        var outbox = new RecordingGameEventOutbox();
        var service = CreateService(db, definition, new RecordingPublisher(), outbox: outbox);
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Finisher",
            NormalizedName = "FINISHER"
        });
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        await service.GetJournalAsync(characterId, CancellationToken.None);

        var completion = Assert.Single(
            outbox.ChatAnnouncements.Where(x =>
                x.Body.Contains("completed", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(definition.Id, completion.EventQuestId);
        Assert.Contains(definition.Title, completion.Body);
        Assert.Equal("/game/quests", completion.TargetUrl);
        Assert.NotEqual(
            completion.MessageId,
            outbox.ChatAnnouncements.First().MessageId);
    }

    [Fact]
    public async Task Offline_combat_batch_contributes_each_victory()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 10);
        var service = CreateService(db, definition, new RecordingPublisher());
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "OfflineFighter",
            NormalizedName = "OFFLINEFIGHTER"
        });
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                "region_01_area_01",
                true,
                actionCount: 5,
                winningEncounterCount: 3),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        var state = Assert.Single((await service.GetJournalAsync(characterId, CancellationToken.None)).Events);
        Assert.Equal(3, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal(3, state.MyContribution);
        Assert.Equal(3, Assert.Single(await db.EventQuestEventLedgers.ToListAsync()).ContributionAmount);
    }

    [Fact]
    public async Task Tracked_contributions_for_other_characters_do_not_break_progression()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 100);
        var service = CreateService(db, definition, new RecordingPublisher());
        var firstCharacterId = Guid.NewGuid();
        var secondCharacterId = Guid.NewGuid();
        CompleteTutorial(db, firstCharacterId);
        CompleteTutorial(db, secondCharacterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            firstCharacterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        db.ChangeTracker.Clear();
        await service.ProcessAsync(
            secondCharacterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        db.ChangeTracker.Clear();

        await service.ProcessAsync(
            firstCharacterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        await service.ProcessAsync(
            secondCharacterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        var contributions = await db.EventQuestCharacterContributions
            .OrderBy(x => x.CharacterId)
            .ToListAsync();
        Assert.Equal(2, contributions.Count);
        Assert.All(contributions, contribution => Assert.Equal(2, contribution.TotalAmount));
    }

    [Fact]
    public async Task Event_completes_when_the_shared_target_is_reached()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 1);
        var service = CreateService(db, definition, new RecordingPublisher());
        var contributorId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        CompleteTutorial(db, contributorId);
        CompleteTutorial(db, viewerId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            contributorId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        var state = Assert.Single((await service.GetJournalAsync(viewerId, CancellationToken.None)).Events);
        Assert.Equal(EventQuestStatus.Completed, state.Status);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public async Task Personal_contribution_continues_after_the_shared_target_is_reached()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 1);
        var service = CreateService(db, definition, new RecordingPublisher());
        var characterId = Guid.NewGuid();
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        var state = Assert.Single((await service.GetJournalAsync(characterId, CancellationToken.None)).Events);
        Assert.Equal(EventQuestStatus.Completed, state.Status);
        Assert.Equal(1, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal(2, state.MyContribution);
        Assert.True(state.PersonalMilestones[0].IsUnlocked);
        Assert.False(state.PersonalMilestones[1].IsUnlocked);
    }

    [Fact]
    public async Task Claim_all_grants_each_unlocked_milestone_only_once()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 1);
        var writer = new RecordingLootRewardWriter();
        var service = CreateService(db, definition, new RecordingPublisher(), writer);
        var characterId = Guid.NewGuid();
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        var journal = await service.ClaimAllMilestonesAsync(
            characterId,
            definition.Id,
            CancellationToken.None);

        var milestones = Assert.Single(journal.Events).PersonalMilestones;
        Assert.True(milestones[0].IsClaimed);
        Assert.False(milestones[1].IsClaimed);
        Assert.Single(writer.Items);
        Assert.Equal("ore", writer.Items[0].ItemInstance.ItemBaseId);
        Assert.Equal("event-quest-reward", writer.Source);
        Assert.Null(writer.Location);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ClaimAllMilestonesAsync(characterId, definition.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Community_reward_grants_sigil_fragments_only_once()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 1);
        definition.Rewards =
        [
            new QuestRewardDefinition
            {
                Key = "sigil-fragments",
                Type = "SigilFragments",
                Quantity = 20
            }
        ];
        var service = CreateService(db, definition, new RecordingPublisher());
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "SigilTester",
            NormalizedName = "SIGILTESTER"
        });
        CompleteTutorial(db, characterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        await service.ClaimAsync(characterId, definition.Id, CancellationToken.None);

        Assert.Equal(20, (await db.Characters.SingleAsync(x => x.Id == characterId)).SigilFragments);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ClaimAsync(characterId, definition.Id, CancellationToken.None));
        Assert.Equal(20, (await db.Characters.SingleAsync(x => x.Id == characterId)).SigilFragments);
    }

    [Fact]
    public async Task Tutorial_character_cannot_see_or_contribute_to_event_quests()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(requiredAmount: 2);
        var service = CreateService(db, definition, new RecordingPublisher());
        var eligibleCharacterId = Guid.NewGuid();
        var tutorialCharacterId = Guid.NewGuid();
        CompleteTutorial(db, eligibleCharacterId);
        await db.SaveChangesAsync();

        await service.ProcessAsync(
            eligibleCharacterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);
        await service.ProcessAsync(
            tutorialCharacterId,
            QuestTrigger.CombatCompleted("region_01_area_01", true),
            Guid.NewGuid(),
            "IdleCombatEncounterCompleted",
            CancellationToken.None);

        Assert.Empty((await service.GetJournalAsync(
            tutorialCharacterId,
            CancellationToken.None)).Events);
        var instance = Assert.Single(await db.EventQuestInstances
            .Include(x => x.Objectives)
            .Include(x => x.Contributions)
            .ToListAsync());
        Assert.Equal(1, Assert.Single(instance.Objectives).CurrentAmount);
        Assert.DoesNotContain(
            instance.Contributions,
            contribution => contribution.CharacterId == tutorialCharacterId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ClaimAsync(
                tutorialCharacterId,
                definition.Id,
                CancellationToken.None));
    }

    private static EventQuestService CreateService(
        LLDbContext db,
        EventQuestDefinition definition,
        RecordingPublisher publisher,
        RecordingLootRewardWriter? lootRewardWriter = null,
        RecordingGameEventOutbox? outbox = null) =>
        new(
            new EventQuestRepository(db),
            new QuestRepository(db),
            new StubDefinitionProvider(definition),
            new StubItemBaseRepository(),
            new RecordingInventoryItemFactory(),
            lootRewardWriter ?? new RecordingLootRewardWriter(),
            new FixedTimeProvider(Now),
            publisher,
            outbox ?? new RecordingGameEventOutbox());

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<EventQuestChatAnnouncementPayload> ChatAnnouncements { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            if (payload is EventQuestChatAnnouncementPayload announcement)
                ChatAnnouncements.Add(announcement);
            return Task.CompletedTask;
        }
    }

    private static void CompleteTutorial(LLDbContext db, Guid characterId) =>
        db.CharacterQuestProgresses.Add(new CharacterQuestProgress
        {
            CharacterId = characterId,
            QuestId = QuestConstants.IntoLumoRuins,
            DefinitionVersion = 1,
            Status = QuestStatus.Completed,
            CompletedAt = Now,
            CreatedAt = Now,
            UpdatedAt = Now
        });

    private static EventQuestDefinition CreateActiveDefinition(long requiredAmount) =>
        new()
        {
            Id = "event.test",
            Version = 1,
            Enabled = true,
            Title = "Test Event",
            Summary = "A shared test.",
            StartsAtUtc = Now.AddHours(-1),
            EndsAtUtc = Now.AddHours(1),
            ClaimEndsAtUtc = Now.AddDays(1),
            MinimumContribution = 1,
            PersonalMilestones =
            [
                new EventQuestPersonalMilestoneDefinition
                {
                    Key = "two-actions",
                    RequiredContribution = 2,
                    Rewards =
                    [
                        new QuestRewardDefinition
                        {
                            Key = "ore",
                            Type = "Item",
                            ItemBaseId = "ore",
                            Quantity = 1
                        }
                    ]
                },
                new EventQuestPersonalMilestoneDefinition
                {
                    Key = "three-actions",
                    RequiredContribution = 3,
                    Rewards =
                    [
                        new QuestRewardDefinition
                        {
                            Key = "wood",
                            Type = "Item",
                            ItemBaseId = "wood",
                            Quantity = 1
                        }
                    ]
                }
            ],
            Objectives =
            [
                new QuestObjectiveDefinition
                {
                    Key = "victories",
                    Description = "Win in Lumo Ruins.",
                    Type = "CombatEncounterCompleted",
                    RequiredAmount = requiredAmount,
                    Filters = new QuestObjectiveFilterDefinition
                    {
                        AreaId = "region_01_area_01",
                        RequiresVictory = true
                    }
                }
            ]
        };

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static string FindApiRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var path in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (Directory.Exists(Path.Combine(path, "Data", "event-quests"))) return path;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate API.LL/Data/event-quests.");
    }

    private sealed class StubDefinitionProvider(EventQuestDefinition definition)
        : IEventQuestDefinitionProvider
    {
        public IReadOnlyList<EventQuestDefinition> GetAll() => [definition];
        public EventQuestDefinition Get(string eventQuestId, int? version = null) => definition;
    }

    private sealed class StubItemBaseRepository : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(
                itemIds.ToDictionary(
                    id => id,
                    id => new ItemBase { Id = id },
                    StringComparer.OrdinalIgnoreCase));

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>());

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(
            string itemBaseId,
            CancellationToken cancellationToken) => Task.FromResult<EquipmentBase?>(null);

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingInventoryItemFactory : IInventoryItemFactory
    {
        public InventoryItem Create(ItemBase itemBase, int quantity, Guid? inventoryId = null) =>
            new()
            {
                InventoryId = inventoryId ?? Guid.Empty,
                ItemInstanceId = Guid.NewGuid(),
                ItemInstance = new ItemInstance
                {
                    Id = Guid.NewGuid(),
                    ItemBaseId = itemBase.Id,
                    ItemBase = itemBase
                },
                Quantity = quantity
            };

        public IReadOnlyList<InventoryItem> CreateForQuantity(
            ItemBase itemBase,
            int quantity,
            Guid? inventoryId = null) => [Create(itemBase, quantity, inventoryId)];
    }

    private sealed class RecordingLootRewardWriter : ILootRewardWriter
    {
        public List<InventoryItem> Items { get; } = [];
        public string? Source { get; private set; }
        public string? Location { get; private set; }

        public Task AddLootAsync(
            Guid characterId,
            IReadOnlyCollection<InventoryItem> items,
            string source,
            string? location,
            CancellationToken cancellationToken)
        {
            Items.AddRange(items);
            Source = source;
            Location = location;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPublisher : IGameRealtimeBroadcaster
    {
        public List<GameRealtimeEvent> Messages { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
