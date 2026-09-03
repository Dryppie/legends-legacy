# Milestone 1 Balance Runner

Milestone 1 establishes a dedicated executable that loads production combat and Essence data, runs a deterministic production-engine smoke simulation, and persists machine-readable and human-readable reports.

## Equipment reference mode — 3 September 2026

The separate [equipment reference command](equipment-reference-builds.md) uses canonical Tier-1 rank/style equipment and production combat. The existing pipeline below retains its legacy Quality/rarity gear packages and calibration policies; its results do not certify equipment progression readiness.

## Command

The one-action entry point from the repository root is:

```powershell
.\build\run-balance.ps1
```

The wrapper always starts the complete implemented pipeline with `--full`. Additional runner options can be passed through, for example:

```powershell
.\build\run-balance.ps1 --seed 8471 --output C:\temp\balance-run
```

The equivalent direct command is:

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance -- --full --seed 1337
```

Supported options:

```text
--seed <number>         Deterministic simulation seed.
--build-count <number>  Random builds per 4/5/6-slot Essence profile (default: 10).
--optimizer-population <number>  Candidates per E4/E5/E6 population (default: 20).
--optimizer-generations <number> Search generations (default: 4).
--optimizer-elites <number>      Elites preserved per generation (default: 5).
--optimizer-mutation <number>    Per-slot mutation probability (default: 0.25).
--optimizer-random <number>      Random-injection population share (default: 0.10).
--optimizer-diversity <number>   Similarity penalty in score points (default: 8).
--optimizer-retained <number>    Final candidates retained per profile (default: 10).
--representative-count <number>  Builds retained per P50/P75/P90 profile (default: 10).
--progression-curve <value>      linear, ease-in, ease-out, or smooth-step (default).
--tower-simulations <number>     Seeded party simulations per Floor 1-10 (default: 10).
--calibration-iterations <number>  Bounded encounter-search iterations (default: 10).
--encounter-candidate-simulations <number>  Trials per encounter-specific candidate (default: 3).
--encounter-retained <number>      Specialized builds retained per floor (default: 5).
--certification-profile <value>    developer (default) or release.
--elite-search-only                Skip holdouts and party search; never certifies.
--elite-restarts <number>          Independent elite-search restarts.
--elite-population <number>        Candidates per restart and slot profile.
--elite-generations <number>       Search generations per restart.
--elite-max-generations <number>   Hard ceiling for adaptive certification generations.
--elite-elites <number>            Elites retained per certification generation.
--elite-crossover <number>         Experimental elite-parent crossover rate, 0.00-1.00 (default: 0).
--elite-basin-jump <number>        Restart-local coordinated 3/4-gene mutation rate, 0.00-1.00 (default: 0).
--elite-explorer-archive <number>  Persistent restart-local explorer candidates, 0-100 (default: 0).
--elite-stratified-portfolio <number>  Separately seeded legal candidates per restart/profile, 0-5000 (default: 0).
--elite-quality-island <number> Restart-local quality-diversity island budget per profile, 0-5000 (default: 0).
--elite-mechanic-island <number> Restart-local mechanic-archetype island budget per profile, 0-5000 (default: 0).
--elite-valley-beam-width <number> Opt-in restart valley-search beam width; 0 disables.
--elite-valley-beam-depth <number> Opt-in restart valley-search depth; 0 disables.
--elite-valley-budget <number>     Candidate budget per restart/profile; 0 disables.
--elite-valley-prefilter <number> Fully benchmark at most this many valley candidates per depth; 0 disables.
--elite-bridge-audit              Audit shortest legal bridges between differing restart winners; disabled by default.
--elite-descriptor-audit          Audit E5 descriptor separability and coarse-niche collisions; disabled by default.
--elite-benchmark-confidence-audit  Repeat a stratified E5 cohort on common PvE seeds; disabled by default.
--elite-confidence-cohort <number>  Confidence-audit E5 cohort size (default: 512).
--elite-confidence-seeds <number>   Confidence-audit common seed count (default: 16).
--elite-confidence-margin <number>  Target approximate 95% score half-width (default: 0.25).
--elite-finalists <number>         Pareto-diverse finalists per slot profile.
--elite-local-swap-depth <1|2>     Local-neighborhood challenge depth.
--elite-two-swap-limit <number>    Two-swap challengers per finalist; 0 means complete.
--elite-restart-refinement <number>  Local refinement passes per restart beam seed.
--elite-restart-seeds <number>     Pareto-diverse refinement seeds per restart.
--elite-restart-two-swap-limit <number> Two-swap escape candidates per stalled restart pass; 0 disables.
--elite-finalist-refinement <number> Neighborhood absorption rounds before final challenge.
--elite-holdout-seeds <number>     Independent elite holdout seeds.
--elite-simulations <number>       Elite holdout trials per seed.
--elite-party-genomes <number>     Party genomes evaluated per floor.
--elite-policy <path>              Certification policy JSON override.
--top-player-builds <path>         Curated player fixture JSON override.
--validation-seeds <number>        Holdout seeds per floor (default: 8).
--validation-simulations <number>  Calibrated trials per holdout seed (default: 50).
--validation-probe-simulations <number>  Trials per sensitivity probe and seed (default: 25).
--meta-simulator-battles <number>  Complementary 1v1 Essence battles (default: 2000).
--meta-simulator-rounds-per-matchup <number> Balanced all-Essence round robin; 0 disables (default: 0).
--content-root <path>   API.LL directory containing the production Data folder.
--output <path>         Override the report root.
--full                  Run the currently implemented pipeline.
--help                  Display command help.
```

The default content root is discovered from the repository layout. The default report root is `balance-output/` at the repository root.

Valley search is diagnostic and disabled in both approved profiles. Width, depth, and total candidate budget must be enabled together. `--elite-valley-prefilter` may then limit authoritative PvE benchmarks per depth using Essence usage, performance, and pair-synergy metadata. That surrogate never becomes a certification score: generated, rejected, and fully benchmarked counts are serialized, and only full benchmark results can affect a verdict. The measured width-16/depth-3 experiments on seed `8471` failed convergence and runtime, so none of those experimental settings are frozen defaults.

The bridge audit is also diagnostic and disabled in both approved profiles. `--elite-bridge-audit` selects the strongest and lowest-scoring distinct restart winners per slot profile, enumerates every legal genome on their minimum-substitution bridge, benchmarks all nodes through the production PvE boundary, and reports the deterministic best maximin path plus regression reachability. Audit builds and counts are serialized separately and cannot affect certification candidates, restart evidence, percentiles, finalists, challenges, verdicts, or `TotalUniqueCandidatesEvaluated`.

Coordinated mutation is experimental and disabled in both approved profiles. `--elite-basin-jump` replaces the requested share of ordinary genetic births with deterministic legal three/four-gene jumps from that restart's own elites; it does not add births, use bridge nodes, or exchange genomes between restarts. Crossover and basin jumps cannot be enabled together. Successful jump counts are serialized per generation and restart.

`--elite-explorer-archive` retains a bounded set of recent explorer descendants as alternate parents in the same restart. Approximately half of later explorer births continue archived genomes through ordinary mutation and half create new coordinated seeds. Archived candidates receive no automatic elite status or score adjustment. The archive requires basin jumps, remains disabled by default, and reports continuation counts separately.

`--elite-stratified-portfolio` runs only after the unchanged baseline optimizer and refines baseline and portfolio beams separately. It reports direct portfolio evaluations plus the fully refined baseline and final ceilings for each restart/profile. It is mutually exclusive with crossover, basin jumps/archive, and valley search, and remains disabled by default.

`--elite-quality-island` runs after the complete baseline optimizer and refinement. It pre-fills a strongest/weakest-PvE-scenario niche map from the same restart's baseline, then authoritatively benchmarks a 32-candidate seed batch and one-swap descendants of niche champions until the fixed budget is exhausted. Initial/descendant counts, niche occupancy/replacements, island ceiling, and baseline/final ceilings are serialized. It is mutually exclusive with all other search experiments and remains disabled by default.

The seed-`8471` algorithm-v11 run used search-only `96`/`24`-`40`/`12` settings with crossover and valley search disabled. It took `1,247.34` seconds, retained `59,006` certification candidates, and separately benchmarked `146` bridge nodes. The complete 70-node E5 bridge had a best maximin score of `83.69` from the `85.27` source, a `1.30` largest step regression, and no non-regressing or `0.50`-bounded route to `86.21`. This confirms a genuine shortest-path valley but does not approve a search budget or exclude longer detours.

The algorithm-v12 `20%` basin-jump trial used seed `8471` with the same search-only `96`/`24`-`40`/`12` settings and every other experimental option disabled. It replaced `3,366` ordinary births, ran for `1,407.29` seconds, and evaluated `62,296` certification candidates. E4 reached `78.61` with `0.46` spread, E6 retained `87.46` with `0.37` spread, but E5 worsened to `1.09` spread while retaining the `86.21` ceiling in only one restart. The option remains forensic-only; seed `1337` was not run and no default was changed.

The algorithm-v13 archive follow-up used the same rate with 12 persistent candidates. It produced `1,664` seed jumps and `1,513` continuation births, ran for `1,386.22` seconds, and evaluated `59,793` certification candidates. Its E5 spread of `0.15` was invalid because the observed ceiling fell from `86.21` to `85.27`; E6 widened to `0.99`, and the overall verdict remained `SearchUnstable`. Seed `1337` was not run. The archive remains forensic-only and no default changed.

The algorithm-v14 isolated-portfolio follow-up used `256` deterministic candidates per restart/profile and separate baseline/portfolio refinement beams. It directly benchmarked `2,304` portfolio candidates, evaluated `80,560` local candidates, retained `109,724` unique certification candidates, and ran for approximately `517.35` seconds. E4 and E6 passed at `0.29` and `0.34` spread while retaining their known ceilings. E5 retained `86.21` but failed at `1.09` spread (`86.21`, `85.12`, `85.27`). Seed `1337` was not run. The option remains forensic-only; no default, budget, or tolerance changed.

The corrected algorithm-v15 quality-island follow-up used `256` candidates per restart/profile, retained `62,865` unique certification candidates, and ran for approximately `256` seconds. The 7–11 occupied scenario-pair niches produced 1–13 champion replacements per restart, but no island exceeded its baseline. E4 failed at `0.63`, E5 failed at `1.30` (`86.21`, `84.91`, `85.27`), and E6 retained its baseline `0.34` spread. Seed `1337` was not run. The option remains forensic-only; no default, budget, or tolerance changed.

## Complete Automated Flow

One invocation executes every completed stage as a single orchestration:

1. production content loading and deterministic smoke simulation;
2. Region 1 Floor 1/Floor 10 gear packages;
3. seeded legal E4/E5/E6 build generation;
4. five-scenario PvE benchmarking;
5. Combat Rating validation;
6. Essence optimization;
7. P50/P75/P90 representative-build selection;
8. power-anchor measurement;
9. Floor 1–10 progression-band interpolation;
10. production World Tower encounter analysis;
11. Essence usage, pairing, synergy, and complementary simulator analysis;
12. bounded encounter calibration recommendations;
13. encounter-specific optimization and cheese/hard-counter detection;
14. elite-build certification with adaptive restarts, local challenges, optional search diagnostics, party search, and P95/P99 holdouts;
15. Region 1 holdout scaling validation and sensitivity probes.

There are no manual handoffs or separate milestone commands within this flow. Reports are written only after the complete run succeeds. Encounter recommendations are generated automatically, but applying them to production content remains an intentional developer-approval step.

## Outputs

Each invocation generates:

```text
balance-output/
├── latest/
│   ├── gear-packages.json
│   ├── essence-builds.json
│   ├── benchmarks.json
│   ├── combat-rating.json
│   ├── optimizer.json
│   ├── representative-builds.json
│   ├── essence-meta-analysis.json
│   ├── power-anchors.json
│   ├── progression-bands.json
│   ├── world-tower-analysis.json
│   ├── encounter-calibration.json
│   ├── encounter-specific-optimization.json
│   ├── elite-build-certification.json
│   ├── scaling-validation.json
│   ├── summary.json
│   └── summary.md
└── history/
    └── <run-id>/
        ├── gear-packages.json
        ├── essence-builds.json
        ├── benchmarks.json
        ├── combat-rating.json
        ├── optimizer.json
        ├── representative-builds.json
        ├── essence-meta-analysis.json
        ├── power-anchors.json
        ├── progression-bands.json
        ├── world-tower-analysis.json
        ├── encounter-calibration.json
        ├── encounter-specific-optimization.json
        ├── elite-build-certification.json
        ├── scaling-validation.json
        ├── summary.json
        └── summary.md
```

History directories are immutable. A report records its run ID, UTC timestamp, seed, balance schema version, simulator algorithm version, combat-engine assembly version, Git commit when available, production catalog counts, and combat result. Milestone 2 extended the output with `gear-packages.json`; Milestone 3 added `essence-builds.json`; Milestone 4 added `benchmarks.json`; Milestone 5 added `combat-rating.json` and CR-health summaries; Milestone 6 added deterministic Essence optimization and `optimizer.json`; Milestone 7 added the P50/P75/P90 library and `representative-builds.json`; Milestones 8 and 9 added `power-anchors.json` and `progression-bands.json`; Milestone 10 added `world-tower-analysis.json`; Milestone 11 added `essence-meta-analysis.json`; Milestone 12 added `encounter-calibration.json`; Milestone 13 added `encounter-specific-optimization.json`; the elite gate added `elite-build-certification.json`; the pre-Milestone-14 scaling gate added `scaling-validation.json`.

Generated reports are ignored by Git.

## Architecture Boundary

The runner loads the production ability, status, summon, Essence, creature, creature-ability, progression-scaling, and World Tower catalogs. World Tower analysis materializes detached canonical player builds, then uses `CombatPreparationPipeline`, `WorldTowerCombatRuntimeFactory`, Guardian scaling, authored abilities, and `CombatEngineExecutor`/`FastCombatEngine`. Encounter calibration, encounter-specific optimization, and scaling validation reuse that production path with temporary health/offense values and never persist them. Validation derives distinct holdout seeds and uses the calibrated values only as temporary simulation inputs. No database or persisted player state is required.

The `production-essence-smoke-1v1` scenario remains a foundation check rather than a balance benchmark. The one command now runs Milestones 1–13 plus the Region 1 scaling-validation gate, including real World Tower Floors 1–10, optimizer-population Essence meta analysis, bounded encounter recommendations, encounter-specific exploit analysis, and 18,000 default holdout/control/sensitivity battles.

## Verification

Automated coverage verifies:

- identical production content and seed produce identical combat results;
- one `BalanceRunRequest` produces every schema-15 stage from gear packages through elite certification and scaling validation;
- World Tower Floors 1–10 use deterministic varied parties and authored encounter content;
- Essence percentile usage, common partners, additive pair deltas, and warning thresholds are deterministic;
- encounter calibration reuses parties and combat seeds, stays within its approved bounds, and never modifies production content;
- encounter-specific optimization is deterministic, preserves generic progression data, and classifies narrow cheese evidence separately from hard counters;
- elite certification preserves restart independence, records raw/refined genomes and distances, reports coordinated-mutation seeds, explorer continuations, portfolio evidence, quality-island evaluations/niches/ceilings, optional valley generation, prefilter, evaluation, depth, exhaustion, and improvement evidence, and keeps shortest-path bridge evaluations in a separate audit-only section;
- holdout confidence, seed stability, monotonicity, sensitivity, and percentile-order checks can reject unsafe scaling;
- JSON and Markdown reports are written to both `latest` and immutable `history` locations;
- invalid command-line arguments fail explicitly.

An end-to-end smoke invocation also verifies production catalog discovery, combat execution, Git metadata collection, and report rendering.
