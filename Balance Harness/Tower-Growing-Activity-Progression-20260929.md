# World Tower: earned progression in acquisition — 29 September 2026

The fresh paired comparison completes all **32/32 seven-supply histories in both arms**, with no change in their coarse idle-activity completion checkpoints. Applying earned character/Essence XP and persistent dungeon mastery changes the inputs, ordinary rewards and combat duration, but does not demonstrate earlier player acquisition. All 32 grown paths use fewer engine combat seconds than their matched fixed-state paths. **Zero player-time samples were measured.** No Tower boss, gear band, supply cadence or search algorithm was changed.

This follows the [seed-free growth qualification](Tower-Earned-Growth-Qualification-20260929.md). It integrates dungeon XP claiming, mastery, supported prophecy events and an explicit clock rather than transferring old combat outcomes onto stronger characters. The earlier [native-offer comparison](Tower-Native-Prophecy-Progression-20260929.md) remains historical; it is not the baseline for this study.

## Results and limits

| Progression arm | Assumed idle outcome | Seven-item histories | Paid attempts | Successful runs | Failures | Completion at 72 / 240 idle cadence hours |
| --- | --- | ---: | ---: | ---: | --- | ---: |
| Fixed level/Essence/mastery benefits | Perfect | 16/16 | 112 | 112 | None | 8 / 8 |
| Earned progression | Perfect | 16/16 | 112 | 112 | None | 8 / 8 |
| Fixed level/Essence/mastery benefits | Four in five | 16/16 | 113 | 112 | 1 Catacombs attrition | 6 / 10 |
| Earned progression | Four in five | 16/16 | 113 | 112 | 1 Catacombs attrition | 6 / 10 |

The attrition failure occurs in the controller path-3 four-in-five history in each arm, after won room combats. Each loses **10,000 pending dungeon XP**, spends its sigil, receives no completion supply and still earns 55 mastery XP. That failure remains visible; no boss or Vigor rule was adjusted. There are no combat-readiness failures in this panel. These are descriptive results from four paths per recipe, not estimated population success rates. The two idle scenarios share sixteen personal identities and the policy arms share panels; they are not 64 independent player samples.

The 32 grown final characters reach:

| Character level | Histories | Final four Essence levels |
| ---: | ---: | --- |
| 35 | 6 | 4, 4, 4, 4 |
| 36 | 8 | 5, 5, 5, 5 |
| 42 | 8 | 10, 10, 10, 10 |
| 43 | 2 | 10, 10, 10, 10 |
| 44 | 4 | 10, 10, 10, 10 |
| 45 | 4 | 10, 10, 10, 10 |

Grown histories apply **35,957,314 idle XP, 1,254,331 prophecy character XP and 2,323,000 claimed dungeon XP** across their stopped ledgers. Unascended Essences cap at ten; no ascension, extra Essence ownership or new slot filling is granted. The fixed comparison withholds native XP and entry mastery benefits explicitly. Its successful dungeon claims record 2,335,000 withheld XP; this is a diagnostic counterfactual, not a legal alternative leveling rule for players.

There are **38 grown Mines entries at mastery 1**, with the other 412 acquisition entries at effective mastery zero. Native mastery 1 adds visibility and five percentage points to completion-equipment drop chance. It does not yet reduce combat Vigor or improve rest recovery. Actual native room/mastery/claim receipts are retained per attempt, including duplicate-claim and duplicate-mastery rejection. Entry snapshots stay fixed for the entire run, even when a prophecy claim changes the underlying character during it.

Grown arms retain 101 ordinary dungeon items versus 93 in the fixed arms. Both retain every owned item, including replaced equipment. Across all histories: **448 selected supply items, 194 ordinary dungeon items, 146 unspent blueprint items and 4,900 owned equipment instances**, including initial and ordinary idle gear. Prophecy assembly adds 55 Mines sigils in the grown arms and 40 in the fixed arms. The 442 claims comprise 322 kill-objective claims, 14 earned Essence-XP claims and 106 favor milestones. These extra resources did not move a completion to an earlier declared checkpoint.

Across the 32 matched histories, mean engine combat time is **44.71 minutes fixed versus 41.91 minutes grown**. This includes failed acquisition fights and excludes controls/replays. It is not total acquisition time. Fourteen histories per arm finish at the 72-hour idle checkpoint and eighteen at 240 hours. The serial scenario adds engine duration to those cadence budgets, yielding roughly 72.4–241.0 scenario hours. It omits attendance, input, travel, queueing, navigation, ordinary idle combat measurement and real interruptions; do not present it as a player calendar forecast or an approved farming target.

## Reproducible model

The [new fixture](../LL/tools/BalanceHarness/Fixtures/tower-growing-activity.json) freezes two policies, `fixed-progression` and `earned-progression`. Both keep full-slot readiness, Mines-first/either-family spending, the deterministic materialized-stat-budget equipment selection, personally retained ordinary dungeon gear, native daily offers and funded whole-sigil assembly at the four existing idle checkpoints. Four recipes × four historical owner/Armor Chest identities × two idle outcomes × two policies produce 64 histories. The historical idle reward identity clock remains the same deterministic random stream for pairing; it is not used as the prophecy calendar.

The declared serial clock starts Monday 28 September 2026 at 00:00 UTC. An idle award is processed at the start of each ten-second cadence; the full cadence then elapses. Dungeon rooms add their actual engine ticks before publishing room combat events. No idle farming occurs simultaneously. Offers are observed before idle encounters and dungeon actions, with immediate available claims; crossing UTC midnight inside a room does not retroactively accept a new daily offer. Time to accept, claim, choose a route or open a supply is assumed zero. Starting at level 30, three supplied level-1 quest Essences, zero within-level XP, one creature per idle victory, no bonuses and no defeat XP retention are explicit conditions. The fourth Essence starts training only after the retained quest gate. Idle creature-type diversity is not known from the original reward stream and earns no unique-type objective credit; actual dungeon creature identities are preserved in its progress events.

Production `DungeonCombatRewardFactBuilder`, `DungeonCombatRewardCalculator`, `DungeonPendingRewardWriter`, `DungeonRunRewardClaimer`, character XP writer, leveling, Essence progression and `DungeonMasteryService` perform their native work with personal in-memory repositories. The real run service clears pending XP on failure. Successful XP claims train only activity-attuned Essences; prophecy character XP does not train them. Completion-table XP is guarded to remain absent for the two scoped grade-I definitions. Entry mastery feeds native route/Vigor decisions and completion equipment chances. Currency rewards are recorded at the combat boundary but are not made available to excluded shops; cores, creature consumables, resonance drops, completion treasure, later quests, caches, crafting and ascension have no spendable credit in this scenario.

The existing loot adapter had recognized miniboss rewards only for an explicit `fight` action. Production `choose_route` also resolves combat. The new study path accepts that resolved action; the legacy mode is retained for historical reproduction. **This panel contains zero miniboss rolls**, so the correction does not explain this comparison's item or completion differences. Mastery affects ordinary completion-equipment chance; it does not modify the native miniboss drop chance.

The repeating ten-floor gear curve and `affinity-creation-with-benchmark-validation-v1` remain unchanged. Selection never deletes or downgrades the owned inventory, and the existing floor-10→11 carrying tests remain in the passing regression. This study concerns the first seven supply awards, not a proof that seven purchases are necessary to enter Tower, a growing full-party Tower win rate, or an earned level-50/60 journey through floors 10 and 11.

## Verification and frozen evidence

**504 relevant backend tests passed**, with twelve intentional study opt-in skips. The separately owned bounded study passed one test. No frontend source was edited, so frontend tests were not repeated. The initial sandbox build could not read the user's NuGet configuration; the same required repository wrapper succeeded with escalation. No required command remains blocked.

The study executed **450 acquisition attempts, 64 supplied Rare controls, 115 matching complete-run replays and 5,705 room combats**. Controls completed 60/64; they are diagnostic already-owned loadouts and cannot establish acquisition. Another 228 idle reward windows reproduced exactly under different batch boundaries. Final independent auditing checked **1,970 input hashes, all 64 historical loot prefixes, 678 entry decisions, 450 loot ledgers, 896 loot retry checks, 4,006 production-prepared diagnostic rooms, all 64 final preparations, native XP/source/mastery histories and every reserved combat seed**. No combat was executed by the audit. There are no identical fixed/grown combat inputs to transfer across arms.

The first frozen auditor stopped at a failed-run flag, not an XP or combat mismatch. Production marks `MaxLevelRewardPreviouslyClaimed` true on non-completion receipts to suppress the completion-only mastery-cap reward, even when actual mastery is below the cap. The auditor had incorrectly expected false. The original failed log, original frozen verifier and all runtime/results remain intact. A separately pinned [auditor-only amendment](../TestResults/tower-growing-activity-owner-20260929/verifier-amendment-v1.json) corrects that single expectation, and the [amended audit](../TestResults/tower-growing-activity-owner-20260929/independent-audit.json) passes. Amended helper SHA-256: **`ada9310a5b3d7d0ca19ec0262fbc7a15b33a0bb0b6bfd6233e9095bcd899c8fb`**. No rerun, sample extension or game/runtime amendment occurred. The updated optional audit helpers also revalidated the historical native-offer archive without new fights; see [legacy compatibility receipt](../TestResults/tower-growing-activity-owner-20260929/legacy-audit-compatibility.json).

Output: `TestResults/tower-growing-activity-study-20260929`. [Manifest](../TestResults/tower-growing-activity-study-20260929/files.json) SHA-256: **`f8f05eb4cf6d1e9c28a87b72bfeb0f1a9ff9f1229c5ad687c0413b042d7fbdc5`**. [Result](../TestResults/tower-growing-activity-study-20260929/result.json): **`d480c355f41fadc900eeeff402d9750757545f8932f0e2daa0972564713be893`**. [Derived per-history summary](../TestResults/tower-growing-activity-owner-20260929/derived-summary.json).

Owner: `TestResults/tower-growing-activity-owner-20260929`, retaining frozen source/runtime, [request](../TestResults/tower-growing-activity-owner-20260929/request.json), [declaration](../TestResults/tower-growing-activity-owner-20260929/declaration.json), [process receipt](../TestResults/tower-growing-activity-owner-20260929/process.json), [study TRX](../TestResults/tower-growing-activity-owner-20260929/study-tests.trx), [regression TRX](../TestResults/tower-growing-activity-build-20260929/regression-tests.trx) and [final checks](../TestResults/tower-growing-activity-owner-20260929/final-checks.json). Owned process: **48.125 seconds, exit zero, no timeout, zero active children**. Frozen bounds: 900 process seconds, 840 native seconds, 64,000 room fights, 256 MiB output and 1 MiB log; output totals 99,731,888 bytes. `TestResults` is ignored and local; preserve these directories, including incomplete/failed audit evidence.

The [seed ledger](../TestResults/tower-growing-activity-owner-20260929/seed-ledger.json) is SHA-256 **`3d16dfa5acedb1f3c17a01fd75aa0b480eb907c72e7231267b4c60d7da868aff`**. All 6,240 reservations remain excluded, including **5,824 unconsumed values**, for a union of **876,935**. All older used and unused reservations remain excluded. Sixteen historical owner/Armor Chest seeds are deliberately paired identities, not new combat samples.

Commands executed (completed output directories must not be reused):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-growing-activity-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessGrowingActivityTests|FullyQualifiedName~BalanceHarnessGrowthTests'
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-growing-activity-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessGrowingActivityTests|FullyQualifiedName~BalanceHarnessGrowthTests|FullyQualifiedName~BalanceHarnessProphecyOfferTests|FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~BalanceHarnessDungeonLootTests|FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon|FullyQualifiedName~Prophecy|FullyQualifiedName~SigilFragment|FullyQualifiedName~CharacterExperienceProgressionTests|FullyQualifiedName~EssenceProgressionServiceTests'
$env:PYTHONDONTWRITEBYTECODE='1'
$python='C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python 'Balance Harness/analysis/run-tower-growing-activity.py' --owner TestResults/tower-growing-activity-owner-20260929 --output TestResults/tower-growing-activity-study-20260929 --artifacts TestResults/tower-growing-activity-build-20260929 --projection-owner TestResults/tower-growth-owner-20260929 --projection-pin c2e4d73bf3268d99217d00e52287b41e7240ae3cd4df984b87d9f1763ca00355
& $python 'TestResults/tower-growing-activity-owner-20260929/amended-verifier-v1/verify-tower-growing-activity.py' --owner TestResults/tower-growing-activity-owner-20260929 --manifest-pin f8f05eb4cf6d1e9c28a87b72bfeb0f1a9ff9f1229c5ad687c0413b042d7fbdc5 --receipt TestResults/tower-growing-activity-owner-20260929/independent-audit.json
```

## Changed files and next step

- [Journey progression adapter](../LL/tools/BalanceHarness/TowerJourneyProgression.cs), [growth adapter](../LL/tools/BalanceHarness/TowerGrowthProgression.cs) and [source adapter](../LL/tools/BalanceHarness/TowerEntrySources.cs): serial clock, native XP claims, progress events and persistent mastery.
- [Activity study](../LL/tools/BalanceHarness/TowerActivityStudy.cs), [dungeon runner](../LL/tools/BalanceHarness/DungeonAcquisitionRunner.cs) and [loot adapter](../LL/tools/BalanceHarness/TowerDungeonLoot.cs): the paired opt-in study and entry mastery integration, preserving legacy output contracts.
- [Fixture](../LL/tools/BalanceHarness/Fixtures/tower-growing-activity.json) and [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessGrowingActivityTests.cs): explicit assumptions, pending/success/failure/counterfactual XP and UTC-boundary coverage.
- [Bounded owner](analysis/run-tower-growing-activity.py), [comparison verifier](analysis/verify-tower-growing-activity.py), [chronological growth auditor](analysis/audit-growing-activity.py), [route/Vigor auditor](analysis/verify-dungeon-acquisition.py) and [loot auditor](analysis/audit-dungeon-equipment-loot.py): frozen evidence, historical seed exclusion, independent reconstruction and compatible optional mastery support.
- This report, the [handoff](Tower-Continuation-Handoff-20260928.md), [supply implementation report](Tower-Equipment-Supplies-Implementation-20260928.md) and growth-qualification forward link.

Next, prepare the actual retained grown inventories as legal Tower parties, with their personal identities and earned Essence levels, before evaluating changed Tower combat. Keep the seven-purchase target separate from minimum sufficient preparation. Extend the personal ledger through declared level/Essence/party gates toward floors 10 and 11; the authored later-floor carrying ledger proves retention, not that these first-supply histories have already earned level 50/60 or the larger party. Start with seed-free preparation/dependency checks and use a fresh bounded owner only where combat is necessary. Measured attendance and a product farming-time target are still absent, so no supply-cadence or economy adjustment is justified by this result alone.

All prior archives and unrelated uncommitted LiveOps/analytics work are preserved. No production source, dependency, deployment configuration, migration, shared database, API startup, seeding or deployment changed. The new fixture configures only the local harness. Original supply rollout requirements remain unchanged.


## Subsequent earned-party validation — 29 September

The [earned-party continuation](Tower-Earned-Party-Progression-20260929.md) completes the next preparation step using these exact histories. It prepares 224 parties and audits 1,024 fresh floor-one trials plus 64 replays. Grown parties win 87/128 at the 72-hour conditional idle checkpoint versus 58/128 fixed, with wins before full supply sets. No player-time measurement or earned later-floor progression is implied. Continue from the new report and handoff; preserve this acquisition archive.
