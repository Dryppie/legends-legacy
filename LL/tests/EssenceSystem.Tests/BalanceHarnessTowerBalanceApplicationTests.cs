using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBalanceApplicationTests
{
    private sealed record Request(string Source, string ManifestPin, string Audit, string AuditPin, string Output,
        string? QualificationPlan = null, string? QualificationPlanPin = null,
        string? Aggregate = null, string? AggregatePin = null, string? CandidateRoot = null);
    private sealed record Qualification(string Version, string Source, string SourceManifestSha256, int Floor,
        Dictionary<string, string> SourceContentHashes, Dictionary<string, string> CurrentContentHashes,
        Dictionary<string, string> AssemblyHashes, string TestAssemblySha256);
    private sealed class OwnedFactAttribute : FactAttribute
    {
        public OwnedFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_BALANCE_APPLICATION")))
            Skip = "Requires a confirmed Tower family and a bounded local application check."; }
    }

    internal static void ValidateAggregatePanel(JsonElement evidence, JsonElement declaration)
    {
        var version = evidence.GetProperty("version").GetString();
        var recovery = version == "tower-balance-recovery-aggregate-v1";
        var limitedArmor = version == "tower-balance-limited-armor-aggregate-v1";
        var limitedResistance = version == "tower-balance-limited-resistance-aggregate-v1";
        var miasma = version == "tower-balance-miasma-aggregate-v1";
        var miasmaResource = version == "tower-balance-miasma-resource-aggregate-v1";
        var sharedPenetration = version == "tower-balance-shared-penetration-aggregate-v1";
        var fixedKodoku = version == "tower-balance-kodoku-fixed-aggregate-v1";
        var midpointKodoku = version == "tower-balance-kodoku-midpoint-aggregate-v1";
        var fixedAcceptance = fixedKodoku || midpointKodoku;
        if ((version != "tower-balance-aggregate-v1" && !recovery && !limitedArmor && !limitedResistance && !miasma && !miasmaResource && !sharedPenetration && !fixedAcceptance) ||
            declaration.GetProperty("version").GetString() != version ||
            evidence.GetProperty("status").GetString() != "Verified" || evidence.GetProperty("phase").GetString() != "confirm" ||
            evidence.GetProperty("assessment").GetProperty("verdict").GetString() != "Pass")
            throw new InvalidDataException("A passing aggregate confirmation is required.");
        var batches = evidence.GetProperty("batches").EnumerateArray().ToArray();
        var paths = declaration.GetProperty("phases").GetProperty("confirm").EnumerateArray().Select(p => p.GetString()).ToArray();
        var family = declaration.GetProperty("familySize").GetInt32();
        var samples = declaration.GetProperty("samplesPerBatch").GetInt32();
        var count = declaration.GetProperty("batchCount").GetInt32();
        var assessment = evidence.GetProperty("assessment");
        if (recovery && (family != 120 || declaration.GetProperty("floor").GetInt32() != 7 ||
            declaration.GetProperty("candidatePlan").GetProperty("version").GetString() != "tower-recovery-pressure-refinement-v1"))
            throw new InvalidDataException("The recovery aggregate requires the fixed floor-seven candidate and family.");
        if ((limitedArmor || limitedResistance) && (family != (limitedResistance ? 148 : 177) || declaration.GetProperty("floor").GetInt32() != (limitedResistance ? 9 : 8) ||
            declaration.GetProperty("candidatePlan").GetProperty("version").GetString() != "tower-unchanged-catalog-v1" ||
            declaration.GetProperty("candidatePlan").EnumerateObject().Count() != 1))
            throw new InvalidDataException("The limited-equipment aggregate requires unchanged content and its exact floor and family.");
        if (miasma || miasmaResource)
        {
            if (family != 177 || declaration.GetProperty("floor").GetInt32() != 8)
                throw new InvalidDataException("Miasma requires the complete floor-eight family.");
            ValidateMiasmaPlan(declaration.GetProperty("candidatePlan"));
        }
        if (sharedPenetration || fixedAcceptance)
        {
            if (family != (fixedAcceptance ? 186 : 177) || declaration.GetProperty("floor").GetInt32() != 8)
                throw new InvalidDataException("Shared penetration requires the complete floor-eight family.");
            ValidateSharedPenetrationPlan(declaration.GetProperty("candidatePlan"), fixedKodoku, midpointKodoku);
        }
        if (count != (fixedAcceptance ? 32 : recovery || miasmaResource || sharedPenetration ? 8 : 4) || samples != (miasmaResource || sharedPenetration || fixedAcceptance ? 16 : limitedArmor || limitedResistance || miasma ? 32 : 128) || batches.Length != count || paths.Length != count || paths.Distinct().Count() != count ||
            !paths.SequenceEqual(batches.Select(b => b.GetProperty("source").GetString())) ||
            assessment.GetProperty("familySize").GetInt32() != family || assessment.GetProperty("samples").GetInt32() != count*samples ||
            evidence.GetProperty("evaluationFights").GetInt32() != count*samples*family ||
            evidence.GetProperty("screeningManifests").GetArrayLength() != count)
            throw new InvalidDataException("Every declared complete confirmation batch is required.");
        var seeds = new HashSet<int>();
        foreach (var batch in batches)
            if (batch.GetProperty("fights").GetInt32() != family*samples || batch.GetProperty("seeds").GetArrayLength() != samples ||
                batch.GetProperty("seeds").EnumerateArray().Any(s => !seeds.Add(s.GetInt32())))
                throw new InvalidDataException("Confirmation seed panels must be complete and disjoint.");
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("repeated")]
    [InlineData("overlap")]
    [InlineData("wrong-count")]
    public void Aggregate_application_requires_complete_independent_confirmation(string change)
    {
        var paths = Enumerable.Range(0, 4).Select(i => "batch-" + i).ToArray();
        var declaration = JsonSerializer.SerializeToElement(new { version = "tower-balance-aggregate-v1", familySize = 115, samplesPerBatch = 128, batchCount = 4,
            phases = new { confirm = paths } });
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version = "tower-balance-aggregate-v1", status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 115, samples = 512 }, evaluationFights = 58880,
            screeningManifests = paths, batches = paths.Select((p, i) => new { source = p, fights = 14720, seeds = Enumerable.Range(i*128, 128).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(3);
        if (change == "repeated") evidence["batches"]![1]!["source"] = "batch-0";
        if (change == "overlap") evidence["batches"]![1]!["seeds"]![0] = 0;
        if (change == "wrong-count") evidence["assessment"]!["samples"] = 128;
        var value = JsonSerializer.SerializeToElement(evidence);
        if (change == "valid") ValidateAggregatePanel(value, declaration);
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(value, declaration));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("repeated")]
    [InlineData("overlap")]
    [InlineData("wrong-count")]
    [InlineData("legacy-version")]
    [InlineData("batch-count")]
    [InlineData("wrong-candidate")]
    [InlineData("wrong-floor")]
    [InlineData("wrong-family")]
    public void Aggregate_recovery_requires_all_eight_independent_batches(string change)
    {
        var paths = Enumerable.Range(0, 8).Select(i => "batch-" + i).ToArray();
        var declaration = JsonNode.Parse(JsonSerializer.Serialize(new { version = "tower-balance-recovery-aggregate-v1",
            floor = 7, familySize = 120, samplesPerBatch = 128, batchCount = 8,
            candidatePlan = new { version = "tower-recovery-pressure-refinement-v1" }, phases = new { confirm = paths } }))!;
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version = "tower-balance-recovery-aggregate-v1", status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 120, samples = 1024 }, evaluationFights = 122880,
            screeningManifests = paths, batches = paths.Select((p, i) => new { source = p, fights = 15360, seeds = Enumerable.Range(i*128, 128).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(7);
        if (change == "repeated") evidence["batches"]![1]!["source"] = "batch-0";
        if (change == "overlap") evidence["batches"]![1]!["seeds"]![0] = 0;
        if (change == "wrong-count") evidence["assessment"]!["samples"] = 512;
        if (change == "legacy-version") evidence["version"] = "tower-balance-aggregate-v1";
        if (change == "batch-count") declaration["batchCount"] = 4;
        if (change == "wrong-candidate") declaration["candidatePlan"]!["version"] = "tower-recovery-pressure-candidate-v1";
        if (change == "wrong-floor") declaration["floor"] = 4;
        if (change == "wrong-family") declaration["familySize"] = 119;
        if (change == "valid") ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration)));
    }

    public static IEnumerable<object[]> LimitedEquipmentAggregateCases()
    {
        foreach (var floor in new[] { 8, 9 })
            foreach (var change in new[] { "valid", "screen", "rejected", "missing", "repeated", "overlap", "wrong-count", "legacy-version", "batch-count", "wrong-candidate", "extra-candidate-field", "wrong-floor", "wrong-family", "wrong-samples", "missing-screen" })
                yield return new object[] { change, floor };
    }

    [Theory]
    [MemberData(nameof(LimitedEquipmentAggregateCases))]
    public void Aggregate_limited_equipment_requires_four_complete_32_seed_batches(string change, int floor)
    {
        var version = floor == 9 ? "tower-balance-limited-resistance-aggregate-v1" : "tower-balance-limited-armor-aggregate-v1";
        var family = floor == 9 ? 148 : 177;
        var paths = Enumerable.Range(0, 4).Select(i => "batch-" + i).ToArray();
        var declaration = JsonNode.Parse(JsonSerializer.Serialize(new { version,
            floor, familySize = family, samplesPerBatch = 32, batchCount = 4,
            candidatePlan = new { version = "tower-unchanged-catalog-v1" }, phases = new { confirm = paths } }))!;
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = family, samples = 128 }, evaluationFights = family*128,
            screeningManifests = paths, batches = paths.Select((p, i) => new { source = p, fights = family*32, seeds = Enumerable.Range(i*32, 32).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(3);
        if (change == "repeated") evidence["batches"]![1]!["source"] = "batch-0";
        if (change == "overlap") evidence["batches"]![1]!["seeds"]![0] = 0;
        if (change == "wrong-count") evidence["assessment"]!["samples"] = 32;
        if (change == "legacy-version") evidence["version"] = "tower-balance-aggregate-v1";
        if (change == "batch-count") declaration["batchCount"] = 8;
        if (change == "wrong-candidate") declaration["candidatePlan"]!["version"] = "tower-health-pressure-candidate-v1";
        if (change == "extra-candidate-field") declaration["candidatePlan"]!["offenseFactor"] = .9;
        if (change == "wrong-floor") declaration["floor"] = 7;
        if (change == "wrong-family") declaration["familySize"] = family-1;
        if (change == "wrong-samples") declaration["samplesPerBatch"] = 128;
        if (change == "missing-screen") evidence["screeningManifests"]!.AsArray().RemoveAt(3);
        if (change == "valid") ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration)));
    }

    private static JsonNode MiasmaPlan(string abilities, string tower)
    {
        var plan = JsonNode.Parse("""
        {
          "version":"tower-kodoku-miasma-v1",
          "abilityId":"ability.creature.kodoku.withering_miasma",
          "removeEffect":{"id":"effect.creature.kodoku.withering_miasma.regeneration","operation":"ModifyRegenerationRate","target":"AllEnemies","baseValue":-80,"durationTicks":150,"procCoefficient":1},
          "preserveEffects":[{"id":"effect.creature.kodoku.withering_miasma.healing","operation":"ModifyHealingReceived","target":"AllEnemies","baseValue":-80,"durationTicks":150,"procCoefficient":1}],
          "descriptionFrom":"Reduce all enemies' healing received and Health Regeneration by 80% for 15 seconds.",
          "descriptionTo":"Reduce all enemies' healing received, including Health Regeneration amounts, by 80% for 15 seconds."
        }
        """)!;
        plan["sourceAbilitiesSha256"] = abilities;
        plan["sourceTowerSha256"] = tower;
        return plan;
    }

    private static void ValidateMiasmaPlan(JsonElement plan)
    {
        var abilities = plan.GetProperty("sourceAbilitiesSha256").GetString()!;
        var tower = plan.GetProperty("sourceTowerSha256").GetString()!;
        if (new[] { abilities, tower }.Any(h => h.Length != 64 || h.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f'))) ||
            !JsonElement.DeepEquals(plan, JsonSerializer.SerializeToElement(MiasmaPlan(abilities, tower))))
            throw new InvalidDataException("Only the declared extra Miasma regeneration-rate effect may be removed.");
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("overlap")]
    [InlineData("wrong-family")]
    [InlineData("wrong-floor")]
    [InlineData("wrong-samples")]
    [InlineData("wrong-count")]
    [InlineData("missing-screen")]
    [InlineData("wrong-version")]
    [InlineData("healing")]
    [InlineData("duration")]
    [InlineData("remove-other")]
    [InlineData("extra")]
    [InlineData("description")]
    [InlineData("hash")]
    public void Aggregate_miasma_requires_exact_candidate_and_complete_panel(string change)
    {
        var paths = Enumerable.Range(0, 4).Select(i => "batch-" + i).ToArray();
        var declaration = JsonNode.Parse(JsonSerializer.Serialize(new { version = "tower-balance-miasma-aggregate-v1",
            floor = 8, familySize = 177, samplesPerBatch = 32, batchCount = 4, phases = new { confirm = paths } }))!;
        declaration["candidatePlan"] = MiasmaPlan(new string('0', 64), new string('1', 64));
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version = "tower-balance-miasma-aggregate-v1", status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 177, samples = 128 }, evaluationFights = 22656, screeningManifests = paths,
            batches = paths.Select((p, i) => new { source = p, fights = 5664, seeds = Enumerable.Range(i*32, 32).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(3);
        if (change == "overlap") evidence["batches"]![1]!["seeds"]![0] = 0;
        if (change == "wrong-family") declaration["familySize"] = 176;
        if (change == "wrong-floor") declaration["floor"] = 7;
        if (change == "wrong-samples") declaration["samplesPerBatch"] = 128;
        if (change == "wrong-count") declaration["batchCount"] = 8;
        if (change == "missing-screen") evidence["screeningManifests"]!.AsArray().RemoveAt(3);
        if (change == "wrong-version") declaration["candidatePlan"]!["version"] = "tower-unchanged-catalog-v1";
        if (change == "healing") declaration["candidatePlan"]!["preserveEffects"]![0]!["baseValue"] = -60;
        if (change == "duration") declaration["candidatePlan"]!["removeEffect"]!["durationTicks"] = 149;
        if (change == "remove-other") declaration["candidatePlan"]!["removeEffect"]!["id"] = "other";
        if (change == "extra") declaration["candidatePlan"]!["offenseFactor"] = .5;
        if (change == "description") declaration["candidatePlan"]!["descriptionTo"] = "changed";
        if (change == "hash") declaration["candidatePlan"]!["sourceTowerSha256"] = "bad";
        if (change == "valid") ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration)));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("repeated")]
    [InlineData("overlap")]
    [InlineData("wrong-family")]
    [InlineData("wrong-floor")]
    [InlineData("wrong-samples")]
    [InlineData("wrong-count")]
    [InlineData("missing-screen")]
    [InlineData("wrong-version")]
    [InlineData("healing")]
    [InlineData("duration")]
    [InlineData("extra")]
    [InlineData("hash")]
    [InlineData("old-layout")]
    [InlineData("mixed-layout")]
    [InlineData("wrong-fights")]
    [InlineData("wrong-total")]
    public void Aggregate_miasma_resource_requires_eight_complete_16_seed_batches(string change)
    {
        var paths = Enumerable.Range(0, 8).Select(i => "batch-" + i).ToArray();
        const string version = "tower-balance-miasma-resource-aggregate-v1";
        var declaration = JsonNode.Parse(JsonSerializer.Serialize(new { version,
            floor = 8, familySize = 177, samplesPerBatch = 16, batchCount = 8, phases = new { confirm = paths } }))!;
        declaration["candidatePlan"] = MiasmaPlan(new string('0', 64), new string('1', 64));
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 177, samples = 128 }, evaluationFights = 22656, screeningManifests = paths,
            batches = paths.Select((p, i) => new { source = p, fights = 2832, seeds = Enumerable.Range(i*16, 16).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(7);
        if (change == "repeated") evidence["batches"]![7]!["source"] = "batch-0";
        if (change == "overlap") evidence["batches"]![7]!["seeds"]![0] = 0;
        if (change == "wrong-family") declaration["familySize"] = 176;
        if (change == "wrong-floor") declaration["floor"] = 7;
        if (change == "wrong-samples") declaration["samplesPerBatch"] = 32;
        if (change == "wrong-count") declaration["batchCount"] = 4;
        if (change == "missing-screen") evidence["screeningManifests"]!.AsArray().RemoveAt(7);
        if (change == "wrong-version") declaration["candidatePlan"]!["version"] = "tower-unchanged-catalog-v1";
        if (change == "healing") declaration["candidatePlan"]!["preserveEffects"]![0]!["baseValue"] = -60;
        if (change == "duration") declaration["candidatePlan"]!["removeEffect"]!["durationTicks"] = 149;
        if (change == "extra") declaration["candidatePlan"]!["offenseFactor"] = .5;
        if (change == "hash") declaration["candidatePlan"]!["sourceTowerSha256"] = "bad";
        if (change == "old-layout")
        {
            declaration["version"] = "tower-balance-miasma-aggregate-v1";
            evidence["version"] = "tower-balance-miasma-aggregate-v1";
        }
        if (change == "mixed-layout") evidence["batches"]![7]!["seeds"] = JsonSerializer.SerializeToNode(Enumerable.Range(112, 32));
        if (change == "wrong-fights") evidence["batches"]![7]!["fights"] = 5664;
        if (change == "wrong-total") evidence["assessment"]!["samples"] = 256;
        if (change == "valid") ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration)));
    }

    private static JsonNode SharedPenetrationPlan(string abilities, string tower, string summons)
    {
        var plan = JsonNode.Parse("""
        {"version":"tower-kodoku-shared-penetration-v1","floor":8,"offenseFactor":0.525,"penetrationFactor":40,"sourceOffense":8.8260253906,"sourcePenetration":1.0,"summonId":"venomSpawn","appendAttribute":{"attribute":"ArmorPenetration","baseValue":0,"minimumValue":0,"scalingAttribute":"ArmorPenetration","scalingCoefficient":1}}
        """)!;
        plan["sourceAbilitiesSha256"] = abilities; plan["sourceTowerSha256"] = tower; plan["sourceSummonsSha256"] = summons;
        return plan;
    }

    private static void ValidateSharedPenetrationPlan(JsonElement plan, bool fixedKodoku = false, bool midpointKodoku = false)
    {
        var hashes = new[] { "sourceAbilitiesSha256", "sourceTowerSha256", "sourceSummonsSha256" }
            .Select(k => plan.GetProperty(k).GetString()!).ToArray();
        var expected = SharedPenetrationPlan(hashes[0], hashes[1], hashes[2]);
        if (fixedKodoku)
        {
            expected["version"] = "tower-kodoku-eight-item-pressure-refinement-v1";
            expected["offenseFactor"] = .575;
        }
        if (midpointKodoku)
        {
            expected["version"] = "tower-kodoku-eight-item-pressure-midpoint-v1";
            expected["offenseFactor"] = .5875;
        }
        if (hashes.Any(h => h.Length != 64 || h.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f'))) ||
            !JsonElement.DeepEquals(plan, JsonSerializer.SerializeToElement(expected)))
            throw new InvalidDataException("Only the frozen floor-eight shared-penetration candidate is supported.");
    }

    private static void ValidateQualifiedSummonsDelta(string acceptedVersion, JsonElement candidate, JsonNode before, JsonNode after)
    {
        var fixedKodoku = acceptedVersion == "applied-tower-kodoku-fixed-aggregate-v1";
        var midpointKodoku = acceptedVersion == "applied-tower-kodoku-midpoint-aggregate-v1";
        if (!fixedKodoku && !midpointKodoku && acceptedVersion != "applied-tower-shared-penetration-aggregate-v1")
            throw new InvalidDataException("Summon qualification requires an accepted Kodoku aggregate.");
        ValidateSharedPenetrationPlan(candidate, fixedKodoku, midpointKodoku);
        var expected = before.DeepClone();
        var matches = expected.AsArray().Where(s => s?["id"]?.GetValue<string>() == "venomSpawn").ToArray();
        if (matches.Length != 1)
            throw new InvalidDataException("Exactly one original Venomspawn is required.");
        var attributes = matches[0]!["attributes"]!.AsArray();
        if (attributes.Any(a => a?["attribute"]?.GetValue<string>() == "ArmorPenetration"))
            throw new InvalidDataException("Historical Venomspawn already has penetration.");
        attributes.Add(JsonNode.Parse(candidate.GetProperty("appendAttribute").GetRawText()));
        if (!JsonNode.DeepEquals(expected, after))
            throw new InvalidDataException("Only the accepted Venomspawn penetration inheritance may change.");
    }

    private static void ValidateQualifiedSummons(JsonElement qualification, string source, string current)
    {
        if (!qualification.TryGetProperty("acceptedAggregate", out var accepted))
            throw new InvalidDataException("Changed summons require an accepted aggregate.");
        JsonElement BoundReceipt(string name)
        {
            var path = accepted.GetProperty(name).GetString()!;
            if (qualification.GetProperty("receiptPins").GetProperty(path).GetString() != HarnessJson.FileHash(path))
                throw new InvalidDataException("Accepted summon qualification receipt changed.");
            return HarnessJson.Read<JsonElement>(path);
        }
        var declaration = BoundReceipt("declaration");
        var confirmation = BoundReceipt("confirm");
        var completion = BoundReceipt("completion");
        var version = accepted.GetProperty("version").GetString()!;
        if (declaration.GetProperty("version").GetString() != version.Replace("applied-tower-", "tower-balance-") ||
            confirmation.GetProperty("declarationSha256").GetString() != HarnessJson.FileHash(accepted.GetProperty("declaration").GetString()!))
            throw new InvalidDataException("Accepted summon aggregate binding differs.");
        ValidateAggregatePanel(confirmation, declaration);
        const string summons = "combat/summons.json";
        var beforePath = Path.Combine(source, "content/Data", summons);
        var afterPath = Path.Combine(current, "Data", summons);
        var candidate = declaration.GetProperty("candidatePlan");
        var currentHash = HarnessJson.FileHash(afterPath);
        if (candidate.GetProperty("sourceSummonsSha256").GetString() != HarnessJson.FileHash(beforePath) ||
            declaration.GetProperty("candidateContentHashes").GetProperty(summons).GetString() != currentHash ||
            completion.GetProperty("status").GetString() != "AppliedAndVerified" ||
            completion.GetProperty("newSeeds").GetInt32() != 0 ||
            completion.GetProperty("matchedInputs").GetInt32() != confirmation.GetProperty("evaluationFights").GetInt32() ||
            completion.GetProperty("fullReplays").GetInt32() != declaration.GetProperty("familySize").GetInt32() * declaration.GetProperty("batchCount").GetInt32() ||
            completion.GetProperty("afterSummonsSha256").GetString() != currentHash)
            throw new InvalidDataException("Summon qualification requires the exact fully applied candidate.");
        ValidateQualifiedSummonsDelta(version, candidate,
            JsonNode.Parse(File.ReadAllText(beforePath))!, JsonNode.Parse(File.ReadAllText(afterPath))!);
    }

    [Theory]
    [InlineData("shared", "valid")]
    [InlineData("fixed", "valid")]
    [InlineData("midpoint", "valid")]
    [InlineData("midpoint", "unaccepted")]
    [InlineData("midpoint", "other-plan")]
    [InlineData("midpoint", "other-coefficient")]
    [InlineData("midpoint", "other-summon")]
    [InlineData("midpoint", "other-attribute")]
    [InlineData("midpoint", "missing-venom")]
    [InlineData("midpoint", "duplicate-venom")]
    [InlineData("midpoint", "existing-penetration")]
    [InlineData("midpoint", "missing-inheritance")]
    [InlineData("midpoint", "duplicate-inheritance")]
    [InlineData("midpoint", "wrong-inheritance")]
    public void Catalog_qualification_allows_only_the_accepted_summon_delta(string kind, string change)
    {
        var version = kind == "shared" ? "applied-tower-shared-penetration-aggregate-v1" :
            kind == "fixed" ? "applied-tower-kodoku-fixed-aggregate-v1" : "applied-tower-kodoku-midpoint-aggregate-v1";
        var candidate = SharedPenetrationPlan(new string('0', 64), new string('1', 64), new string('2', 64));
        if (kind != "shared")
        {
            candidate["version"] = kind == "fixed" ? "tower-kodoku-eight-item-pressure-refinement-v1" : "tower-kodoku-eight-item-pressure-midpoint-v1";
            candidate["offenseFactor"] = kind == "fixed" ? .575 : .5875;
        }
        var before = JsonNode.Parse("""[{"id":"other","attributes":[]},{"id":"venomSpawn","attributes":[{"attribute":"Power","scalingCoefficient":0.15}]}]""")!;
        var after = before.DeepClone();
        after[1]!["attributes"]!.AsArray().Add(candidate["appendAttribute"]!.DeepClone());
        if (change == "unaccepted") version = "applied-tower-recovery-aggregate-v1";
        if (change == "other-plan") candidate["version"] = "tower-kodoku-eight-item-pressure-refinement-v1";
        if (change == "other-coefficient") candidate["offenseFactor"] = .575;
        if (change == "other-summon") after[0]!["name"] = "changed";
        if (change == "other-attribute") after[1]!["attributes"]![0]!["scalingCoefficient"] = .2;
        if (change == "missing-venom") before.AsArray().RemoveAt(1);
        if (change == "duplicate-venom") before.AsArray().Add(before[1]!.DeepClone());
        if (change == "existing-penetration") before[1]!["attributes"]!.AsArray().Add(candidate["appendAttribute"]!.DeepClone());
        if (change == "missing-inheritance") after[1]!["attributes"]!.AsArray().RemoveAt(1);
        if (change == "duplicate-inheritance") after[1]!["attributes"]!.AsArray().Add(candidate["appendAttribute"]!.DeepClone());
        if (change == "wrong-inheritance") after[1]!["attributes"]![1]!["scalingCoefficient"] = .5;
        if (change == "valid") ValidateQualifiedSummonsDelta(version, JsonSerializer.SerializeToElement(candidate), before, after);
        else Assert.Throws<InvalidDataException>(() => ValidateQualifiedSummonsDelta(version, JsonSerializer.SerializeToElement(candidate), before, after));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("screen")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("repeated")]
    [InlineData("overlap")]
    [InlineData("wrong-family")]
    [InlineData("wrong-floor")]
    [InlineData("wrong-samples")]
    [InlineData("wrong-count")]
    [InlineData("missing-screen")]
    [InlineData("wrong-version")]
    [InlineData("healing")]
    [InlineData("duration")]
    [InlineData("extra")]
    [InlineData("hash")]
    [InlineData("old-layout")]
    [InlineData("mixed-layout")]
    [InlineData("wrong-fights")]
    [InlineData("wrong-total")]
    public void Aggregate_shared_penetration_requires_exact_candidate_and_eight_complete_batches(string change)
    {
        var paths = Enumerable.Range(0, 8).Select(i => "batch-" + i).ToArray();
        const string version = "tower-balance-shared-penetration-aggregate-v1";
        var declaration = JsonNode.Parse(JsonSerializer.Serialize(new { version,
            floor = 8, familySize = 177, samplesPerBatch = 16, batchCount = 8, phases = new { confirm = paths } }))!;
        declaration["candidatePlan"] = SharedPenetrationPlan(new string('0', 64), new string('1', 64), new string('2', 64));
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 177, samples = 128 }, evaluationFights = 22656, screeningManifests = paths,
            batches = paths.Select((p, i) => new { source = p, fights = 2832, seeds = Enumerable.Range(i*16, 16).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(7);
        if (change == "repeated") evidence["batches"]![7]!["source"] = "batch-0";
        if (change == "overlap") evidence["batches"]![7]!["seeds"]![0] = 0;
        if (change == "wrong-family") declaration["familySize"] = 176;
        if (change == "wrong-floor") declaration["floor"] = 7;
        if (change == "wrong-samples") declaration["samplesPerBatch"] = 32;
        if (change == "wrong-count") declaration["batchCount"] = 4;
        if (change == "missing-screen") evidence["screeningManifests"]!.AsArray().RemoveAt(7);
        if (change == "wrong-version") declaration["candidatePlan"]!["version"] = "tower-unchanged-catalog-v1";
        if (change == "healing") declaration["candidatePlan"]!["appendAttribute"]!["scalingCoefficient"] = .5;
        if (change == "duration") declaration["candidatePlan"]!["penetrationFactor"] = 39;
        if (change == "extra") declaration["candidatePlan"]!["offenseFactor"] = .5;
        if (change == "hash") declaration["candidatePlan"]!["sourceTowerSha256"] = "bad";
        if (change == "old-layout")
        {
            declaration["version"] = "tower-balance-miasma-aggregate-v1";
            evidence["version"] = "tower-balance-miasma-aggregate-v1";
        }
        if (change == "mixed-layout") evidence["batches"]![7]!["seeds"] = JsonSerializer.SerializeToNode(Enumerable.Range(112, 32));
        if (change == "wrong-fights") evidence["batches"]![7]!["fights"] = 5664;
        if (change == "wrong-total") evidence["assessment"]!["samples"] = 256;
        if (change == "valid") ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("items")]
    [InlineData("characters")]
    [InlineData("gear")]
    [InlineData("version")]
    public void Aggregate_shared_penetration_cannot_weaken_equipment_contract(string change)
        => AssertAggregateEquipmentContract(change, "tower-kodoku-shared-penetration-v1");

    public static IEnumerable<object[]> FixedKodokuCases()
    {
        foreach (var midpoint in new[] { false, true })
            foreach (var change in new[] { "valid", "screen", "rejected", "missing", "repeated", "overlap", "wrong-family", "wrong-floor", "wrong-samples", "wrong-count", "missing-screen", "wrong-version", "healing", "duration", "extra", "hash", "old-layout", "mixed-layout", "wrong-fights", "wrong-total", "other-plan", "other-coefficient", "other-aggregate" })
                yield return new object[] { change, midpoint };
    }

    [Theory]
    [MemberData(nameof(FixedKodokuCases))]
    public void Aggregate_fixed_kodoku_requires_exact_candidate_and_thirty_two_complete_batches(string change, bool midpoint)
    {
        var paths = Enumerable.Range(0, 32).Select(i => "batch-" + i).ToArray();
        var version = midpoint ? "tower-balance-kodoku-midpoint-aggregate-v1" : "tower-balance-kodoku-fixed-aggregate-v1";
        var declaration = JsonNode.Parse(JsonSerializer.Serialize(new { version,
            floor = 8, familySize = 186, samplesPerBatch = 16, batchCount = 32, phases = new { confirm = paths } }))!;
        declaration["candidatePlan"] = SharedPenetrationPlan(new string('0', 64), new string('1', 64), new string('2', 64));
        declaration["candidatePlan"]!["version"] = midpoint ? "tower-kodoku-eight-item-pressure-midpoint-v1" : "tower-kodoku-eight-item-pressure-refinement-v1";
        declaration["candidatePlan"]!["offenseFactor"] = midpoint ? .5875 : .575;
        if (change == "other-plan") declaration["candidatePlan"]!["version"] = midpoint ? "tower-kodoku-eight-item-pressure-refinement-v1" : "tower-kodoku-eight-item-pressure-midpoint-v1";
        if (change == "other-coefficient") declaration["candidatePlan"]!["offenseFactor"] = midpoint ? .575 : .5875;
        var evidence = JsonNode.Parse(JsonSerializer.Serialize(new { version, status = "Verified", phase = "confirm",
            assessment = new { verdict = "Pass", familySize = 186, samples = 512 }, evaluationFights = 95232, screeningManifests = paths,
            batches = paths.Select((p, i) => new { source = p, fights = 2976, seeds = Enumerable.Range(i*16, 16).ToArray() }) }))!;
        if (change == "screen") evidence["phase"] = "screen";
        if (change == "rejected") evidence["assessment"]!["verdict"] = "NotAccepted";
        if (change == "missing") evidence["batches"]!.AsArray().RemoveAt(31);
        if (change == "repeated") evidence["batches"]![31]!["source"] = "batch-0";
        if (change == "overlap") evidence["batches"]![31]!["seeds"]![0] = 0;
        if (change == "wrong-family") declaration["familySize"] = 176;
        if (change == "wrong-floor") declaration["floor"] = 7;
        if (change == "wrong-samples") declaration["samplesPerBatch"] = 32;
        if (change == "wrong-count") declaration["batchCount"] = 4;
        if (change == "missing-screen") evidence["screeningManifests"]!.AsArray().RemoveAt(31);
        if (change == "wrong-version") declaration["candidatePlan"]!["version"] = "tower-unchanged-catalog-v1";
        if (change == "healing") declaration["candidatePlan"]!["appendAttribute"]!["scalingCoefficient"] = .5;
        if (change == "duration") declaration["candidatePlan"]!["penetrationFactor"] = 39;
        if (change == "extra") declaration["candidatePlan"]!["offenseFactor"] = .5;
        if (change == "hash") declaration["candidatePlan"]!["sourceTowerSha256"] = "bad";
        if (change == "old-layout")
        {
            declaration["version"] = "tower-balance-miasma-aggregate-v1";
            evidence["version"] = "tower-balance-miasma-aggregate-v1";
        }
        if (change == "other-aggregate")
        {
            var other = midpoint ? "tower-balance-kodoku-fixed-aggregate-v1" : "tower-balance-kodoku-midpoint-aggregate-v1";
            declaration["version"] = other; evidence["version"] = other;
        }
        if (change == "mixed-layout") evidence["batches"]![31]!["seeds"] = JsonSerializer.SerializeToNode(Enumerable.Range(112, 32));
        if (change == "wrong-fights") evidence["batches"]![31]!["fights"] = 5664;
        if (change == "wrong-total") evidence["assessment"]!["samples"] = 256;
        if (change == "valid") ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration));
        else Assert.Throws<InvalidDataException>(() => ValidateAggregatePanel(JsonSerializer.SerializeToElement(evidence), JsonSerializer.SerializeToElement(declaration)));
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData("items", false)]
    [InlineData("characters", false)]
    [InlineData("gear", false)]
    [InlineData("version", false)]
    [InlineData("missing", true)]
    [InlineData("items", true)]
    [InlineData("characters", true)]
    [InlineData("gear", true)]
    [InlineData("version", true)]
    public void Aggregate_fixed_kodoku_cannot_weaken_equipment_contract(string change, bool midpoint)
        => AssertAggregateEquipmentContract(change, midpoint ? "tower-kodoku-eight-item-pressure-midpoint-v1" : "tower-kodoku-eight-item-pressure-refinement-v1");

    internal static void ValidateSingleCandidateConfirmation(JsonElement request, JsonElement audit)
    {
        if (request.GetProperty("mode").GetString() != "confirm" ||
            request.GetProperty("searchSeeds").GetArrayLength() != 0 ||
            audit.GetProperty("status").GetString() != "Verified" ||
            audit.GetProperty("assessment").GetProperty("verdict").GetString() != "Pass")
            throw new InvalidDataException("An isolated single candidate requires a verified passing confirmation.");
    }

    internal static void ValidateSingleCandidateEquipment(JsonElement cells, JsonElement audit, string boundsProperty = "bounds")
    {
        var family = cells.EnumerateArray().ToArray();
        var assessment = audit.GetProperty("assessment");
        var bounds = assessment.GetProperty(boundsProperty).EnumerateArray().ToArray();
        var ids = family.Select(c => c.GetProperty("id").GetString()).ToArray();
        if (ids.Length == 0 || ids.Distinct().Count() != ids.Length ||
            assessment.GetProperty("familySize").GetInt32() != ids.Length ||
            !ids.SequenceEqual(bounds.Select(b => b.GetProperty("id").GetString())))
            throw new InvalidDataException("Every confirmed recipe and bound must be retained exactly once.");
        var qualifying = new HashSet<string>();
        foreach (var (cell, bound) in family.Zip(bounds))
        {
            var lower = bound.GetProperty("lower").GetDouble();
            var upper = bound.GetProperty("upper").GetDouble();
            if (!(0 <= lower && lower <= upper && upper <= .5))
                throw new InvalidDataException("Every recipe must pass the adjusted upper ceiling.");
            var party = cell.GetProperty("scenario").GetProperty("party").EnumerateArray().ToArray();
            var specialized = party.SelectMany(m => m.GetProperty("build").GetProperty("equipment").EnumerateArray()
                .Where(i => i.GetProperty("definitionId").GetString()!.Contains(".spec.", StringComparison.Ordinal))
                .Select(_ => m.GetProperty("partySlot").GetInt32())).ToArray();
            if (specialized.Length <= 8 && specialized.Distinct().Count() <= 2 && lower >= .1)
                qualifying.Add(HarnessJson.Hash(party.OrderBy(m => m.GetProperty("partySlot").GetInt32()).Select(m => new
                {
                    slot = m.GetProperty("partySlot").GetInt32(),
                    essences = m.GetProperty("build").GetProperty("essenceIds").EnumerateArray()
                        .Select(e => e.GetString()).Order(StringComparer.Ordinal).ToArray()
                }).ToArray()));
        }
        if (qualifying.Count < 2)
            throw new InvalidDataException("Two actual compositions must qualify with at most eight specialized items on two characters.");
    }

    internal static void ValidateAggregateEquipment(JsonElement evidence, JsonElement declaration, JsonElement cells)
    {
        var healthPressure = declaration.GetProperty("candidatePlan").GetProperty("version").GetString()
            is "tower-health-pressure-candidate-v1" or "tower-recovery-pressure-refinement-v1" or "tower-unchanged-catalog-v1" or "tower-kodoku-miasma-v1" or "tower-kodoku-shared-penetration-v1" or "tower-kodoku-eight-item-pressure-refinement-v1" or "tower-kodoku-eight-item-pressure-midpoint-v1";
        var hasContract = declaration.TryGetProperty("equipmentEligibility", out var contract) && contract.ValueKind != JsonValueKind.Null;
        if (!healthPressure && !hasContract) return;
        if (!hasContract || contract.GetProperty("version").GetString() != "tower-limited-equipment-v1" ||
            contract.GetProperty("maximumSpecializedItems").GetInt32() != 8 ||
            contract.GetProperty("maximumSpecializedCharacters").GetInt32() != 2 ||
            contract.EnumerateObject().Count() != 3 ||
            (declaration.TryGetProperty("gear", out var gear) && gear.ValueKind != JsonValueKind.Null))
            throw new InvalidDataException("Health-pressure aggregate requires the declared actual equipment limits.");
        ValidateSingleCandidateEquipment(cells, evidence, "rows");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("items")]
    [InlineData("characters")]
    [InlineData("gear")]
    [InlineData("version")]
    public void Aggregate_health_pressure_cannot_weaken_equipment_contract(string change)
        => AssertAggregateEquipmentContract(change, "tower-health-pressure-candidate-v1");

    [Theory]
    [InlineData("missing")]
    [InlineData("items")]
    [InlineData("characters")]
    [InlineData("gear")]
    [InlineData("version")]
    public void Aggregate_recovery_cannot_weaken_equipment_contract(string change)
        => AssertAggregateEquipmentContract(change, "tower-recovery-pressure-refinement-v1");

    [Theory]
    [InlineData("missing")]
    [InlineData("items")]
    [InlineData("characters")]
    [InlineData("gear")]
    [InlineData("version")]
    public void Aggregate_limited_armor_cannot_weaken_equipment_contract(string change)
        => AssertAggregateEquipmentContract(change, "tower-unchanged-catalog-v1");

    [Theory]
    [InlineData("missing")]
    [InlineData("items")]
    [InlineData("characters")]
    [InlineData("gear")]
    [InlineData("version")]
    public void Aggregate_miasma_cannot_weaken_equipment_contract(string change)
        => AssertAggregateEquipmentContract(change, "tower-kodoku-miasma-v1");

    private static void AssertAggregateEquipmentContract(string change, string candidateVersion)
    {
        var declaration = JsonNode.Parse("""
            {"candidatePlan":{"version":"tower-health-pressure-candidate-v1"},"gear":null,
             "equipmentEligibility":{"version":"tower-limited-equipment-v1","maximumSpecializedItems":8,"maximumSpecializedCharacters":2}}
            """)!;
        declaration["candidatePlan"]!["version"] = candidateVersion;
        if (change == "missing") declaration.AsObject().Remove("equipmentEligibility");
        if (change == "items") declaration["equipmentEligibility"]!["maximumSpecializedItems"] = 9;
        if (change == "characters") declaration["equipmentEligibility"]!["maximumSpecializedCharacters"] = 3;
        if (change == "gear") declaration["gear"] = "label";
        if (change == "version") declaration["equipmentEligibility"]!["version"] = "other";
        Assert.Throws<InvalidDataException>(() => ValidateAggregateEquipment(
            JsonSerializer.SerializeToElement(new { }), JsonSerializer.SerializeToElement(declaration), JsonSerializer.SerializeToElement(Array.Empty<int>())));
    }

    [Theory]
    [InlineData("confirm", "Verified", "Pass", false)]
    [InlineData("screen", "Verified", "Pass", false)]
    [InlineData("confirm", "Verified", "Fail", false)]
    [InlineData("confirm", "Verified", "Inconclusive", false)]
    [InlineData("confirm", "Incomplete", "Pass", false)]
    [InlineData("confirm", "Verified", "Pass", true)]
    public void Single_candidate_application_requires_verified_confirmation(
        string mode, string status, string verdict, bool hasSearch)
    {
        var request = JsonSerializer.SerializeToElement(new { mode, searchSeeds = hasSearch ? new[] { 1 } : Array.Empty<int>() });
        var audit = JsonSerializer.SerializeToElement(new { status, assessment = new { verdict } });
        if (mode == "confirm" && status == "Verified" && verdict == "Pass" && !hasSearch)
            ValidateSingleCandidateConfirmation(request, audit);
        else
            Assert.Throws<InvalidDataException>(() => ValidateSingleCandidateConfirmation(request, audit));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("labels-only")]
    [InlineData("essence-order-only")]
    [InlineData("nine-items")]
    [InlineData("three-characters")]
    [InlineData("lower-miss")]
    [InlineData("ineligible-ceiling")]
    [InlineData("missing-bound")]
    [InlineData("repeated-bound")]
    [InlineData("wrong-family")]
    public void Single_candidate_application_requires_two_actual_limited_gear_compositions(string change)
    {
        var family = JsonNode.Parse(JsonSerializer.Serialize(Enumerable.Range(0, 3).Select(i => new
        {
            id = "cell-" + i, composition = "label-" + i, gear = "label-is-not-evidence",
            scenario = new { party = Enumerable.Range(1, 3).Select(slot => new
            {
                partySlot = slot,
                build = new { essenceIds = new[] { "essence-" + i, "shared" }, equipment =
                    Enumerable.Range(0, slot <= 2 || i == 2 ? 4 : 0).Select(j => new { definitionId = "item.spec." + j }) }
            }) }
        })))!.AsArray();
        var audit = JsonNode.Parse(JsonSerializer.Serialize(new { status = "Verified", assessment = new
        {
            verdict = "Pass", familySize = 3,
            bounds = Enumerable.Range(0, 3).Select(i => new { id = "cell-" + i, lower = .1, upper = .5 })
        } }))!;
        if (change is "labels-only" or "essence-order-only")
            for (var slot = 0; slot < 3; slot++)
                family[1]!["scenario"]!["party"]![slot]!["build"]!["essenceIds"] = JsonSerializer.SerializeToNode(
                    change == "labels-only" ? new[] { "essence-0", "shared" } : new[] { "shared", "essence-0" });
        if (change == "nine-items") family[1]!["scenario"]!["party"]![0]!["build"]!["equipment"]!.AsArray()
            .Add(JsonSerializer.SerializeToNode(new { definitionId = "item.spec.extra" }));
        if (change == "three-characters")
        {
            var item = family[1]!["scenario"]!["party"]![0]!["build"]!["equipment"]!.AsArray()[0]!.DeepClone();
            family[1]!["scenario"]!["party"]![0]!["build"]!["equipment"]!.AsArray().RemoveAt(0);
            family[1]!["scenario"]!["party"]![2]!["build"]!["equipment"]!.AsArray().Add(item);
        }
        if (change == "lower-miss") audit["assessment"]!["bounds"]![1]!["lower"] = .099;
        if (change == "ineligible-ceiling") audit["assessment"]!["bounds"]![2]!["upper"] = .501;
        if (change == "missing-bound") audit["assessment"]!["bounds"]!.AsArray().RemoveAt(2);
        if (change == "repeated-bound") audit["assessment"]!["bounds"]![2]!["id"] = "cell-1";
        if (change == "wrong-family") audit["assessment"]!["familySize"] = 2;
        var cells = JsonSerializer.SerializeToElement(family); var evidence = JsonSerializer.SerializeToElement(audit);
        if (change == "valid") ValidateSingleCandidateEquipment(cells, evidence);
        else Assert.Throws<InvalidDataException>(() => ValidateSingleCandidateEquipment(cells, evidence));
        var aggregate = audit.DeepClone();
        aggregate["assessment"]!["rows"] = aggregate["assessment"]!["bounds"]!.DeepClone();
        aggregate["assessment"]!.AsObject().Remove("bounds");
        var declaration = JsonSerializer.SerializeToElement(new { candidatePlan = new { version = "tower-health-pressure-candidate-v1" },
            gear = (string?)null, equipmentEligibility = new { version = "tower-limited-equipment-v1", maximumSpecializedItems = 8, maximumSpecializedCharacters = 2 } });
        if (change == "valid") ValidateAggregateEquipment(JsonSerializer.SerializeToElement(aggregate), declaration, cells);
        else Assert.Throws<InvalidDataException>(() => ValidateAggregateEquipment(JsonSerializer.SerializeToElement(aggregate), declaration, cells));
    }

    [OwnedFact]
    public async Task Applied_floor_matches_confirmed_inputs_and_one_full_replay_per_cell()
    {
        var q = HarnessJson.Read<Request>(Environment.GetEnvironmentVariable("LL_TOWER_BALANCE_APPLICATION")!);
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.ManifestPin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        Assert.Equal(q.AuditPin, HarnessJson.FileHash(q.Audit));
        if (q.CandidateRoot is not null && q.Aggregate is null)
        {
            Assert.Null(q.QualificationPlan);
            ValidateSingleCandidateConfirmation(
                HarnessJson.Read<JsonElement>(Path.Combine(q.Source, "request.json")), HarnessJson.Read<JsonElement>(q.Audit));
            ValidateSingleCandidateEquipment(
                HarnessJson.Read<JsonElement>(Path.Combine(q.Source, "cells.json")), HarnessJson.Read<JsonElement>(q.Audit));
        }
        if (q.Aggregate is null)
            Assert.Equal("Pass", HarnessJson.Read<JsonElement>(q.Audit).GetProperty("assessment").GetProperty("verdict").GetString());
        else
        {
            Assert.Null(q.QualificationPlan);
            Assert.Equal(q.AggregatePin, HarnessJson.FileHash(q.Aggregate));
            var aggregate = HarnessJson.Read<JsonElement>(q.Aggregate);
            var declarationPath = aggregate.GetProperty("declaration").GetString()!;
            Assert.Equal(aggregate.GetProperty("declarationSha256").GetString(), HarnessJson.FileHash(declarationPath));
            var declaration = HarnessJson.Read<JsonElement>(declarationPath);
            ValidateAggregatePanel(aggregate, declaration);
            var originalCells = Path.Combine(declaration.GetProperty("source").GetString()!, "cells.json");
            Assert.Equal(declaration.GetProperty("cellsSha256").GetString(), HarnessJson.FileHash(originalCells));
            ValidateAggregateEquipment(aggregate, declaration, HarnessJson.Read<JsonElement>(originalCells));
            foreach (var batch in aggregate.GetProperty("batches").EnumerateArray())
            {
                var source = batch.GetProperty("source").GetString()!;
                Assert.Equal(batch.GetProperty("manifestSha256").GetString(), HarnessJson.FileHash(Path.Combine(source, "files.json")));
                Assert.Equal(declaration.GetProperty("cellsSha256").GetString(), HarnessJson.FileHash(Path.Combine(source, "cells.json")));
                var request = HarnessJson.Read<JsonElement>(Path.Combine(source, "request.json"));
                Assert.Equal(HarnessJson.Hash(batch.GetProperty("seeds")), HarnessJson.Hash(request.GetProperty("seeds")));
                Assert.Equal(batch.GetProperty("auditSha256").GetString(), HarnessJson.FileHash(batch.GetProperty("audit").GetString()!));
            }
            var member = aggregate.GetProperty("batches").EnumerateArray().Single(b => b.GetProperty("source").GetString() == q.Source);
            Assert.Equal(q.ManifestPin, member.GetProperty("manifestSha256").GetString());
            Assert.Equal(q.AuditPin, member.GetProperty("auditSha256").GetString());
        }
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        foreach (var name in new[] { "scope.json", "cells.json", "evaluation/files.json", "completion.json" })
            Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
        Assert.Equal("Complete", HarnessJson.Read<JsonElement>(Path.Combine(q.Source, "completion.json")).GetProperty("status").GetString());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(600)); var token = timeout.Token;
        var root = q.CandidateRoot ?? TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(q.Source, "scope.json"));
        if (q.Aggregate is not null || q.CandidateRoot is not null)
            Assert.Equal(HarnessJson.Hash(scope.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(settings));
        Qualification? qualification = null;
        if (q.QualificationPlan is not null)
        {
            Assert.Equal(q.QualificationPlanPin, HarnessJson.FileHash(q.QualificationPlan));
            qualification = HarnessJson.Read<Qualification>(q.QualificationPlan);
            Assert.Equal("tower-catalog-qualification-v1", qualification.Version);
            Assert.Equal(Path.GetFullPath(q.Source), Path.GetFullPath(qualification.Source));
            Assert.Equal(q.ManifestPin, qualification.SourceManifestSha256);
            Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(qualification.SourceContentHashes));
            Assert.Equal(scope.ContentHashes.Keys.Order(), qualification.CurrentContentHashes.Keys.Order());
            Assert.Equal(HarnessJson.Hash(qualification.AssemblyHashes), HarnessJson.Hash(ExecutionIdentity.Current().AssemblyHashes));
            Assert.Equal(qualification.TestAssemblySha256, HarnessJson.FileHash(typeof(BalanceHarnessTowerBalanceApplicationTests).Assembly.Location));
            var oldTower = JsonNode.Parse(File.ReadAllText(Path.Combine(q.Source, "content/Data", TowerBattleRunner.FloorFile)))!;
            var newTower = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Data", TowerBattleRunner.FloorFile)))!;
            Assert.True(JsonNode.DeepEquals(
                oldTower["floors"]!.AsArray().Single(f => (int)f!["floorNumber"]! == qualification.Floor),
                newTower["floors"]!.AsArray().Single(f => (int)f!["floorNumber"]! == qualification.Floor)));
            oldTower.AsObject().Remove("floors"); newTower.AsObject().Remove("floors");
            Assert.True(JsonNode.DeepEquals(oldTower, newTower));
        }
        foreach (var pair in scope.ContentHashes)
        {
            var current = Path.Combine(root, "Data", pair.Key);
            if (qualification is not null)
            {
                Assert.Equal(qualification.CurrentContentHashes[pair.Key], HarnessJson.FileHash(current));
                if (pair.Key == "combat/summons.json" && pair.Value != qualification.CurrentContentHashes[pair.Key])
                    ValidateQualifiedSummons(HarnessJson.Read<JsonElement>(q.QualificationPlan!), q.Source, root);
                else if (pair.Key != TowerBattleRunner.FloorFile && pair.Key != "combat/abilities.json")
                    Assert.Equal(pair.Value, qualification.CurrentContentHashes[pair.Key]);
            }
            else if (pair.Key != TowerBattleRunner.FloorFile) Assert.Equal(pair.Value, HarnessJson.FileHash(current));
            else Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(current)),
                JsonNode.Parse(File.ReadAllText(Path.Combine(q.Source, "content/Data", pair.Key)))));
        }
        var archive = Path.Combine(q.Source, "evaluation");
        var trials = TowerLoadoutArchive.Verify(archive, token);
        var runner = new TowerBattleRunner(root, OfflineContent.ForTower(root, settings));
        var replayed = new HashSet<string>(); var matched = 0;
        foreach (var trial in trials)
        {
            token.ThrowIfCancellationRequested();
            var scenario = HarnessJson.Read<TowerScenario>(Path.Combine(archive, "recipes", trial.Recipe + ".json"));
            var input = runner.CreateInput(scenario, trial.Seed, settings.Threat, settings.CheckpointIntervalTicks);
            Assert.Equal(trial.InputHash, HarnessJson.Hash(input)); matched++;
            if (replayed.Add(trial.Stage))
            {
                var actual = await runner.RunAsync(input, token: token);
                Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(archive, trial.Id, scope.ReportStorage)), HarnessJson.Hash(actual));
            }
        }
        var cells = HarnessJson.Read<BalanceHarnessTowerBalancePassTests.Cell[]>(Path.Combine(q.Source, "cells.json"));
        Assert.Equal(cells.Length, replayed.Count);
        if (qualification is not null) Assert.All(cells, c => Assert.Equal(qualification.Floor, c.Scenario.FloorNumber));
        HarnessJson.WriteNew(q.Output, new { status = q.Aggregate is not null ? "AggregateInputsAndReplaysVerified" : q.CandidateRoot is not null ? "IsolatedInputsAndReplaysVerified" : qualification is null ? "AppliedInputsAndReplaysVerified" : "QualifiedInputsAndReplaysVerified", matchedInputs = matched,
            fullReplays = replayed.Count, newSeeds = 0, manifestPin = q.ManifestPin,
            qualificationPlanPin = q.QualificationPlanPin, execution = ExecutionIdentity.Current() });
    }
}
