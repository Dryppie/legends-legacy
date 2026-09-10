using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerLoadoutContext(string Id, TowerScenario Scenario, int TargetPartySlot,
    IReadOnlyList<string> AllowedEssences, IReadOnlyDictionary<int, string> PinnedSlots, string OwnershipAssumption);
public sealed record TowerLoadoutDefinition(int SchemaVersion, string Id, IReadOnlyList<TowerLoadoutContext> Contexts,
    int GenerationSeed, int MaxProposalsPerContext, IReadOnlyList<string> AuditContexts, IReadOnlyList<int> AuditSeeds,
    string Progression = "level-1-unascended", string OrderPolicy = "preserve-slots");
public sealed record TowerLoadoutCandidate(string Id, string Origin, IReadOnlyList<string> Essences,
    string? Rejection, TowerScenario? Scenario, string? InputHash);
public sealed record TowerLoadoutPreparation(string Context, int Floor, int TargetPartySlot, int EssenceSlots,
    IReadOnlyList<TowerLoadoutCandidate> Candidates);
public sealed record TowerOrderObservation(string Context, int Seed, bool GameplayChanged, bool OutcomeChanged,
    string OriginalOutcome, string ReversedOutcome, double OriginalDurationSeconds, double ReversedDurationSeconds);
public sealed record TowerLoadoutFoundationReport(int SchemaVersion, string Status, string OrderPolicy,
    IReadOnlyList<TowerLoadoutPreparation> Contexts, IReadOnlyList<TowerOrderObservation> OrderAudit);

/// <summary>Legal, ordered candidate preparation and order audit. No fitness ranking or optimizer.</summary>
public static class TowerLoadoutFoundation
{
    public static TowerLoadoutDefinition ReadDefinition(string path) => JsonSerializer.Deserialize<TowerLoadoutDefinition>(
        File.ReadAllText(path), new JsonSerializerOptions(HarnessJson.Options)
        { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }) ?? throw new InvalidDataException("Empty search definition.");

    public static TowerLoadoutDefinition Default(string root, string catalogs)
    {
        var pool = new OfflineContent(root, TowerBundle.ReadSettings(root).Threat).Essences.GetAll()
            .Select(e => e.Id).Order(StringComparer.Ordinal).ToArray();
        var contexts = new List<TowerLoadoutContext>();
        void Add(string file, string prefix, bool allRoles)
        {
            var catalog = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(catalogs, file));
            if (allRoles) catalog = catalog with { Parties = catalog.Parties.Where(p => p.Id == "balanced").ToArray() };
            foreach (var scenario in TowerBenchmark.Expand(catalog, root, 20260910, 1))
                foreach (var slot in allRoles ? new[] { 1, 2, 3, 5 } : new[] { 1 })
                    contexts.Add(FreezeIdentity(new($"{prefix}-{scenario.Id}-slot-{slot}", scenario, slot, pool,
                        new Dictionary<int, string>(), "Hypothetical ownership of the declared pool; acquisition is not modeled.")));
        }
        Add("tower-curve.json", "curve", true);
        Add("tower-essence-slots.json", "controlled", false);
        var audit = contexts.Where(c => c.Id.StartsWith("curve-", StringComparison.Ordinal)
            && (c.Scenario.FloorNumber == 1 || (c.TargetPartySlot == 1 && c.Scenario.FloorNumber is 10 or 15)))
            .Select(c => c.Id).ToArray();
        return new(1, "tower-loadout-foundation-v1", contexts, 20260910, 4, audit, [1337, 17, -12345]);
    }

    public static void ValidateDefinition(TowerLoadoutDefinition definition)
    {
        if (definition.SchemaVersion != 1 || !TowerBenchmark.SafeId(definition.Id)
            || definition.Contexts is not { Count: > 0 and <= 256 }
            || definition.Contexts.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id))
            || definition.Contexts.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != definition.Contexts.Count
            || definition.MaxProposalsPerContext is < 2 or > 100
            || definition.Contexts.Count * definition.MaxProposalsPerContext > 4096
            || definition.Progression != "level-1-unascended" || definition.OrderPolicy != "preserve-slots"
            || definition.AuditContexts is not { Count: > 0 and <= 8 } || definition.AuditSeeds is not { Count: > 0 and <= 5 }
            || definition.AuditContexts.Distinct().Count() != definition.AuditContexts.Count
            || definition.AuditContexts.Except(definition.Contexts.Select(c => c.Id)).Any()
            || definition.AuditSeeds.Distinct().Count() != definition.AuditSeeds.Count)
            throw new InvalidDataException("Invalid foundation contract: bounded contexts/proposals, explicit ordered level-1 budget and distinct audit schedule required.");
    }

    public static TowerLoadoutContext FreezeIdentity(TowerLoadoutContext context) => context with
    {
        Scenario = context.Scenario with { Party = context.Scenario.Party.Select(p => p.PartySlot == context.TargetPartySlot
            ? p with { Build = p.Build with { IdentityEssenceIds = p.Build.IdentityEssenceIds ?? p.Build.EssenceIds.ToArray() } } : p).ToArray() }
    };

    public static IReadOnlyList<TowerLoadoutCandidate> Generate(TowerLoadoutContext context, OfflineContent content,
        TowerBattleRunner runner, TowerSettings settings, string scopeIdentity, int generationSeed, int maximum)
    {
        if (maximum is < 2 or > 100 || context.Scenario is null || context.AllowedEssences is not { Count: > 0 and <= 1000 }
            || context.PinnedSlots is null || string.IsNullOrWhiteSpace(context.OwnershipAssumption))
            throw new InvalidDataException("A bounded allowed pool, pins and ownership assumption are required.");
        // Validate the full unchanged party first, not just the character under investigation.
        context = FreezeIdentity(context);
        runner.CreateInput(context.Scenario, context.Scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
        var target = context.Scenario.Party.SingleOrDefault(p => p.PartySlot == context.TargetPartySlot)
            ?? throw new InvalidDataException("Target party slot is absent.");
        var original = target.Build.EssenceIds.ToArray();
        if (original.Length is < 4 or > 10) throw new InvalidDataException("Foundation requires four through ten equipped Essences.");
        string Resolve(string id) => content.Essences.GetById(id)?.Id
            ?? throw new InvalidDataException($"Unknown allowed Essence '{id}'.");
        var pool = context.AllowedEssences.Select(Resolve).ToArray();
        if (pool.Distinct(StringComparer.OrdinalIgnoreCase).Count() != pool.Length
            || original.Any(id => !pool.Contains(Resolve(id), StringComparer.Ordinal))
            || context.PinnedSlots.Any(p => p.Key < 0 || p.Key >= original.Length
                || !string.Equals(Resolve(p.Value), Resolve(original[p.Key]), StringComparison.Ordinal)))
            throw new InvalidDataException("Pool must be unique and contain the control; zero-based pins must agree with its ordered slots.");
        pool = pool.OrderBy(id => StableRandom.Seed("tower-loadout-proposals-v1", generationSeed.ToString(CultureInfo.InvariantCulture), id))
            .ThenBy(id => id, StringComparer.Ordinal).ToArray();
        IEnumerable<(string Origin, string[] Ids)> Proposals()
        {
            yield return ("control", original);
            yield return ("reverse-order-probe", original.Reverse().ToArray());
            foreach (var id in pool)
                for (var slot = 0; slot < original.Length; slot++)
                {
                    var ids = original.ToArray(); ids[slot] = id;
                    yield return ($"replace-slot-{slot}", ids);
                }
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<TowerLoadoutCandidate>();
        foreach (var (origin, proposed) in Proposals())
        {
            var ids = proposed.Select(Resolve).ToArray();
            var signature = HarnessJson.Hash(ids); // Array order is deliberate; never sort equipped Essences.
            if (!seen.Add(signature)) continue;
            var id = HarnessJson.Hash(new { Algorithm = "tower-loadout-foundation-v1", Scope = scopeIdentity, Context = context, Essences = ids });
            try
            {
                if (context.PinnedSlots.Any(p => ids[p.Key] != Resolve(p.Value))) throw new InvalidDataException("Candidate changes a pinned slot.");
                // Preserve scenario, character, equipment and ally identities across all candidates.
                var scenario = context.Scenario with { Party = context.Scenario.Party.Select(p => p.PartySlot == context.TargetPartySlot
                    ? p with { Build = p.Build with { EssenceIds = ids } } : p).ToArray() };
                var input = runner.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
                result.Add(new(id, origin, ids, null, scenario, HarnessJson.Hash(input)));
            }
            catch (Exception error) when (error is ArgumentException or InvalidDataException)
            { result.Add(new(id, origin, ids, error.Message, null, null)); }
            if (result.Count == maximum) break;
        }
        return result;
    }

    public static async Task<TowerLoadoutFoundationReport> CreateAsync(string root, TowerLoadoutDefinition definition,
        string output, CancellationToken token = default, Action<string>? progress = null)
    {
        if (Path.Exists(output)) throw new IOException("Foundation output exists; use a new directory.");
        ValidateDefinition(definition);
        definition = definition with { Contexts = definition.Contexts.Select(FreezeIdentity).ToArray() };
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        var status = "Invalid";
        var preparations = new List<TowerLoadoutPreparation>();
        var observations = new List<TowerOrderObservation>();
        try
        {
            var settings = TowerBundle.ReadSettings(root);
            var snapshot = Path.Combine(output, "content");
            var hashes = TowerBundle.CopyContent(root, snapshot, token);
            var execution = ExecutionIdentity.Current();
            var scope = HarnessJson.Hash(new { Content = hashes, Execution = execution, Settings = settings });
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
            HarnessJson.WriteNew(Path.Combine(output, "settings.json"), settings);
            HarnessJson.WriteNew(Path.Combine(output, "scope.json"), new { Content = hashes, Execution = execution, Identity = scope });
            var content = new OfflineContent(snapshot, settings.Threat);
            var runner = new TowerBattleRunner(snapshot, content);
            var mechanics = EssenceMechanicsInventory.Create(snapshot, settings.Threat);
            HarnessJson.WriteNew(Path.Combine(output, "mechanics.json"), mechanics);
            File.WriteAllText(Path.Combine(output, "mechanics.md"), EssenceMechanicsInventory.Markdown(mechanics));
            var recipes = Path.Combine(output, "recipes"); Directory.CreateDirectory(recipes);
            foreach (var context in definition.Contexts)
            {
                token.ThrowIfCancellationRequested();
                var candidates = Generate(context, content, runner, settings, scope, definition.GenerationSeed, definition.MaxProposalsPerContext);
                foreach (var candidate in candidates.Where(c => c.Rejection is null))
                {
                    var input = runner.CreateInput(candidate.Scenario!, candidate.Scenario!.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
                    await runner.PrepareAsync(input, token);
                    HarnessJson.WriteNew(Path.Combine(recipes, candidate.Id + ".json"), candidate.Scenario);
                }
                preparations.Add(new(context.Id, context.Scenario.FloorNumber, context.TargetPartySlot,
                    context.Scenario.Party.Single(p => p.PartySlot == context.TargetPartySlot).Build.EssenceIds.Count, candidates));
                progress?.Invoke($"Prepared {preparations.Count}/{definition.Contexts.Count} contexts.");
            }
            foreach (var id in definition.AuditContexts)
            {
                var context = definition.Contexts.Single(c => c.Id == id);
                var recipe = context.Scenario with { Seeds = definition.AuditSeeds };
                var reverse = recipe with { Party = recipe.Party.Select(p => p.PartySlot == context.TargetPartySlot
                    ? p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } } : p).ToArray() };
                var auditRoot = Path.Combine(output, "order-audit", HarnessJson.Hash(id)); Directory.CreateDirectory(auditRoot);
                HarnessJson.WriteNew(Path.Combine(auditRoot, "original.json"), recipe);
                HarnessJson.WriteNew(Path.Combine(auditRoot, "reversed.json"), reverse);
                var a = await TowerBundle.CreateAsync(snapshot, Path.Combine(auditRoot, "original.json"), Path.Combine(auditRoot, "original"), token, settingsOverride: settings);
                var b = await TowerBundle.CreateAsync(snapshot, Path.Combine(auditRoot, "reversed.json"), Path.Combine(auditRoot, "reversed"), token, settingsOverride: settings);
                foreach (var (first, second) in a.Trials.Zip(b.Trials))
                    observations.Add(new(id, first.Seed, GameplayFingerprint(first.Report) != GameplayFingerprint(second.Report),
                        first.Report.Succeeded != second.Report.Succeeded || first.Report.Battle.Summary.ContentOutcome != second.Report.Battle.Summary.ContentOutcome,
                        first.Report.Battle.Summary.ContentOutcome.ToString(), second.Report.Battle.Summary.ContentOutcome.ToString(),
                        first.Report.Battle.Summary.DurationSeconds, second.Report.Battle.Summary.DurationSeconds));
                progress?.Invoke($"Order audit saved: {id}.");
            }
            status = "Complete";
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        catch (Exception error)
        { HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new { error.Message, Type = error.GetType().Name }); throw; }
        finally
        {
            var report = new TowerLoadoutFoundationReport(1, status, "preserve-slots", preparations, observations);
            HarnessJson.WriteNew(Path.Combine(output, "foundation.json"), report);
            File.WriteAllText(Path.Combine(output, "foundation.md"), Markdown(report));
            var files = Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
                .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);
            HarnessJson.WriteNew(Path.Combine(output, "foundation-files.json"), files);
        }
        return new(1, status, "preserve-slots", preparations, observations);
    }

    // Ignore ordering of descriptive ability/stat arrays. A changed outcome, duration, terminal
    // state or per-entity contribution is an observable gameplay difference, not a metadata reorder.
    public static string GameplayFingerprint(TowerBattleReport report)
    {
        var summary = report.Battle.Summary;
        return HarnessJson.Hash(new { summary.EngineOutcome, summary.ContentOutcome, summary.DurationTicks,
            report.GuardianHealthRemainingPercent,
            Friendly = summary.Friendly.OrderBy(e => e.Id, StringComparer.Ordinal),
            Hostile = summary.Hostile.OrderBy(e => e.Id, StringComparer.Ordinal),
            Stats = summary.Statistics.OrderBy(s => s.EntityId, StringComparer.Ordinal).Select(s => new
            { s.EntityId, s.DamageDone, s.DamageTaken, s.HealingDone, s.BarrierGenerated, s.Health, s.Barrier,
                s.TargetedAttacks, s.Deaths, s.Revivals, s.FirstDeathTick, s.ActionDeniedTicks, s.StaggerContributed }) });
    }

    public static string Markdown(TowerLoadoutFoundationReport report)
    {
        var text = new StringBuilder($"# Tower loadout foundation\n\nStatus: {report.Status}. No quality ranking or balance acceptance.\n\n");
        text.AppendLine($"Prepared contexts: {report.Contexts.Count}; accepted proposals: {report.Contexts.Sum(c => c.Candidates.Count(p => p.Rejection is null))}; rejected proposals: {report.Contexts.Sum(c => c.Candidates.Count(p => p.Rejection is not null))}. Full ordered candidates/rejection reasons are in foundation.json; accepted scenarios are in recipes/.\n");
        text.AppendLine("Only the target character's ordered Essence IDs vary. Gear, character identities, allies, level and progression stay fixed. Hypothetical ownership is not an acquisition guarantee. Metadata describes authored mechanics, not final effective power.\n");
        text.AppendLine("## Slot order audit\n\nPreserve slots. A gameplay difference disproves general order equivalence; matching limited probes cannot prove it. Comparison uses outcomes, duration, terminal states and per-entity numeric contributions, ignoring ability-list order. Audit reversals are diagnostic interventions and may violate search pins; they are not recommended candidates.\n\n| Context | Seed | Gameplay changed | Outcome changed | Original | Reversed | Original / reversed seconds |\n| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var item in report.OrderAudit) text.AppendLine(FormattableString.Invariant($"| {item.Context} | {item.Seed} | {item.GameplayChanged} | {item.OutcomeChanged} | {item.OriginalOutcome} | {item.ReversedOutcome} | {item.OriginalDurationSeconds:F2} / {item.ReversedDurationSeconds:F2} |"));
        return text.ToString();
    }
}
