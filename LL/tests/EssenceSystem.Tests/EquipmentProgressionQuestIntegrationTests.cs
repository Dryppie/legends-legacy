using System.Text.Json;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Outbox;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Persistence.LL.Repositories.Quests;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    private static JsonQuestDefinitionProvider EquipmentDefinitions() =>
        new(new ConfigurationBuilder().Build(), FindApiRoot(), new(JsonSerializerDefaults.Web));

    [Fact]
    public void Equipment_quest_catalog_preserves_area_tokens_and_core_rewards_without_scrap()
    {
        var definitions = EquipmentDefinitions();
        Assert.Equal(28, definitions.GetAll().Count);
        Assert.Contains(definitions.Get(QuestConstants.SoulArchive).Rewards,
            x => x.Type == "Cinders" && x.Quantity == 500);
        Assert.Contains(definitions.Get(QuestConstants.SoulArchive).Rewards,
            x => x.Type == "Item" && x.ItemBaseId == "item.arms_chest" && x.Quantity == 1);
        Assert.Empty(definitions.Get(QuestConstants.FirstWeapon).Rewards);
        foreach (var quest in definitions.GetAll().Where(x => x.Chain?.Id == "quest.chain.shenic" && x.Version == 4))
        {
            Assert.DoesNotContain(quest.Rewards, x => new[] { "ore", "wood", "rawhide", "soul_dust" }.Contains(x.ItemBaseId));
        }
        Assert.Contains(definitions.Get(QuestConstants.BetweenDayAndNight).Rewards, x => x.ItemBaseId == "sigil_goblin_mines" && x.Quantity == 1);
        Assert.Contains(definitions.Get(QuestConstants.CrystalCurrents).Rewards, x => x.ItemBaseId == "sigil_forgotten_catacombs" && x.Quantity == 1);
        var adaptation = definitions.Get(QuestConstants.RestlessDead);
        Assert.Equal("Sequential", adaptation.ObjectiveMode);
        Assert.Equal("ModelEAreaDropEquipped", adaptation.Objectives[0].Type);
    }

    [Fact]
    public async Task EquipmentProgression_currency_rewards_are_recorded_once_and_existing_active_quests_keep_their_version()
    {
        var id = Guid.NewGuid();
        var definitions = EquipmentDefinitions();
        var repository = new RecordingQuestRepository(1);
        var firstHunt = CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay));
        firstHunt.SelectedOptionKey = definitions.Get(QuestConstants.TrainingDay).Choice!.Options[0].Key;
        repository.Progresses.Add(firstHunt);
        var soul = CreateCompletedProgress(id, definitions.Get(QuestConstants.SoulArchive));
        soul.Status = QuestStatus.Active;
        soul.CompletedAt = null;
        repository.Progresses.Add(soul);
        repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.FirstWeapon, 2), false));
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Characters.Add(new Character { Id = id, UserId = Guid.NewGuid(), Name = "Quest", NormalizedName = "QUEST", Cinders = 7 });
        await db.SaveChangesAsync();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(), new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            currencyRewards: new QuestEquipmentRewardRepository(db), equipmentProgressionEquipment: new QuestEquipmentSupport());
        for (var i = 0; i < 2; i++)
            await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        Assert.Null(soul.RewardsGrantedAt);
        await service.TurnInAsync(id, soul.QuestId, default);
        await service.TurnInAsync(id, soul.QuestId, default);
        await db.SaveChangesAsync();
        Assert.Equal(507, (await db.Characters.SingleAsync()).Cinders);
        var ledger = await db.EconomyLedger.SingleAsync();
        Assert.Equal(500, ledger.Quantity);
        Assert.Equal(QuestConstants.SoulArchive, ledger.Source);
        Assert.NotNull(soul.RewardsGrantedAt);
        Assert.Equal(2, repository.Progresses.Single(x => x.QuestId == QuestConstants.FirstWeapon).DefinitionVersion);
    }

    [Fact]
    public async Task EquipmentProgression_first_weapon_unlocks_into_lumo_when_the_weapon_is_equipped()
    {
        var id = Guid.NewGuid();
        var definitions = EquipmentDefinitions();
        var repository = new RecordingQuestRepository(1);
        repository.Progresses.Add(CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay)));
        repository.Progresses.Add(CreateCompletedProgress(id, definitions.Get(QuestConstants.SoulArchive)));
        repository.Progresses.Add(CreateActiveProgress(id, definitions.Get(QuestConstants.FirstWeapon), true));
        var equipment = new QuestEquipmentSupport();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(), new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            equipmentProgressionEquipment: equipment);
        var initial = await service.GetJournalAsync(id, CancellationToken.None);
        Assert.All(initial.Quests.Single(x => x.QuestId == QuestConstants.FirstWeapon).Objectives,
            objective => Assert.False(objective.IsCompleted));
        await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        equipment.Claimed = true;
        var opened = await service.GetJournalAsync(id, CancellationToken.None);
        var firstWeapon = opened.Quests.Single(x => x.QuestId == QuestConstants.FirstWeapon);
        Assert.Collection(firstWeapon.Objectives,
            chest =>
            {
                Assert.Equal("open_arms_chest", chest.Key);
                Assert.True(chest.IsCompleted);
            },
            weapon =>
            {
                Assert.Equal("equip_starter_loadout", weapon.Key);
                Assert.False(weapon.IsCompleted);
            });
        await Assert.ThrowsAsync<ArgumentException>(() => service.TurnInAsync(id, QuestConstants.FirstWeapon, default));
        equipment.Equipped = true;
        var result = await service.ProcessAsync(id, QuestTrigger.EquipmentChanged(), null, GameEventTypes.EquipmentChanged, CancellationToken.None);
        Assert.Empty(result.Loot);
        Assert.DoesNotContain(result.Journal.Quests, x => x.QuestId == QuestConstants.IntoLumoRuins);
        var journal = await service.TurnInAsync(id, QuestConstants.FirstWeapon, default);
        Assert.Contains(journal.Quests, x => x.QuestId == QuestConstants.IntoLumoRuins && x.Version == 2);
        Assert.Equal(QuestStatus.Completed, repository.Progresses.Single(x => x.QuestId == QuestConstants.FirstWeapon).Status);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    public async Task First_weapon_existing_progress_adds_the_chest_objective_without_resetting_credit(
        bool claimed, bool equippedBefore, bool completed)
    {
        var id = Guid.NewGuid();
        var definitions = EquipmentDefinitions();
        var repository = new RecordingQuestRepository(1);
        repository.Progresses.Add(CreateCompletedProgress(id, definitions.Get(QuestConstants.TrainingDay)));
        repository.Progresses.Add(CreateCompletedProgress(id, definitions.Get(QuestConstants.SoulArchive)));
        var progress = CreateCompletedProgress(id, definitions.Get(QuestConstants.FirstWeapon));
        if (!completed)
        {
            progress.Status = QuestStatus.Active;
            progress.CompletedAt = null;
            progress.RewardsGrantedAt = null;
        }
        progress.Objectives.Remove(progress.Objectives.Single(x => x.ObjectiveKey == "open_arms_chest"));
        var weapon = Assert.Single(progress.Objectives);
        if (!equippedBefore)
        {
            weapon.CurrentAmount = 0;
            weapon.CompletedAt = null;
        }
        var equippedAt = weapon.CompletedAt;
        repository.Progresses.Add(progress);
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System,
            equipmentProgressionEquipment: new QuestEquipmentSupport { Claimed = claimed });

        var repaired = completed
            ? await service.TurnInAsync(id, QuestConstants.FirstWeapon, default)
            : await service.PinAsync(id, QuestConstants.FirstWeapon, default);
        Assert.Equal(2, repaired.Quests.Single(x => x.QuestId == QuestConstants.FirstWeapon).Objectives.Count);

        // Repeated reads must repair old saves only once, even when the weapon is no longer equipped.
        await service.GetJournalAsync(id, default);
        var saves = repository.SaveCalls;
        var journal = await service.GetJournalAsync(id, default);

        var quest = journal.Quests.Single(x => x.QuestId == QuestConstants.FirstWeapon);
        Assert.Equal(2, quest.Version);
        Assert.Equal(completed ? QuestStatus.Completed : QuestStatus.Active, quest.Status);
        Assert.Equal(2, progress.Objectives.Count);
        Assert.Equal(claimed || equippedBefore || completed, quest.Objectives[0].IsCompleted);
        Assert.Equal(equippedBefore, quest.Objectives[1].IsCompleted);
        Assert.Equal(equippedAt, weapon.CompletedAt);
        Assert.Equal(saves, repository.SaveCalls);
    }

    private sealed class QuestEquipmentSupport : IEquipmentQuestSupport
    {
        public bool Claimed { get; set; }
        public bool Equipped { get; set; }
        public Task<bool> HasStarterClaimAsync(Guid id, string? kind, CancellationToken ct) => Task.FromResult(Claimed);
        public Task<bool> IsEquippedAsync(Guid id, string objective, string? kind, CancellationToken ct) => Task.FromResult(Equipped);
    }
}
