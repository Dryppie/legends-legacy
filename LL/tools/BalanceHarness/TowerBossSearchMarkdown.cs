using System.Text;

namespace BalanceHarness;

public static partial class TowerBossSearch
{
    public static string Markdown(TowerBossSearchReport report, TowerBossSearchDefinition d, BossDiagnosticPlan? diagnosticPlan = null) =>
        d.SchemaVersion == 1 ? MarkdownV1(report, d) : MarkdownRefinement(report, d, diagnosticPlan);

    private static string MarkdownV1(TowerBossSearchReport report, TowerBossSearchDefinition d)
    {
        var b = d.Budget;
        var text = new StringBuilder($"# Boss-specific Essence strategies\n\n{report.Status}; {report.ActualBattles}/{report.PlannedMaximum} upper-bound actual combats; {report.CacheHits} cache hits.\n\n");
        text.AppendLine($"Objective {Version}: floors {string.Join(", ", d.Objective.TargetFloorWeights.Select(p => p.Key + " (weight " + p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"))}; intent {d.Objective.StrategyIntent}. Rank by worst-context weighted paired clear-rate gain, then target guardian health, party survival, victory-only duration and stable recipe ID. No duration reward for fast defeats.\n");
        text.AppendLine($"Budget: {b.EssenceSlots} ordered Essences per character, level {b.CharacterLevel}, Uncommon {b.Quality} tier {b.Tier} rank {b.Rank}, baseline rolls. Mutable absolute party slots: {string.Join(", ", d.MutablePartySlots)}. All other characters, identities, gear and training are fixed. Level-1 unascended/unevolved Essences; no Combat Styles. {d.OwnershipAssumption}\n");
        text.AppendLine("Best found for these bosses under this budget, not an optimum or universal Essence tier list. Zero-clear candidates remain unsuccessful exploratory alternatives. Existing generalist controls are historical recipes retested here. The legacy arm reuses structural beam/mutation ideas under the new objective; it is not a rerun or reinterpretation of a schema-3 generalist study.\n");
        text.AppendLine("Each method/restart receives the same controls, legal pool, candidate count, paired discovery seeds and actual fight allowance. Random exploration is uniform over legal ordered loadouts; graph compatibility proposes hypotheses only. Restarts and identical contexts are not independent extra observations. Confirmation/diagnostic schedules, finalists and strategy labels freeze before fresh outcomes; there is no confirmation-driven reselection.\n");
        text.AppendLine("## Search arms\n\n| Method | Generation seed | Candidates | Actual discovery fights | Stop |\n| --- | --- | --- | --- | --- |");
        foreach (var arm in report.Arms)
            text.AppendLine($"| {arm.Method} | {arm.Seed} | {arm.Evaluations.Count} | {arm.Evaluations.SelectMany(e => e.Cells).SelectMany(c => c.Trials).Distinct().Count()} | {arm.StopReason} |");
        text.AppendLine("\n## Frozen strategy views\n\nMeasured categories describe behavior and do not prove causality. Summon active ticks do not distinguish death from expiry; prevention uses existing damage attribution; healing is a qualified reported total; denied ticks describe actual hostile denial. Barrier generation, raw damage taken and threat are not fitness. Attribution limits are in boss-profiles.json.\n\n| View | Party |\n| --- | --- |");
        foreach (var niche in report.StrategyArchive) text.AppendLine($"| {niche.Intent} | {niche.Id[..12]} |");
        text.AppendLine("\n## Exact party dependencies\n\nAll required participants and gear are present in each exported recipes/ scenario. Absolute slots beyond this table keep the authored context's fixed builds.\n\n| Party | Source | Slot: ordered Essences |\n| --- | --- | --- |");
        foreach (var p in report.Selection) text.AppendLine($"| {p.Id[..12]} | {p.Source} | {string.Join("; ", p.Builds.Select(pair => pair.Key + ": " + string.Join(" / ", pair.Value)))} |");
        text.AppendLine("\n## Fresh confirmation and 15-floor transfer\n\nWilson 95% intervals are descriptive pointwise intervals without multiplicity adjustment. Gained/lost outcomes pair identical seeds against the control within each context. Negative transfer is reported explicitly; success against a known boss on fresh seeds is repeatability, not unseen-boss generalization. Identical context rows refer to the same trial IDs and must not be pooled.\n\n| Party | Context | Floor | Target | Wins/trials | Draws | Wilson 95% | Gained/lost | Transfer |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        if (report.Confirmation.Count > 0)
            foreach (var party in report.Confirmation)
                foreach (var cell in party.Cells)
                {
                    var control = report.Confirmation[0].Cells.Single(c => c.Context == cell.Context && c.Floor == cell.Floor);
                    var wins = cell.Clears.Count(x => x);
                    var gained = cell.Clears.Zip(control.Clears).Count(p => p.First && !p.Second);
                    var lost = cell.Clears.Zip(control.Clears).Count(p => !p.First && p.Second);
                    var wilson = SuiteScorecard.Wilson(wins, cell.Clears.Count)!;
                    text.AppendLine(FormattableString.Invariant($"| {party.Id[..12]} | {cell.Context} | {cell.Floor} | {d.Objective.TargetFloorWeights.ContainsKey(cell.Floor)} | {wins}/{cell.Clears.Count} | {cell.Draws} | {100 * wilson.Lower:F1}–{100 * wilson.Upper:F1}% | {gained}/{lost} | {(lost > gained ? "Observed regression" : gained > lost ? "Observed gain" : "No net change")} |"));
                }
        text.AppendLine("\n## Context equivalence\n\n| Floor | Authored context | Evaluated context |\n| --- | --- | --- |");
        foreach (var alias in report.ContextAliases) text.AppendLine($"| {alias.Floor} | {alias.Context} | {alias.EvaluatedContext} |");
        text.AppendLine("\n## Reserved interaction diagnostic\n\nThe predeclared quartet (reference, enabler replacement, consumer replacement, combination) uses separately reserved diagnostic seeds. Inspect diagnostic-plan.json for exact replacements or an unsupported reason. This supports local matched comparisons only; reported healing cannot establish deaths prevented. No diagnostic outcome selects a finalist.\n\n| Party | Worst paired gain | Guardian health | Survival | Hostile summon ticks | Health deficit | Prevention | Reported healing | Denied ticks |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var row in report.Diagnostics)
            text.AppendLine(FormattableString.Invariant($"| {row.Id[..12]} | {row.Fitness.PrimaryGain:F3} | {row.Fitness.GuardianHealth:F2} | {row.Fitness.Survival:F3} | {row.Behavior.SummonActiveTicks:F1} | {row.Behavior.HealthDeficit:F3} | {row.Behavior.DamagePrevented:F1} | {row.Behavior.Healing:F1} | {row.Behavior.DeniedTicks:F1} |"));
        text.AppendLine("\nExact replay uses tower-loadout-replay with an archived trial ID and the recorded executable/runtime/content. Reconstruct this experiment with tower-boss-verify. Cancelled or invalid runs preserve completed trials and remain unconfirmed. No production tuning, migration, account change, deployment or Phase 2 integration is included.");
        return text.ToString();
    }

    private static string MarkdownRefinement(TowerBossSearchReport report, TowerBossSearchDefinition d, BossDiagnosticPlan? plan)
    {
        var b = d.Budget; var refinement = d.Refinement!;
        var controls = d.Controls.ToDictionary(c => c.Id);
        var measured = report.Confirmation.ToDictionary(r => r.Id);
        bool Canonical(PartyFloorScore c) => !report.ContextAliases.Any(a => a.Floor == c.Floor && a.Context == c.Context && a.EvaluatedContext != a.Context);
        (int Gained, int Lost) Pair(PartyFloorScore cell, PartyFloorScore reference) => (
            cell.Clears.Zip(reference.Clears).Count(p => p.First && !p.Second),
            cell.Clears.Zip(reference.Clears).Count(p => !p.First && p.Second));
        string Number(double? value) => value?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) ?? "unavailable";
        var text = new StringBuilder($"# Boss-specific Essence refinement\n\n{report.Status}; {report.ActualBattles}/{report.PlannedMaximum} actual combats; {report.CacheHits} cache hits.\n\n");
        text.AppendLine($"Objective {Algorithm(d)}; target floors {string.Join(", ", d.Objective.TargetFloorWeights.Keys)}; intent {d.Objective.StrategyIntent}. Fixed budget: {b.EssenceSlots} ordered Essences, level {b.CharacterLevel}, Uncommon {b.Quality} tier {b.Tier} rank {b.Rank}. Mutable absolute party slots: {string.Join(", ", d.MutablePartySlots)}. Level-1 unascended/unevolved Essences, fixed identities and gear, no styles. {d.OwnershipAssumption}\n");
        text.AppendLine($"Retained references: {d.Controls.Count}; frozen finalists: {report.Selection.Count}/{d.Finalists} maximum. Historical reference set: {refinement.ReferenceSetId ?? "none; authored controls"}. Search anchor: {refinement.AnchorId}. The anchor guides bounded proposals and the reserved quartet; it does not replace any retained control.\n");
        text.AppendLine((refinement.ReferenceEvidenceStatus ?? "No imported pilot observations for this boss/budget; the authored reference is retested.") + "\n");
        text.AppendLine("Rank discovery by worst-context weighted target clear-rate gain, then guardian health, survival, victory-only duration and stable ID. All methods receive the same controls, proposal/evaluation allocation and paired discovery schedule. Random exploration remains uniform; the other methods refine the declared anchor. Finalists and behavior labels freeze before fresh confirmation. Restarts and alias contexts are not independent samples. No optimum or method-superiority claim.\n");
        text.AppendLine("## Search arms\n\n| Method | Generation seed | Evaluated candidates | Discovery fights | Stop |\n| --- | --- | --- | --- | --- |");
        foreach (var arm in report.Arms)
            text.AppendLine($"| {arm.Method} | {arm.Seed} | {arm.Evaluations.Count} | {arm.Evaluations.SelectMany(e => e.Cells).SelectMany(c => c.Trials).Distinct().Count()} | {arm.StopReason} |");
        text.AppendLine("\n## Fresh target confirmation\n\nWilson 95% intervals are pointwise descriptive estimates without multiplicity adjustment. Zero clears remain unsuccessful. The original finalist order is preserved. Transfer weaknesses count non-target canonical cells with fewer wins than at least one retained control. Full matched comparisons and trial IDs remain in boss-search.json.\n\n| Party | Kind | Target/context | Wins/trials | Wilson 95% | Guardian health | Gained/lost vs anchor | Transfer weaknesses |\n| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var choice in report.Selection)
        {
            if (!measured.TryGetValue(choice.Id, out var row)) continue;
            var weaker = row.Cells.Where(c => Canonical(c) && !d.Objective.TargetFloorWeights.ContainsKey(c.Floor)).Count(cell =>
                controls.Keys.Where(measured.ContainsKey).Any(id => Pair(cell, measured[id].Cells.Single(c => c.Context == cell.Context && c.Floor == cell.Floor)) is var p && p.Lost > p.Gained));
            foreach (var cell in row.Cells.Where(c => Canonical(c) && d.Objective.TargetFloorWeights.ContainsKey(c.Floor)))
            {
                var wins = cell.Clears.Count(x => x); var interval = SuiteScorecard.Wilson(wins, cell.Clears.Count)!;
                var againstAnchor = measured.TryGetValue(refinement.AnchorId, out var anchor)
                    ? Pair(cell, anchor.Cells.Single(c => c.Floor == cell.Floor && c.Context == cell.Context)) : ((int Gained, int Lost)?)null;
                var kind = choice.Id == refinement.AnchorId ? "Retained anchor" : controls.ContainsKey(choice.Id) ? "Retained reference" : "New finalist";
                text.AppendLine(FormattableString.Invariant($"| {choice.Id[..12]} | {kind} | {cell.Floor}/{cell.Context} | {wins}/{cell.Clears.Count} | {100 * interval.Lower:F1}–{100 * interval.Upper:F1}% | {cell.GuardianHealth:F2}% | {(againstAnchor is { } paired ? $"{paired.Gained}/{paired.Lost}" : "unavailable")} | {weaker} |"));
            }
        }
        text.AppendLine("\n<details><summary>Target comparisons against every retained reference</summary>\n\n| Party | Target/context | Reference | Reference wins | Gained/lost |\n| --- | --- | --- | --- | --- |");
        foreach (var row in report.Confirmation)
            foreach (var cell in row.Cells.Where(c => Canonical(c) && d.Objective.TargetFloorWeights.ContainsKey(c.Floor)))
                foreach (var id in controls.Keys.Where(measured.ContainsKey))
                {
                    var other = measured[id].Cells.Single(c => c.Floor == cell.Floor && c.Context == cell.Context);
                    var paired = Pair(cell, other);
                    text.AppendLine($"| {row.Id[..12]} | {cell.Floor}/{cell.Context} | {id[..12]} | {other.Clears.Count(x => x)} | {paired.Gained}/{paired.Lost} |");
                }
        text.AppendLine("\n</details>\n\n## Observed healing and regeneration\n\nMeans over target encounters. Friendly reported healing retains the combat observer's direct/periodic/lifesteal attribution limits; effective friendly regeneration is separate. Guardian healing and effective regeneration refer to the original guardian. These totals can reflect fight duration, targeting, other party effects and equipment/base regeneration. They do not establish which Essence caused an outcome or deaths prevented. Missing recovery fields are unavailable, never assumed zero.\n\n| Party | Friendly reported healing | Friendly regeneration | Guardian healing | Guardian regeneration |\n| --- | --- | --- | --- | --- |");
        foreach (var row in report.Confirmation)
            text.AppendLine($"| {row.Id[..12]} | {Number(row.Behavior.Healing)} | {Number(row.Behavior.Recovery?.FriendlyRegeneration)} | {Number(row.Behavior.Recovery?.GuardianHealing)} | {Number(row.Behavior.Recovery?.GuardianRegeneration)} |");
        text.AppendLine("\n<details><summary>Frozen behavior labels and complete ordered loadouts</summary>\n\nBehavior labels describe discovery observations; they are not confirmed causal roles. Full exported scenarios include every required ally and fixed gear.\n\n| Party | Source | Frozen labels | Slot: ordered Essences |\n| --- | --- | --- | --- |");
        foreach (var choice in report.Selection)
            text.AppendLine($"| {choice.Id[..12]} | {choice.Source} | {string.Join(", ", report.StrategyArchive.Where(s => s.Id == choice.Id).Select(s => s.Intent))} | {string.Join("; ", choice.Builds.Select(p => p.Key + ": " + string.Join(" / ", p.Value)))} |");
        text.AppendLine("\n</details>\n\n## Reserved A/B replacement diagnostic\n\nThe quartet compares the historical anchor, replacement A alone, replacement B alone and A+B on independent reserved seeds. Legacy enabler/consumer fields identify the inserted A/B Essences; whole-Essence substitutions do not establish enabler/consumer synergy. Other effects, existing suppliers, cooldowns and targeting remain confounders. No diagnostic outcome reselects finalists.\n");
        if (plan is null) text.AppendLine("The frozen diagnostic plan is unavailable here. Inspect diagnostic-plan.json; no replacement-specific interpretation is supplied.\n");
        else
        {
            text.AppendLine($"Plan status: {plan.Status}. Inserted A: {plan.Enabler ?? "unavailable"}; inserted B: {plan.Consumer ?? "unavailable"}.\n");
            text.AppendLine("<details><summary>Exact replacements, mechanism evidence and limitations</summary>\n\n" + plan.Note + "\n\n</details>\n");
        }
        text.AppendLine("| Variant | Party | Target wins/trials | Guardian health | Friendly reported healing | Friendly regeneration | Guardian healing | Guardian regeneration |\n| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var row in report.Diagnostics)
        {
            var source = plan?.Parties.FirstOrDefault(p => p.Id == row.Id)?.Source;
            var variant = source switch { "baseline" => "Anchor", "enabler-alone" => "A alone", "consumer-alone" => "B alone", "combination" => "A+B", _ => "Unmapped variant" };
            var wins = string.Join("; ", row.Cells.Where(Canonical).Select(c => $"F{c.Floor}: {c.Clears.Count(x => x)}/{c.Clears.Count}"));
            text.AppendLine($"| {variant} | {row.Id[..12]} | {wins} | {Number(row.Fitness.GuardianHealth)}% | {Number(row.Behavior.Healing)} | {Number(row.Behavior.Recovery?.FriendlyRegeneration)} | {Number(row.Behavior.Recovery?.GuardianHealing)} | {Number(row.Behavior.Recovery?.GuardianRegeneration)} |");
        }
        text.AppendLine("\nConfirm known-boss repeatability on the fresh schedule; transfer rows do not establish unseen-boss generalization. Reconstruct with tower-boss-verify, export exact party scenarios, and replay archived trials with the recorded executable/runtime. Cancelled or invalid studies remain unconfirmed. No production tuning, migration, account changes or deployment is included.");
        return text.ToString();
    }
}
