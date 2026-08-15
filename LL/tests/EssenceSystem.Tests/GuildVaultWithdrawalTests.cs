using Domain.Models.Entities.Characters;
using Domain.Models.Economy;
using Domain.Models.Guilds;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Economy;
using Services.LL.Guilds;

public sealed class GuildVaultWithdrawalTests
{
    [Fact]
    public async Task WithdrawAsync_AllowsLeaderAndTransfersEquipmentToInventory()
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, GuildRole.Leader, canWithdrawVault: false);
        var service = CreateService(db);

        var result = await service.WithdrawAsync(seeded.CharacterId, seeded.VaultItemId, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("Vault Keeper", result.Value!.CharacterName);
        Assert.Equal(seeded.EquipmentInstanceId, result.Value.Equipment.Id);
        Assert.False(await db.GuildVaultItems.AnyAsync(x => x.Id == seeded.VaultItemId));
        Assert.True(await db.InventoryItems.AnyAsync(x =>
            x.InventoryId == seeded.CharacterId && x.ItemInstanceId == seeded.EquipmentInstanceId));
        var ledgerEntry = await db.EconomyLedger.SingleAsync();
        Assert.Equal(EconomyEventType.GuildVaultWithdrawal, ledgerEntry.EventType);
        Assert.Equal(seeded.CharacterId, ledgerEntry.RecipientCharacterId);
        Assert.Equal(seeded.EquipmentInstanceId, ledgerEntry.DestinationItemInstanceId);
    }

    [Fact]
    public async Task DonateAsync_ReturnsEquipmentAndActorForGuildChatMessage()
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, GuildRole.Member, canWithdrawVault: false);
        var vaultItem = await db.GuildVaultItems.SingleAsync(x => x.Id == seeded.VaultItemId);
        db.GuildVaultItems.Remove(vaultItem);
        db.InventoryItems.Add(new InventoryItem
        {
            InventoryId = seeded.CharacterId,
            ItemInstanceId = seeded.EquipmentInstanceId,
            Quantity = 1
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);

        var result = await service.DonateAsync(
            seeded.CharacterId,
            seeded.EquipmentInstanceId,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Vault Keeper", result.Value!.CharacterName);
        Assert.Equal(seeded.EquipmentInstanceId, result.Value.Equipment.Id);
    }

    [Fact]
    public async Task DonateAsync_RejectsStaleEquippedReference()
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, GuildRole.Member, canWithdrawVault: false);
        var vaultItem = await db.GuildVaultItems.SingleAsync(x => x.Id == seeded.VaultItemId);
        db.GuildVaultItems.Remove(vaultItem);
        db.InventoryItems.Add(new InventoryItem
        {
            InventoryId = seeded.CharacterId,
            ItemInstanceId = seeded.EquipmentInstanceId,
            Quantity = 1
        });
        db.EquipmentSlots.Add(new EquipmentSlot
        {
            EntityId = seeded.CharacterId,
            EquipmentSlotType = EquipmentSlotType.Head,
            EquipmentInstanceId = seeded.EquipmentInstanceId
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);

        var result = await service.DonateAsync(
            seeded.CharacterId,
            seeded.EquipmentInstanceId,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Equipped equipment must be unequipped before it can be donated.",
            result.Error);
    }

    [Fact]
    public async Task WithdrawAsync_RepairsStaleInventoryAndLoadoutReferences()
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, GuildRole.Leader, canWithdrawVault: true);
        var staleHolderId = Guid.NewGuid();
        db.InventoryItems.Add(new InventoryItem
        {
            InventoryId = staleHolderId,
            ItemInstanceId = seeded.EquipmentInstanceId,
            Quantity = 1
        });
        db.EquipmentSlots.Add(new EquipmentSlot
        {
            EntityId = staleHolderId,
            EquipmentSlotType = EquipmentSlotType.Head,
            EquipmentInstanceId = seeded.EquipmentInstanceId
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateService(db);

        var result = await service.WithdrawAsync(
            seeded.CharacterId,
            seeded.VaultItemId,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.Succeeded);
        var inventoryItem = Assert.Single(await db.InventoryItems
            .Where(x => x.ItemInstanceId == seeded.EquipmentInstanceId)
            .ToListAsync());
        Assert.Equal(seeded.CharacterId, inventoryItem.InventoryId);
        Assert.False(await db.EquipmentSlots.AnyAsync(
            x => x.EquipmentInstanceId == seeded.EquipmentInstanceId));
    }

    [Fact]
    public async Task WithdrawAsync_AllowsOfficerWhenPermissionIsEnabled()
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, GuildRole.Officer, canWithdrawVault: true);
        var service = CreateService(db);

        var result = await service.WithdrawAsync(seeded.CharacterId, seeded.VaultItemId, CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(GuildRole.Officer)]
    [InlineData(GuildRole.Member)]
    public async Task WithdrawAsync_RejectsRoleWithoutWithdrawalAuthority(GuildRole role)
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, role, canWithdrawVault: role == GuildRole.Member);
        var service = CreateService(db);

        var result = await service.WithdrawAsync(seeded.CharacterId, seeded.VaultItemId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Your guild role cannot withdraw vault equipment.", result.Error);
    }

    [Fact]
    public async Task WithdrawAsync_RejectsBorrowedEquipment()
    {
        await using var db = CreateDb();
        var seeded = await SeedVaultAsync(db, GuildRole.Leader, canWithdrawVault: true, borrowed: true);
        var service = CreateService(db);

        var result = await service.WithdrawAsync(seeded.CharacterId, seeded.VaultItemId, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Borrowed equipment must be returned before it can be withdrawn.", result.Error);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static GuildVaultService CreateService(LLDbContext db) =>
        new(db, new EconomyLedgerRepository(db));

    private static async Task<SeededVault> SeedVaultAsync(
        LLDbContext db,
        GuildRole role,
        bool canWithdrawVault,
        bool borrowed = false)
    {
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var equipmentInstanceId = Guid.NewGuid();
        var vaultItemId = Guid.NewGuid();
        var character = new Character
        {
            Id = characterId,
            Name = "Vault Keeper",
            NormalizedName = "VAULT KEEPER"
        };
        var guild = new Guild
        {
            Id = guildId,
            Name = "Test Guild",
            OwnerId = characterId,
            Owner = character
        };
        guild.RolePermissions.Add(new GuildRolePermission
        {
            GuildId = guildId,
            Guild = guild,
            Role = role,
            CanWithdrawVault = canWithdrawVault
        });
        guild.Members.Add(new GuildMember
        {
            GuildId = guildId,
            Guild = guild,
            CharacterId = characterId,
            Character = character,
            Role = role
        });

        var equipmentBase = new EquipmentBase
        {
            Id = $"test-equipment-{Guid.NewGuid():N}",
            Name = "Test Equipment",
            EquipmentType = EquipmentType.Head
        };
        var equipment = new EquipmentInstance
        {
            Id = equipmentInstanceId,
            ItemBaseId = equipmentBase.Id,
            ItemBase = equipmentBase
        };
        var vaultItem = new GuildVaultItem
        {
            Id = vaultItemId,
            GuildId = guildId,
            Guild = guild,
            EquipmentInstanceId = equipmentInstanceId,
            EquipmentInstance = equipment,
            DonatedByCharacterId = characterId,
            BorrowedByCharacterId = borrowed ? characterId : null
        };
        guild.VaultItems.Add(vaultItem);

        db.Guilds.Add(guild);
        db.ItemBases.Add(equipmentBase);
        db.ItemInstances.Add(equipment);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return new SeededVault(characterId, equipmentInstanceId, vaultItemId);
    }

    private sealed record SeededVault(Guid CharacterId, Guid EquipmentInstanceId, Guid VaultItemId);
}
