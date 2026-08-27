# Milestone 1 Balance Runner

Milestone 1 establishes a dedicated executable that loads production combat and Essence data, runs a deterministic production-engine smoke simulation, and persists machine-readable and human-readable reports.

## Command

Run from the repository root:

```powershell
dotnet run --project LL/tools/LegendsLegacy.Balance -- --seed 1337
```

Supported options:

```text
--seed <number>         Deterministic simulation seed.
--build-count <number>  Random builds per 4/5/6-slot Essence profile (default: 10).
--content-root <path>   API.LL directory containing the production Data folder.
--output <path>         Override the report root.
--full                  Run the currently implemented pipeline.
--help                  Display command help.
```

The default content root is discovered from the repository layout. The default report root is `balance-output/` at the repository root.

## Outputs

Each invocation generates:

```text
balance-output/
├── latest/
│   ├── gear-packages.json
│   ├── essence-builds.json
│   ├── benchmarks.json
│   ├── summary.json
│   └── summary.md
└── history/
    └── <run-id>/
        ├── gear-packages.json
        ├── essence-builds.json
        ├── benchmarks.json
        ├── summary.json
        └── summary.md
```

History directories are immutable. A report records its run ID, UTC timestamp, seed, balance schema version, simulator algorithm version, combat-engine assembly version, Git commit when available, production catalog counts, and combat result. Milestone 2 extended the output with `gear-packages.json`; Milestone 3 added `essence-builds.json`; Milestone 4 added `benchmarks.json` and performance summaries.

Generated reports are ignored by Git.

## Architecture Boundary

The runner loads the production ability, status, summon, and Essence JSON catalogs and executes the existing `AbilityBalanceSimulator`, which uses `FastCombatEngine`. No copied or simplified combat model was introduced.

The `production-essence-smoke-1v1` scenario remains a foundation check rather than a balance benchmark. Gear packages, legal multi-Essence generation, and the first PvE benchmark suite are now implemented. Combat Rating analysis and World Tower calibration belong to later milestones.

## Verification

Automated coverage verifies:

- identical production content and seed produce identical combat results;
- JSON and Markdown reports are written to both `latest` and immutable `history` locations;
- invalid command-line arguments fail explicitly.

An end-to-end smoke invocation also verifies production catalog discovery, combat execution, Git metadata collection, and report rendering.
