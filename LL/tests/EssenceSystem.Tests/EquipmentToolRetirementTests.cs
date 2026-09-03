using Application.Interfaces.Services.LL.Items;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentToolRetirementTests
{
    [Theory]
    [InlineData(true, null, false, EquipmentType.Tool)]
    [InlineData(true, EquipmentSlotType.Tool, false, EquipmentType.Tool)]
    [InlineData(true, EquipmentSlotType.MainHand, false, EquipmentType.Tool)]
    [InlineData(false, null, false, EquipmentType.Tool)]
    [InlineData(true, null, false, EquipmentType.Head)]
    [InlineData(true, EquipmentSlotType.Tool, false, EquipmentType.Head)]
    public async Task Tool_guard_checks_the_item_even_when_slot_is_omitted_or_forged(bool modern, EquipmentSlotType? slot, bool allowed, EquipmentType type)
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var characterId = Guid.NewGuid();
        var tool = new EquipmentInstance { Id = Guid.NewGuid(), ItemBaseId = "test-equipment", ItemBase = new EquipmentBase { Id = "test-equipment", EquipmentType = type } };
        db.InventoryItems.Add(new() { InventoryId = characterId, ItemInstanceId = tool.Id, ItemInstance = tool });
        await db.SaveChangesAsync();
        var repository = new Slots();
        var service = new EquipmentSlotService(repository, new InventoryRepository(db));
        var result = await service.EquipEquipmentAsync(characterId, tool.Id, slot, default);
        Assert.Equal(allowed, result.Succeeded);
        Assert.Equal(allowed ? 1 : 0, repository.EquipCalls);
        Assert.True(await service.UnequipEquipmentAsync(characterId, EquipmentSlotType.Tool, default));
        Assert.Equal(1, repository.UnequipCalls);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    private sealed class Slots : IEquipmentSlotRepository
    {
        public int EquipCalls, UnequipCalls;
        public Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid id, CancellationToken ct) => Task.FromResult(new List<EquipmentSlot>());
        public Task<EquipmentEquipResult> EquipEquipmentAsync(Guid id, Guid item, EquipmentSlotType? slot, CancellationToken ct)
        { EquipCalls++; return Task.FromResult(EquipmentEquipResult.Success()); }
        public Task<bool> UnequipEquipmentAsync(Guid id, EquipmentSlotType slot, CancellationToken ct)
        { UnequipCalls++; return Task.FromResult(true); }
    }
}
