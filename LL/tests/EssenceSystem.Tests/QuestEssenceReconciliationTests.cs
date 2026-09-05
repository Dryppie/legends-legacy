using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Quests;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    [Theory]
    [InlineData(false, "essence.hollow_stag", 1)]
    [InlineData(true, "essence.hollow_stag", 1)]
    [InlineData(true, "essence.goblin_warrior", 0)]
    public async Task Journal_recognizes_only_the_selected_essence_for_new_and_stuck_quests(
        bool alreadyActive, string ownedEssence, int expectedAmount)
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        var firstHunt = CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = "hollow_stag";
        repository.Progresses.Add(firstHunt);
        repository.OwnedEssences.Add(ownedEssence);
        if (alreadyActive)
            repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.SoulArchive), true));
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System);

        var journal = await service.GetJournalAsync(id, default);

        var soul = Assert.Single(journal.Quests, x => x.QuestId == QuestConstants.SoulArchive);
        Assert.Equal(expectedAmount, soul.Objectives[0].CurrentAmount);
        Assert.Equal(0, soul.Objectives[1].CurrentAmount);
        Assert.Equal(QuestStatus.Active, soul.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Owned_and_attuned_essence_completes_the_chain_and_grants_cinders_once(bool finishFirstHuntByEvent)
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        var firstHunt = finishFirstHuntByEvent
            ? CreateActiveProgress(id, definitions.Get(QuestConstants.TrainingDay), true)
            : CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = "hollow_stag";
        repository.Progresses.Add(firstHunt);
        repository.OwnedEssences.Add("essence.hollow_stag");
        repository.EquippedEssences.Add("essence.hollow_stag");
        var chat = new RecordingQuestSystemChatPublisher();
        var stateSync = new EssenceQuestStateSync();
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Characters.Add(new Character { Id = id, UserId = Guid.NewGuid(), Name = "Quest", NormalizedName = "QUEST", Cinders = 7 });
        await db.SaveChangesAsync();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            stateSync: stateSync, systemChatPublisher: chat, currencyRewards: new QuestEquipmentRewardRepository(db),
            equipmentProgressionEquipment: new QuestEquipmentSupport());

        if (finishFirstHuntByEvent)
        {
            var result = await service.ProcessAsync(id,
                QuestTrigger.CombatCompleted("tutorial_area_training_grounds", true), Guid.NewGuid(),
                GameEventTypes.IdleCombatEncounterCompleted, default);
            Assert.Empty(result.CompletedQuestIds);
            Assert.Empty(result.Loot);
            await service.TurnInAsync(id, QuestConstants.TrainingDay, default);
        }
        var journal = await service.GetJournalAsync(id, default);
        Assert.Equal(QuestStatus.Active, journal.Quests.Single(x => x.QuestId == QuestConstants.SoulArchive).Status);
        Assert.DoesNotContain(journal.Quests, x => x.QuestId == QuestConstants.FirstWeapon);
        journal = await service.TurnInAsync(id, QuestConstants.SoulArchive, default);
        var saveCalls = repository.SaveCalls;
        var soul = Assert.Single(journal.Quests, x => x.QuestId == QuestConstants.SoulArchive);
        Assert.Equal(QuestStatus.Completed, soul.Status);
        Assert.All(soul.Objectives, x => Assert.True(x.IsCompleted));
        Assert.Contains(journal.Quests, x => x.QuestId == QuestConstants.FirstWeapon && x.Status == QuestStatus.Active);
        Assert.Equal(QuestConstants.FirstWeapon, journal.PinnedQuestId);
        await service.GetJournalAsync(id, default);
        Assert.Equal(saveCalls, repository.SaveCalls);
        var eventId = Guid.NewGuid();
        for (var i = 0; i < 2; i++)
            await service.ProcessAsync(id, QuestTrigger.EssenceAbsorbed("essence.hollow_stag"), eventId,
                GameEventTypes.EssenceAbsorbed, default);
        await db.SaveChangesAsync();

        Assert.Equal(507, (await db.Characters.SingleAsync()).Cinders);
        Assert.Equal(500, (await db.EconomyLedger.SingleAsync()).Quantity);
        Assert.Single(chat.Publications.SelectMany(x => x.Completions), x => x.QuestId == QuestConstants.SoulArchive);
    }

    [Fact]
    public async Task Soul_archive_accepts_any_attuned_essence()
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        var firstHunt = CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = "hollow_stag";
        repository.Progresses.Add(firstHunt);
        repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.SoulArchive), true));
        repository.OwnedEssences.Add("essence.hollow_stag");
        repository.EquippedEssences.Add("essence.wood_nymph");
        var currency = new RecordingQuestCurrencyRewardRepository();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            currencyRewards: currency);

        var journal = await service.GetJournalAsync(id, default);

        var soul = Assert.Single(journal.Quests, x => x.QuestId == QuestConstants.SoulArchive);
        Assert.Equal(QuestStatus.Active, soul.Status);
        Assert.All(soul.Objectives, objective => Assert.True(objective.IsCompleted));
        Assert.Equal(0, currency.AwardedCinders);
        await service.TurnInAsync(id, soul.QuestId, default);
        Assert.Equal(500, currency.AwardedCinders);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Second_soul_repairs_existing_requirement_and_uses_distinct_ownership(int ownedCount)
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        var progress = CreateActiveProgress(id, definitions.Get(QuestConstants.ASecondSoul), true);
        Assert.Single(progress.Objectives).RequiredAmount = 1; // Persisted before the repair.
        repository.Progresses.Add(progress);
        foreach (var essence in new[] { "essence.hollow_stag", "essence.skeleton", "essence.goblin_warrior" }.Take(ownedCount))
            repository.OwnedEssences.Add(essence);
        var loot = new RecordingLootRewardWriter();
        var stateSync = new EssenceQuestStateSync();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), loot, TimeProvider.System, stateSync: stateSync);

        var journal = await service.GetJournalAsync(id, default);
        await service.GetJournalAsync(id, default);
        var secondSoul = Assert.Single(journal.Quests, x => x.QuestId == QuestConstants.ASecondSoul);
        Assert.Equal(2, secondSoul.Objectives[0].RequiredAmount);
        Assert.Equal(Math.Min(2, ownedCount), secondSoul.Objectives[0].CurrentAmount);
        Assert.Equal(QuestStatus.Active, secondSoul.Status);
        Assert.Empty(loot.GrantedItems);
        if (ownedCount >= 2)
        {
            journal = await service.TurnInAsync(id, secondSoul.QuestId, default);
            Assert.Equal(10, Assert.Single(loot.GrantedItems).Quantity);
            Assert.Contains(journal.Quests, x => x.QuestId == QuestConstants.TheArchiveDeepens);
        }
        else Assert.Empty(loot.GrantedItems);
    }

    [Fact]
    public async Task Second_soul_counts_new_ownership_without_adding_replayed_absorption_events()
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.ASecondSoul), true));
        repository.OwnedEssences.Add("essence.hollow_stag");
        var loot = new RecordingLootRewardWriter();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), loot, TimeProvider.System);
        await service.GetJournalAsync(id, default);
        for (var i = 0; i < 2; i++)
            await service.ProcessAsync(id, QuestTrigger.EssenceAbsorbed("essence.hollow_stag"), Guid.NewGuid(),
                GameEventTypes.EssenceAbsorbed, default);
        Assert.Empty(loot.GrantedItems);

        repository.OwnedEssences.Add("essence.skeleton");
        var eventId = Guid.NewGuid();
        var result = await service.ProcessAsync(id, QuestTrigger.EssenceAbsorbed("essence.skeleton"), eventId,
            GameEventTypes.EssenceAbsorbed, default);
        var retry = await service.ProcessAsync(id, QuestTrigger.EssenceAbsorbed("essence.skeleton"), eventId,
            GameEventTypes.EssenceAbsorbed, default);

        Assert.Empty(result.CompletedQuestIds);
        Assert.Empty(result.Loot);
        Assert.Empty(retry.Loot);
        Assert.Empty(loot.GrantedItems);
        await service.TurnInAsync(id, QuestConstants.ASecondSoul, default);
        await service.TurnInAsync(id, QuestConstants.ASecondSoul, default);
        Assert.Single(loot.GrantedItems);
    }

    [Fact]
    public async Task Completed_second_soul_keeps_its_previous_completion_and_reward_marker()
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        var progress = CreateCompletedProgress(id, definitions.Get(QuestConstants.ASecondSoul));
        var objective = Assert.Single(progress.Objectives);
        objective.RequiredAmount = objective.CurrentAmount = 1;
        progress.RewardsGrantedAt = progress.CompletedAt;
        var originalCompletedAt = progress.CompletedAt;
        repository.Progresses.Add(progress);
        var loot = new RecordingLootRewardWriter();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), loot, TimeProvider.System);

        await service.GetJournalAsync(id, default);

        Assert.Equal(QuestStatus.Completed, progress.Status);
        Assert.Equal(originalCompletedAt, progress.CompletedAt);
        Assert.Equal(originalCompletedAt, progress.RewardsGrantedAt);
        Assert.Empty(loot.GrantedItems);
    }

    [Fact]
    public async Task Existing_collection_does_not_bypass_first_hunt_prerequisite_or_choice()
    {
        var id = Guid.NewGuid();
        var repository = new RecordingQuestRepository(1);
        repository.OwnedEssences.Add("essence.hollow_stag");
        repository.EquippedEssences.Add("essence.hollow_stag");
        var service = new QuestService(repository, CreateDefinitions(), new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System);

        var journal = await service.GetJournalAsync(id, default);

        var firstHunt = Assert.Single(journal.Quests);
        Assert.Equal(QuestConstants.TrainingDay, firstHunt.QuestId);
        Assert.Equal(QuestStatus.Active, firstHunt.Status);
        Assert.Null(firstHunt.Choice?.SelectedOptionKey);
    }

    [Fact]
    public async Task Journal_persists_ownership_repair_and_rewards_once_across_separate_reads()
    {
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var definitions = CreateDefinitions();
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Characters.Add(new Character { Id = id, UserId = Guid.NewGuid(), Name = "Quest", NormalizedName = "QUEST", Level = 1 });
        var firstHunt = CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = "hollow_stag";
        db.CharacterQuestProgresses.Add(firstHunt);
        var owned = new PlayerEssence { Id = Guid.NewGuid(), CharacterId = id, EssenceDefinitionId = "essence.hollow_stag" };
        db.PlayerEssences.AddRange(owned,
            new PlayerEssence { Id = Guid.NewGuid(), CharacterId = otherId, EssenceDefinitionId = "essence.skeleton" });
        db.EssenceLoadouts.Add(new EssenceLoadout
        {
            Id = Guid.NewGuid(), CharacterId = id, Name = "Existing",
            Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), PlayerEssenceId = owned.Id, PlayerEssence = owned }]
        });
        await db.SaveChangesAsync();
        var repository = new QuestRepository(db);

        Assert.Equal("essence.hollow_stag", Assert.Single(await repository.GetOwnedEssenceDefinitionIdsAsync(id, default)));
        Assert.True(await repository.HasEssenceInAnyLoadoutAsync(id, "essence.hollow_stag", default));
        Assert.True(await repository.HasAnyEssenceInLoadoutAsync(id, default));
        Assert.False(await repository.HasEssenceInAnyLoadoutAsync(otherId, "essence.hollow_stag", default));
        Assert.False(await repository.HasAnyEssenceInLoadoutAsync(otherId, default));

        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            currencyRewards: new QuestEquipmentRewardRepository(db));
        await service.GetJournalAsync(id, default);
        db.ChangeTracker.Clear();
        await service.GetJournalAsync(id, default);
        db.ChangeTracker.Clear();

        var savedSoul = await db.CharacterQuestProgresses.SingleAsync(x => x.CharacterId == id && x.QuestId == QuestConstants.SoulArchive);
        Assert.Equal(QuestStatus.Active, savedSoul.Status);
        Assert.Null(savedSoul.RewardsGrantedAt);
        Assert.Empty(await db.EconomyLedger.ToListAsync());
        await service.TurnInAsync(id, savedSoul.QuestId, default);
        db.ChangeTracker.Clear();
        await service.TurnInAsync(id, savedSoul.QuestId, default);
        savedSoul = await db.CharacterQuestProgresses.SingleAsync(x => x.CharacterId == id && x.QuestId == QuestConstants.SoulArchive);
        Assert.Equal(QuestStatus.Completed, savedSoul.Status);
        Assert.NotNull(savedSoul.RewardsGrantedAt);
        Assert.Equal(500, (await db.Characters.SingleAsync()).Cinders);
        Assert.Single(await db.EconomyLedger.ToListAsync());
    }

    private sealed class EssenceQuestStateSync : IStateSyncService
    {
        public List<string> InvalidatedScopes { get; } = [];
        public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId) => new Dictionary<string, long>();
        public Task InvalidateCharacterAsync(Guid characterId, string reason, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task InvalidateCharacterScopeAsync(Guid characterId, string scope, string reason, CancellationToken cancellationToken = default)
        {
            InvalidatedScopes.Add(scope);
            return Task.CompletedTask;
        }
        public Task InvalidateWorldScopeAsync(string scope, string reason, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StateSyncCheckpoint> GetCheckpointAsync(Guid characterId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingQuestCurrencyRewardRepository : IQuestEquipmentRewardRepository
    {
        public long AwardedCinders { get; private set; }

        public Task<IReadOnlyList<EquipmentData>> GetEquippedAsync(Guid characterId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EquipmentData>>([]);

        public Task AwardCindersAsync(Guid characterId, string questId, long amount, CancellationToken ct)
        {
            AwardedCinders += amount;
            return Task.CompletedTask;
        }
    }
}
