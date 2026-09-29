using API.LiveOps.Hosting;
using Application.UseCases.Administration.Commands.GrantCompensationPackage;
using Application.UseCases.Administration.Commands.SaveCompensationPackage;
using Application.UseCases.Outbox;
using Domain.Models.Administration;
using Domain.Models.Inventories;
using Domain.Models.Items;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Persistence.LL;
using Persistence.LL.Migrations;
using Services.LL;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

// Opt in only against a disposable local server with CREATEDB permission. Each
// test creates and drops its own randomly named database, never the supplied one.
public sealed class LiveOpsLocalPostgresAttribute : FactAttribute
{
    public LiveOpsLocalPostgresAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_LIVEOPS_TEST_POSTGRES")))
            Skip = "Set LL_LIVEOPS_TEST_POSTGRES to a disposable local PostgreSQL server.";
    }
}

public sealed partial class LiveOpsAdministrationTests
{
    [LiveOpsLocalPostgres]
    public async Task LiveOps_Postgres_migration_upgrades_the_previous_schema_and_preserves_existing_rows()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync();
        await using var db = fixture.CreateContext();
        var (accountId, characterId) = AddPlayer(db); await db.SaveChangesAsync();

        // EnsureCreated provides all historical schema, then remove only this
        // feature's empty tables to exercise its real upgrade from the prior version.
        var sql = db.GetService<IMigrationsSqlGenerator>();
        foreach (var command in sql.Generate(new ImproveLiveOpsAdministration().DownOperations))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var history = db.GetService<IHistoryRepository>();
        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        var migrationId = Assert.Single(db.Database.GetMigrations(), x => x.EndsWith("_ImproveLiveOpsAdministration"));
        foreach (var migration in db.Database.GetMigrations().Where(x => x != migrationId && !x.EndsWith("_ScheduleSupportCaseFollowUps")))
            await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, "10.0.8")));
        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.True(await db.Characters.AnyAsync(x => x.Id == characterId && x.UserId == accountId));
        var id = Guid.NewGuid();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Assert.True((await Cases(db).ApplyAsync(new(id, id, characterId, 0, SupportCaseEntryKind.Created,
            "Preserved player", "Other", "Upgrade support case"), SupportActor, default)).IsSuccess);
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        Assert.Single(await db.Set<SupportCaseEntry>().ToListAsync());
        Assert.Empty(await db.Set<CompensationPackageVersion>().ToListAsync());
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_Postgres_case_locks_reject_stale_edits_and_replay_concurrent_duplicates()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync();
        Guid characterId;
        await using (var db = fixture.CreateContext())
        { (_, characterId) = AddPlayer(db); await db.SaveChangesAsync(); }
        var id = Guid.NewGuid();
        await Apply(new(id, id, characterId, 0, SupportCaseEntryKind.Created, "Initial evidence", "Other", "Concurrent support case"));

        var writes = await Task.WhenAll(
            Apply(new(Guid.NewGuid(), id, Guid.Empty, 1, SupportCaseEntryKind.Note, "First operator")),
            Apply(new(Guid.NewGuid(), id, Guid.Empty, 1, SupportCaseEntryKind.Note, "Second operator")));
        Assert.Single(writes, x => x.IsSuccess); Assert.Single(writes, x => x.IsConflict);
        var retry = new SupportCaseChange(Guid.NewGuid(), id, Guid.Empty, 2, SupportCaseEntryKind.Note, "Unchanged retry");
        var duplicates = await Task.WhenAll(Apply(retry), Apply(retry));
        Assert.All(duplicates, x => Assert.True(x.IsSuccess));
        await using var verify = fixture.CreateContext();
        Assert.Equal(3, (await verify.Set<SupportCase>().SingleAsync()).Version);
        Assert.Equal(3, await verify.Set<SupportCaseEntry>().CountAsync());
        Assert.Equal(3, await verify.AdminActions.CountAsync());

        async Task<Common.Primitives.Response<SupportCaseDetails>> Apply(SupportCaseChange change)
        {
            await using var db = fixture.CreateContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var result = await Cases(db).ApplyAsync(change, SupportActor, default);
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return result;
        }
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_Postgres_package_pipeline_rolls_back_partial_failure_and_replays_once()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync();
        Guid characterId;
        var failedOperation = Guid.NewGuid(); var packageId = Guid.NewGuid();
        await using (var db = fixture.CreateContext())
        {
            (_, characterId) = AddPlayer(db); db.Inventories.Add(new Inventory { CharacterId = characterId });
            db.ItemBases.Add(new MiscItemBase { Id = "liveops-test-potion", Name = "Potion", Description = "Test", ItemType = ItemType.Misc, Stackable = true });
            db.AdminActions.Add(new AdminAction { Id = CompensationPackageService.ChildOperation(failedOperation, 1),
                ActionType = AdminActionType.AccountBanned, Permission = "test", ActorSubject = "another", ActorDisplayName = "Another",
                Reason = "Deliberately conflicting child operation", OccurredAt = Now });
            await db.SaveChangesAsync();
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LegendsLegacyDB"] = fixture.ConnectionString,
            ["Database:TimeoutInSeconds"] = "30"
        }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddPersistence(config); services.AddRepositories();
        services.AddLiveOpsApplication(); services.AddLiveOpsServices(config);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var scope = provider.CreateAsyncScope())
        {
            var saved = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new SaveCompensationPackageCommand(
                new(Guid.NewGuid(), packageId, 0, characterId, "Replacement", "Verified support issue", false,
                    [new("liveops-test-potion", 1), new("liveops-test-potion", 2)]), SupportActor));
            Assert.True(saved.IsSuccess, saved.ErrorMessage);
        }
        await using (var scope = provider.CreateAsyncScope())
            await Assert.ThrowsAsync<CompensationPackageConflictException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Send(
                new GrantCompensationPackageCommand(failedOperation, characterId, packageId, 1, SupportActor, "Case", null)));
        await using (var db = fixture.CreateContext())
        {
            Assert.Empty(await db.InventoryItems.ToListAsync());
            Assert.False(await db.AdminActions.AnyAsync(x => x.Id == CompensationPackageService.ChildOperation(failedOperation, 0)));
            Assert.False(await db.AdminActions.AnyAsync(x => x.Id == failedOperation));
            Assert.Empty(await db.GameEventOutboxMessages.ToListAsync());
        }
        var operationId = Guid.NewGuid();
        var results = await Task.WhenAll(Grant(), Grant());
        Assert.All(results, x => Assert.True(x.IsSuccess, x.ErrorMessage));
        Assert.Single(results, x => x.Data!.WasAlreadyProcessed);
        await using var verify = fixture.CreateContext();
        Assert.Equal(3, (await verify.InventoryItems.SingleAsync()).Quantity);
        Assert.Single(await verify.AdminActions.Where(x => x.Id == operationId).ToListAsync());
        Assert.Single(await verify.GameEventOutboxMessages.Where(x => x.EventType == GameEventTypes.InventoryItemsGranted).ToListAsync());

        async Task<Common.Primitives.Response<Application.UseCases.Administration.Dtos.CompensationPackageReceiptDto>> Grant()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(
                new GrantCompensationPackageCommand(operationId, characterId, packageId, 1, SupportActor, "Case", null));
        }
    }

    private sealed class LiveOpsPostgresDatabase : IAsyncDisposable
    {
        public required string ConnectionString { get; init; }
        public LLDbContext CreateContext() => new(new DbContextOptionsBuilder<LLDbContext>().UseNpgsql(ConnectionString).Options);
        public static async Task<LiveOpsPostgresDatabase> CreateAsync()
        {
            var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LL_LIVEOPS_TEST_POSTGRES"));
            Assert.Contains(connection.Host, new[] { "127.0.0.1", "localhost", "::1" });
            connection.Database = "ll_liveops_test_" + Guid.NewGuid().ToString("N");
            var fixture = new LiveOpsPostgresDatabase { ConnectionString = connection.ConnectionString };
            try { await using var db = fixture.CreateContext(); await db.Database.EnsureCreatedAsync(); return fixture; }
            catch { await fixture.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync()
        {
            var connection = new NpgsqlConnectionStringBuilder(ConnectionString);
            Assert.StartsWith("ll_liveops_test_", connection.Database, StringComparison.Ordinal);
            await using var db = CreateContext(); await db.Database.EnsureDeletedAsync();
        }
    }
}
