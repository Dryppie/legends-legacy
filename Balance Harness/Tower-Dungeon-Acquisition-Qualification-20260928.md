# Tower dungeon acquisition qualification — 2026-09-28

The fixed full-run diagnostic supports farming later supply bands with previously earned equipment. It does **not** establish a route from starter rewards to the first Rare set, or real-player acquisition time. No guardian, gear curve, search algorithm, supply cadence or economy setting changed.

## What ran

[DungeonAcquisitionRunner.cs](../LL/tools/BalanceHarness/DungeonAcquisitionRunner.cs) composes the production dungeon catalog, `DungeonRunFactory`, `DungeonRunService`, route and Vigor services, dungeon planner/orchestrator, combat preparation, resolution session and `CombatEngineExecutor.ExecuteAsync`. It tests whole routes, including failed attempts and the terminal completion callback. This differs from the existing isolated-boss harness. Production initial ability cooldowns and the 6,000-tick encounter limit apply.

The only orchestration substitutions are deterministic encounter/participant identities, reserved seeds, fixed combat timestamps and a non-expiring offline session. The simulation factory starts the run; the normal API start/entry transaction is not executed. Every attempt is charged one matching sigil in the diagnostic ledger after checking the production entry-cost definition. This assumes supplied starting stock; no real inventory is accessed or debited.

File-only repository adapters fail on unexpected calls. Combat outcomes reach the run service unchanged. Ordinary loot, reward persistence, guild contributions and mastery awards are excluded from progression feedback; the terminal callback is counted rather than granting a chest. Actual supply issuance/opening has separate production service tests. Mastery is fixed at zero for every attempt, and no item, level or Essence improves between rooms or attempts. Production reconstructs the entry snapshot for each room; Vigor persists. The harness follows that rule rather than inventing persistent health loss between rooms.

The fixed route policy takes a Rest Site first, then the lowest **visible** maximum Vigor forecast, minimum forecast and room index. It excludes Treasury choices and never reads a future combat result. Forecast widening and actual health-dependent combat tolls remain production behavior.

[DungeonAcquisitionStudy.cs](../LL/tools/BalanceHarness/DungeonAcquisitionStudy.cs) declares 26 loadouts, both accessible region-1 grade-I sources (`goblin_mines`, `forgotten_catacombs`) and eight layouts per source:

- Six conservative bootstrap controls: the three First Hunt Essence choices, each with mace or wand, at an assumed level 30. One Common tier-1 rank-0 weapon, one level-1 Essence, no armor. These are **not** claimed to represent typical level-30 inventories.
- Four already-geared controls: the authored guardian, restorer, striker and controller at floor 1 with a full Rare set and four supplied Essences. These controls cannot prove how that set was obtained.
- Those four roles before floors 4, 7, 10 and 11: current declared level/ordered Essences, with exact personally owned equipment from floors 3, 6, 9 and 10 respectively. Before floor 10 this means **tier-1 Unique rank-4 gear**, not the tier-2 Legendary reward being sought. Before floor 11 the exact tier-2 Legendary items persist.

The owned equipment comes from the pinned [acquisition report](../TestResults/tower-acquisition-20260928/report-v2.json), SHA-256 `6df2d6196bf51a024860d34e00a5cb4b347398de3378a8dbcd2222a2a3e63dc5`. Character leveling, Essence acquisition and training remain external assumptions. No Tower-party victory is treated as multiple characters' dungeon completions.

## Observed results

There were **416 diagnostic attempts**, plus **52 full-run qualification replays**, totaling **3,485 room combats**. Every replay matched complete normalized run output, including preparation and combat summaries. The independent audit reconstructed gear carryover, route choices, Vigor changes, terminal outcomes, sigil accounting and combat-time totals. It checked **1,823 input hashes** without running additional fights.

The counts below combine four separately reported role cells for compact presentation. They are not population estimates. Each role/source has only eight layouts. The two early geared rows are deliberately identical loadouts with identical identities and seeds; they provide no additional independent evidence when pooled together.

| Entry loadout | Goblin Mines successes | Catacombs successes | Range of role means, successful simulated combat minutes: Mines / Catacombs |
| --- | ---: | ---: | --- |
| Starter controls, assumed level 30 | 0/48 | 0/48 | No successful runs |
| Already owns floor-1 Rare set | 32/32 | 30/32 | 2.56–14.56 / 3.85–18.04 |
| Before floor 4, retains Rare set | 32/32 | 30/32 | 2.56–14.56 / 3.85–18.04 |
| Before floor 7, retains Epic set | 32/32 | 32/32 | 1.84–8.68 / 3.01–12.63 |
| Before floor 10, retains tier-1 Unique set | 32/32 | 32/32 | 1.41–5.65 / 2.03–8.65 |
| Before floor 11, retains tier-2 Legendary set | 32/32 | 32/32 | 1.01–2.20 / 1.45–3.33 |

All 96 starter attempts failed in ordinary combat. Mean simulated combat time to failure ranged from 32.85 to 42.91 seconds by cell. The early geared striker and controller each failed one Catacombs layout through **Vigor attrition after a won fight**, before the boss. These two failures repeat in the identical pre-floor-4 controls, producing four recorded geared failures. An isolated boss test would have missed this mechanism.

The totals are 316 successful and 100 failed diagnostic attempts; qualification replays are excluded from those totals. These totals should not become an aggregate player success rate: loadouts, levels, roles and reused controls differ.

All durations above are **engine combat seconds**, summed across a run. They omit player input, navigation, animations, breaks, resource gathering, queues and offline time. There are **zero measured player samples**. Eight successes cannot justify setting future success probability to 100%, and zero successes cannot prove no viable bootstrap build exists. The output intentionally leaves seven-item expected attempts unset. The earlier conditional 84 random-sigil idle hours for a seven-item set remains a source scenario, not a forecast made more certain by these fights.

## Bounded workflow and evidence

The [owner](analysis/qualify-dungeon-acquisition.py) freezes inputs before execution and uses the existing `build/bounded_windows_process.py` suspended Windows Job owner. It runs the opt-in test through the required backend wrapper, with a 900-second process deadline, 840-second native cancellation, 30,000-fight cap, 64-action cap per run, 256 MiB output cap and 1 MiB process-log cap. No tuning, adaptive extension, combat retry or search was performed. Actual study output was 185,640,837 bytes; the test execution took about nine seconds, separate from setup and input hashing.

The prior retained union contained 835,319 values. The owner reserved **1,040 fresh values**: 16 layout seeds and 64 room seeds for each layout. Panels are intentionally shared across loadouts. There were 165 distinct consumed layout/combat values and **875 unconsumed reservations**, all retained. The exclusion union is now **836,359**. Future allocation must include [this reservation ledger](../TestResults/tower-dungeon-owner-20260928/seed-ledger.json); unconsumed seeds are not available for recycling.

Evidence:

- [Declaration and bounds](../TestResults/tower-dungeon-owner-20260928/declaration.json), [frozen request](../TestResults/tower-dungeon-owner-20260928/request.json), [process receipt](../TestResults/tower-dungeon-owner-20260928/process.json).
- [Results and per-cell summaries](../TestResults/tower-dungeon-study-20260928/result.json); SHA-256 `c5cd72e4078ba8343d355c4b461c015055b2249522c6f0087771034d73f5a109`.
- [Output manifest](../TestResults/tower-dungeon-study-20260928/files.json); trusted pin `1a0acf0e7efc89a6af2bcb200db3d0446a695e134e8a3d2e3427eebf3ee503b1`.
- [Passing independent audit](../TestResults/tower-dungeon-owner-20260928/independent-audit.json), produced by the [independent verifier](analysis/verify-dungeon-acquisition.py).
- [Study test log](../TestResults/tower-dungeon-owner-20260928/study.log) and [preserved study TRX](../TestResults/tower-dungeon-owner-20260928/study-tests.trx).

These ignored local archives are retained, not committed. Historical Tower archives and manifest pins are unchanged. The search remains `affinity-creation-with-benchmark-validation-v1`; this fixed diagnostic did not invoke or replace it.

After the completed study and passing audit, concurrent work changed broadly captured Core/Services administration sources. A later live-tree re-audit correctly refused those changed hashes; see [the first drift observation](../TestResults/tower-dungeon-owner-20260928/later-source-drift.json). Seven original source files were subsequently recovered from Git only after their exact bytes matched the producing hashes, and retained in [historical-sources.json](../TestResults/tower-dungeon-owner-20260928/historical-sources.json). No working-tree file was restored or overwritten. The original audit describes the successfully verified producing state, not later edits. Frozen dungeon/harness code, content, original runtime assemblies and output remain retained. No receipt was repinned. The final regression build is separately identified below; its assemblies have different hashes and are not silently substituted for the qualified runtime.

## Verification and changed files

Added the runner, study, owner/verifier scripts, [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessDungeonAcquisitionTests.cs) and this report. Updated the continuation handoff, acquisition report and supply implementation report with these findings. The only existing test correction replaces hard-coded ancestor traversal with `TestContentPaths.FindApiRoot()` in [DungeonRunFactoryLayoutTests.cs](../LL/tests/EssenceSystem.Tests/DungeonRunFactoryLayoutTests.cs), allowing the repository-supported external artifact directory.

The initial sandbox build could not read the local NuGet configuration; the same required wrapper succeeded with escalation. Initial focused verification passed 13 tests with the study opt-in skipped. The bounded study passed both selected tests. A broader run passed 359 tests but exposed the existing content-path failure; after the helper correction, **360 passed**, with one intentional opt-in skip. [Final log](../TestResults/tower-dungeon-final-verification-20260928.log), [preserved regression TRX](../TestResults/tower-dungeon-owner-20260928/regression-tests.trx). No backend verification command remains blocked. No frontend code changed in this continuation.

Commands (PowerShell, repository root; use fresh output paths for any new execution):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-dungeon-build-20260928 -Filter 'FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~TowerEquipmentSupplyTests'

$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/qualify-dungeon-acquisition.py' --owner TestResults/tower-dungeon-owner-20260928 --output TestResults/tower-dungeon-study-20260928 --artifacts TestResults/tower-dungeon-build-20260928
& $python 'Balance Harness/analysis/verify-dungeon-acquisition.py' --owner TestResults/tower-dungeon-owner-20260928 --manifest-pin 1a0acf0e7efc89a6af2bcb200db3d0446a695e134e8a3d2e3427eebf3ee503b1 --receipt TestResults/tower-dungeon-owner-20260928/independent-audit.json

./build/run-tests.ps1 -ArtifactsPath TestResults/tower-dungeon-verified-build-20260928 -Filter 'FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon'
```

There are no new dependencies, migrations, application configuration changes or deployment requirements for this harness continuation. No API startup, shared database access, seeding or deployment occurred. The earlier supply feature's future rollout requirements remain separate. All concurrent LiveOps/analytics/support-case work is preserved. Guardian data still hashes to `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`.

## Next decision

Validate a non-circular route to the **first** supply set: use actually reachable level-30 inventories, including quest armor outcomes, ordinary drops/reinforcement and earned/trained Essences. Compare lower pre-supply gear at the same Essence budget to separate missing equipment from missing Essences. The starter-only versus full-Rare comparison here changes both and cannot attribute the gap to one cause.

Then combine observed player activity, failures, sigil stock and source costs with the acquisition ledger. Until that evidence or an explicit desired farming budget exists, do not select a new chest cadence, lower bosses or present simulated combat minutes as calendar acquisition time. The floor-10 tier transition and stronger floor-11 carryover are viable in these fixed combat inputs; the first-set route and complete resource/time economy remain the main unresolved progression questions.

## Subsequent first-supply diagnostic

The [matched first-supply study](Tower-First-Supply-Progression-20260928.md) has now tested pre-dungeon quest-token recipes at the same four-Essence budget across sparse, Common, Uncommon, reinforced Common and already-owned Rare gear. Full Common sets earned the first Rare set in **16/16 Goblin Mines paths**, each taking seven attempts; sparse quest armor plus an ordinary weapon completed **0/16**. Level-10 Essences without ascension produced identical outcomes to level 1. This resolves a conditional combat route, while useful pre-supply gear acquisition and joint gear/sigil activity remain unresolved pacing questions.

The later study retains 2,374 attempts, 18,472 room combats, 80 matching replays and an independent frozen-input audit; 364 regression tests passed. It does not amend or repin this study's runtime, recipes, outcomes or manifests. Exact quest-box outcomes and ordinary gear remain supplied conditions, not typical inventories. No boss, supply cadence, supported search or repeating gear curve changed. The latest handoff records the new **842,599-value** exclusion union and all retained reservations.
