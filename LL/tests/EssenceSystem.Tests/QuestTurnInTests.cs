using Domain.Models.Quests;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
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
