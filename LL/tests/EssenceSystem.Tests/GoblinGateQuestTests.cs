using Domain.Models.Quests;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    [Fact]
    public void Goblin_sigil_comes_from_the_level_20_area_before_the_gate_quest()
    {
        var definitions = CreateDefinitions();
        var sigilQuest = Assert.Single(definitions.GetAll(), quest =>
            quest.Rewards.Any(reward => reward.ItemBaseId == "sigil_goblin_mines"));
        Assert.Equal(QuestConstants.BetweenDayAndNight, sigilQuest.Id);
        Assert.Equal(20, sigilQuest.Availability.MinimumLevel);
        var gateQuest = Assert.Single(definitions.GetAll(), quest =>
            quest.Objectives.Any(objective => objective.Key == "break_the_goblin_gate"));
        Assert.Equal(QuestConstants.RootsRemember, gateQuest.Id);
        Assert.True(gateQuest.Availability.MinimumLevel >= 20);
        Assert.Contains(sigilQuest.Id, gateQuest.Availability.CompletedQuestIds);
        Assert.DoesNotContain(definitions.Get(QuestConstants.BloodInTheGrove).Objectives,
            objective => objective.Type == "DungeonRunCompleted");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_roots_quests_receive_the_gate_without_reopening_completed_quests(bool completed)
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var definition = definitions.Get(QuestConstants.RootsRemember);
        var saved = completed ? CreateCompletedProgress(id, definition) : CreateActiveProgress(id, definition, true);
        saved.Objectives.Remove(saved.Objectives.Single(x => x.ObjectiveKey == "break_the_goblin_gate"));
        var repository = new RecordingQuestRepository(level: 25);
        repository.Progresses.Add(saved);
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            null!, null!, TimeProvider.System);

        await service.GetJournalAsync(id, CancellationToken.None);
        await service.GetJournalAsync(id, CancellationToken.None);

        var gate = Assert.Single(saved.Objectives, x => x.ObjectiveKey == "break_the_goblin_gate");
        Assert.Equal(completed ? 1 : 0, gate.CurrentAmount);
        Assert.Equal(completed ? QuestStatus.Completed : QuestStatus.Active, saved.Status);
    }

    [Fact]
    public async Task Existing_blood_grove_quest_is_ready_for_turn_in_without_the_removed_gate_on_journal_refresh()
    {
        var id = Guid.NewGuid();
        var definitions = CreateDefinitions();
        var saved = CreateCompletedProgress(id, definitions.Get(QuestConstants.BloodInTheGrove));
        saved.Status = QuestStatus.Active;
        saved.CompletedAt = null;
        saved.Objectives.Add(new CharacterQuestObjectiveProgress
        {
            CharacterId = id, QuestId = saved.QuestId,
            ObjectiveKey = "break_the_goblin_gate", RequiredAmount = 1
        });
        var repository = new RecordingQuestRepository(level: 10);
        repository.Progresses.Add(saved);
        var service = new QuestService(repository, definitions, new RecordingItemBaseRepository(),
            new RecordingInventoryItemFactory(), new RecordingLootRewardWriter(), TimeProvider.System);

        await service.GetJournalAsync(id, CancellationToken.None);
        await service.GetJournalAsync(id, CancellationToken.None);

        Assert.Equal(QuestStatus.Active, saved.Status);
        Assert.DoesNotContain(saved.Objectives, x => x.ObjectiveKey == "break_the_goblin_gate");
        Assert.DoesNotContain(repository.Progresses, x => x.QuestId == QuestConstants.CrystalCurrents);
        await service.TurnInAsync(id, saved.QuestId, default);
        Assert.Equal(QuestStatus.Completed, saved.Status);
        Assert.Contains(repository.Progresses, x => x.QuestId == QuestConstants.CrystalCurrents);
    }
}
