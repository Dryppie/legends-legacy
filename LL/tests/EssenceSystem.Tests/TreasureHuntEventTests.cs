using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.Outbox;
using Domain.Models.Entities.Characters;
using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Persistence.LL.Repositories.Quests;
using Services.LL.Quests.Events;

namespace EssenceSystem.Tests;

public sealed partial class EventQuestSystemTests
{
    private static EventQuestDefinition TreasureHunt() => new JsonEventQuestDefinitionProvider(
        new ConfigurationBuilder().Build(), FindApiRoot(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        .Get("event.great_treasure_hunt.2026_09_07");

    private static EventQuestService TreasureHuntService(
        LLDbContext db, EventQuestDefinition definition, DateTimeOffset now, RecordingLootRewardWriter writer) => new(
        new EventQuestRepository(db), new QuestRepository(db), new StubDefinitionProvider(definition),
        new StubItemBaseRepository(), new RecordingInventoryItemFactory(), writer,
        new FixedTimeProvider(now), new RecordingPublisher(), new RecordingGameEventOutbox());

    [Fact]
    public void Treasure_hunt_content_has_a_full_week_and_the_requested_rewards()
    {
        var definition = TreasureHunt();
        Assert.True(definition.Enabled);
        Assert.Equal(new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.FromHours(2)), definition.StartsAtUtc);
        Assert.Equal(TimeSpan.FromDays(7), definition.EndsAtUtc - definition.StartsAtUtc);
        Assert.Equal(TimeSpan.FromDays(7), definition.ClaimEndsAtUtc - definition.EndsAtUtc);
        Assert.Equal(1, definition.MinimumContribution);
        var objective = Assert.Single(definition.Objectives);
        Assert.Equal("EquipmentFound", objective.Type);
        Assert.Equal(1000, objective.RequiredAmount);
        Assert.Equal((RandomEquipmentBoxCatalog.UncommonItemBaseId, 1),
            (Assert.Single(definition.Rewards).ItemBaseId, Assert.Single(definition.Rewards).Quantity));
        Assert.Equal(new long[] { 5, 15, 50 }, definition.PersonalMilestones.Select(x => x.RequiredContribution));
        Assert.Collection(definition.PersonalMilestones.Select(x => Assert.Single(x.Rewards)),
            reward => Assert.Equal(("Item", RandomEquipmentBoxCatalog.UncommonItemBaseId, 1), (reward.Type, reward.ItemBaseId, reward.Quantity)),
            reward => Assert.Equal(("Item", "soul_dust", 50), (reward.Type, reward.ItemBaseId, reward.Quantity)),
            reward => Assert.Equal(("SigilFragments", 50), (reward.Type, reward.Quantity)));
    }

    [Fact]
    public async Task Treasure_hunt_tracks_unique_drops_and_grants_milestones_and_community_box_once()
    {
        await using var db = CreateDb();
        var definition = TreasureHunt();
        var now = definition.StartsAtUtc.AddHours(12);
        var characterId = Guid.NewGuid();
        var helperId = Guid.NewGuid();
        foreach (var id in new[] { characterId, helperId })
        {
            db.Characters.Add(new Character { Id = id, Name = id.ToString() });
            CompleteTutorial(db, id);
        }
        SigilFragmentTestItems.Seed(db, characterId);
        await db.SaveChangesAsync();
        var writer = new RecordingLootRewardWriter();
        var service = TreasureHuntService(db, definition, now, writer);
        var messageId = Guid.NewGuid();
        await service.ProcessAsync(characterId, QuestTrigger.EquipmentFound(5, now), messageId, GameEventTypes.EquipmentFound, default);
        await service.ProcessAsync(characterId, QuestTrigger.EquipmentFound(5, now), messageId, GameEventTypes.EquipmentFound, default);
        var state = Assert.Single((await service.GetJournalAsync(characterId, default)).Events);
        Assert.Equal(5, state.MyContribution);
        await service.ClaimMilestoneAsync(characterId, definition.Id, "find-5-items", default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAsync(characterId, definition.Id, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimMilestoneAsync(characterId, definition.Id, "find-15-items", default));

        await service.ProcessAsync(characterId, QuestTrigger.EquipmentFound(45, now), Guid.NewGuid(), GameEventTypes.EquipmentFound, default);
        await service.ClaimAllMilestonesAsync(characterId, definition.Id, default);
        Assert.Equal(50, writer.Items.Where(x => x.ItemInstance.ItemBaseId == "soul_dust").Sum(x => x.Quantity));
        Assert.Equal(50, await db.InventoryItems.Where(x => x.InventoryId == characterId && x.ItemInstance.ItemBaseId == "sigil_fragment").SumAsync(x => x.Quantity));

        await service.ProcessAsync(helperId, QuestTrigger.EquipmentFound(950, now), Guid.NewGuid(), GameEventTypes.EquipmentFound, default);
        await service.ClaimAsync(characterId, definition.Id, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAsync(characterId, definition.Id, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAllMilestonesAsync(characterId, definition.Id, default));
        Assert.Equal(2, writer.Items.Where(x => x.ItemInstance.ItemBaseId == RandomEquipmentBoxCatalog.UncommonItemBaseId).Sum(x => x.Quantity));
        Assert.Equal(3, await db.EventQuestMilestoneClaims.CountAsync());

        var lateContributor = Guid.NewGuid();
        CompleteTutorial(db, lateContributor);
        await db.SaveChangesAsync();
        // Each player's HTTP request gets a fresh context, including its filtered claim collection.
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAsync(lateContributor, definition.Id, default));
        await service.ProcessAsync(lateContributor, QuestTrigger.EquipmentFound(1, now), Guid.NewGuid(), GameEventTypes.EquipmentFound, default);
        await service.ClaimAsync(lateContributor, definition.Id, default);
        state = Assert.Single((await service.GetJournalAsync(lateContributor, default)).Events);
        Assert.Equal(1, state.MyContribution);
        Assert.Equal(1000, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal(EventQuestStatus.Completed, state.Status);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(604799, 1)]
    [InlineData(604800, 0)]
    public async Task Equipment_found_uses_the_original_event_time_when_delivery_is_delayed(int seconds, int expected)
    {
        await using var db = CreateDb();
        var definition = TreasureHunt();
        var id = Guid.NewGuid();
        CompleteTutorial(db, id);
        await db.SaveChangesAsync();
        var service = TreasureHuntService(db, definition, definition.EndsAtUtc.AddHours(1), new RecordingLootRewardWriter());
        await service.ProcessAsync(id, QuestTrigger.EquipmentFound(1, definition.StartsAtUtc.AddSeconds(seconds)),
            Guid.NewGuid(), GameEventTypes.EquipmentFound, default);
        var state = Assert.Single((await service.GetJournalAsync(id, default)).Events);
        Assert.Equal(expected, state.MyContribution);
        Assert.Equal(expected, Assert.Single(state.Objectives).CurrentAmount);
    }
}
