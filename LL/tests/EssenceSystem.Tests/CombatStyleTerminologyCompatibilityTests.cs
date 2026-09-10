using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.CombatStyles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Persistence.LL;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

public sealed class CombatStyleTerminologyCompatibilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Channeled_aliases_preserve_complete_historical_snapshot_shape_and_hash(bool web)
    {
        var options = Options(web);
        var legacy = LegacyElement(LegacySnapshot, web);
        var snapshot = legacy.Deserialize<CombatStyleSnapshot>(options)!;
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), snapshot.ChanneledPlayerEssenceId);
        Assert.Equal("essence.legacy", snapshot.ChanneledEssenceDefinitionId);
        Assert.Equal(.8, snapshot.Tuning.ChanneledBaseMultiplier);
        Assert.Equal(.2, snapshot.Tuning.ChanneledPerCharge);
        Assert.Null(snapshot.Tuning.ChanneledPerMasteryLevel);
        Assert.Equal(.06, snapshot.ChanneledMasteryBonus, 8);

        Assert.Equal(JsonSerializer.Serialize(legacy, options), JsonSerializer.Serialize(snapshot, options));
        if (web) Assert.Equal(HarnessJson.Hash(legacy), HarnessJson.Hash(snapshot));

        var changed = snapshot with
        {
            ChanneledEssenceDefinitionId = "essence.new",
            Tuning = snapshot.Tuning with { ChanneledPerMasteryLevel = .01 }
        };
        var json = JsonSerializer.SerializeToElement(changed, options);
        Assert.Equal("essence.new", json.GetProperty(web ? "focusEssenceDefinitionId" : "FocusEssenceDefinitionId").GetString());
        Assert.Equal(.01, json.GetProperty(web ? "tuning" : "Tuning")
            .GetProperty(web ? "focusPerMasteryLevel" : "FocusPerMasteryLevel").GetDouble());
        Assert.Equal(.06, changed.ChanneledMasteryBonus, 8);
        Assert.Equal("essence.legacy", snapshot.ChanneledEssenceDefinitionId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Channeled_diagnostics_and_retired_recipe_keep_historical_serialization(bool web)
    {
        var options = Options(web);
        var legacy = LegacyElement(LegacySummary, web);
        var summary = legacy.Deserialize<CombatStyleCombatSummary>(options)!;
        Assert.Equal(2, summary.ChanneledCastsByCharge[3]);
        Assert.Equal(2.8, summary.ChanneledMultiplierTotal);
        Assert.Equal(80, summary.ChanneledOutputAdded);
        Assert.Equal(40, summary.ChanneledOutputLost);
        Assert.Equal(JsonSerializer.Serialize(legacy, options), JsonSerializer.Serialize(summary, options));
        if (web) Assert.Equal(HarnessJson.Hash(legacy), HarnessJson.Hash(summary));

        var recipeJson = LegacyElement("""
            {"Id":"conduit","Level":5,"RefinementId":null,"UpgradeIds":null,"FocusEssenceDefinitionId":"essence.legacy"}
            """, web);
        var recipe = recipeJson.Deserialize<FixtureCombatStyle>(options)!;
        Assert.Equal("essence.legacy", recipe.ChanneledEssenceDefinitionId);
        Assert.Equal(JsonSerializer.Serialize(recipeJson, options), JsonSerializer.Serialize(recipe, options));
        if (web) Assert.Equal(HarnessJson.Hash(recipeJson), HarnessJson.Hash(recipe));
    }

    [Fact]
    public void Retired_manual_choice_uses_existing_database_columns()
    {
        using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        foreach (var type in new[] { typeof(CharacterCombatStyle), typeof(CharacterCombatStyleSelection) })
        {
            var entity = db.Model.FindEntityType(type)!;
            var property = entity.FindProperty(nameof(CharacterCombatStyle.ChanneledPlayerEssenceId))!;
            Assert.Equal("FocusPlayerEssenceId", property.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema())));
            Assert.Null(entity.FindProperty("FocusPlayerEssenceId"));
        }
    }

    private static JsonSerializerOptions Options(bool web) => web
        ? new(HarnessJson.Options) { WriteIndented = false }
        : new();

    private static JsonElement LegacyElement(string json, bool web)
    {
        var node = JsonNode.Parse(json)!;
        if (web)
        {
            // Historical harness JSON used camelCase and string enum values; persisted snapshots used PascalCase and numbers.
            if (node is JsonObject root && root.ContainsKey("Kind")) root["Kind"] = "Conduit";
            node = CamelCase(node);
        }
        return JsonSerializer.SerializeToElement(node);
    }

    private static JsonNode? CamelCase(JsonNode? node) => node switch
    {
        JsonObject value => new JsonObject(value.Select(pair =>
            new KeyValuePair<string, JsonNode?>(JsonNamingPolicy.CamelCase.ConvertName(pair.Key), CamelCase(pair.Value)))),
        JsonArray value => new JsonArray(value.Select(CamelCase).ToArray()),
        _ => node?.DeepClone()
    };

    private const string LegacySnapshot = """
        {"CombatStyleId":"conduit","Kind":2,"ContentVersion":"combat-styles.v4","Level":6,"CoreRank":3,
         "RefinementId":null,"UpgradeIds":[],"MasteredUpgradeId":null,
         "FocusPlayerEssenceId":"11111111-1111-1111-1111-111111111111","FocusEssenceDefinitionId":"essence.legacy",
         "Tuning":{"HealthFraction":0.25,"BarrierFraction":0.75,"BarrierPerCoreRank":0.02,
           "RebuildHealthThreshold":0.35,"CounterweightBarrierThreshold":0.2,"CounterweightBarrierCost":0.1,
           "ShelterOwnerShare":0.5,"PreparedWallHealthThreshold":0.8,"PreparedWallBarrierBonus":0.1,
           "HoldTheBreachBarrierBonus":0.1,"MeasuredRecoveryHealthBonus":0.2,"ChargeCap":3,"DistinctContributors":true,
           "FocusBaseMultiplier":0.8,"FocusPerCharge":0.2,"FocusPerCoreRank":0.02,"RelayMinimumSpent":0,
           "RelayChargeReturn":0,"FullCircuitBonus":0.05,"PartialFlowBonus":0.05,
           "EmergencyChannelHealthThreshold":0.35,"EmergencyChannelBonus":0.05},
         "MilestoneTuning":{"OpeningBarrierFraction":0,"OpeningCharge":0,"PreparedWallEmptyBarrier":false,
           "HoldTheBreachHealthBonus":0,"MeasuredRecoveryOverhealBarrierFraction":0,"FullCircuitMinimumCharge":0,
           "PartialFlowMaximumCharge":0,"EmergencyChannelHealthThreshold":0}}
        """;

    private const string LegacySummary = """
        {"EntityId":"actor","CombatStyleId":"conduit","Level":6,"RefinementId":null,
         "HealingConverted":0,"HealthRestored":0,"HealthRecoveryWasted":0,"ConvertedBarrierGranted":0,
         "ConvertedBarrierAbsorbed":0,"BarrierOverflow":0,"CounterweightBarrierSpent":0,"CounterweightDamage":0,
         "ShelterRecipients":{},"Charge":1,"ChargeGenerated":7,"ChargeSpent":6,"RelayChargeReturned":0,
         "Contributors":[],"FocusCastsByCharge":{"3":2},"FocusMultiplierTotal":2.8,"FocusOutputAdded":80,"FocusOutputLost":40}
        """;
}
