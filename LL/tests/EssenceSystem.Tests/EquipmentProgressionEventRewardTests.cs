using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.Quests;
using Services.LL.Quests.Events;

namespace EssenceSystem.Tests;

public sealed partial class EventQuestSystemTests
{
    private static EventQuestService EquipmentProgressionEventService(LLDbContext db, EventQuestDefinition definition,
        RecordingLootRewardWriter? writer = null, bool missingItems = false) => new(
        new EventQuestRepository(db), new QuestRepository(db), new StubDefinitionProvider(definition),
        missingItems ? new ItemBaseRepository(db) : new StubItemBaseRepository(),
        new RecordingInventoryItemFactory(), writer ?? new RecordingLootRewardWriter(),
        new FixedTimeProvider(Now), new RecordingPublisher(), new RecordingGameEventOutbox());

    [Fact]
    public async Task EquipmentProgression_shared_event_counts_victories_with_existing_identity_and_deduplicates()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(100);
        definition.Objectives[0].Type = "CombatEncounterCompleted";
        var frozen = JsonSerializer.Serialize(definition);
        var id = Guid.NewGuid();
        CompleteTutorial(db, id);
        await db.SaveChangesAsync();
        var service = EquipmentProgressionEventService(db, definition);
        var messageId = Guid.NewGuid();
        var batch = QuestTrigger.CombatCompleted("region_01_area_01", true, actionCount: 10, winningEncounterCount: 4, gatheredResourceCount: 99);
        await service.ProcessAsync(id, batch, messageId, "IdleCombatEncounterCompleted", default);
        await service.ProcessAsync(id, batch, messageId, "IdleCombatEncounterCompleted", default);
        await service.ProcessAsync(id, QuestTrigger.CombatCompleted("region_01_area_01", false, winningEncounterCount: 0), Guid.NewGuid(), "IdleCombatEncounterCompleted", default);
        var state = Assert.Single((await service.GetJournalAsync(id, default)).Events);
        Assert.Equal(4, state.MyContribution);
        Assert.Equal(4, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal("CombatEncounterCompleted", Assert.Single(state.Objectives).Type);
        Assert.Equal(1, await db.EventQuestEventLedgers.CountAsync());
        Assert.Equal(frozen, JsonSerializer.Serialize(definition));
        var legacyState = Assert.Single((await CreateService(db, definition, new RecordingPublisher()).GetJournalAsync(id, default)).Events);
        Assert.Equal("CombatEncounterCompleted", Assert.Single(legacyState.Objectives).Type);
        Assert.Equal(4, legacyState.MyContribution);
    }

    [Fact]
    public async Task Contributors_share_one_event_counter()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(100);
        definition.Objectives[0].Type = "CombatEncounterCompleted";
        var legacy = Guid.NewGuid();
        var modern = Guid.NewGuid();
        CompleteTutorial(db, legacy);
        CompleteTutorial(db, modern);
        await db.SaveChangesAsync();
        await CreateService(db, definition, new RecordingPublisher()).ProcessAsync(legacy,
            QuestTrigger.CombatCompleted("region_01_area_01", true, winningEncounterCount: 7), Guid.NewGuid(), "IdleCombatEncounterCompleted", default);
        var service = EquipmentProgressionEventService(db, definition);
        await service.ProcessAsync(modern, QuestTrigger.CombatCompleted("region_01_area_01", true, winningEncounterCount: 3), Guid.NewGuid(), "IdleCombatEncounterCompleted", default);
        var state = Assert.Single((await service.GetJournalAsync(modern, default)).Events);
        Assert.Equal(10, Assert.Single(state.Objectives).CurrentAmount);
        Assert.Equal(3, state.MyContribution);
        Assert.Single(await db.EventQuestInstances.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EquipmentProgression_milestone_preview_and_individual_or_bulk_claims_agree(bool claimAll)
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(1);
        foreach (var milestone in definition.PersonalMilestones) { milestone.Rewards[0].ItemBaseId = "item.monster_core.lesser"; milestone.Rewards[0].Quantity = 1; }
        var id = Guid.NewGuid();
        CompleteTutorial(db, id);
        await db.SaveChangesAsync();
        var writer = new RecordingLootRewardWriter();
        var service = EquipmentProgressionEventService(db, definition, writer);
        await service.ProcessAsync(id, QuestTrigger.CombatCompleted("region_01_area_01", true, winningEncounterCount: 3), Guid.NewGuid(), "IdleCombatEncounterCompleted", default);
        var preview = Assert.Single((await service.GetJournalAsync(id, default)).Events);
        Assert.All(preview.PersonalMilestones, x => Assert.Equal("item.monster_core.lesser", Assert.Single(x.Rewards).ItemBaseId));
        if (claimAll) await service.ClaimAllMilestonesAsync(id, definition.Id, default);
        else foreach (var milestone in definition.PersonalMilestones) await service.ClaimMilestoneAsync(id, definition.Id, milestone.Key, default);
        Assert.Equal(2, writer.Items.Sum(x => x.Quantity));
        Assert.All(writer.Items, x => Assert.Equal("item.monster_core.lesser", x.ItemInstance.ItemBaseId));
        Assert.Equal(2, await db.EventQuestMilestoneClaims.CountAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAllMilestonesAsync(id, definition.Id, default));
        Assert.Equal(2, writer.Items.Sum(x => x.Quantity));
    }

    [Fact]
    public async Task EquipmentProgression_event_completion_grants_item_currency_and_claim_once()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(1);
        definition.Rewards = [new() { Key = "core", Type = "Item", ItemBaseId = "item.monster_core.lesser", Quantity = 2 },
            new() { Key = "fragments", Type = "SigilFragments", Quantity = 5 }];
        var id = Guid.NewGuid();
        db.Characters.Add(new Character { Id = id, Name = "Event hero" });
        CompleteTutorial(db, id);
        await db.SaveChangesAsync();
        var writer = new RecordingLootRewardWriter();
        var service = EquipmentProgressionEventService(db, definition, writer);
        await service.ProcessAsync(id, QuestTrigger.CombatCompleted("region_01_area_01", true), Guid.NewGuid(), "IdleCombatEncounterCompleted", default);
        var preview = Assert.Single((await service.GetJournalAsync(id, default)).Events);
        Assert.Contains(preview.Rewards, x => x.ItemBaseId == "item.monster_core.lesser" && x.Quantity == 2);
        await service.ClaimAsync(id, definition.Id, default);
        Assert.Equal(2, Assert.Single(writer.Items).Quantity);
        Assert.Equal(5, (await db.Characters.SingleAsync()).SigilFragments);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAsync(id, definition.Id, default));
        Assert.Single(writer.Items);
    }

    [Fact]
    public async Task EquipmentProgression_missing_reward_definition_leaves_milestone_receipts_and_currency_untouched()
    {
        await using var db = CreateDb();
        var definition = CreateActiveDefinition(1);
        var id = Guid.NewGuid();
        CompleteTutorial(db, id);
        await db.SaveChangesAsync();
        var writer = new RecordingLootRewardWriter();
        var service = EquipmentProgressionEventService(db, definition, writer, missingItems: true);
        await service.ProcessAsync(id, QuestTrigger.CombatCompleted("region_01_area_01", true, winningEncounterCount: 3), Guid.NewGuid(), "IdleCombatEncounterCompleted", default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ClaimAllMilestonesAsync(id, definition.Id, default));
        Assert.Empty(db.EventQuestMilestoneClaims.Local);
        Assert.Empty(writer.Items);
    }
}
