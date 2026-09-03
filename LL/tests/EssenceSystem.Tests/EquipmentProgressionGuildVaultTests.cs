using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using EssenceSystem.Tests;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Guilds;
using Services.LL.Items;

public sealed partial class GuildVaultWithdrawalTests
{
    [Fact]
    public async Task EquipmentProgression_donation_loan_equip_return_and_reborrow_preserve_permanent_identity()
    {
        await using var db = CreateDb();
        var seed = await SeedEquipmentProgressionVaultAsync(db);
        var before = seed.Equipment.ProgressionData!;
        var service = CreateService(db);
        Assert.True((await service.DonateAsync(seed.CharacterId, seed.Equipment.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var vault = await db.GuildVaultItems.Include(x => x.EquipmentInstance).SingleAsync();
        var frozen = vault.EquipmentInstance.ProgressionData!.Serialize();
        Assert.Equal(seed.GuildId, vault.EquipmentInstance.ProgressionData.State.Ownership.OwnerId);
        Assert.Equal(EquipmentOwnershipKind.GuildOwned, vault.EquipmentInstance.ProgressionData.State.Ownership.Kind);
        Assert.Equal(before.Stats, vault.EquipmentInstance.ProgressionData.Stats);
        Assert.Equal(before.State.Rank, vault.EquipmentInstance.ProgressionData.State.Rank);
        Assert.Equal(before.State.Provenance, vault.EquipmentInstance.ProgressionData.State.Provenance);
        Assert.False((await service.WithdrawAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        Assert.True((await service.BorrowAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.False((await service.BorrowAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        Assert.True((await new EquipmentSlotRepository(db).EquipEquipmentAsync(seed.CharacterId, seed.Equipment.Id, EquipmentSlotType.Head, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.Equal(frozen, vault.EquipmentInstance.ProgressionData.Serialize());
        Assert.True((await service.ReturnAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.Empty(await db.InventoryItems.ToListAsync());
        Assert.False(await db.EquipmentSlots.AnyAsync(x => x.EquipmentInstanceId == seed.Equipment.Id));
        Assert.Null(vault.BorrowedByCharacterId);
        Assert.Equal(frozen, vault.EquipmentInstance.ProgressionData.Serialize());
        Assert.True((await service.BorrowAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.Single(await db.InventoryItems.ToListAsync());
        Assert.Equal(4, await db.EconomyLedger.CountAsync());
    }

    [Theory]
    [InlineData("bound")]
    [InlineData("favorite")]
    [InlineData("foreign")]
    public async Task EquipmentProgression_invalid_donation_does_not_move_or_change_equipment(string restriction)
    {
        await using var db = CreateDb();
        var seed = await SeedEquipmentProgressionVaultAsync(db);
        if (restriction == "bound") seed.Equipment.BindEquipmentProgressionForEquip(seed.CharacterId);
        if (restriction == "favorite") (await db.InventoryItems.SingleAsync()).IsFavorite = true;
        if (restriction == "foreign") seed.Equipment.TransferEquipmentProgressionToCharacter(seed.CharacterId, Guid.NewGuid());
        await db.SaveChangesAsync();
        var before = seed.Equipment.ProgressionData!.Serialize();
        Assert.False((await CreateService(db).DonateAsync(seed.CharacterId, seed.Equipment.Id, default)).Succeeded);
        Assert.Equal(before, seed.Equipment.ProgressionData.Serialize());
        Assert.Single(await db.InventoryItems.ToListAsync());
        Assert.Empty(await db.GuildVaultItems.ToListAsync());
        Assert.Empty(await db.EconomyLedger.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EquipmentProgression_equipping_a_stale_loan_requires_current_borrower_and_membership(bool removeMember)
    {
        await using var db = CreateDb();
        var seed = await SeedEquipmentProgressionVaultAsync(db);
        var service = CreateService(db);
        Assert.True((await service.DonateAsync(seed.CharacterId, seed.Equipment.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        var vault = await db.GuildVaultItems.SingleAsync();
        Assert.True((await service.BorrowAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        if (removeMember) db.GuildMembers.Remove(await db.GuildMembers.SingleAsync());
        else vault.BorrowedByCharacterId = Guid.NewGuid();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.False((await new EquipmentSlotRepository(db).EquipEquipmentAsync(seed.CharacterId, seed.Equipment.Id, EquipmentSlotType.Head, default)).Succeeded);
        Assert.False(await db.EquipmentSlots.AnyAsync(x => x.EquipmentInstanceId == seed.Equipment.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EquipmentProgression_leaving_or_being_kicked_returns_equipped_property(bool kick)
    {
        await using var db = CreateDb();
        var seed = await SeedEquipmentProgressionVaultAsync(db);
        var service = CreateService(db);
        Assert.True((await service.DonateAsync(seed.CharacterId, seed.Equipment.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        var vault = await db.GuildVaultItems.Include(x => x.EquipmentInstance).SingleAsync();
        Assert.True((await service.BorrowAsync(seed.CharacterId, vault.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        Assert.True((await new EquipmentSlotRepository(db).EquipEquipmentAsync(seed.CharacterId, seed.Equipment.Id, EquipmentSlotType.Head, default)).Succeeded);
        (await db.GuildMembers.SingleAsync()).Role = GuildRole.Member;
        await db.SaveChangesAsync();
        var frozen = vault.EquipmentInstance.ProgressionData!.Serialize();
        var repository = new GuildRepository(db);
        Assert.True(kick ? await repository.KickMemberAsync(seed.GuildId, seed.CharacterId, default)
            : await repository.LeaveGuildAsync(seed.CharacterId, default));
        await db.SaveChangesAsync();
        Assert.Null(vault.BorrowedByCharacterId);
        Assert.Equal(frozen, vault.EquipmentInstance.ProgressionData.Serialize());
        Assert.Empty(await db.InventoryItems.ToListAsync());
        Assert.False(await db.EquipmentSlots.AnyAsync(x => x.EquipmentInstanceId == seed.Equipment.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EquipmentProgression_disband_retires_available_and_equipped_property_without_personal_rewards(bool equipped)
    {
        await using var db = CreateDb();
        var seed = await SeedEquipmentProgressionVaultAsync(db);
        var service = CreateService(db);
        Assert.True((await service.DonateAsync(seed.CharacterId, seed.Equipment.Id, default)).Succeeded);
        await db.SaveChangesAsync();
        if (equipped)
        {
            Assert.True((await service.BorrowAsync(seed.CharacterId, (await db.GuildVaultItems.SingleAsync()).Id, default)).Succeeded);
            await db.SaveChangesAsync();
            Assert.True((await new EquipmentSlotRepository(db).EquipEquipmentAsync(seed.CharacterId, seed.Equipment.Id, EquipmentSlotType.Head, default)).Succeeded);
            await db.SaveChangesAsync();
        }
        Assert.True(await new GuildRepository(db).DisbandGuildAsync(seed.CharacterId, default));
        await db.SaveChangesAsync();
        Assert.Empty(await db.InventoryItems.ToListAsync());
        Assert.False(await db.ItemInstances.AnyAsync(x => x.Id == seed.Equipment.Id));
        Assert.False(await db.EquipmentSlots.AnyAsync(x => x.EquipmentInstanceId == seed.Equipment.Id));
    }

    private static async Task<(Guid CharacterId, Guid GuildId, EquipmentInstance Equipment)> SeedEquipmentProgressionVaultAsync(LLDbContext db)
    {
        var seeded = await SeedVaultAsync(db, GuildRole.Leader, true);
        var vault = await db.GuildVaultItems.Include(x => x.EquipmentInstance).SingleAsync();
        var equipment = vault.EquipmentInstance;
        var guildId = vault.GuildId;
        db.GuildVaultItems.Remove(vault);
        var catalog = JsonStarterEquipmentCatalog.Load(Path.Combine(EquipmentProgressionSharedContentTests.ApiRoot(), "Data/equipment/equipment-starters.v1.json"));
        var data = EquipmentData.Create(EquipmentState.Award(equipment.Id, catalog.Evaluator,
            "plain.heavy_helm", 1, 3, new(EquipmentAwardKind.RandomDiscovery, "test", "guild-drop"),
            new(EquipmentOwnershipKind.UnboundPersonal, seeded.CharacterId)), catalog.Evaluator);
        var itemBase = new EquipmentBase { Id = data.ItemBaseId, Name = data.DisplayName, EquipmentType = data.EquipmentType };
        db.ItemBases.Add(itemBase);
        equipment.ItemBase = itemBase;
        equipment.ItemBaseId = itemBase.Id;
        equipment.ApplyProgressionData(data);
        var character = await db.Characters.SingleAsync();
        character.Inventory = new Inventory { CharacterId = character.Id };
        character.EquipmentSlots = [new EquipmentSlot { EntityId = character.Id, EquipmentSlotType = EquipmentSlotType.Head }];
        (await db.GuildRolePermissions.SingleAsync()).CanBorrowVault = true;
        db.InventoryItems.Add(new InventoryItem { InventoryId = character.Id, ItemInstanceId = equipment.Id, Quantity = 1 });
        await db.SaveChangesAsync();
        return (character.Id, guildId, equipment);
    }
}
