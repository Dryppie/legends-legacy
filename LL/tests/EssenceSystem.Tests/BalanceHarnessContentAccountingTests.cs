using System.Text;
using System.Text.Json;
using BalanceHarness;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.CombatStyles;
using Services.LL.Content;
using Services.LL.Essences;
using Services.LL.Items;
using Services.LL.Regions;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessContentAccountingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "content-accounting-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Content fixture cannot fight.")).Activate();
    private readonly IConfiguration config = new ConfigurationBuilder().Build();
    private readonly JsonEssenceDefinitionRepository essences;
    private static readonly string[] Providers = ["essences", "loot", "creature", "scaling", "abilities", "equipment", "styles", "floors"];
    private static readonly Dictionary<string, string[]> Inputs = new()
    {
        ["essences"] = ["essences/essences.json", "combat/abilities.json"],
        ["loot"] = ["world/creature-essence-loot-tables.json"],
        ["creature"] = ["combat/creature-abilities.json"],
        ["scaling"] = ["progression/region-combat-balance.json"],
        ["abilities"] = ["combat/abilities.json", "combat/statuses.json", "combat/summons.json"],
        ["equipment"] = ["equipment/equipment-starters.v1.json", "equipment/equipment-named.v1.json",
            "equipment/equipment-styles.v1.json", "equipment/equipment-sets.v1.json", "items/items.json"],
        ["styles"] = ["combat-styles/combat-styles.v1.json"],
        ["floors"] = [TowerBattleRunner.FloorFile]
    };
    private string Data(string name) => Path.Combine(root, "Data", name);
    public BalanceHarnessContentAccountingTests()
    {
        foreach (var name in OfflineContent.Files.Append(TowerBattleRunner.FloorFile))
        {
            var target = Data(name); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(TestContentPaths.FindApiRoot(), "Data", name), target);
        }
        essences = new(config, root, HarnessJson.Options, new EssenceDefinitionValidator());
    }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private object Load(string kind, ContentJsonReader? reader = null) => kind switch
    {
        "essences" => EssenceValue(new(config, root, HarnessJson.Options, new EssenceDefinitionValidator(), reader)),
        "loot" => new JsonCreatureEssenceLootTableRepository(config, root, HarnessJson.Options, essences, reader).GetAll(),
        "creature" => CreatureValue(new(config, root, HarnessJson.Options, reader)),
        "scaling" => new RegionCreatureScalingProvider(config, root, HarnessJson.Options, reader).GetCatalog(),
        "abilities" => new JsonAbilityCatalogProvider(config, root, HarnessJson.Options, reader: reader).GetCatalog(),
        "equipment" => JsonStarterEquipmentCatalog.Load(Data(Inputs[kind][0]), reader).GetOptions(1),
        "styles" => new JsonCombatStyleCatalogProvider(Data(Inputs[kind][0]), reader).Catalog,
        "floors" => new JsonWorldTowerDefinitionProvider(Data(Inputs[kind][0]), HarnessJson.Options, reader).GetFloors(),
        _ => throw new ArgumentException(kind)
    };
    private object LoadFactory(string kind) => kind switch
    {
        "essences" => EssenceValue(TowerContentProviders.Essences(config, root, HarnessJson.Options, new EssenceDefinitionValidator())),
        "loot" => TowerContentProviders.Loot(config, root, HarnessJson.Options, essences).GetAll(),
        "creature" => CreatureValue(TowerContentProviders.CreatureAbilities(config, root, HarnessJson.Options)),
        "scaling" => TowerContentProviders.Scaling(config, root, HarnessJson.Options).GetCatalog(),
        "abilities" => TowerContentProviders.Abilities(config, root, HarnessJson.Options, new()).GetCatalog(),
        "equipment" => TowerContentProviders.Equipment(Data(Inputs[kind][0])).GetOptions(1),
        "styles" => TowerContentProviders.Styles(Data(Inputs[kind][0])).Catalog,
        "floors" => TowerContentProviders.Floors(Data(Inputs[kind][0]), HarnessJson.Options).GetFloors(),
        _ => throw new ArgumentException(kind)
    };

    [Theory]
    [InlineData("essences")][InlineData("loot")][InlineData("creature")][InlineData("scaling")]
    [InlineData("abilities")][InlineData("equipment")][InlineData("styles")][InlineData("floors")]
    public void Binary_compatible_factory_preserves_values_and_exact_current_reader_counts(string kind)
    {
        ReadCreatureIds();
        Assert.True(TowerContentProviders.SupportsAccounting);
        var before = new TowerWorkAccounting(); object expected;
        using (before.Activate()) expected = Load(kind, TowerContentJsonReader.Instance);
        var after = new TowerWorkAccounting(); object actual;
        using (after.Activate()) actual = LoadFactory(kind);
        Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(actual));
        Assert.Equal(HarnessJson.Hash(before.Snapshot()), HarnessJson.Hash(after.Snapshot()));
    }

    [Theory]
    [InlineData("essences")][InlineData("loot")][InlineData("creature")][InlineData("scaling")]
    [InlineData("abilities")][InlineData("equipment")][InlineData("styles")][InlineData("floors")]
    public void Binary_compatible_factory_preserves_original_provider_exceptions_and_partial_counts(string kind)
    {
        File.WriteAllText(Data(Inputs[kind][0]), "{ malformed }");
        var before = new TowerWorkAccounting(); Exception? expected;
        using (before.Activate()) expected = Record.Exception(() => Load(kind, TowerContentJsonReader.Instance));
        var after = new TowerWorkAccounting(); Exception? actual;
        using (after.Activate()) actual = Record.Exception(() => LoadFactory(kind));
        Assert.NotNull(expected); Assert.NotNull(actual);
        Assert.Equal(expected.GetType(), actual.GetType());
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(HarnessJson.Hash(before.Snapshot()), HarnessJson.Hash(after.Snapshot()));
    }
    private static object EssenceValue(JsonEssenceDefinitionRepository provider) => new { definitions = provider.GetAll(), abilities = provider.GetAllAbilities() };
    private object CreatureValue(JsonCreatureAbilityDefinitionProvider provider)
    {
        // Read expected identifiers before activating accounting in these tests.
        return creatureIds.ToDictionary(id => id, id => provider.GetAbilityIds(id));
    }
    private string[] creatureIds = [];
    private void ReadCreatureIds()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Data(Inputs["creature"][0])));
        creatureIds = document.RootElement.GetProperty("creatures").EnumerateArray().Select(e => e.GetProperty("monsterId").GetString()!).ToArray();
    }
    private string[] EquipmentFragments()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Data("items/items.json")));
        return document.RootElement.EnumerateArray().Where(e => e.TryGetProperty("itemType", out var type)
            && type.GetString()?.Equals("Equipment", StringComparison.OrdinalIgnoreCase) == true).Select(e => e.GetRawText()).ToArray();
    }
    private long FileBytes(IEnumerable<string> names) => names.Sum(name => new FileInfo(Data(name)).Length);
    private long TextBytes(IEnumerable<string> names) => names.Sum(name => (long)Encoding.UTF8.GetByteCount(File.ReadAllText(Data(name))));

    [Theory]
    [InlineData("essences")][InlineData("loot")][InlineData("creature")][InlineData("scaling")]
    [InlineData("abilities")][InlineData("equipment")][InlineData("styles")][InlineData("floors")]
    public void Optional_reader_preserves_provider_values_and_counts_every_file_and_parse(string kind)
    {
        ReadCreatureIds();
        var inactive = new TowerWorkAccounting(); object expected;
        using (inactive.Activate()) expected = Load(kind);
        Assert.Empty(inactive.Snapshot()); // Service defaults never acquire the harness collector.
        var work = new TowerWorkAccounting(); object actual;
        using (work.Activate()) actual = Load(kind, TowerContentJsonReader.Instance);
        Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(actual));
        var fragments = kind == "equipment" ? EquipmentFragments() : [];
        Assert.Equal(FileBytes(Inputs[kind]), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(TextBytes(Inputs[kind]) + fragments.Sum(s => (long)Encoding.UTF8.GetByteCount(s)), work.Snapshot()["jsonInputBytes"]);
        Assert.Equal(Inputs[kind].Length + fragments.Length, work.Snapshot()["jsonParseAttempts"]);
        Assert.Equal(work.Snapshot()["jsonParseAttempts"], work.Snapshot()["jsonParseCompleted"]);
    }

    [Theory]
    [InlineData("essences")][InlineData("loot")][InlineData("creature")][InlineData("scaling")]
    [InlineData("abilities")][InlineData("equipment")][InlineData("styles")][InlineData("floors")]
    public void Malformed_provider_input_preserves_default_exception_and_partial_work(string kind)
    {
        File.WriteAllText(Data(Inputs[kind][0]), "{ malformed }");
        var expected = Assert.Throws<JsonException>(() => Load(kind));
        var work = new TowerWorkAccounting();
        using (work.Activate())
        {
            var actual = Assert.Throws<JsonException>(() => Load(kind, TowerContentJsonReader.Instance));
            Assert.Equal(expected.Message, actual.Message);
        }
        // Essence loading already read both source files before parsing the first.
        Assert.Equal(FileBytes(Inputs[kind].Take(kind == "essences" ? 2 : 1)), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(1, work.Snapshot()["jsonParseAttempts"]);
        Assert.False(work.Snapshot().ContainsKey("jsonParseCompleted"));
    }

    [Fact]
    public void Offline_composition_counts_duplicate_ability_reads_and_nested_equipment_parses()
    {
        var expected = new OfflineContent(root, new()); var work = new TowerWorkAccounting(); OfflineContent actual;
        using (work.Activate()) actual = new OfflineContent(root, new());
        Assert.Equal(HarnessJson.Hash(FixtureCharacter.From(expected.CreateStarter())), HarnessJson.Hash(FixtureCharacter.From(actual.CreateStarter())));
        var names = Providers.Take(6).SelectMany(kind => Inputs[kind]).ToArray();
        var fragments = EquipmentFragments();
        Assert.Equal(2, names.Count(name => name == "combat/abilities.json"));
        Assert.Equal(FileBytes(names), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(TextBytes(names) + fragments.Sum(s => (long)Encoding.UTF8.GetByteCount(s)), work.Snapshot()["jsonInputBytes"]);
        Assert.Equal(13 + fragments.Length, work.Snapshot()["jsonParseCompleted"]);
        if (Environment.GetEnvironmentVariable("LL_CONTENT_ACCOUNTING_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Path.Exists(export)); Directory.CreateDirectory(export);
            foreach (var name in OfflineContent.Files.Append(TowerBattleRunner.FloorFile))
            {
                var target = Path.Combine(export, "content", name); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Data(name), target);
            }
            HarnessJson.WriteNew(Path.Combine(export, "expected.json"), new { filesRead = names, equipmentFragments = fragments,
                applicationReadBytes = FileBytes(names), jsonInputBytes = TextBytes(names) + fragments.Sum(s => (long)Encoding.UTF8.GetByteCount(s)),
                completedParses = 13 + fragments.Length, starterHash = HarnessJson.Hash(FixtureCharacter.From(actual.CreateStarter())) });
            HarnessJson.WriteNew(Path.Combine(export, "native-work.json"), work.Receipt("nativeAudit", new('a', 64), new('b', 64), true));
            TowerProposalStudy.Seal(export, default);
        }
    }

    [Fact]
    public void Repeated_tower_input_reconstruction_counts_floor_and_guardian_reads_without_combat()
    {
        var scenario = HarnessJson.Read<TowerScenario>(Path.Combine(TestContentPaths.FindApiRoot(), "../../../tools/BalanceHarness/Fixtures/tower-floor-1.json"));
        var runner = new TowerBattleRunner(root, new OfflineContent(root, new()));
        var expected = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
        var work = new TowerWorkAccounting();
        using (work.Activate())
            for (var i = 0; i < 3; i++) Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(runner.CreateInput(scenario, scenario.Seeds[0], new(), 10)));
        var names = new[] { TowerBattleRunner.FloorFile, "world/creatures.json" };
        Assert.Equal(3 * FileBytes(names), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(3 * TextBytes(names), work.Snapshot()["jsonInputBytes"]);
        Assert.Equal(6, work.Snapshot()["jsonParseCompleted"]);
    }

    [Fact]
    public void Styled_input_counts_both_catalog_loads_in_materialization_and_validation()
    {
        var content = new OfflineContent(root, new());
        var suite = HarnessJson.Read<IdleSuiteDefinition>(Path.Combine(TestContentPaths.FindApiRoot(), "../../../tools/BalanceHarness/Fixtures/combat-styles.json"));
        var input = IdleSuite.Resolve(suite with { SamplesPerCell = 1 }, content, new(), 60, 1337).Cells.First(c => c.Build == "bastion-1").Input;
        var expected = content.CreateInput(input.Scenario, 1337, new(), 60);
        var work = new TowerWorkAccounting();
        using (work.Activate()) Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(content.CreateInput(input.Scenario, 1337, new(), 60)));
        var names = new[] { Inputs["styles"][0], "world/regions.json", "world/creatures.json", Inputs["styles"][0] };
        Assert.Equal(FileBytes(names), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(TextBytes(names), work.Snapshot()["jsonInputBytes"]);
        Assert.Equal(4, work.Snapshot()["jsonParseCompleted"]);
    }

    [Fact]
    public void Missing_later_catalog_retains_earlier_read_and_parse_without_inventing_work()
    {
        File.Delete(Data("combat/statuses.json")); var work = new TowerWorkAccounting();
        using (work.Activate()) Assert.Throws<FileNotFoundException>(() => Load("abilities", TowerContentJsonReader.Instance));
        Assert.Equal(FileBytes(["combat/abilities.json"]), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(1, work.Snapshot()["jsonParseCompleted"]);
        Assert.Equal(1, work.Snapshot()["jsonParseAttempts"]);
    }

    [Fact]
    public void Malformed_items_document_retains_the_four_completed_catalog_parses()
    {
        File.WriteAllText(Data("items/items.json"), "[ bad ]"); var work = new TowerWorkAccounting();
        var expected = Assert.ThrowsAny<JsonException>(() => Load("equipment"));
        using (work.Activate())
        {
            var actual = Assert.ThrowsAny<JsonException>(() => Load("equipment", TowerContentJsonReader.Instance));
            Assert.Equal(expected.GetType(), actual.GetType()); Assert.Equal(expected.Message, actual.Message);
        }
        Assert.Equal(FileBytes(Inputs["equipment"]), work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(5, work.Snapshot()["jsonParseAttempts"]);
        Assert.Equal(4, work.Snapshot()["jsonParseCompleted"]);
    }

    [Fact]
    public void Validation_failure_retains_completed_parse_and_configured_BOM_text_semantics()
    {
        var directory = Path.Combine(root, "Alternate", "combat"); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "creature-abilities.json");
        const string text = "{\"creatures\":[{\"monsterId\":\"🐉\",\"abilityIds\":[\"a\",\"a\"]}]}";
        File.WriteAllText(path, text, Encoding.Unicode);
        var configured = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Alternate" }).Build();
        var expected = Assert.Throws<InvalidOperationException>(() => new JsonCreatureAbilityDefinitionProvider(configured, root, HarnessJson.Options));
        var work = new TowerWorkAccounting();
        using (work.Activate())
            Assert.Equal(expected.Message, Assert.Throws<InvalidOperationException>(() => new JsonCreatureAbilityDefinitionProvider(configured, root, HarnessJson.Options, TowerContentJsonReader.Instance)).Message);
        Assert.Equal(new FileInfo(path).Length, work.Snapshot()["applicationReadBytes.json"]);
        Assert.Equal(Encoding.UTF8.GetByteCount(text), work.Snapshot()["jsonInputBytes"]);
        Assert.Equal(1, work.Snapshot()["jsonParseCompleted"]);
    }

    [Fact]
    public void Floor_stream_preserves_existing_BOM_semantics_and_closes_handles()
    {
        var path = Data(TowerBattleRunner.FloorFile); File.WriteAllText(path, File.ReadAllText(path), new UTF8Encoding(true));
        object? expected = null, actual = null;
        var before = Record.Exception(() => expected = Load("floors")); var work = new TowerWorkAccounting(); Exception? after;
        using (work.Activate()) after = Record.Exception(() => actual = Load("floors", TowerContentJsonReader.Instance));
        Assert.Equal(before?.GetType(), after?.GetType()); Assert.Equal(before?.Message, after?.Message);
        if (before is null) Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(actual));
        Assert.True(work.Snapshot()["applicationReadBytes.json"] > 0);
        Assert.Equal(before is null ? 1 : 0, work.Snapshot().GetValueOrDefault("jsonParseCompleted"));
        using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }
}
