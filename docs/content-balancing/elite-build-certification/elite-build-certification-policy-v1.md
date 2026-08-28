# Elite Build Certification Policy v1

**Status:** Implemented as the schema-15 runtime policy; release thresholds remain subject to explicit game-design approval  
**Policy ID:** `WorldTowerEliteCertificationV1`  
**Scope:** World Tower Region 1 and the pre-Milestone-14 elite certification gate

This policy turns the [Elite Build Certification Gate](elite-build-certification-gate.md) into explicit, versioned decisions that can be implemented by the one-click balance runner. It defines percentile semantics, convergence tolerances, encounter expectations, search budgets, curated-player evidence, and the evidence required for a certification verdict.

This policy does not change production content. Generic P75 builds remain the normal progression and calibration target. Elite populations are a separate stress boundary used to detect theoretical ceilings, encounter trivialization, hard counters, and mechanic bypasses.

## 1. Population semantics

The balance report must keep these populations independent:

| Population | Meaning | Use |
| --- | --- | --- |
| Generic P75 | Competently constructed build from the ordinary representative library | Normal progression, recommended Combat Rating, and encounter calibration |
| Certified P95 | Highly optimized generic build from the certification search | High-end difficulty and clear-speed pressure |
| Certified P99 | Extreme generic build from the certification search frontier | Theoretical-ceiling and exploit stress testing |
| Encounter-specialized elite | Party optimized directly against one real encounter | Hard-counter, mechanic-bypass, burst, sustain, and cheese detection |
| Curated player builds | Reviewed builds or parties supplied by developers or trusted testers | External comparison against automated search |

P95 and P99 are percentiles of the deduplicated certification-search population for a specific content fingerprint and certification policy. They are not percentiles of live players and must never be presented as such.

The percentile population combines all unique legal candidates evaluated by every certification restart and search strategy. Duplicate genomes count once. The report must retain the population size, strategy contribution, score distribution, and content fingerprint used to derive each percentile.

Encounter holdouts use deterministic cohorts centered on the P95 and P99 target scores, rather than taking the strongest builds above each threshold. This keeps the percentile populations distinct and prevents both holdouts from collapsing into the same upper-tail party. The selected cohort build IDs must be serialized in the certification artifact.

## 2. Certification tolerances

The following values are the proposed v1 engineering tolerances:

| Check | Requirement |
| --- | --- |
| Independent restart agreement | Best relevant-score spread is no more than `0.50` points |
| Cross-strategy agreement | Best strategies finish within `1.00` point and share at least one equivalent elite archetype |
| Release search plateau | No best-score improvement greater than `0.25` during the final 10 configured generations |
| Generic local challenge | No legal neighbor improves aggregate or relevant scenario fitness by more than `1.00` point |
| Encounter local challenge | No legal neighbor gains more than 3 clear-rate points or reduces median successful kill time by more than 5% without reducing clear rate |
| Holdout precision | The two-sided 95% Wilson clear-rate interval is no wider than 5 percentage points |
| Human-build comparison | No curated build exceeds the automated ceiling by more than 2 benchmark points, 3 clear-rate points, or a 5% kill-time advantage |
| Diversity | Multiple materially distinct elite archetypes are retained unless a mandatory-Essence warning explicitly explains convergence |
| Legality | Every build and party passes production source-family, slot, content-resolution, equipment, level, and progression rules |

A search-budget limit, runtime interruption, or incomplete neighborhood traversal must never be treated as passing evidence. The result must use a blocking incomplete-evidence warning or a non-certified verdict.

Aggregate local improvement is evaluated against the aggregate finalist. Scenario improvement is evaluated against the finalist leading that scenario and is blocking only when every other required benchmark component remains within the same `1.00`-point tolerance. This preserves intended Pareto tradeoffs while still rejecting a locally dominated specialist.

Party genomes are unordered multisets because multiple party members may use the same build. Party optimization is complete when it reaches the approved deterministic budget or exhausts the legal unique search space sooner. Reports must retain both the evaluated count and the computed legal party-search-space size; failing to reach the smaller of those two counts is incomplete evidence.

### Relevant fitness

Relevant fitness depends on the candidate being certified:

- generic finalists use aggregate PvE score and every required benchmark component;
- scenario specialists use their named benchmark component and survival consistency;
- encounter specialists use clear rate first, then survival, deaths, and successful kill time;
- party finalists use party-level clear rate, survival consistency, mechanic execution, and successful kill time.

An aggregate-score tie cannot hide a material improvement or regression on a required Pareto axis.

## 3. World Tower elite expectations

Elite clear rate confirms that the search found genuinely strong builds. Clear rate alone does not establish trivialization because elite players are expected to clear progression content reliably. Kill time and mechanic behavior provide the separate trivialization boundary.

| Population | Expected clear-rate evidence | Initial trivialization threshold |
| --- | --- | --- |
| Generic P75 | Existing 55%-75% Region 1 target | Existing Region 1 validation policy |
| Certified P95 | 95% interval lower bound is at least 80% | Median successful kill time is below 70% of the successful P75 median |
| Certified P99 | 95% interval lower bound is at least 90% | Median successful kill time is below 55% of the successful P75 median |
| Encounter-specialized party | Must materially outperform its generic comparison; no independent minimum | Median successful kill time is below 45% of the successful P75 median |
| Any elite population | Not applicable | A clear below 35% of the P75 median or bypass of a mandatory mechanic is a blocking mechanic-bypass warning |

The 70%, 55%, and 45% kill-time ratios begin as warnings during the first certification cycle. After reviewing the first complete distributions, game design should explicitly decide which ratios become blocking rules. Mandatory-mechanic bypass is blocking from the first version.

## 4. Execution profiles and search budgets

Evaluation-count budgets are deterministic and reproducible. Wall-clock targets are used only to choose or revise those counts.

| Setting | Developer profile | Release profile |
| --- | ---: | ---: |
| May emit `CertifiedElite` | No | Yes |
| Independent restarts | 3 | 8 |
| Population per restart and slot profile | 64 | 256 |
| Minimum generations | 12 | 60 |
| Maximum adaptive generations | 24 | 100 |
| Elites per generation | 8 | 32 |
| Elite-parent crossover rate | Disabled (`0%`) | Disabled (`0%`) |
| Coordinated 3/4-gene mutation rate | Disabled (`0%`) | Disabled (`0%`) |
| Persistent explorer archive | Disabled (`0`) | Disabled (`0`) |
| Isolated stratified portfolio | Disabled (`0`) | Disabled (`0`) |
| Valley beam and metadata prefilter | Disabled | Disabled |
| Restart bridge audit | Disabled | Disabled |
| Required final plateau | 4 generations | 10 generations |
| Approximate genetic candidates per slot profile | 2,500-4,800 | 123,000-205,000 |
| Restart-refinement beam | 4 Pareto-diverse seeds per restart | 8 Pareto-diverse seeds per restart |
| Restart-seed refinement | Up to 6 complete one-swap passes per seed | Up to 12 complete one-swap passes per seed |
| Restart two-swap escape budget | 250 synergy-prioritized candidates per stalled pass | 1,000 synergy-prioritized candidates per stalled pass |
| Pareto-finalist refinement | Up to 3 reselection rounds | Up to 5 reselection rounds |
| Finalists per slot profile | 3-6 | 6-12 Pareto-diverse finalists |
| One-Essence challenge | Complete for every finalist | Complete for every finalist |
| Two-Essence challenge | Deterministic synergy-guided bounded search | Complete for every declared finalist |
| Holdout simulations per encounter and cohort | 4 seeds x 25 = 100 | 8 seeds x 200 = 1,600 |
| Initial party-genome budget per floor | 2,000 | 25,000 |
| Target runtime on the reference developer machine | No more than 15 minutes | No more than 8 hours |

The release sample of 1,600 trials is intended to satisfy the five-percentage-point confidence-width requirement even near the worst-case clear-rate distribution. Sample sizes remain configuration-owned and must be serialized into every report.

`--elite-search-only` is an explicitly non-certifying diagnostic mode for stabilizing generic search budgets. It executes independent genetic searches, restart-beam refinement, percentile construction, convergence checks, and local challenges, but skips encounter holdouts and party optimization. Its overall verdict must include `PartyOptimizationRequired` unless a higher-priority search failure applies. Search-only evidence cannot emit `CertifiedElite`, even with the release execution profile and complete curated fixtures.

Each genetic restart runs for at least the minimum generation count. It then continues until the configured plateau requirement is observed or the serialized maximum generation count is reached. Reaching the maximum without the required quiet window is blocking `SearchUnstable` evidence.

The runner supports an experimental deterministic legal crossover rate through `--elite-crossover`. Crossover combines two distinct elite parents, retains one Essence per source family, and then applies the ordinary per-gene mutation probability. A recombined child is not forced to mutate an additional gene; if it duplicates a previously evaluated genome it is rejected and regenerated. The rate remains disabled in both approved profiles because the initial robustness diagnostics did not demonstrate consistent convergence improvement. Non-crossover births retain the original guarantee that at least one legal gene changes.

`--elite-basin-jump` is a separate experimental rate for restart-local coordinated mutation. A selected birth replaces ordinary mutation within the existing population budget, chooses either three or four gene positions, and replaces every selected Essence with a different legal Essence while preserving one Essence per source family. The parent, replacement sampling, and deterministic RNG belong only to that restart. Crossover and coordinated mutation cannot be enabled together, and neither another restart's winner nor bridge-audit nodes may be used as mutation input. Successful coordinated births are serialized per optimizer generation and certification restart. The rate is zero in both approved profiles.

`--elite-explorer-archive` optionally retains up to the configured number of recent restart-local explorer descendants as alternate parents across generations. Once populated, approximately half of that restart's explorer births continue an archived genome through ordinary mutation and half seed a new three/four-gene jump from the official elites. Archived candidates receive no score bonus, automatic elite placement, cross-restart input, or certification exemption. They affect the official restart only if a descendant independently benchmarks strongly enough. Seed and continuation counts are serialized separately. A positive archive requires a positive basin-jump rate; both remain disabled in approved profiles.

`--elite-stratified-portfolio` is a separate ceiling-preserving coverage experiment. Each restart first completes the unchanged baseline optimizer and baseline refinement beam. The runner then creates the configured number of legal, deterministic, separately seeded portfolio genomes for each slot profile, benchmarks them through the production PvE boundary, and refines their beam separately. Portfolio candidates cannot alter baseline RNG, generation counts, candidates, or seed selection. The report retains the fully refined baseline build ID, genome, and score alongside the final result and reports direct portfolio evaluations per restart/profile. The final result must be at least the baseline result. Portfolio mode cannot be combined with crossover, coordinated mutation/archive, or valley search, and is disabled in both approved profiles.

An opt-in deterministic valley-crossing diagnostic is configured with `--elite-valley-beam-width`, `--elite-valley-beam-depth`, and `--elite-valley-budget`. All three values must be supplied together; all-zero values disable it. Starting from each restart's independently refined winner, the search expands complete deterministic one-swap layers and retains a Pareto-diverse beam from the current layer. Because the next layer may be weaker than the restart winner, the search can cross a temporary regression before finding a stronger genome. Decisions are restart-local: another restart's candidates may satisfy the shared benchmark cache, but never enter the beam unless reached through that restart's own path. This diagnostic remains disabled in both approved profiles until robustness and runtime evidence justify a frozen budget.

`--elite-valley-prefilter` optionally caps the candidates sent to the full PvE benchmark at each depth. The deterministic surrogate sums each genome's observed Essence performance delta, P99 usage signal, admin-adjusted score delta, and measured pair-synergy deltas, with canonical genome signature as the tie-breaker. The surrogate never contributes to a certification score or verdict; it only controls which legal candidates receive the expensive authoritative benchmark. Reports retain generated, rejected, and fully evaluated counts so prefilter aggressiveness is visible.

`--elite-bridge-audit` is a separate opt-in diagnostic. For each slot profile whose independently refined restart winners use different genomes, it chooses the strongest winner as the target and the lowest-scoring distinct winner as the source, with restart and canonical build ID tie-breakers. It enumerates every legal node on a minimum-substitution bridge, benchmarks those materialized builds through the production PvE boundary, and computes the deterministic maximin path. It reports endpoint restart/build IDs, endpoint genomes and scores, distance, node count, every path node, path minimum, largest step regression, accumulated deficit below the source, and reachability under zero-regression and restart-tolerance-bounded edges. Bridge builds live in an audit-only collection: they never enter certification candidates, restarts, percentiles, finalists, local challenges, verdicts, or `TotalUniqueCandidatesEvaluated`. The stronger winner and intermediates are never fed into another restart. The option is disabled in both approved profiles.

Before restart agreement is measured, each independent restart contributes a small deterministic beam containing its aggregate leader, scenario leaders, and Pareto-diverse alternatives. Every beam seed receives bounded deterministic one-swap hill-climbing using the `0.25`-point material-improvement threshold. When a pass is one-swap-stalled, the search evaluates the profile's separately budgeted deterministic two-swap escape set, ordered by measured strong-pair synergy and then stable legal-genome order. A material two-swap improvement becomes the seed for the next complete one-swap pass. Convergence compares the strongest independently refined optimum from each restart. Pareto finalists then absorb material one- and two-swap discoveries and are reselected for a bounded number of rounds before the final `1.00`-point certification challenge. The report retains raw and refined restart scores, build IDs and Essence genomes, distance from the strongest restart genome, actual generations, beam-seed and refinement-pass counts, one- and two-swap candidate counts, optional valley depth/candidate/exhaustion/improvement evidence, and finalist-refinement rounds.

### Runtime measurement before final budget approval

Before marking release budgets approved:

1. Benchmark 1,000 generic candidate evaluations.
2. Benchmark one complete one-Essence neighborhood.
3. Benchmark one complete E6 two-Essence neighborhood.
4. Benchmark 1,000 party-genome evaluations against a production Guardian.
5. Project the complete developer and release runtimes.
6. Adjust deterministic counts or improve execution performance while preserving required evidence.

If the release search cannot complete within the intended runtime, the runner must preserve partial evidence and emit a non-certified verdict. It must not silently skip required checks.

### Developer stabilization evidence, 2026-08-28

Search-only diagnostics measured a proposed `96` population, `24` minimum generations, `40` maximum generations, and `12` elites against two root seeds. Seed `1337` passed all automated generic-search checks with E4/E5/E6 restart spreads of `0.00`, `0.00`, and `0.45`. Seed `8471` retained an E5 spread of `0.94`; increasing its restart-refinement beam from four to eight seeds did not change the failing optimum. Experimental 35% crossover also failed to produce robust agreement and is therefore disabled by default.

The proposed higher developer budget is not approved as the new default. The existing `64` population, `12` minimum generations, `24` maximum generations, and `8` elites remain the quick diagnostic profile. Further stabilization must address the E5 basin barrier measured by the completed bridge audit below rather than relax the `0.50` tolerance or continue increasing local seed counts blindly.

The first valley-crossing experiment is intentionally diagnostic-only: width `16`, depth `3`, and at most `5,000` layer candidates per restart and slot profile. Promotion requires both root seeds (`1337` and `8471`) to meet restart spread `<= 0.50`, cross-strategy delta `<= 1.00`, and the existing local challenge, while the complete search-only run remains within the intended developer runtime. Budget exhaustion and any best-score gain must be reported; a passing result caused only by truncation is not sufficient evidence to freeze the budget.

Seed `8471` rejected that exact proposal. The search-only run took `1,538.39` seconds, evaluated `80,681` unique certification candidates, and exhausted the `5,000`-candidate valley allowance for every restart/profile. E4 converged at `0.29` spread and E6 at `0.07`, but E5 remained at `0.94` despite two restarts reaching the same `86.21` ceiling; the third remained four substitutions away at `85.27`. Cross-strategy and local-challenge checks passed. Because the run exceeded the `15`-minute criterion and did not fix the known E5 failure, the identical seed-`1337` run was not repeated. Width `16`, depth `3`, budget `5,000` therefore remains an opt-in forensic configuration and is not approved for either frozen profile.

The cheaper follow-up keeps width `16` and depth `3`, prefilters each layer to `256` authoritative benchmarks, and caps each restart/profile at `768` valley candidates. It is subject to the same spread, cross-strategy, local-challenge, determinism, and `15`-minute complete-run requirements. This setting is experimental evidence, not an approved profile default.

Seed `8471` also rejected the prefiltered proposal. The run took `1,249.75` seconds, generated `79,022` distinct restart-local valley candidates, rejected `72,110` through the surrogate, and admitted `6,912` to the authoritative benchmark. No restart exhausted its authoritative budget. E4 reported `0.00` spread, but the prefilter omitted the stronger `78.61` genome found by the exhaustive diagnostic and converged on the lower `78.32` result instead. E6 retained `0.07` spread. E5 remained unchanged at `0.94`: two restarts reached `86.21`, while the third remained four substitutions away at `85.27`. The setting therefore failed both runtime and convergence, and its apparent E4 agreement demonstrates that reduced spread alone is insufficient when pruning lowers the observed ceiling. Seed `1337` was not repeated. The prefilter remains available only for forensic experiments and is disabled by default.

The algorithm-v11 bridge audit completed on seed `8471` with the same search-only `96` population, `24`-to-`40` generation, and `12` elite settings, with crossover and valley search disabled. The complete run took `1,247.34` seconds, retained `59,006` unique certification candidates, and separately evaluated `146` legal bridge nodes. Certification evidence was unchanged by the audit: E4/E5/E6 spreads were `0.34`, `0.94`, and `0.12`; E5 remained `SearchUnstable`; and missing curated evidence continued to block the other profiles.

E5 contributed all `70` legal nodes between the four-substitution endpoints. Its deterministic best maximin path scored `85.27 -> 83.97 -> 83.69 -> 83.87 -> 86.21`: minimum `83.69`, largest one-step regression `1.30`, and total temporary regression `4.28` below the source. No non-regressing bridge and no bridge bounded to the frozen `0.50` restart tolerance exists within the complete minimum-substitution state space. E4 similarly required a `1.50` step across six nodes; E6 required a `1.80` step across 70 nodes. The E5 restarts are therefore separated by a genuine shortest-path fitness valley, rather than an overlooked monotone bridge. This bounded result does not exclude longer detours through Essences outside the two endpoint genomes.

The `96`/`24`-`40`/`12` budget is still not approved: the complete audit run exceeded the `15`-minute developer criterion, the frozen E5 spread remained failed, and an audit cannot establish robustness. The next search experiment should be a separately opt-in, restart-local diversity or coordinated-mutation mechanism capable of independently crossing a four-substitution basin, evaluated at a fixed candidate budget against seeds `1337` and `8471`. It must not seed one restart from another or weaken the `0.50` tolerance.

Algorithm v12 tested a `20%` coordinated-mutation rate on seed `8471` using the same `96` population, `24`-to-`40` generation, and `12` elite settings. Crossover, valley search, prefiltering, and bridge auditing were disabled. The operator replaced `3,366` ordinary mutation births with legal three/four-gene jumps; population and maximum-generation budgets were unchanged, while adaptive stopping and the resulting local-refinement populations remained evidence-dependent. The run took `1,407.29` seconds and evaluated `62,296` unique certification candidates.

The proposal failed. E4 found the stronger known `78.61` ceiling and passed at `0.46` spread, but E5 worsened from `0.94` to `1.09`: its restart results were `86.21`, `85.50`, and `85.12`, so only one restart reached the known ceiling. E6 retained `87.46` but widened from `0.12` to `0.37`. The run remained `SearchUnstable` and exceeded the `15`-minute criterion; seed `1337` was therefore not repeated. A `20%` direct-jump rate remains opt-in and is not approved. The result suggests that unretained one-generation jumps provide diversity but do not reliably carry weaker lineages across the E5 basin. Any next experiment should test a small persistent restart-local exploratory lineage or archive at a fixed maximum budget, while preserving the official elite and never importing cross-restart knowledge.

Algorithm v13 tested that follow-up using a `20%` explorer-birth rate and a 12-candidate persistent archive on seed `8471`, with the same `96`/`24`-`40`/`12` configuration and all other experiments disabled. It produced `1,664` coordinated seeds and `1,513` archive continuations, took `1,386.22` seconds, and evaluated `59,793` unique certification candidates. Persistence was therefore materially exercised.

The archive also failed. E4 reported `0.17` spread but only the lower `78.32` ceiling. E5 reported an apparently passing `0.15` spread, but all restarts collapsed to `85.27`, `85.12`, and `85.27`, losing the required `86.21` ceiling. E6 retained `87.46` in one restart but widened to `0.99`. The overall verdict remained `SearchUnstable`, runtime again exceeded 15 minutes, and seed `1337` was not run. This configuration remains opt-in and unapproved. The result demonstrates that spread reduction without ceiling retention is invalid and that undirected persistent exploration can displace successful search trajectories. Further random diversity knobs should not be promoted; the next investigation should isolate candidate-stream effects and evaluate a deterministic, ceiling-preserving portfolio or stratified initialization before spending another full diagnostic budget.

Algorithm v14 implemented that isolation with `256` deterministic stratified candidates per restart/profile. Every baseline optimizer and four-seed refinement beam completed before a separate four-seed portfolio beam, and the report retained both fully refined ceilings. The seed-`8471` run directly benchmarked `2,304` portfolio genomes, evaluated `80,560` baseline-plus-portfolio local candidates, retained `109,724` unique certification candidates, and completed in approximately `517.35` seconds. Crossover, coordinated mutation/archive, valley search/prefiltering, and bridge auditing were disabled.

Ceiling preservation worked: E4, E5, and E6 retained `78.61`, `86.21`, and `87.46`. E4 improved to a passing `0.29` spread (`78.61`, `78.61`, `78.32`) and E6 passed at `0.34` (`87.46`, `87.46`, `87.12`). E5 still failed at `1.09` (`86.21`, `85.12`, `85.27`); the portfolio improved the second restart only from its isolated baseline `84.91` to `85.12`, while the other two were unchanged. The run was therefore `SearchUnstable`; seed `1337` was not run, no search budget is promoted, and the frozen `0.50` tolerance is unchanged. Missing curated player evidence also remained blocking. This rejects undirected arithmetic stratification at this size. A further bounded experiment, if pursued, should isolate a structured quality-diversity island with explicit mechanic/source-family niches and retain the complete baseline path; simply increasing this portfolio is not justified.

## 5. Search and neighborhood requirements

Release certification requires at least two complementary deterministic strategies:

1. independent restarts of the diversity-aware genetic or beam search;
2. deterministic local or beam refinement seeded from top builds, measured Essence usage, pair synergy, and Pareto-frontier candidates.

Every declared finalist must receive:

- every legal one-Essence substitution;
- the configured two-Essence substitution search;
- high-synergy pair insertions;
- replacement challenges for repeatedly mandatory Essences;
- scenario-specific reranking;
- a production legality and content-resolution check.

Release mode requires a complete two-Essence challenge for each declared finalist. The finalist cohort should therefore remain small, Pareto-diverse, and explicitly recorded. If the implementation later permits a bounded release challenge, that is a policy-version change and cannot be introduced silently.

## 6. Pareto retention and party optimization

The certification library must preserve a Pareto frontier across:

- short single-target performance;
- sustained single-target performance;
- high incoming-damage survival;
- multi-target performance;
- attrition and sustain;
- real-encounter clear rate;
- survival consistency;
- successful kill time.

The system must optimize complete parties against real encounters. Party genomes must represent complementary kits, role coverage, duplicated versus diversified builds, buffs, debuffs, summons, statuses, burst timing, and sustain loops.

Strong individual candidates without completed party optimization cannot produce `CertifiedElite`; they produce `PartyOptimizationRequired` or another applicable non-certified verdict.

## 7. Curated top-player fixtures

The default repository-owned fixture should live outside production gameplay content at:

```text
LL/tools/LegendsLegacy.Balance/Fixtures/top-player-builds.json
```

The CLI should additionally support:

```text
--top-player-builds <path>
```

This allows private or pre-release fixture collections to participate without committing personal or environment-specific data.

Each fixture entry must contain:

```text
Stable build or party ID
Source category
Review date
Content-version fingerprint
Gear package or exact equipment
Character level and progression state
Essence IDs and evolution state
Intended role or encounter
Optional observed result
Reviewer note
```

Do not store player names, account IDs, or other personal identifiers. A fingerprint mismatch, missing production definition, illegal progression state, or invalid loadout must fail explicitly rather than evaluating stale or impossible content.

### Minimum player evidence

The proposed minimum evidence for `CertifiedElite` is:

- three independently authored and reviewed curated builds for each of E4, E5, and E6;
- at least one curated party for every encounter being certified;
- successful production materialization for every fixture;
- comparison of every fixture against the automated ceiling on common holdout seeds.

Do not invent player fixtures to satisfy the count. When automated evidence passes but credible player evidence is absent, the report may expose an `AutomatedEvidenceStable` sub-result, but the overall verdict remains `InsufficientPlayerEvidence`. Continuing toward Region 1 remediation in that state requires an explicit developer exception; it must not weaken the meaning of `CertifiedElite`.

## 8. Configuration and report ownership

The runtime configuration should be stored as a versioned machine-readable file, for example:

```text
LL/tools/LegendsLegacy.Balance/Configuration/elite-certification-policy.v1.json
```

It must own:

- policy ID and version;
- content type;
- developer and release budgets;
- convergence and plateau tolerances;
- local-challenge tolerances;
- P95 and P99 expectations;
- kill-time and mechanic-bypass rules;
- holdout precision and seed requirements;
- curated-fixture minimums;
- every prerequisite for each verdict.

The generated `elite-build-certification.json`, `summary.json`, and `summary.md` must include the resolved policy, policy fingerprint, content fingerprint, execution profile, and actual evaluation counts. Historical reports must remain interpretable after later policy revisions.

This policy is implemented in balance report schema version 15.

## 9. Verdict prerequisites

`CertifiedElite` is permitted only in the release profile and only when all of these pass:

- every required slot profile and encounter population was evaluated;
- independent restart and cross-strategy agreement passed;
- the required plateau completed;
- all declared finalists passed their one- and two-Essence challenges;
- Pareto scenario coverage passed;
- complete parties were optimized and validated;
- holdout confidence requirements passed;
- every curated fixture was valid and compared;
- minimum curated-player evidence was present;
- no human build materially exceeded the automated ceiling;
- no blocking legality, stale-content, mechanic-bypass, or incomplete-evidence warning remained.

Partial results remain valuable, but they must use a non-certified verdict such as:

```text
SearchUnstable
LocalImprovementFound
ScenarioCoverageFailure
PartyOptimizationRequired
HumanBuildOutperformed
InsufficientPlayerEvidence
```

## 10. Remaining approval and execution sequence

Policy configuration, CLI parsing, schema snapshots, independent restarts, Pareto retention, deterministic neighborhood challenges, party optimization, release holdouts, explicit verdicts, and report persistence are implemented. Proceed with the remaining work in this order:

1. Review and approve the percentile semantics and World Tower expectations in this policy.
2. Approve the proposed convergence and local-improvement tolerances.
3. Add genuine curated fixtures or explicitly accept that the first result will be `InsufficientPlayerEvidence`.
4. Implement timing-only benchmarks and measure the reference machine.
5. Finalize the deterministic developer and release budgets from those measurements.
6. Design a deterministic ceiling-preserving portfolio or stratified initialization that isolates experimental candidate streams, informed by the rejected direct-jump and archive experiments, without feeding cross-restart knowledge into certification decisions.
7. Stabilize independent generic search on both root seeds without weakening the frozen tolerances or losing the observed ceiling.
8. Run the complete release profile with reviewed curated evidence.
9. Permit `CertifiedElite` only when every required evidence source passes.
10. Freeze the certified cohorts against the content fingerprint.
11. Recalibrate unstable Region 1 Floors 2-5.
12. Review mechanic and build-profile interactions on Floors 1, 6, 7, 9, and 10.
13. Stress every recommendation against certified P95/P99, specialized, and curated parties.
14. Rerun the existing [Region 1 Scaling Validation Gate](../region-1-scaling-validation.md) unchanged.
15. Begin Milestone 14 only after both gates pass or explicit exceptions are documented.

## 11. Approval checklist

Change this document's status to `Approved` only after each item has an explicit decision:

- [ ] P95/P99 search-population semantics accepted.
- [ ] P75 remains the normal progression target.
- [ ] Restart, cross-strategy, plateau, and local-challenge tolerances accepted.
- [ ] P95/P99 clear-rate expectations accepted.
- [ ] Kill-time warning ratios and mechanic-bypass rule accepted.
- [ ] Developer profile budget accepted.
- [ ] Release profile budget accepted after runtime measurement.
- [ ] Curated-fixture schema and storage location accepted.
- [ ] Minimum curated-player evidence accepted or revised.
- [ ] Missing player evidence confirmed to block `CertifiedElite`.
- [ ] Release-only certification behavior accepted.
- [ ] No certification stage may mutate production content automatically.
