# Milestone 1 Balance Runner

Milestone 1 establishes a dedicated executable that loads production combat and Essence data, runs a deterministic production-engine smoke simulation, and persists machine-readable and human-readable reports.

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
--elite-restarts <number>          Independent elite-search restarts.
--elite-population <number>        Candidates per restart and slot profile.
--elite-generations <number>       Search generations per restart.
--elite-max-generations <number>   Hard ceiling for adaptive certification generations.
--elite-elites <number>            Elites retained per certification generation.
--elite-finalists <number>         Pareto-diverse finalists per slot profile.
--elite-local-swap-depth <1|2>     Local-neighborhood challenge depth.
--elite-two-swap-limit <number>    Two-swap challengers per finalist; 0 means complete.
--elite-restart-refinement <number>  One-swap refinement passes per restart winner.
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
--content-root <path>   API.LL directory containing the production Data folder.
--output <path>         Override the report root.
--full                  Run the currently implemented pipeline.
--help                  Display command help.
```

The default content root is discovered from the repository layout. The default report root is `balance-output/` at the repository root.

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
14. elite-build certification, local challenges, party search, and P95/P99 holdouts;
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
- holdout confidence, seed stability, monotonicity, sensitivity, and percentile-order checks can reject unsafe scaling;
- JSON and Markdown reports are written to both `latest` and immutable `history` locations;
- invalid command-line arguments fail explicitly.

An end-to-end smoke invocation also verifies production catalog discovery, combat execution, Git metadata collection, and report rendering.
