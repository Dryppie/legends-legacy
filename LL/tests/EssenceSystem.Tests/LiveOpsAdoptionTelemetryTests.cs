using System.Text.Json;
using Application.Interfaces.Services.LL.CombatStyles;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Analytics;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Repositories.Analytics;

namespace EssenceSystem.Tests;

public sealed partial class LiveOpsAdministrationTests
{
    [Fact]
    public void LiveOps_legacy_adoption_json_does_not_claim_zero_coverage()
    {
        const string json = """
            {"reportDateUtc":"2026-09-20","generatedAtUtc":"2026-09-21T02:00:00Z","snapshotAtUtc":"2026-09-21T02:00:00Z",
             "population":null,"outcomes":[],"adoption":[],"economy":[]}
            """;
        var report = JsonSerializer.Deserialize<TelemetrySnapshot>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.False(report.AdoptionIncludesZeroObservations);
    }

    [LiveOpsLocalPostgres]
    public async Task LiveOps_adoption_captures_catalog_zeros_distinct_characters_and_preserves_historical_coverage()
    {
        await using var fixture = await LiveOpsPostgresDatabase.CreateAsync();
        await using var db = fixture.CreateContext();
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var (firstAccount, first) = AddPlayer(db);
        var (secondAccount, _) = AddPlayer(db);
        var (highAccount, high) = AddPlayer(db);
        var (monthAccount, _) = AddPlayer(db);
        var (_, inactive) = AddPlayer(db);
        (await db.Characters.FindAsync(high))!.Level = 55;
        foreach (var account in new[] { firstAccount, secondAccount, highAccount })
            db.AccountActivityDays.Add(new AccountActivityDay { AccountId = account, ActivityDateUtc = day, FirstSeenAtUtc = DateTimeOffset.UtcNow });
        db.AccountActivityDays.Add(new AccountActivityDay { AccountId = monthAccount, ActivityDateUtc = day.AddDays(-10), FirstSeenAtUtc = DateTimeOffset.UtcNow.AddDays(-10) });
        var owned = new PlayerEssence { Id = Guid.NewGuid(), CharacterId = first, EssenceDefinitionId = "owned" };
        db.PlayerEssences.AddRange(owned,
            new PlayerEssence { Id = Guid.NewGuid(), CharacterId = high, EssenceDefinitionId = "retired" },
            new PlayerEssence { Id = Guid.NewGuid(), CharacterId = inactive, EssenceDefinitionId = "unused" });
        foreach (var preset in new[] { 1, 2 })
            db.EssenceLoadouts.Add(new EssenceLoadout { Id = Guid.NewGuid(), CharacterId = first, PresetSlot = preset, Name = $"Test {preset}",
                Slots = [new EssenceLoadoutSlot { Id = Guid.NewGuid(), SlotIndex = 0, PlayerEssenceId = owned.Id }] });
        db.CharacterCombatStyleSelections.Add(new CharacterCombatStyleSelection { CharacterId = first, CombatStyleId = "selected" });

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var legacy = new TelemetrySnapshot(day.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(-1),
            new PopulationMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0), [],
            [new AdoptionMetric(7, "essence-owned", "owned", "1-19", 100, 50)], []);
        db.DailyTelemetryReports.Add(new DailyTelemetryReport { ReportDateUtc = legacy.ReportDateUtc,
            GeneratedAtUtc = legacy.GeneratedAtUtc, PayloadJson = JsonSerializer.Serialize(legacy, options) });
        await db.SaveChangesAsync();
        var essences = new AdoptionEssenceCatalog();
        var repository = new TelemetryRepository(db, essences, new AdoptionStyleCatalog());
        await repository.GenerateDailyReportsAsync(day, default);
        var reports = await repository.GetReportsAsync(30, default);
        var current = reports.Single(x => x.ReportDateUtc == day);
        Assert.True(current.AdoptionIncludesZeroObservations);
        Assert.Equal(2, Row("owned", "essence-owned", 7, "1-19").CohortCharacters);
        Assert.Equal(3, Row("owned", "essence-owned", 30, "1-19").CohortCharacters);
        Assert.Equal(1, Row("owned", "essence-saved", 7, "1-19").ObservedCharacters); // Two presets, one character.
        Assert.Equal(0, Row("unused", "essence-owned", 7, "1-19").ObservedCharacters); // Inactive owner excluded.
        Assert.Equal(0, Row("unused", "essence-saved", 7, "1-19").ObservedCharacters);
        Assert.Equal(1, Row("retired", "essence-owned", 7, "50+").ObservedCharacters);
        Assert.Equal(0, Row("retired", "essence-saved", 7, "50+").ObservedCharacters);
        Assert.Equal(1, Row("selected", "style-selected", 7, "1-19").ObservedCharacters);
        Assert.Equal(0, Row("unselected", "style-selected", 7, "50+").ObservedCharacters);
        Assert.DoesNotContain(current.Adoption, x => x.CohortCharacters == 0);
        Assert.DoesNotContain(reports.Single(x => x.ReportDateUtc == day.AddDays(-2)).Adoption, x => x.CohortDays == 7);
        var historical = reports.Single(x => x.ReportDateUtc == legacy.ReportDateUtc);
        Assert.False(historical.AdoptionIncludesZeroObservations);
        Assert.Equal(legacy.Adoption, historical.Adoption);
        Assert.Equal(legacy.SnapshotAtUtc, historical.SnapshotAtUtc);

        essences.Definitions.Add(new EssenceDefinition { Id = "new-after-snapshot" });
        await repository.GenerateDailyReportsAsync(day, default);
        var repeated = (await repository.GetReportsAsync(30, default)).Single(x => x.ReportDateUtc == day);
        Assert.Equal(current.Adoption, repeated.Adoption);
        Assert.Equal(current.SnapshotAtUtc, repeated.SnapshotAtUtc);
        Assert.True(repeated.AdoptionIncludesZeroObservations);

        AdoptionMetric Row(string key, string kind, int window, string band) =>
            Assert.Single(current.Adoption, x => x.Key == key && x.Kind == kind && x.CohortDays == window && x.LevelBand == band);
    }

    private sealed class AdoptionEssenceCatalog : IEssenceDefinitionRepository
    {
        public List<EssenceDefinition> Definitions { get; } = [new() { Id = "owned" }, new() { Id = "unused" }];
        public IReadOnlyList<EssenceDefinition> GetAll() => Definitions;
        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];
        public EssenceDefinition? GetById(string id) => Definitions.Find(x => x.Id == id);
        public AbilitySpec? GetAbilityById(string id) => null;
    }
    private sealed class AdoptionStyleCatalog : ICombatStyleCatalogProvider
    {
        public CombatStyleCatalog Catalog { get; } = new() { Styles = [new() { Id = "selected" }, new() { Id = "unselected" }] };
    }
}
