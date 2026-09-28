using System.Text.Json;
using Common.Randomness;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record AttributeAllocationPartyMember(EquipmentReferenceBuildDefinition Build, FixtureCombatStyle? Doctrine = null);

public sealed record AttributeAllocationCell(string Id, EquipmentReferenceBuildDefinition Reference,
    EquipmentReferenceBuildDefinition Candidate, IdleScenario? Idle = null, TowerScenario? Tower = null,
    EquipmentReferenceBuildDefinition? Opponent = null, FixtureCombatStyle? Doctrine = null,
    FixtureCombatStyle? OpponentDoctrine = null, AttributeDungeonScenario? Dungeon = null, string? ComparisonGroup = null,
    IReadOnlyDictionary<AttributeType, float>? ReferenceDiagnosticPoints = null,
    IReadOnlyDictionary<AttributeType, float>? CandidateDiagnosticPoints = null,
    IReadOnlyList<AttributeAllocationPartyMember>? Allies = null,
    IReadOnlyList<AttributeAllocationPartyMember>? AdditionalOpponents = null);
public sealed record AttributeAllocationRequest(int SchemaVersion, string ContentRoot, string OutputDirectory,
    IReadOnlyList<int> ExplorationSeeds, IReadOnlyList<int> ConfirmationSeeds, IReadOnlyList<AttributeAllocationCell> Cells,
    int MaximumBattles = 10000, int ReferenceRulesVersion = AttributeRules.CurrentVersion, bool SyntheticFutureProjection = false,
    int? ReferenceEquipmentBalanceVersion = null, int? CandidateEquipmentBalanceVersion = null, bool AllowBudgetChanges = false,
    string? ReferenceAbilityBalanceProfile = null, string? CandidateAbilityBalanceProfile = null);
public sealed record AttributeAllocationTrial(string Cell, string Phase, int Seed, bool Mirrored,
    BattleSummary Reference, BattleSummary Candidate);
public sealed record AttributeAllocationEstimate(string Cell, string Phase, PairedEstimate WinRate,
    PairedEstimate DurationTicks, PairedEstimate HealthDamage, PairedEstimate Healing,
    int ReferenceDraws, int CandidateDraws, bool NonDominated);

/// <summary>Bounded, legal equal-budget exchanges through production preparation/execution.
/// Separate confirmation seeds are declared before any fight and cannot be reused for search.</summary>
public static partial class AttributeAllocationStudy
{
    public static async Task<int> RunAsync(AttributeAllocationRequest request, CancellationToken ct)
    {
        Validate(request);
        Rank(request, []); // Validate comparison groups before executing any fights.
        var output = Path.GetFullPath(request.OutputDirectory);
        if (Directory.Exists(output) || File.Exists(output)) throw new InvalidDataException("Choose a new output directory; studies never overwrite artifacts.");
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "request.json"), request);
        var frozenRoot = Path.Combine(output, "content");
        var profileFiles = new[] { request.ReferenceAbilityBalanceProfile, request.CandidateAbilityBalanceProfile }
            .Where(x => x is not null).Select(x => JsonAbilityCatalogProvider.ProfileRelativePath(x!).Replace('\\', '/'));
        var files = OfflineContent.Files.Concat(ReleaseFiles(request)).Concat(profileFiles).Concat([TowerBattleRunner.FloorFile, AttributeDungeonBattleRunner.ContentFile]).Distinct().Order().ToArray();
        var hashes = new Dictionary<string, string>();
        foreach (var file in files)
        {
            var source = Path.Combine(request.ContentRoot, "Data", file);
            var target = Path.Combine(frozenRoot, "Data", file);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, false);
            if (request.SyntheticFutureProjection && file.StartsWith("equipment/equipment-starters.", StringComparison.Ordinal))
            {
                var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(target))!;
                document["maximumTier"] = 10;
                File.WriteAllText(target, document.ToJsonString(HarnessJson.Options));
            }
            hashes[file] = HarnessJson.FileHash(target);
        }
        var referenceRoot = frozenRoot;
        if (request.ReferenceRulesVersion == AttributeRules.LegacyVersion)
        {
            referenceRoot = Path.Combine(output, "legacy-content");
            foreach (var file in files)
            {
                var target = Path.Combine(referenceRoot, "Data", file);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var sourceFile = file is "equipment/equipment-starters.v1.json" or "equipment/equipment-styles.v1.json" or "equipment/equipment-sets.v1.json"
                    ? file.Replace(".v1.json", ".legacy-v1.json", StringComparison.Ordinal) : file;
                File.Copy(Path.Combine(frozenRoot, "Data", sourceFile), target, false);
            }
        }
        HarnessJson.WriteNew(Path.Combine(output, "manifest.json"), new
        {
            request.SchemaVersion, request.SyntheticFutureProjection, RulesVersion = AttributeRules.CurrentVersion, request.ReferenceRulesVersion,
            request.ReferenceEquipmentBalanceVersion, request.CandidateEquipmentBalanceVersion, request.AllowBudgetChanges,
            request.ReferenceAbilityBalanceProfile, request.CandidateAbilityBalanceProfile,
            StatVersion = EquipmentStatBudgetCatalog.BalanceVersion, ContentHashes = hashes,
            Execution = ExecutionIdentity.Current(), RequestHash = HarnessJson.Hash(request),
            Notes = "Prespecified legal exchanges. Paired seeds can diverge in random-event order. PvP mirrors share a seed and form one statistical cluster. Synthetic or future encounters must be labeled in their assumptions. No automatic balance promotion."
        });
        var threat = new ThreatAndTankingOptions();
        var current = new OfflineContent(frozenRoot, threat, request.SyntheticFutureProjection, request.CandidateEquipmentBalanceVersion, request.CandidateAbilityBalanceProfile);
        var reference = request.ReferenceRulesVersion == AttributeRules.CurrentVersion && request.ReferenceEquipmentBalanceVersion == request.CandidateEquipmentBalanceVersion
            && request.ReferenceAbilityBalanceProfile == request.CandidateAbilityBalanceProfile
            ? current : new OfflineContent(referenceRoot, threat, request.SyntheticFutureProjection, request.ReferenceEquipmentBalanceVersion, request.ReferenceAbilityBalanceProfile);
        if (current.Equipment.Evaluator.Balance.AttributeVersion != AttributeRules.CurrentVersion
            || reference.Equipment.Evaluator.Balance.AttributeVersion != request.ReferenceRulesVersion)
            throw new InvalidDataException("Selected equipment release is incompatible with the study's combat rules.");
        // Validate every build and budget before starting any fight.
        foreach (var cell in request.Cells)
        {
            ValidateDiagnostics(cell.ReferenceDiagnosticPoints, cell.Reference.Tier, request.ReferenceRulesVersion, reference.Equipment.Evaluator.Balance);
            ValidateDiagnostics(cell.CandidateDiagnosticPoints, cell.Candidate.Tier, AttributeRules.CurrentVersion, current.Equipment.Evaluator.Balance);
            var a = reference.CreateBuild(cell.Reference);
            var b = current.CreateBuild(cell.Candidate);
            var doctrine = cell.Doctrine ?? cell.Tower?.Party.FirstOrDefault()?.Doctrine;
            if (doctrine is not null)
            {
                reference.FreezeCombatStyle(FixtureCharacter.From(a), doctrine);
                current.FreezeCombatStyle(FixtureCharacter.From(b), doctrine);
            }
            var budgetA = Budget(a, reference.Equipment.Evaluator);
            var budgetB = Budget(b, current.Equipment.Evaluator);
            if (!request.AllowBudgetChanges && Math.Abs(budgetA - budgetB) > .0001) throw new InvalidDataException($"'{cell.Id}' changes nominal budget ({budgetA} -> {budgetB}).");
            if (cell.Opponent is not null)
                foreach (var provider in new[] { reference, current })
                {
                    var opponent = FixtureCharacter.From(provider.CreateBuild(cell.Opponent));
                    if (cell.OpponentDoctrine is not null) provider.FreezeCombatStyle(opponent, cell.OpponentDoctrine);
                }
            foreach (var member in (cell.Allies ?? []).Concat(cell.AdditionalOpponents ?? []))
                foreach (var provider in new[] { reference, current })
                {
                    var ally = FixtureCharacter.From(provider.CreateBuild(member.Build));
                    if (member.Doctrine is not null) provider.FreezeCombatStyle(ally, member.Doctrine);
                }
            HarnessJson.WriteNew(Path.Combine(output, $"{cell.Id}.builds.json"), new { Reference = FixtureCharacter.From(a), Candidate = FixtureCharacter.From(b), NormalizedBudget = budgetA, CandidateNormalizedBudget = budgetB });
        }
        var trials = new List<AttributeAllocationTrial>();
        foreach (var (phase, seeds) in new[] { ("exploration", request.ExplorationSeeds), ("confirmation", request.ConfirmationSeeds) })
        {
            foreach (var cell in request.Cells)
            {
                foreach (var seed in seeds)
                    foreach (var mirrored in cell.Opponent is null ? new[] { false } : new[] { false, true })
                    {
                        ct.ThrowIfCancellationRequested();
                        var detailed = seed == seeds[0] && !mirrored;
                        var a = await RunBattle(referenceRoot, reference, cell, cell.Reference, seed, mirrored, detailed, threat, ct, cell.ReferenceDiagnosticPoints);
                        var b = await RunBattle(frozenRoot, current, cell, cell.Candidate, seed, mirrored, detailed, threat, ct, cell.CandidateDiagnosticPoints);
                        trials.Add(new(cell.Id, phase, seed, mirrored, a.Summary, b.Summary));
                        if (detailed) HarnessJson.WriteNew(Path.Combine(output, $"{cell.Id}.{phase}.replay.json"), new { Reference = a, Candidate = b });
                    }
                Console.WriteLine($"{phase}: {cell.Id} completed ({seeds.Count} paired seeds).");
            }
        }
        HarnessJson.WriteNew(Path.Combine(output, "trials.json"), trials);
        var estimates = Summarize(trials);
        HarnessJson.WriteNew(Path.Combine(output, "rankings.json"), Rank(request, trials));
        HarnessJson.WriteNew(Path.Combine(output, "estimates.json"), estimates);
        var lines = new List<string>
        {
            "# Attribute allocation study", "", $"{trials.Count * 2} production-engine fights; rules {request.ReferenceRulesVersion} → 18; equipment {reference.Equipment.Evaluator.Balance.Version} → {current.Equipment.Evaluator.Balance.Version}; budget changes allowed: {request.AllowBudgetChanges}; diagnostic exchanges and synthetic projection are labeled in the request.", "",
            $"Ability profiles: {request.ReferenceAbilityBalanceProfile ?? "baseline"} → {request.CandidateAbilityBalanceProfile ?? "baseline"}.", "",
            "Confirmation seeds were fixed before exploration. The intervals describe these fixtures, not population balance. No product acceptance bands are assumed.", "",
            "| Cell | Phase | Pairs | Win change (pp) | 95% interval (when supported) | Duration change (ticks) | Draws before/after |", "|---|---|---:|---:|---|---:|---|"
        };
        foreach (var x in estimates) lines.Add(FormattableString.Invariant($"| {x.Cell} | {x.Phase} | {x.WinRate.Pairs} | {x.WinRate.MeanChange:F2} | {x.WinRate.Lower:F2} to {x.WinRate.Upper:F2} | {x.DurationTicks.MeanChange:F2} | {x.ReferenceDraws}/{x.CandidateDraws} |"));
        lines.AddRange(["", "rankings.json contains descriptive per-context rankings and multi-objective Pareto flags. Intervals are unavailable for fewer than 30 independent paired seeds or constant observed differences; see estimates.json for the estimator note. Duration is reported but is not a dominance objective because dying faster must not improve a build rank.", "", "Full prevention, health, control, healing, barrier, summon and first-death telemetry is in trials.json. Survivors have no death tick; timeout survival is censored. Detailed deterministic replays are saved for the first seed in each phase.",
            "", "NonDominated is a descriptive within-cell comparison on wins, duration and health damage. It is not a proof of global build viability. A release decision also requires migration impact and live cohort review."]);
        await File.WriteAllLinesAsync(Path.Combine(output, "summary.md"), lines, ct);
        return 0;
    }

    public static void Validate(AttributeAllocationRequest request)
    {
        if (request.SchemaVersion != 1 || request.Cells.Count is < 1 or > 200 || request.MaximumBattles is < 1 or > 100000
            || request.ExplorationSeeds.Count is < 1 or > 1024 || request.ConfirmationSeeds.Count is < 1 or > 1024)
            throw new InvalidDataException("Invalid or unbounded attribute study.");
        AttributeRules.ValidateVersion(request.ReferenceRulesVersion);
        var seeds = request.ExplorationSeeds.Concat(request.ConfirmationSeeds).ToArray();
        if (seeds.Distinct().Count() != seeds.Length) throw new InvalidDataException("Exploration and confirmation seeds must be distinct and disjoint.");
        var cost = request.Cells.Sum(x => x.Opponent is null ? 2 : 4) * seeds.Length;
        if (cost > request.MaximumBattles) throw new InvalidDataException($"Study requires {cost} fights, exceeding its explicit budget.");
        if (request.Cells.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != request.Cells.Count)
            throw new InvalidDataException("Cell IDs must be unique.");
        foreach (var cell in request.Cells)
        {
            if ((cell.Allies?.Count ?? 0) > 4 || (cell.AdditionalOpponents?.Count ?? 0) > 4
                || (cell.Opponent is null && ((cell.Allies?.Count ?? 0) + (cell.AdditionalOpponents?.Count ?? 0) > 0)))
                throw new InvalidDataException("Additional party members require PvP and at most five actors per side.");
            if (request.ReferenceEquipmentBalanceVersion is null) ValidateDiagnostics(cell.ReferenceDiagnosticPoints, cell.Reference.Tier, request.ReferenceRulesVersion);
            if (request.CandidateEquipmentBalanceVersion is null) ValidateDiagnostics(cell.CandidateDiagnosticPoints, cell.Candidate.Tier, AttributeRules.CurrentVersion);
            if (string.IsNullOrWhiteSpace(cell.Id) || cell.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
                || new[] { cell.Idle is not null, cell.Tower is not null, cell.Opponent is not null, cell.Dungeon is not null }.Count(x => x) != 1)
                throw new InvalidDataException("Each safe-named cell needs exactly one idle, Dungeon, Tower or PvP encounter.");
            if (cell.Reference.CharacterLevel != cell.Candidate.CharacterLevel || cell.Reference.Tier != cell.Candidate.Tier
                || cell.Reference.Rank != cell.Candidate.Rank || cell.Reference.Quality != cell.Candidate.Quality
                || cell.Reference.AttributeRollMultiplier != cell.Candidate.AttributeRollMultiplier
                || !cell.Reference.EssenceIds.SequenceEqual(cell.Candidate.EssenceIds))
                throw new InvalidDataException("An allocation exchange must hold level, tier, rank, quality, roll and ordered Essences fixed.");
        }
    }

    private static IEnumerable<string> ReleaseFiles(AttributeAllocationRequest request)
    {
        var versions = new[] { request.ReferenceEquipmentBalanceVersion, request.CandidateEquipmentBalanceVersion }
            .Where(x => x is > 2).Select(x => x!.Value).Distinct().ToArray();
        if (versions.Length == 0) yield break;
        const string registry = "equipment/equipment-releases.json";
        yield return registry;
        var releases = HarnessJson.Read<Dictionary<int, JsonStarterEquipmentCatalog.ReleaseFiles>>(Path.Combine(request.ContentRoot, "Data", registry));
        foreach (var version in versions)
        {
            if (!releases.TryGetValue(version, out var release)) throw new InvalidDataException($"Unknown equipment release {version}.");
            foreach (var file in new[] { release.Starters, release.Styles, release.Sets, release.Named })
            {
                if (Path.GetFileName(file) != file || string.IsNullOrWhiteSpace(file)) throw new InvalidDataException("Invalid equipment release filename.");
                yield return "equipment/" + file;
            }
        }
    }

    private static double Budget(EquipmentReferenceBuild build, EquipmentEvaluator evaluator) => build.Equipment.Sum(item =>
        evaluator.Evaluate(item.ProgressionData!.EquipmentState).TargetBudget / EquipmentTierBudgetCurve.GetScale(item.Tier));

    private static async Task<BattleReport> RunBattle(string root, OfflineContent content, AttributeAllocationCell cell,
        EquipmentReferenceBuildDefinition build, int seed, bool mirrored, bool detailed, ThreatAndTankingOptions threat, CancellationToken ct, IReadOnlyDictionary<AttributeType, float>? diagnostic)
    {
        if (cell.Idle is { } idle)
        {
            var scenario = idle with { CharacterProfile = build.Id, Build = build, CombatStyle = cell.Doctrine };
            var input = content.CreateInput(scenario, seed, threat, 10);
            var idleRuntime = await new IdleBattleRunner(content).PrepareAsync(input, ct);
            return await Execute(idleRuntime, input.Rules);
        }
        if (cell.Tower is { } tower)
        {
            var scenario = tower with { Seeds = [seed], Party = tower.Party.Select((x, index) => index == 0 ? x with { Build = build, Doctrine = cell.Doctrine ?? x.Doctrine } : x).ToArray() };
            var runner = new TowerBattleRunner(root, content);
            var input = runner.CreateInput(scenario, seed, threat, 100);
            return await Execute(await runner.PrepareAsync(input, ct), input.Rules);
        }
        if (cell.Dungeon is { } dungeon)
            return await new AttributeDungeonBattleRunner(root, content).RunAsync(dungeon, build, cell.Doctrine, seed, detailed, ct, actor => ApplyDiagnostics(actor, diagnostic));
        var first = FixtureCharacter.From(content.CreateBuild(build));
        var second = FixtureCharacter.From(content.CreateBuild(cell.Opponent!));
        if (first.Id == second.Id) second = second with { Id = StableRandom.Guid("attribute-study-opponent", cell.Id) };
        if (cell.Doctrine is not null) first = first with { CombatStyle = content.FreezeCombatStyle(first, cell.Doctrine) };
        if (cell.OpponentDoctrine is not null) second = second with { CombatStyle = content.FreezeCombatStyle(second, cell.OpponentDoctrine) };
        var focalSide = mirrored ? CombatSide.Hostile : CombatSide.Friendly;
        var opposingSide = mirrored ? CombatSide.Friendly : CombatSide.Hostile;
        var p1 = await Prepare(first, focalSide, focalSide.ToString());
        var p2 = await Prepare(second, opposingSide, opposingSide.ToString());
        var focalParty = new List<CombatRuntimeParticipant> { p1 };
        var opposingParty = new List<CombatRuntimeParticipant> { p2 };
        await AddMembers(cell.Allies, focalParty, focalSide);
        await AddMembers(cell.AdditionalOpponents, opposingParty, opposingSide);
        var encounterId = StableRandom.Guid("attribute-study-pvp", cell.Id, seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var plan = new CombatEncounterPlan(encounterId, CombatMode.Pvp, 1, DateTimeOffset.UnixEpoch, focalParty.Concat(opposingParty).Select(x => x.Slot).ToArray(),
            new PvpEncounterSourceContext(encounterId, first.Id, second.Id)) { ContentType = CombatContentType.Arena, RandomSeed = seed };
        var runtime = new CombatEncounterRuntime(plan, mirrored ? opposingParty : focalParty, mirrored ? focalParty : opposingParty);
        ApplyDiagnostics(p1.Combatant, diagnostic);
        return await Execute(runtime, new CombatRuleset(seed, 6000, CaptureEventLog: detailed), adjust: false);

        async Task<BattleReport> Execute(CombatEncounterRuntime battle, CombatRuleset rules, bool adjust = true)
        {
            if (adjust) ApplyDiagnostics(battle.FriendlyParticipants[0].Combatant, diagnostic);
            var prepared = IdleBattleRunner.DescribeParticipants(battle);
            var result = await content.CreateExecutor().ExecuteSimulationAsync(battle, rules with { CaptureEventLog = detailed }, ct);
            return new(1, cell.Id, seed, FastCombatEngine.TicksPerSecond, prepared, BattleSummary.From(result, rules.MaxTicks), detailed ? result.EventLog : null);
        }

        async Task AddMembers(IReadOnlyList<AttributeAllocationPartyMember>? members, List<CombatRuntimeParticipant> party, CombatSide side)
        {
            foreach (var member in members ?? [])
            {
                var fixture = FixtureCharacter.From(content.CreateBuild(member.Build)) with
                { Id = StableRandom.Guid("attribute-study-party", cell.ComparisonGroup ?? cell.Id, side.ToString(), party.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)) };
                if (member.Doctrine is not null) fixture = fixture with { CombatStyle = content.FreezeCombatStyle(fixture, member.Doctrine) };
                party.Add(await Prepare(fixture, side, $"{side}:{party.Count}"));
            }
        }

        async Task<CombatRuntimeParticipant> Prepare(FixtureCharacter fixture, CombatSide side, string slotId)
        {
            var character = fixture.Materialize(content.Equipment);
            var setup = content.CreateSetup(character, fixture.MaterializeEssences());
            var pipeline = new CombatPreparationPipeline(setup);
            var participants = await pipeline.PrepareAsync(CombatContentType.Arena,
                [new(new(slotId, character.Id, side), new LiveCombatantPreparationSource(character))], ct);
            var participant = participants.Single();
            participant.Combatant.CombatStyle = fixture.CombatStyle;
            participant.Combatant.HasCombatStyleSnapshot = true;
            return participant;
        }
    }

    public static IReadOnlyList<AttributeAllocationEstimate> Summarize(IEnumerable<AttributeAllocationTrial> trials) =>
        trials.GroupBy(x => (x.Cell, x.Phase)).Select(group =>
        {
            double Win(BattleSummary b, bool mirrored) => b.EngineOutcome == (mirrored ? BattleOutcome.Defeat : BattleOutcome.Victory) ? 1d : 0d;
            double Stat(BattleSummary b, bool mirrored, Func<EntityStats, double> value) => b.Statistics
                .Where(x => x.Team == (mirrored ? "Hostile" : "Friendly")).Sum(value);
            var pairs = group.GroupBy(x => x.Seed).Select(g => new
            {
                Win = g.Average(x => 100 * (Win(x.Candidate, x.Mirrored) - Win(x.Reference, x.Mirrored))),
                Time = g.Average(x => x.Candidate.DurationTicks - x.Reference.DurationTicks),
                Damage = g.Average(x => Stat(x.Candidate, x.Mirrored, s => s.DirectHealthDamage + s.PeriodicHealthDamage) - Stat(x.Reference, x.Mirrored, s => s.DirectHealthDamage + s.PeriodicHealthDamage)),
                Healing = g.Average(x => Stat(x.Candidate, x.Mirrored, s => s.HealingDone) - Stat(x.Reference, x.Mirrored, s => s.HealingDone))
            }).ToArray();
            var wins = !group.Any(x => x.Mirrored)
                ? PairedStatistics.ClearRate(pairs.Count(x => x.Win > 0), pairs.Count(x => x.Win < 0), pairs.Length)
                : PairedStatistics.Mean(pairs.Select(x => x.Win), "percentage points; mirrored seeds clustered");
            return new AttributeAllocationEstimate(group.Key.Cell, group.Key.Phase, wins,
                PairedStatistics.Mean(pairs.Select(x => x.Time), "ticks"), PairedStatistics.Mean(pairs.Select(x => x.Damage), "health damage"),
                PairedStatistics.Mean(pairs.Select(x => x.Healing), "healing"), group.Count(x => x.Reference.EngineOutcome == BattleOutcome.Draw),
                group.Count(x => x.Candidate.EngineOutcome == BattleOutcome.Draw),
                !(pairs.Average(x => x.Win) <= 0 && pairs.Average(x => x.Time) >= 0 && pairs.Average(x => x.Damage) < 0));
        }).ToArray();
}
