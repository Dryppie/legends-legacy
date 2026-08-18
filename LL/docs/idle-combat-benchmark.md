# Reproducible idle-combat benchmark

This benchmark measures the real authenticated `CharacterActions/Resolve` request while keeping the database state, server clock, encounter seeds, and background workload identical across runs.

## Golden snapshot

The benchmark uses the local, Git-ignored file `LL/legends_legacy_idle_benchmark.sql`. Despite the extension, this file is a PostgreSQL custom-format archive (`PGDMP`), not plain SQL. Keep it local because a complete development database archive can contain accounts, tokens, and other sensitive data.

The archive's original database name is ignored. The automation restores it only into the dedicated local database:

`legends_legacy_idle_benchmark`

The script refuses every other database name and every non-loopback database host. It drops and recreates the dedicated benchmark database before each run, so nothing in that database should be treated as persistent.

## Fixed workload

Benchmark mode uses the fixed instant `2026-08-18T12:00:00Z` by default. After each snapshot restore, the admin character's action boundary is set to exactly 24 hours before that instant.

The API then:

- Replaces the normal `TimeProvider` with a fixed provider.
- Disables outbox, tower, tournament, calibration, backfill, and development progression workers.
- Uses the isolated benchmark database.
- Resolves exactly 8,641 due actions through the normal authenticated API endpoint.

The fixed boundary is important because idle encounter IDs, spawning, and combat random seeds include the encounter timestamp.

## Prerequisites

- PostgreSQL 17 command-line tools under `C:\Program Files\PostgreSQL\17\bin`.
- `dotnet-counters` in the local NuGet package cache.
- The benchmark snapshot above.
- PowerShell 7 or newer.

Set local credentials for the current shell. They are passed to child processes but are not written to the report:

```powershell
$env:LL_BENCH_DB_PASSWORD = '<local postgres password>'
$env:LL_BENCH_ADMIN_PASSWORD = '<local seeded admin password>'
```

## Run the benchmark

From the repository root:

```powershell
./build/measure-idle-combat.ps1
```

The default run performs three complete restore/start/resolve cycles and reports the median. To validate the harness with one cycle:

```powershell
./build/measure-idle-combat.ps1 -Runs 1
```

Use `-Configuration Release` when a Release comparison is wanted. Never compare a Debug sample with a Release sample.

## Capture diagnostic profiles

The same guarded workflow can capture one CPU or verbose allocation trace instead of runtime counters. Always use one restored run and the accepted correctness fingerprint:

```powershell
./build/measure-idle-combat.ps1 `
    -Runs 1 `
    -Diagnostics Cpu `
    -ExpectedFingerprint 'a6c348f6d81ebb54092d776d88bf0e34ac9d3b13ce2712fc35ce04aff0ec918f'

./build/measure-idle-combat.ps1 `
    -Runs 1 `
    -Diagnostics Allocation `
    -ExpectedFingerprint 'a6c348f6d81ebb54092d776d88bf0e34ac9d3b13ce2712fc35ce04aff0ec918f'
```

CPU mode uses the `dotnet-sampled-thread-time` and `dotnet-common` profiles. Allocation mode combines sampled thread time with `gc-verbose` and uses a larger trace buffer. Both modes still restore the snapshot, fix time, issue the real authenticated request, record normalized state, and enforce the expected fingerprint.

Profiling adds overhead. Use these traces to rank call stacks and allocation types, not as latency baselines. Use the default `Counters` mode for before/after duration and allocation measurements.

## Results

Each invocation creates a timestamped directory under:

`TestResults/idle-combat-benchmark/`

It contains:

- One raw counter JSON file per run.
- Or one `.nettrace` file per run when `-Diagnostics Cpu` or `Allocation` is selected.
- Normalized response and persisted gameplay-state JSON for every run.
- `summary.json` with every run and the median.

The captured metrics include HTTP and server duration, simulation duration, simulation and request-window allocation, CPU time, GC pauses and collections, and working-set bounds.

## Correctness fingerprint

Every run also computes one SHA-256 correctness fingerprint from two independently recorded views:

- The normalized `Resolve` response, excluding generated row/item identities plus transport-only revision, acquisition/audit timestamps, and row-version fields.
- Persisted gameplay state for the benchmark character, including character progression and currencies, the action, inventory and generated item attributes, professions, creature archive, achievements, quests, loot history, and economy ledger entries.

Generated database identifiers and audit-only timestamps are removed from the persisted view, while reward quantities, item definitions and rolls, combat results, progression, and schedule state remain. Array values are sorted where database row order has no gameplay meaning. The harness fails immediately if any run produces a different fingerprint and leaves both normalized artifacts in the result directory for comparison.

To turn a known-good result into an explicit before/after gate, pass the fingerprint recorded in its `summary.json`:

```powershell
./build/measure-idle-combat.ps1 `
    -ExpectedFingerprint '<64-character SHA-256 fingerprint>'
```

This detects a consistently different result after an optimization, while the automatic cross-run check detects nondeterministic behavior within one benchmark invocation. A changed snapshot intentionally requires a new reviewed fingerprint.

The current known-good fingerprint for the local snapshot and fixed clock documented here is:

`a6c348f6d81ebb54092d776d88bf0e34ac9d3b13ce2712fc35ce04aff0ec918f`

Use that value as the expected fingerprint until the snapshot or intended gameplay output changes.

Only compare reports when all of these match:

- Snapshot file.
- Fixed clock and boundary.
- Build configuration.
- Encounter and batch counts.
- Run count and warm/cold-start policy.
- Correctness fingerprint, unless the fixture was intentionally changed and reviewed.

The current harness intentionally measures a cold API process for every run. This keeps catalog compilation and JIT policy consistent between builds.

## Updating the snapshot

Create a new custom-format archive only when the intended fixture or schema changes. After replacing it:

1. Run the harness once.
2. Confirm the admin action exists exactly once.
3. Confirm 8,641 actions are processed and no work remains.
4. Review the normalized response and database-state artifacts and accept the new correctness fingerprint.
5. Run the required backend correctness suite.
6. Record why the snapshot changed in the optimization plan.

Do not use a production database dump for this fixture.
