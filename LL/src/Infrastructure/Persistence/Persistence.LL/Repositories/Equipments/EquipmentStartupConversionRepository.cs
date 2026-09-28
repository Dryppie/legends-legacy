using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Persistence.LL.Repositories.Equipments;

public sealed class EquipmentStartupConversionRepository(LLDbContext db, IEquipmentMigrationRepository migration)
    : IEquipmentStartupConversionRepository
{
    // Only live references establish which retained instance rows still belong to the game.
    private const string References = """
        EXISTS (SELECT 1 FROM "InventoryItems" r WHERE r."ItemInstanceId" = i."Id")
        OR EXISTS (SELECT 1 FROM "EquipmentSlots" r WHERE r."EquipmentInstanceId" = i."Id")
        OR EXISTS (SELECT 1 FROM "MarketPlaceListings" r WHERE r."ItemInstanceId" = i."Id")
        OR EXISTS (SELECT 1 FROM "GuildVaultItems" r WHERE r."EquipmentInstanceId" = i."Id")
        OR EXISTS (SELECT 1 FROM "EquipmentLoadoutSlots" r WHERE r."EquipmentInstanceId" = i."Id")
        """;

    private const string OldItem = """
        i."ItemType" = 0 AND (i."ModelEData" IS NULL
            OR (i."ModelEData" -> 'State' ->> 'BalanceVersion')::int < @version)
        """;

    private IQueryable<TargetRow> Targets(int version) => db.Database.SqlQueryRaw<TargetRow>("""
        SELECT i."Id" AS "ItemId", 0 AS "Location", NULL::uuid AS "ContainerId"
        FROM "ItemInstances" i WHERE
        """ + " " + OldItem + " AND (" + References + ") UNION ALL " + """
        SELECT (r."ModelEData" -> 'State' ->> 'Id')::uuid AS "ItemId", 1 AS "Location", r."DungeonRunId" AS "ContainerId"
        FROM "RunRewards" r JOIN "DungeonRuns" d ON d."Id" = r."DungeonRunId"
        WHERE d."RewardsClaimedAt" IS NULL AND (r."ModelEData" -> 'State' ->> 'BalanceVersion')::int < @version
        """, new NpgsqlParameter("version", version));

    // Use frozen equipment versions too: a rules-18 snapshot can still contain pre-conversion gear.
    private const string OldSnapshot = """
        s."AttributeRulesVersion" < 18 OR EXISTS (
            SELECT 1 FROM "EquipmentSnapshot" e WHERE e."CharacterSnapshotId" = s."Id"
            AND (e."ModelEData" IS NULL OR (e."ModelEData" -> 'State' ->> 'BalanceVersion')::int < @version))
        """;

    private IQueryable<Guid> ArenaDefenses(int version) => db.Database.SqlQueryRaw<Guid>("""
        SELECT a."CharacterId" AS "Value" FROM "ArenaDefenseSnapshots" a
        JOIN "CharacterSnapshots" s ON s."Id" = a."CharacterSnapshotId"
        WHERE a."IsValid" AND (
        """ + OldSnapshot + ")", new NpgsqlParameter("version", version));

    public async Task<IAsyncDisposable> AcquireRunnerLockAsync(CancellationToken ct)
    {
        // Dedicated non-pooled session: closing it always releases the session advisory lock,
        // including exceptions and process termination. Item commands use their own transactions.
        var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString())
        { Pooling = false }.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(721832, 4)", connection) { CommandTimeout = 0 };
            await command.ExecuteNonQueryAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<EquipmentMigrationTarget>> GetTargetsAsync(int targetVersion, int limit, CancellationToken ct) =>
        (await Targets(targetVersion).OrderBy(x => x.Location).ThenBy(x => x.ItemId)
            .Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct))
        .Select(x => new EquipmentMigrationTarget(x.ItemId, (EquipmentMigrationLocation)x.Location, x.ContainerId)).ToArray();

    public async Task LockCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct)
    {
        var characters = await migration.GetAffectedCharactersAsync(target, ct);
        foreach (var character in characters.Order()) await db.AcquireCharacterCommandLockAsync(character, ct);
        await db.AcquireCharacterRowsLockAsync(characters, ct);
        if (!characters.SequenceEqual(await migration.GetAffectedCharactersAsync(target, ct)))
            throw new InvalidOperationException("Equipment ownership changed while acquiring conversion locks. Retry startup.");
    }

    public Task<bool> IsCandidateAsync(EquipmentMigrationTarget target, int targetVersion, CancellationToken ct) =>
        Targets(targetVersion).AnyAsync(x => x.ItemId == target.ItemId && x.Location == (int)target.Location
            && x.ContainerId == target.ContainerId, ct);

    public async Task<IReadOnlyList<Guid>> GetArenaDefensesAsync(int targetVersion, int limit, CancellationToken ct) =>
        await ArenaDefenses(targetVersion).OrderBy(x => x).Take(Math.Clamp(limit, 1, 100)).ToArrayAsync(ct);

    public async Task<EquipmentStartupConversionAudit> AuditAsync(int targetVersion, CancellationToken ct)
    {
        var remaining = await Targets(targetVersion).CountAsync(ct);
        var unsupported = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::int AS "Value" FROM "RunRewards" r
            JOIN "DungeonRuns" d ON d."Id" = r."DungeonRunId"
            WHERE d."RewardsClaimedAt" IS NULL AND r."ItemType" = 0 AND r."ModelEData" IS NULL
            """).SingleAsync(ct);
        var tournaments = await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*)::int AS "Value" FROM "TournamentCombatSnapshots" c
            JOIN "ArenaTournaments" t ON t."Id" = c."TournamentId"
            JOIN "CharacterSnapshots" s ON s."Id" = c."CharacterSnapshotId"
            WHERE t."Status" NOT IN (@completed, @cancelled) AND (
            """ + OldSnapshot + ")", new NpgsqlParameter("version", targetVersion),
            new NpgsqlParameter("completed", (int)Domain.Models.Colosseum.Tournaments.TournamentStatus.Completed),
            new NpgsqlParameter("cancelled", (int)Domain.Models.Colosseum.Tournaments.TournamentStatus.Cancelled)).SingleAsync(ct);
        var retained = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM \"ItemInstances\" i WHERE "
            + OldItem + " AND NOT (" + References + ")", new NpgsqlParameter("version", targetVersion)).SingleAsync(ct);
        return new(remaining, unsupported, await ArenaDefenses(targetVersion).CountAsync(ct), tournaments, retained);
    }

    private sealed class TargetRow
    {
        public Guid ItemId { get; set; }
        public int Location { get; set; }
        public Guid? ContainerId { get; set; }
    }
}
