using System.Text.Json;
using Application.UseCases.Outbox;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Quests;
using Services.LL.Items;
using Services.LL.Outbox;
using Services.LL.Quests;

namespace EssenceSystem.Tests;

public sealed partial class EquipmentAcquisitionTests
{
    private static PlainEquipmentRecoveryService PlainService(Fixture f) => new(new PlainEquipmentRepository(f.Db), f.Repository,
        new StarterEquipmentRepository(f.Db), Options.Create(f.Flags), TimeProvider.System,
        new GameEventOutbox(f.Db, new GameEventOutboxConsumerRegistry(), new(JsonSerializerDefaults.Web), TimeProvider.System));

    private static EquipmentData PlainAward(Fixture f) => EquipmentData.Create(EquipmentState.Award(
        Guid.NewGuid(), f.Equipment.Evaluator, "plain.dagger", 1, 0,
        new(EquipmentAwardKind.ProtectedReward, "shenic", Guid.NewGuid().ToString()),
        new(EquipmentOwnershipKind.BoundPersonal, f.Id)), f.Equipment.Evaluator);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Earned_plain_recovery_combines_starter_copies_without_duplicate_replacements(bool starterFirst)
    {
        await using var f = await Fixture.Create(false);
        var grant = await f.Baseline(dual: true);
        var award = PlainAward(f);
        await new PlainEquipmentRepository(f.Db).RecordAwardAsync(f.Id, award, Ct);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var service = PlainService(f);
        var option = Assert.Single(await service.GetOptionsAsync(f.Id, Ct));
        Assert.Equal(3, option.Entitled);
        Assert.Equal(3, option.Missing);
        var operation = Guid.NewGuid();
        if (starterFirst) await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), grant.Kind, Ct);
        var result = await service.RecoverAsync(f.Id, operation, "plain.dagger", 1, Ct);
        Assert.Null(result.Error);
        Assert.Equal(starterFirst ? 1 : 3, result.Recovery!.Equipment.Count);
        if (!starterFirst) await f.Service.RecoverAsync(f.Id, Guid.NewGuid(), grant.Kind, Ct);
        await f.Db.SaveChangesAsync();
        Assert.Equal(6, await f.Db.InventoryItems.CountAsync());
        Assert.Equal(3, (await f.Repository.GetOwnedAndPendingAsync(f.Id, Ct)).Count(x => x.State.DefinitionId == "plain.dagger"));
        Assert.Equal(0, Assert.Single(await service.GetOptionsAsync(f.Id, Ct)).Missing);
        Assert.Equal(1000, (await f.Db.Characters.SingleAsync()).Cinders);
        Assert.All(result.Recovery.Equipment, item =>
        {
            Assert.Equal(0, item.State.Rank);
            Assert.Null(item.State.ActiveStyleId);
            Assert.Empty(item.State.Investments);
            Assert.Equal(0, item.EquipmentState.GetSalvageScrap());
            Assert.Equal(EquipmentOwnershipKind.BoundPersonal, item.State.Ownership.Kind);
        });
        var lost = (await f.Db.InventoryItems.Include(x => x.ItemInstance).ToListAsync()).First(x => result.Recovery.Equipment.Any(e => e.State.Id == x.ItemInstanceId));
        f.Db.InventoryItems.Remove(lost);
        f.Db.ItemInstances.Remove(lost.ItemInstance);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var retry = await service.RecoverAsync(f.Id, operation, "plain.dagger", 1, Ct);
        Assert.Equal(result.Recovery.Equipment.Select(x => x.State.Id), retry.Recovery!.Equipment.Select(x => x.State.Id));
        Assert.Equal(5, await f.Db.InventoryItems.CountAsync());
        Assert.NotNull((await service.RecoverAsync(f.Id, operation, "plain.staff", 1, Ct)).Error);
        var replacement = await service.RecoverAsync(f.Id, Guid.NewGuid(), "plain.dagger", 1, Ct);
        Assert.Single(replacement.Recovery!.Equipment);
        await f.Db.SaveChangesAsync();
        Assert.Equal(6, await f.Db.InventoryItems.CountAsync());
        var events = await f.Db.GameEventOutboxMessages.ToListAsync();
        Assert.All(events, x => Assert.Contains(x.EventType, new[] { GameEventTypes.PlainEquipmentRecovered, GameEventTypes.BaselineEquipmentRecovered }));
        var consumer = new QuestGameEventOutboxConsumer(null!, new(JsonSerializerDefaults.Web));
        Assert.False(consumer.CanHandle(GameEventTypes.PlainEquipmentRecovered));
        Assert.True(consumer.CanHandle(GameEventTypes.PlainEquipmentTargetSecured));
    }

    [Fact]
    public async Task EquipmentProgression_equipment_objectives_require_complete_equipped_kit_and_earned_archetype()
    {
        await using var f = await Fixture.Create(false);
        var grant = await f.Baseline(dual: true);
        var support = new EquipmentQuestSupport(new QuestEquipmentRewardRepository(f.Db), new StarterEquipmentRepository(f.Db), new PlainEquipmentRepository(f.Db));
        foreach (var item in grant.Equipment.Skip(1))
        {
            var instance = await f.AddItem(item);
            Assert.True((await new EquipmentSlotRepository(f.Db).EquipEquipmentAsync(f.Id, instance.Id, null, Ct)).Succeeded);
        }
        await f.Db.SaveChangesAsync();
        Assert.False(await support.IsEquippedAsync(f.Id, "ModelEStarterLoadoutEquipped", "FirstWeapon", Ct));
        Assert.False(await support.IsEquippedAsync(f.Id, "ModelEPlainTargetEquipped", null, Ct));
        var last = await f.AddItem(grant.Equipment[0]);
        Assert.True((await new EquipmentSlotRepository(f.Db).EquipEquipmentAsync(f.Id, last.Id, EquipmentSlotType.OffHand, Ct)).Succeeded);
        await f.Db.SaveChangesAsync();
        Assert.True(await support.IsEquippedAsync(f.Id, "ModelEStarterLoadoutEquipped", "FirstWeapon", Ct));
        await new PlainEquipmentRepository(f.Db).RecordAwardAsync(f.Id, PlainAward(f), Ct);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        Assert.True(await support.IsEquippedAsync(f.Id, "ModelEPlainTargetEquipped", null, Ct));
        Assert.False(await support.IsEquippedAsync(f.Id, "ModelEStarterLoadoutEquipped", "ReadyForRoad", Ct));
    }

    [Fact]
    public async Task Unearned_and_disabled_plain_recovery_cannot_award_equipment()
    {
        await using var f = await Fixture.Create(false);
        var service = PlainService(f);
        Assert.NotNull((await service.RecoverAsync(f.Id, Guid.NewGuid(), "plain.dagger", 1, Ct)).Error);
        await new PlainEquipmentRepository(f.Db).RecordAwardAsync(f.Id, PlainAward(f), Ct);
        f.Flags.BaselineRecoveryEnabled = false;
        Assert.Empty(await service.GetOptionsAsync(f.Id, Ct));
        Assert.NotNull((await service.RecoverAsync(f.Id, Guid.NewGuid(), "plain.dagger", 1, Ct)).Error);
        Assert.Empty(f.Db.InventoryItems.Local);
        Assert.Empty(f.Db.PlainEquipmentRecoveryReceipts.Local);
    }
}
