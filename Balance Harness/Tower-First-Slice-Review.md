# First Tower harness slice — 9 September 2026

The bounded first Phase 3 slice is complete. Floor 1 runs offline using production Tower preparation, combat and outcome semantics, with independent persisted-snapshot parity tests, frozen inputs/content, saved reports and verified detailed replay. **All 2,157 backend tests passed**, including **20 Tower harness tests**. Phase 2 integration remains deferred.

## Fixed scenario and observed results

The [fixture](../LL/tools/BalanceHarness/Fixtures/tower-floor-1.json) was written before the measurement. It selects the existing **The Waking Step**, guardian **Garran, the Gatekeeper**, with the actual **RequiredSlots = 5**. Each of the five distinct level-10 characters carries a Shortsword, Heavy Breastplate and Amulet at Standard quality, tier 1, rank 0, baseline rolls and no styles. Goblin Warrior + Goblin occupy the two unlocked Essence slots; both are level 1, unascended and unevolved. This is a homogeneous conditional starter-reward party, not a claim about the intended Tower cohort, an optimized role roster or guaranteed chest outcomes.

Party slots 1–5 are ordered and map to PartyNumber 1 through production party rules. Snapshots start with full prepared health, production resource defaults and no Combat Style or additional persistent/temporary bonuses. The floor is uncleared with no contributions, so weapon/ward/weak-point modifiers are zero. Ownership, access and pre-combat rally procedures are assumed; acquisition, waiting and rewards are not simulated.

The fixed 20-seed schedule is `1337, 17, -12345, 0, 918101–918116`. The whole schedule, resolved builds, floor, guardian, rules and settings were saved before the first combat. No samples were appended or selected based on outcomes.

| Measurement | Result |
| --- | --- |
| Planned / valid battles | 20 / 20 |
| Wins / defeats / draws | 0 / 20 / 0 |
| Invalid / cancelled / not run / tick limits | 0 / 0 / 0 / 0 |
| Clear rate, pointwise 95% Wilson interval | 0.00% [0.00%, 16.11%] |
| Defeat duration, mean / median / p90 | 55.475 / 56.1 / 56.1 seconds |
| Guardian health remaining, mean / minimum / maximum | 91.412% / 90.19% / 92.60% |
| Detailed replays | `tower.0001`–`tower.0004`, all matched |

These results describe this fixed party. **No Tower win-rate or duration target is approved.** The starter's 50–90% band does not apply, and no gameplay tuning or baseline promotion followed this run.

## Implementation and parity

- [TowerBattleRunner.cs](../LL/tools/BalanceHarness/TowerBattleRunner.cs) defines the versioned recipe/input/report and validates released floors, RequiredSlots, unique slots/characters, production equipment/Essence legality and frozen-input/rules consistency. Each battle uses a fresh executor.
- [SnapshotCombatantBuilder.cs](../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/SnapshotCombatantBuilder.cs) exposes its existing rehydration body as `BuildFromItemBases`. The normal database-backed builder delegates to it; the offline adapter supplies item bases from the saved catalog. Existing working-tree Combat Style handling is preserved.
- `WorldTowerCombatRuntimeFactory` applies production guardian scaling, stagger, party identity and preparation. Normal trials call `ExecuteTowerPlaybackAsync`, which supplies the normal 6,000-tick cap, 30-tick base attack interval and opening cooldowns. The ordinary encounter result factory supplies outcomes; Tower success requires Victory. Guardian health and rounded display duration follow `WorldTowerService`.
- [TowerBundle.cs](../LL/tools/BalanceHarness/TowerBundle.cs) saves a separate Tower envelope. It captures the current 15 idle content files plus the Tower floor catalog, selected non-secret threat/playback settings, assembly/runtime/platform identity and result hashes. It preserves partial results on cancellation and refuses existing output directories.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs) adds `tower` and dispatches existing `replay --battle` to Tower bundles. Detailed replay uses the simulation executor with equivalent explicit rules, checks saved hashes/build identity, and requires matching preparation, terminal state, statistics/telemetry and Tower interpretation. Missing results cannot count as verified replay.
- [BalanceHarnessTowerTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerTests.cs) builds equivalent characters independently from recipes, persists and reloads their equipment/Essence snapshots through the normal database-backed builder, then executes the normal Tower factory/playback route for seeds 1337, 17, -12345 and 0. Comparisons cover prepared attributes/abilities/equipment, party/stagger metadata, outcomes, duration, terminal teams and statistics. The tests also check repeatability, detailed/compact equivalence, party recipe reordering, illegal recipes, changed inputs/rules, corrupt/missing evidence, output preservation and cancellation.

The shared seam changes no combat formula, content coefficient, dependency direction or database query. Historical idle content allowlists and manifest versions are unchanged. Existing starter fixtures, targets, accepted baselines and retained evidence were not rewritten. Other working-tree changes remain in place.

## Retained evidence and commands

The local evidence root is [TestResults/balance/tower-floor-1-20260909](../TestResults/balance/tower-floor-1-20260909/):

- [Markdown report](../TestResults/balance/tower-floor-1-20260909/run/scorecard.md) and [JSON report](../TestResults/balance/tower-floor-1-20260909/run/scorecard.json).
- `run/tower-input.json`, `run/tower-manifest.json`, `run/content/Data/`, `run/battles/` and `run/tower-results.json` preserve the replay contract and completed trials.
- `executable/` preserves the measured Release executable and dependencies. Identity: **.NET 10.0.12, Windows 10.0.26200, X64**. The manifest stores all five assembly hashes. No runtime archive was created; future replay still requires a matching runtime/platform.
- `replays/tower.0001.json` through `tower.0004.json` contain verified detailed logs. These were the first four predeclared trials, not selected for outcomes.
- `backend-tests.trx`, `backend-tests.log` and [verification.json](../TestResults/balance/tower-floor-1-20260909/verification.json) retain verification results and key artifact checksums. `TestResults/` remains Git-ignored; this source review and the fixture are the small checked-in record.

Commands run from the repository root:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'
./build/run-tests.ps1
dotnet TestResults/balance/tower-floor-1-20260909/executable/BalanceHarness.dll tower --content-root LL/src/API/API.LL --output TestResults/balance/tower-floor-1-20260909/run
dotnet TestResults/balance/tower-floor-1-20260909/executable/BalanceHarness.dll replay --run TestResults/balance/tower-floor-1-20260909/run --battle tower.0001 --detailed
```

The targeted run passed all 19 tests then present; the final full run includes the added recipe-order check, **20 Tower tests and 2,157 total passing tests, zero failures/skips**. The full build completed with 31 warnings outside the added Tower code. Detailed replay was also run for `.0002`, `.0003` and `.0004`. The new output directory was created once; rerunning the measurement command requires another output path. `git diff --check` passed for the modified tracked files.

Initial sandboxed test invocation could not read the user NuGet configuration, and a fallback test-project build with `--no-restore` could not write existing API build outputs. The authorized full repository runner subsequently passed with the needed local access. No required verification remains blocked. Initial test-development errors (a missing namespace and a settings reader that rejected production JSON comments) were corrected before the successful runs.

## Remaining limitations

This increment validates one floor, one homogeneous party and one no-contribution state. It does not certify progression appropriateness, alternative compositions, multiple five-character parties, scouting states, full Tower progression or the difficulty curve. Victory and draw examples for this adapter still need representative coverage; this scenario's measured outcomes are defeats. Independent parity exercises preparation/combat/outcome semantics, not the full rally HTTP/background-worker/persistence/reward lifecycle.

Tower baseline acceptance, paired comparison, goal evaluation, hosted integration, performance benchmarking and optimization remain separate work. Phase 2 integration, starter retuning/archive repackaging and the boss/dungeon/PvP adapters stay deferred. No migration, production configuration change, deployment, shared-database operation or external-environment action was performed.
