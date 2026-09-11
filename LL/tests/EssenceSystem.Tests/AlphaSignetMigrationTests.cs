using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Nobility;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Persistence.LL;
using Persistence.LL.Migrations;
using Persistence.LL.Repositories.Nobility;
using Services.LL.Nobility;

namespace EssenceSystem.Tests;

public sealed class AlphaSignetMigrationTests
{
    // Supply an isolated local PostgreSQL server with CREATEDB permission. Each case
    // creates and removes its own randomly named database, never the supplied database.
    public sealed class LocalPostgresTheoryAttribute : TheoryAttribute
    {
        public LocalPostgresTheoryAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_SIGNET_TEST_POSTGRES")))
                Skip = "Set LL_SIGNET_TEST_POSTGRES to an isolated local PostgreSQL test server.";
        }
    }

    [LocalPostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_characters_receive_one_usable_signet_once(bool hasEarlierGrant)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LL_SIGNET_TEST_POSTGRES"));
        Assert.Contains(connection.Host, new[] { "127.0.0.1", "localhost", "::1" });
        connection.Database = "ll_signet_migration_test_" + Guid.NewGuid().ToString("N");
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>().UseNpgsql(connection.ConnectionString).Options);
        try
        {
            // The follow-up migration changes data/discriminator mapping, not table layout.
            // Baseline the current schema; every migration except the grant is already
            // represented by EnsureCreated, including later column removals.
            await db.Database.EnsureCreatedAsync();
            var history = db.GetService<IHistoryRepository>();
            await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
            var migrations = db.Database.GetMigrations().ToArray();
            var grantMigration = Assert.Single(migrations, x => x.EndsWith("_GrantAlphaSignetToExistingCharacters"));
            foreach (var migration in migrations.Where(x => x != grantMigration))
                await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, "10.0.0")));

            var registered = new Character { Id = Guid.NewGuid(), Name = "Registered", User = AppUser.Register("tester", "tester@example.com", "hash") };
            registered.Inventory = new Inventory { Character = registered, CharacterId = registered.Id };
            var guest = new Character { Id = Guid.NewGuid(), Name = "Guest", User = AppUser.Guest() };
            db.Characters.AddRange(registered, guest);
            db.Add(new Creature { Id = Guid.NewGuid(), Name = "Creature" });
            db.Add(AppUser.Register("empty", "empty@example.com", "hash"));
            if (hasEarlierGrant)
                db.ItemBases.Add(new ItemBase { Id = "signet", Name = "Old Signet", ItemType = ItemType.Resource, Stackable = true });
            await db.SaveChangesAsync();

            var service = new NobilityService(new NobilityRepository(db), TimeProvider.System);
            if (hasEarlierGrant)
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                Assert.True((await service.GrantAlphaAsync("tester", registered.Id, Guid.NewGuid(), 2, "Earlier alpha grant", default)).IsSuccess);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            db.ChangeTracker.Clear();
            await db.Database.MigrateAsync();
            var signet = Assert.IsType<MiscItemBase>(await db.ItemBases.SingleAsync(x => x.Id == "signet"));
            Assert.Equal(ItemType.Misc, signet.ItemType);
            Assert.True(signet.Stackable);
            Assert.False(signet.IsBound);
            var gifts = await db.Set<SignetIssuance>().Where(x => x.ActorSubject == "migration:alpha-signet").ToListAsync();
            Assert.Equal(2, gifts.Count);
            Assert.All(gifts, gift => { Assert.Equal(1, gift.Quantity); Assert.Equal(SignetOrigin.AlphaGrant, gift.Origin); });
            Assert.Equal(2, await db.Set<SignetMovement>().CountAsync(x => gifts.Select(g => g.Id).Contains(x.OperationId)));
            Assert.Empty(await db.Set<NobilityMembership>().ToListAsync());
            Assert.Empty(await db.Set<NobilityDailyGrant>().ToListAsync());
            Assert.Equal(hasEarlierGrant ? 3 : 1, await AvailableInventory(db, registered.Id));
            Assert.Equal(1, await AvailableInventory(db, guest.Id));
            Assert.False((await service.GetStatusAsync(registered.UserId, registered.Id, default)).HasSupportHistory);
            Assert.False((await service.PreviewAsync(guest.UserId, guest.Id, 1, default)).IsSuccess);

            // The actual gift can activate membership through the production service.
            var quantity = hasEarlierGrant ? 3 : 1;
            var preview = (await service.PreviewAsync(registered.UserId, registered.Id, quantity, default)).Data!;
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                Assert.True((await service.RedeemAsync(registered.UserId, registered.Id, Guid.NewGuid(),
                    preview.MembershipVersion, preview.UnitIds, DateOnly.FromDateTime(preview.ExpiresAt.UtcDateTime), default)).IsSuccess);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            // Even manually replaying the grant SQL cannot mint a replacement for spent units.
            db.ChangeTracker.Clear();
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                foreach (var sql in new GrantAlphaSignetToExistingCharacters().UpOperations.Cast<SqlOperation>())
                    await db.Database.ExecuteSqlRawAsync(sql.Sql);
                await transaction.CommitAsync();
            }
            Assert.Equal(0, await AvailableInventory(db, registered.Id));
            Assert.Equal(1, await AvailableInventory(db, guest.Id));
            Assert.Equal(quantity, await db.Set<SignetUnit>().CountAsync(x => x.State == SignetState.Redeemed));
            Assert.Equal(2, await db.Set<SignetIssuance>().CountAsync(x => x.ActorSubject == "migration:alpha-signet"));
            Assert.Single(await db.Set<NobilityMembership>().ToListAsync());

            // Normal startup applies this migration only once, including after new players join.
            var newcomer = new Character { Id = Guid.NewGuid(), Name = "Newcomer", User = AppUser.Guest() };
            db.Characters.Add(newcomer);
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            Assert.False(await db.Set<SignetUnit>().AnyAsync(x => x.OwnerCharacterId == newcomer.Id));
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<int> AvailableInventory(LLDbContext db, Guid characterId)
    {
        var rows = await db.InventoryItems.Where(x => x.InventoryId == characterId && x.ItemInstance.ItemBaseId == "signet").ToListAsync();
        Assert.True(rows.Count <= 1, "Available Signets should share one inventory stack.");
        var quantity = rows.Sum(x => x.Quantity);
        Assert.Equal(await db.Set<SignetUnit>().CountAsync(x => x.OwnerCharacterId == characterId && x.State == SignetState.Available), quantity);
        return quantity;
    }
}
