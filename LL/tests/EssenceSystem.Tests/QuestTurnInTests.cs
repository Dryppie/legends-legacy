using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Quests;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    [Theory]
    [InlineData(QuestConstants.IntoLumoRuins, RandomEquipmentBoxCatalog.ArmorChestItemBaseId)]
    [InlineData(QuestConstants.BloodInTheGrove, RandomEquipmentBoxCatalog.JewelryChestItemBaseId)]
    public async Task Early_quest_turn_in_awards_its_chest_once_alongside_existing_rewards(string questId, string chestId)
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var definition = definitions.Get(questId);
        var repository = new RecordingQuestRepository(10);
        var progress = CreateCompletedProgress(id, definition);
        progress.Status = QuestStatus.Active;
        progress.CompletedAt = null;
        repository.Progresses.Add(progress);
        var loot = new RecordingLootRewardWriter();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), loot, TimeProvider.System);

        var journal = await service.TurnInAsync(id, questId, default);
        await service.TurnInAsync(id, questId, default);
        await service.GetJournalAsync(id, default);

        Assert.Equal(QuestStatus.Completed, progress.Status);
        Assert.NotNull(progress.RewardsGrantedAt);
        Assert.Equal(1, Assert.Single(loot.GrantedItems, item => item.ItemInstance.ItemBaseId == chestId).Quantity);
        Assert.Contains(journal.Quests.Single(quest => quest.QuestId == questId).Rewards,
            reward => reward.ItemBaseId == chestId && reward.Quantity == 1);
        if (questId == QuestConstants.BloodInTheGrove)
        {
            Assert.Equal(2, loot.GrantedItems.Count);
            Assert.Equal(1, Assert.Single(loot.GrantedItems,
                item => item.ItemInstance.ItemBaseId == "item.essence_token.blood_grove").Quantity);
        }
        else
        {
            Assert.Single(loot.GrantedItems);
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("incomplete")]
    [InlineData("unselected-choice")]
    public async Task Turn_in_rejects_unavailable_or_unfinished_quests_without_rewards(string scenario)
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var repository = new RecordingQuestRepository(1);
        var progress = CreateActiveProgress(id, definitions.Get(QuestConstants.TrainingDay), true);
        if (scenario == "unselected-choice")
            foreach (var objective in progress.Objectives)
            {
                objective.CurrentAmount = objective.RequiredAmount;
                objective.CompletedAt = DateTimeOffset.UtcNow;
            }
        if (scenario != "missing") repository.Progresses.Add(progress);
        var loot = new RecordingLootRewardWriter();
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), loot, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.TurnInAsync(id, progress.QuestId, default));

        Assert.Empty(loot.GrantedItems);
        Assert.Null(progress.RewardsGrantedAt);
        Assert.Equal(QuestStatus.Active, progress.Status);
    }
}
