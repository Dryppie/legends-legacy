using BalanceHarness;

namespace EssenceSystem.Tests;

// Synthetic evaluator inputs only. No allocator, preparation service or combat engine is used.
internal static class BalanceHarnessCompositionSearchFixture
{
    internal const string Policy = "independent-composition-only-v1", Method = "composition-only-joint";
    private static string Repository()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json"))) return dir.FullName;
        throw new DirectoryNotFoundException("Repository fixture not found.");
    }
    private static TowerScenario Template => HarnessJson.Read<TowerScenario>(Path.Combine(Repository(),
        "LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json"));

    internal static BossDiscoveryInputs Input(string policy = Policy, int candidates = 128, int owners = 2, int poolSize = 12, int attempts = 2048)
    {
        var methods = policy == TowerBossGeneration.Version ? TowerBossDiscovery.Methods
            : policy == TowerBossGeneration.LoadoutCompositionVersion ? TowerBossGeneration.LoadoutCompositionMethods
            : policy == TowerDeepChallenger.Version ? new[] { TowerSearchAllocation.Deep } : new[] { Method };
        if (policy == TowerDeepChallenger.Version) { candidates = 1536; attempts = 16384; }
        var template = Template;
        return new(1, TowerPartyProgression.Budget(4), owners,
            Enumerable.Range(0, poolSize).Select(i => new BossDiscoveryEssence("e" + i.ToString("D2"), "family" + i)).ToArray(), null,
            new(methods, [17], candidates, attempts, 4, TowerBossDiscovery.Objective, policy),
            new Dictionary<string, IReadOnlyList<int>> { ["fixture"] = new[] { 101, 102 } },
            new Dictionary<string, IReadOnlyList<BossDiscoveryCharacterBudget>> { ["fixture"] = Enumerable.Range(1, owners)
                .Select(slot => new BossDiscoveryCharacterBudget(slot, template.Party[(slot - 1) % 5].Build.Equipment, 1)).ToArray() },
            TowerBundle.Files.ToDictionary(f => f, _ => new string('a', 64)), Math.Min(8, candidates));
    }

    internal static TowerBossDiscoveryDefinition Definition(BossDiscoveryInputs input)
    {
        var template = Template;
        var party = Enumerable.Range(1, input.RequiredPartySize).Select(slot => new TowerPartyRecipe(slot,
            template.Party[(slot - 1) % 5].Build with { Id = "tower-discovery-character-" + slot, EssenceIds = [], IdentityEssenceIds = null })).ToArray();
        return new(3, "composition-fixture", TowerBossDiscovery.Independent, input.Budget, input.RequiredPartySize, template.StartsAt,
            [new("fixture", party)], input.AllowedEssences, input.OwnedCopies, input.Generation,
            new(input.ShortlistCandidates, Math.Min(2, input.ShortlistCandidates), 0, 0,
                new Dictionary<string, BossDiscoverySchedule> { ["fixture"] = new([101, 102], [201, 202], [301, 302], []) }),
            [], [], [], input.ContentHashes, new string('b', 64), new string('c', 64), 10000);
    }

    internal static BossGenerationMechanics Mechanics(BossDiscoveryInputs input) => new(input.Floor, ["focused-damage"],
        input.AllowedEssences.Select(e => new TowerEssenceMechanics(e.Id, e.Id, e.Family, [], [], ["intent:focused-damage"])).ToArray(), [],
        TowerBossInventory.SourceFiles.ToDictionary(f => f, f => input.ContentHashes[f]), [], []);

    internal static BossDiscoveryMeasurement Measure(BossDiscoveryInputs input, PartyChoice party)
    {
        var health = Convert.ToUInt32(party.Id[..6], 16) / (double)0xffffff * 100;
        var cells = input.DiscoverySeeds.Select(p => new PartyFloorScore(p.Key, input.Floor, [false, false], 0, health, 40,
            p.Value.Select(seed => p.Key + "/" + seed).ToArray())).ToArray();
        return new(party.Id, TowerBossGeneration.Fitness(input, cells, 100), cells, new(0, .5, 0, 0, 0));
    }

    internal static async Task<BossGenerationResult> Run(BossDiscoveryInputs input)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Zero-combat fixture entered the engine.")).Activate();
        return await TowerBossGeneration.RunAsync(input, Mechanics(input), (p, _, token) => {
            token.ThrowIfCancellationRequested(); return Task.FromResult(Measure(input, p));
        });
    }
}
