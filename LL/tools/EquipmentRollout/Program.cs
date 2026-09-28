using System.Text.Json;
using Application;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.CharacterActions.Commands.DeleteCharacterAction;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Npgsql;
using Persistence.LL;
using Services.LL;
using Services.LL.Combat.Engine;
using Services.LL.Items;

// Deliberately a local operator process: no web listener, hosted worker or outbox
// delivery consumer is started. All gameplay mutations use the production services.
if (args.Length % 2 != 0) throw new ArgumentException("Every option requires a value.");
var arguments = Enumerable.Range(0, args.Length / 2).ToDictionary(i => args[i * 2], i => args[i * 2 + 1]);
string Required(string key) => arguments.GetValueOrDefault(key) ?? throw new ArgumentException($"Missing {key}.");
var mode = Required("--mode");
if (mode is not ("settle" or "apply" or "verify" or "resume")) throw new ArgumentException("Invalid mode.");
var apiRoot = Path.GetFullPath(Required("--api-root"));
var evidenceRoot = Path.GetFullPath(Required("--evidence"));
var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("LL_LOCAL_ROLLOUT_CONNECTION"));
if (connection.Host is not ("localhost" or "127.0.0.1" or "::1")
    || connection.Database != Required("--database") || connection.Port != int.Parse(Required("--port")))
    throw new InvalidOperationException("The connection must match the explicitly selected local database and port.");
connection.ApplicationName = "LL equipment local rollout";
if (!File.Exists(Required("--backup")) || new FileInfo(Required("--backup")).Length == 0)
    throw new InvalidOperationException("A local backup is required before this operator workflow.");
Directory.CreateDirectory(evidenceRoot);
var json = new JsonSerializerOptions { WriteIndented = true };
void Save<T>(string name, T value)
{
    var destination = Path.Combine(evidenceRoot, name);
    File.WriteAllText(destination + ".writing", JsonSerializer.Serialize(value, json));
    File.Move(destination + ".writing", destination, true);
}
T? Read<T>(string name) => File.Exists(Path.Combine(evidenceRoot, name))
    ? JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(evidenceRoot, name))) : default;
var stamp = Read<RunStamp>("run.json") ?? new(connection.Database!, connection.Port, DateTimeOffset.UtcNow);
if (stamp.Database != connection.Database || stamp.Port != connection.Port)
    throw new InvalidOperationException("Evidence belongs to a different database.");
Save("run.json", stamp);
var modern = mode is "verify" or "resume";
var config = new ConfigurationBuilder().SetBasePath(apiRoot).AddJsonFile("appsettings.json")
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:LegendsLegacyDB"] = connection.ConnectionString,
        ["Database:TimeoutInSeconds"] = "180",
        ["Content:Root"] = Path.Combine(apiRoot, "Data"),
        ["AttributeRedesign:LiveVersion"] = modern ? "18" : "17",
        ["EquipmentBalance:LiveVersion"] = modern ? "4" : "1",
        ["Combat:AbilityBalanceProfile"] = modern ? "healing-v1" : "",
        ["Combat:IdleProgression:MaximumEncountersPerResolution"] = "100",
        ["Combat:IdleProgression:MaximumBatchesPerResolution"] = "5"
    }).Build();
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(x => x.AddSimpleConsole().SetMinimumLevel(LogLevel.Error));
services.AddHttpContextAccessor();
services.AddPersistence(config).AddRepositories().AddApplication();
services.AddServices(config, apiRoot);
services.AddSingleton<TimeProvider>(modern ? TimeProvider.System : new CutoverClock(stamp.Cutover));
await using var provider = services.BuildServiceProvider();
await using var inspection = provider.CreateAsyncScope();
var database = inspection.ServiceProvider.GetRequiredService<LLDbContext>();
await using var operatorLock = new NpgsqlConnection(connection.ConnectionString);
await operatorLock.OpenAsync();
await using (var sessions = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND backend_type='client backend' AND application_name <> 'LL equipment local rollout'", operatorLock))
    if (Convert.ToInt64(await sessions.ExecuteScalarAsync()) != 0)
        throw new InvalidOperationException("Stop other database clients and game writers before running the local rollout.");
await using (var guard = new NpgsqlCommand("SELECT pg_try_advisory_lock(721831, 18)", operatorLock))
    if (!Equals(await guard.ExecuteScalarAsync(), true)) throw new InvalidOperationException("Another rollout operator is running.");
const string snapshotSql = "SELECT \"Id\", md5(to_jsonb(s)::text) FROM \"CharacterSnapshots\" s";
const string unreferencedSql = """
    SELECT i."Id", md5(to_jsonb(i)::text || COALESCE((SELECT string_agg(to_jsonb(m)::text, '' ORDER BY m."Id")
    FROM "InstanceAttributeModifier" m WHERE m."ItemInstanceId"=i."Id"), '')) FROM "ItemInstances" i
    WHERE i."ItemType"=0 AND NOT EXISTS (SELECT 1 FROM "InventoryItems" r WHERE r."ItemInstanceId"=i."Id")
    AND NOT EXISTS (SELECT 1 FROM "EquipmentSlots" r WHERE r."EquipmentInstanceId"=i."Id")
    AND NOT EXISTS (SELECT 1 FROM "GuildVaultItems" r WHERE r."EquipmentInstanceId"=i."Id")
    AND NOT EXISTS (SELECT 1 FROM "MarketPlaceListings" r WHERE r."ItemInstanceId"=i."Id")
    AND NOT EXISTS (SELECT 1 FROM "EquipmentLoadoutSlots" r WHERE r."EquipmentInstanceId"=i."Id")
    """;
if (mode == "settle" && !File.Exists(Path.Combine(evidenceRoot, "historical-snapshots.json")))
{
    Save("historical-snapshots.json", await Fingerprints(snapshotSql));
    Save("original-unreferenced.json", await Fingerprints(unreferencedSql));
}

if (mode == "settle")
{
    var schedule = Read<List<PausedAction>>("schedules.json");
    if (schedule is null)
    {
        var actions = await database.CharacterActions.AsNoTracking().Include(x => x.ActionDetails)
            .Where(x => !x.IsDeleted && x.NextResolutionAtUtc != null).OrderBy(x => x.CharacterId).ToListAsync();
        if (actions.Any(x => x.ActionDetails is not CombatActionDetails))
            throw new InvalidOperationException("An unsupported scheduled action needs manual review.");
        schedule = actions.Select(x => new PausedAction(x.CharacterId, ((CombatActionDetails)x.ActionDetails!).AreaId,
            ((CombatActionDetails)x.ActionDetails!).CharacterTeam, x.ScheduleGeneration)).ToList();
        Save("schedules.json", schedule);
    }
    var resolved = Read<Dictionary<Guid, int>>("settled.json") ?? [];
    foreach (var entry in schedule)
    {
        if (resolved.ContainsKey(entry.CharacterId)) continue;
        var total = 0;
        for (var batch = 0; batch < 200; batch++)
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LLDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.AcquireCharacterRowsLockAsync([entry.CharacterId], default);
            var actions = scope.ServiceProvider.GetRequiredService<ICharacterActionService>();
            var prior = await actions.PeekCharacterActionAsync(entry.CharacterId, default);
            if (prior is { IsDeleted: false } && prior.ScheduleGeneration != entry.Generation)
                throw new InvalidOperationException("A schedule changed during maintenance. Stop all writers first.");
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            if (prior?.IsDeleted == false) await mediator.Send(new ResolveCharacterActionCommand(entry.CharacterId));
            var action = await actions.PeekCharacterActionAsync(entry.CharacterId, default);
            total += action?.ProcessedCount ?? 0;
            var finished = action is null || action.IsDeleted || !action.HasMoreDueWork;
            if (finished && action is { IsDeleted: false }) await mediator.Send(new DeleteCharacterActionCommand(entry.CharacterId));
            if (!finished && action!.ProcessedCount == 0) throw new InvalidOperationException("Combat settlement made no progress.");
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            if (finished)
            {
                resolved[entry.CharacterId] = total;
                Save("settled.json", resolved);
                Console.WriteLine($"Settled and paused {resolved.Count}/{schedule.Count} schedules; {total} encounters in this run.");
                break;
            }
            Console.WriteLine($"Settlement progress: character {resolved.Count + 1}/{schedule.Count}, {total} encounters.");
            if (batch == 199) throw new InvalidOperationException("Settlement limit reached; review the retained evidence.");
        }
    }
    if (await database.CharacterActions.AnyAsync(x => !x.IsDeleted && x.NextResolutionAtUtc != null))
        throw new InvalidOperationException("New schedules appeared during maintenance.");
    Console.WriteLine("All captured schedules settled and paused using legacy combat rules.");
}
else if (mode == "apply")
{
    if (await database.CharacterActions.AnyAsync(x => !x.IsDeleted && x.NextResolutionAtUtc != null))
        throw new InvalidOperationException("Settle and pause every combat schedule first.");
    await database.Database.MigrateAsync();
    var plan = Read<List<PlannedConversion>>("conversions.json");
    if (plan is null)
    {
        plan = [];
        var skipped = new List<Guid>();
        for (var release = 1; release < 4; release++)
        for (var page = 0; ; page++)
        {
            var audit = await inspection.ServiceProvider.GetRequiredService<IEquipmentMigrationRepository>().AuditAsync(page, 100, default, release);
            if (audit.UnversionedPendingRewards != 0) throw new InvalidOperationException("Unversioned pending rewards need manual review.");
            foreach (var target in audit.Targets)
            {
                if (target.Location == EquipmentMigrationLocation.Instance && await IsUnreferenced(database, target.ItemId))
                { skipped.Add(target.ItemId); continue; }
                var preview = await Migration(inspection.ServiceProvider).PreviewAsync(target, null, default, 4);
                plan.Add(new(Guid.NewGuid(), target, preview.SourceHash, preview.After.State.DefinitionId, preview.ResultHash));
            }
            if (audit.Targets.Count < 100) break;
        }
        Save("retained-unreferenced.json", skipped);
        Save("conversions.json", plan);
    }
    var index = 0;
    foreach (var conversion in plan)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LLDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var receipt = await Migration(scope.ServiceProvider).ApplyAsync(conversion.OperationId, conversion.Target,
            conversion.SourceHash, conversion.DefinitionId, "local-equipment-rollout", default, 4, conversion.ResultHash);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Save($"receipt-{conversion.OperationId}.json", receipt);
        if (++index % 50 == 0 || index == plan.Count) Console.WriteLine($"Converted {index}/{plan.Count} equipment records.");
    }
}
else if (mode == "verify")
{
    foreach (var (name, sql) in new[] { ("historical-snapshots.json", snapshotSql), ("original-unreferenced.json", unreferencedSql) })
    {
        var original = Read<Dictionary<Guid, string>>(name) ?? throw new InvalidOperationException("Missing pre-cutover preservation evidence.");
        var current = await Fingerprints(sql);
        if (original.Any(x => current.GetValueOrDefault(x.Key) != x.Value))
            throw new InvalidOperationException($"Preservation check failed: {name}.");
    }
    var audit = await inspection.ServiceProvider.GetRequiredService<IEquipmentMigrationRepository>().AuditAsync(0, 100, default);
    var older = await database.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"ItemInstances\" WHERE (\"ModelEData\" #>> '{{State,BalanceVersion}}')::int < 4").SingleAsync();
    var olderPending = await database.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM \"RunRewards\" WHERE (\"ModelEData\" #>> '{{State,BalanceVersion}}')::int < 4 AND EXISTS (SELECT 1 FROM \"DungeonRuns\" WHERE \"DungeonRuns\".\"Id\"=\"RunRewards\".\"DungeonRunId\" AND \"RewardsClaimedAt\" IS NULL)").SingleAsync();
    if (audit.UnversionedInstances != audit.UnreferencedUnversionedInstances || older != 0
        || olderPending != 0 || audit.UnversionedPendingRewards != 0 || !audit.CompetitiveSnapshotsReady)
        throw new InvalidOperationException("Owned equipment or competitive snapshots are not ready.");
    var catalog = inspection.ServiceProvider.GetRequiredService<IAbilityCatalogProvider>().GetCatalog();
    var herb = catalog.AbilitiesById["ability.creature.lizardfolk_shaman.herb_mixture"].Effects[0].ScalingCoefficient;
    var treant = catalog.AbilitiesById["ability.creature.treant_sapling.sprouting_surge"].Effects[0].ScalingCoefficient;
    if (herb != 1.05f || treant != 1.25f) throw new InvalidOperationException("Healing profile mismatch.");
    Save("verified.json", new { Rules = 18, Equipment = 4, HealingProfile = "healing-v1", HerbMixture = herb,
        TreantSaplings = treant, audit.TotalInstances, audit.UnreferencedUnversionedInstances, audit.HistoricalSnapshots });
    Console.WriteLine($"Verified rules 18 / equipment 4 / healing-v1; {audit.UnreferencedUnversionedInstances} unreferenced records retained.");
}
else
{
    if (!File.Exists(Path.Combine(evidenceRoot, "verified.json"))) throw new InvalidOperationException("Verify the conversion before resuming.");
    var schedule = Read<List<PausedAction>>("schedules.json") ?? throw new InvalidOperationException("No captured schedules.");
    foreach (var entry in schedule)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LLDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.AcquireCharacterRowsLockAsync([entry.CharacterId], default);
        var actions = scope.ServiceProvider.GetRequiredService<ICharacterActionService>();
        var prior = await actions.PeekCharacterActionAsync(entry.CharacterId, default);
        if (prior is { IsDeleted: false, NextResolutionAtUtc: not null }) continue;
        var area = await db.Areas.Include(x => x.Creatures).SingleAsync(x => x.Id == entry.AreaId);
        var started = await actions.StartCharacterActionAsync(new CharacterAction(entry.CharacterId,
            new CombatActionDetails(entry.Team, area), DateTimeOffset.UtcNow), DateTimeOffset.UtcNow, default);
        if (started is null) throw new InvalidOperationException("The restart lock is still active; retry resume later.");
        await scope.ServiceProvider.GetRequiredService<Application.Interfaces.Services.LL.IStateSyncService>()
            .InvalidateCharacterAsync(entry.CharacterId, "equipment-rollout-resumed", default);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    Console.WriteLine($"Resumed {schedule.Count} captured schedules with the new attributes and healing profile.");
}

async Task<Dictionary<Guid, string>> Fingerprints(string sql)
{
    await using var command = new NpgsqlCommand(sql, operatorLock);
    await using var reader = await command.ExecuteReaderAsync();
    var result = new Dictionary<Guid, string>();
    while (await reader.ReadAsync()) result.Add(reader.GetGuid(0), reader.GetString(1));
    return result;
}

EquipmentMigrationService Migration(IServiceProvider sp) => new(sp.GetRequiredService<IEquipmentMigrationRepository>(),
    sp.GetRequiredService<EquipmentMigrationCatalog>(), sp.GetRequiredService<IGameEventOutbox>(),
    sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<IEquipmentUpgradeRepository>(), liveRules: new AttributeRulesSelection(17, 1));

static Task<bool> IsUnreferenced(LLDbContext db, Guid id) => db.ItemInstances.OfType<EquipmentInstance>().AnyAsync(x => x.Id == id
    && !db.InventoryItems.Any(i => i.ItemInstanceId == x.Id) && !db.EquipmentSlots.Any(s => s.EquipmentInstanceId == x.Id)
    && !db.MarketPlaceListings.Any(m => m.ItemInstanceId == x.Id) && !db.GuildVaultItems.Any(g => g.EquipmentInstanceId == x.Id)
    && !db.EquipmentLoadoutSlots.Any(l => l.EquipmentInstanceId == x.Id));

sealed record RunStamp(string Database, int Port, DateTimeOffset Cutover);
sealed record PausedAction(Guid CharacterId, string AreaId, List<Guid> Team, long Generation);
sealed record PlannedConversion(Guid OperationId, EquipmentMigrationTarget Target, string SourceHash, string DefinitionId, string ResultHash);
sealed class CutoverClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
