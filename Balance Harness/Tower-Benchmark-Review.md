# Automatic Tower benchmarks — 9 September 2026

The harness now supports **generate legal profiles → run selected floors → compare matching saved measurements → save reports and verified replay** through `tower-benchmark`. This is the command workflow intended to back a future button. It regenerates profiles from current balance definitions; equipment/Essence coefficient changes do not require manual stat edits.

## Fixed catalog and scope

[tower-benchmark.json](../LL/tools/BalanceHarness/Fixtures/tower-benchmark.json) declares five profile recipes and two party cells:

- **Starter:** five distinct copies of the conditional level-10 Shortsword / Heavy Breastplate / Amulet build with Goblin Warrior + Goblin. Three Standard tier-1 rank-0 items, baseline rolls, no styles; two level-1, unascended Essences.
- **Mixed:** level-20 Guardian, Restorer, Striker, Striker, Controller. Each has seven Standard tier-1 rank-0 items filling all eight slots through a two-handed weapon and three level-1 unascended Essences. Equipment and Essence choices follow existing canonical role suggestions but are pinned explicitly in the catalog. Role names are benchmark conventions. Gear/Essence ownership and progression access are conditional assumptions, not acquired-cost estimates or approved floor-audience budgets.

The selected floors are **1 / Garran**, **3 / Morrowmaw**, and **5 / Kharad**, covering the initial guardian, adds and the first ten-slot encounter. RequiredSlots is read from saved production content. The ordered five-character cell repeats for larger floors, assigning distinct IDs and production PartyNumber values. Every fight starts at full prepared health with no contributions or styles and normal Tower cooldown/tick rules.

The fixed reference protocol uses master seed **1337**, **20 trials per cell**, six cells and **120 battles**. Seeds derive from the versioned schedule domain, master seed, floor and trial index; parties on the same floor share seeds. Reordering the catalog preserves generated scenarios. All scenarios and settings are frozen before the first fight. No outcome-dependent selection or automatic tuning occurs.

## Measured results

Both complete runs have 120 valid trials: 120 defeats, no wins, draws, timeouts, invalid or cancelled trials. Each cell's clear estimate is **0/20, 95% Wilson interval 0–16.11%**. Survival of original party members is 0% at termination.

| Cell | Mean seconds | Mean guardian health remaining |
| --- | ---: | ---: |
| Floor 1 mixed | 137.55 | 52.35% |
| Floor 1 starter | 53.97 | 91.52% |
| Floor 3 mixed | 109.12 | 80.25% |
| Floor 3 starter | 42.28 | 99.02% |
| Floor 5 mixed | 80.59 | 85.99% |
| Floor 5 starter | 41.30 | 95.96% |

The repeat compared **120 paired trials with zero changed preparation/gameplay records**; win, survival, duration and guardian-health deltas were zero in all six cells. These repeated identities are not additional independent samples. A detailed replay of the first trial in each cell matched the saved result, for **six verified replays**.

Results are descriptive. **No Tower target or baseline was approved, and the starter 50–90% band does not apply.** The chosen fixed budgets do not establish what players should bring to these floors.

## Commands and evidence

```powershell
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --output TestResults/balance/tower-reference-new
dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release -- tower-benchmark --output TestResults/balance/tower-candidate-new --reference TestResults/balance/tower-reference-new
```

Use `--samples 1` for a six-battle smoke; matching comparisons require matching sample counts and seeds. `--catalog`, `--seed` and `--content-root` select other declared inputs. `tower-compare` compares two already saved benchmarks. `replay --run <benchmark> --battle floor-5.mixed/tower.0001 --detailed` resolves the nested verified trial. The README documents all options and constraints.

Local evidence is retained in [TestResults/balance/tower-benchmark-20260909](../TestResults/balance/tower-benchmark-20260909/):

- [Reference Markdown](../TestResults/balance/tower-benchmark-20260909/reference/benchmark.md) and [JSON](../TestResults/balance/tower-benchmark-20260909/reference/benchmark.json), captured content/inputs and individual Tower bundles under `reference/cells/`.
- [Repeat comparison Markdown](../TestResults/balance/tower-benchmark-20260909/repeat/comparison/comparison.md) and [JSON](../TestResults/balance/tower-benchmark-20260909/repeat/comparison/comparison.json), with complete repeat evidence under `repeat/`.
- `executable/` retains the measured Release build and dependencies; replay still requires its matching runtime/platform. The manifests fingerprint execution identity. No runtime archive was created.
- `replays/` contains the first detailed replay from each cell. `reference.log`, `repeat.log`, backend logs/TRX and `verification.json` retain checks and checksums. These artifacts remain Git-ignored; the catalog and this review are the source record.

The measurement used the retained executable with explicit `--content-root LL/src/API/API.LL`; its repeat used `--reference` against the newly saved reference. All six replays used the retained executable. Neither the original first-Tower evidence nor the accepted starter evidence was overwritten.

## Implementation and verification

- [TowerBenchmark.cs](../LL/tools/BalanceHarness/TowerBenchmark.cs) expands bounded data-driven profiles, validates all generated scenarios before execution, captures one source-content snapshot and invokes existing Tower bundles for every cell. Reports include completeness, clear uncertainty, survival, duration and guardian health. Cancellation preserves completed/partial cells.
- [TowerBenchmarkComparison.cs](../LL/tools/BalanceHarness/TowerBenchmarkComparison.cs) verifies saved evidence and compares matching recipes and seeds. Changed recipes/schedules/rules or missing cells are incompatible; incomplete runs cannot serve as complete references. Content, settings, resolved-input, assembly and platform changes are disclosed. Deltas are candidate minus reference, with paired clear uncertainty and paired mean intervals only when supported; shared-win duration is reported separately.
- [TowerBundle.cs](../LL/tools/BalanceHarness/TowerBundle.cs) adds reusable settings/content capture and a verified historical evidence reader. Historical reads do not demand executing the old assemblies or regenerating old stats; replay retains its strict build check. The existing single-floor bundle schema remains unchanged.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs) adds `tower-benchmark`, `tower-compare` and benchmark-aware replay. Invalid/incompatible comparisons return nonzero exit status while preserving the candidate measurement/report. There is no automatic reference selection or baseline promotion.
- [Benchmark tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBenchmarkTests.cs) cover stable generation, RequiredSlots, legal multi-party identities, budgets/invalid catalogs, end-to-end comparison/replay, reference/output preservation, incompatible seeds/recipes, corrupted trials/reports and partial cancellation. Tests alter equipment budget and Goblin Warrior damage only in temporary content; both change fresh gameplay while generated recipes remain identical.
- [Tower parity tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerTests.cs) now independently persist/reload and execute the mixed party against all three floors through the normal production preparation/playback path, including ten-character slot/stagger behavior. Preparation, terminal state, statistics, outcomes and detailed/compact replay match.

Verification commands:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'
./build/run-tests.ps1
```

**All 33 Tower tests and all 2,234 backend tests passed; zero failed or skipped tests.** The final build had five warnings in other existing tests and no errors. The initial temporary-settings test fixture used incorrect property casing; it was corrected before successful targeted/full runs. The repository runner used authorized access to local NuGet/build outputs, as required by this checkout's sandbox restrictions. No required command remains blocked. Diff whitespace checks passed.

Remaining scope: a UI button, generated build search/optimization, broader progression and acquisition budgets, scouting variants, representative victory/draw cases, full Tower progression, performance benchmarks, Tower target approval and hosted integration. Phase 2 integration remains deferred. No production content, service behavior, configuration, migrations, databases or deployed environments were changed by this increment; unrelated working-tree edits remain intact.
