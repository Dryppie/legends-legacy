# Earned character growth qualification — 29 September 2026

## Finding

Applying earned character and Essence XP changes **all 32 first-entry loadouts** from the [native-offer study](Tower-Native-Prophecy-Progression-20260929.md). At the original first-entry checkpoints, production progression yields character levels **31–44** and Essence levels **2–10**, compared with the earlier fixed level 30 and level-1 Essences. All **64 baseline/grown preparations** succeed while preserving the exact equipped items and Essence identities.

This is a seed-free qualification of changed inputs. It transfers **zero dungeon outcomes** to the grown characters and does not revise the previous 16/16 supply-completion result. In a separate mastery-only replay, earned mastery changes a later entry in **21/32 histories**; replay stops before that entry. A full growing-character combat comparison must include these dependencies rather than importing the old outcomes. There are **zero measured player samples**, zero new fights and zero new combat reservations.

## What is now executed

The [growth adapter](../LL/tools/BalanceHarness/TowerGrowthProgression.cs) uses `CharacterExperienceRewardWriter`, `LevelingService`, `EssenceSystemService`, its native activity-based loadout selection, `EssenceProgressionService` and the production area/character XP providers. Repository and event boundaries are personal in-memory state; no host or database is started. Character levels, XP remainders and base Power/MaxHealth are applied. Each attuned Essence receives the native full XP award, rather than splitting the character's award among slots.

The first three quest Essences are supplied at level 1 with zero XP, matching the archived starting condition. The fourth quest Essence is added after its archived quest-completing encounter and begins training on subsequent encounters. Identical owner/slot identities are retained through preparation. Newly unlocked slots remain empty. There is no ascension, Soul Dust spending, donated Essence or invented duplicate/core budget. Unascended Essences stop at level 10; surplus training XP is discarded by the native service, not banked for a free ascension.

The [entry-source adapter](../LL/tools/BalanceHarness/TowerEntrySources.cs) now optionally accepts a progressing character and native leveling service together. Existing fixed-level source studies keep their original mode and serialized output shape. The growth mode applies prophecy character XP immediately; it does not relabel the current within-level remainder as unapplied XP. Prophecy XP does not train Essences, matching production claims. Offers use the character's current level when generated; reading an existing offer preserves its original reward snapshot.

Daily selection remains the fixed `offered-kills-no-reroll` rule. Native weekly offers remain automatically accepted. Alongside shared kill/win events, actual native Essence-XP grants now advance supported Essence-XP objectives. The 186 audited claims include **12 weekly Essence-XP claims**, 128 kill-objective claims and 46 Revelation milestone claims. This is additional earned activity, not a replacement offer chosen after observing rewards. Caches and side currencies remain unspent. Whole Mines sigils are assembled at the existing checkpoints.

## Supplied conditions and limits

The [growth declaration](../LL/tools/BalanceHarness/Fixtures/tower-growth.json) explicitly uses **one creature per encounter**, one personal XP recipient, zero starting within-level XP, zero XP bonuses and zero defeat-XP retention. The archived ordinary-equipment reward stream records victories and areas, but does not contain hostile creature counts; it cannot establish actual earned XP without that additional condition. One creature continues the earlier prophecy scenario, not a measurement of the production spawn distribution. The native area provider still uses the authored spawn distribution to calculate XP per creature, so a one-creature encounter is not the area's average encounter reward.

Idle victories remain supplied perfect or four-in-five patterns. Native reward rates are applied to those victories; a defeat grants zero XP under the declared retention setting. There is no new idle combat or success-rate estimate. Prior leveling to 30, earlier quest eligibility and the original three Essence acquisitions remain supplied. This is not a from-creation character history.

The projection follows the archived area/quest activity and personally earned ordinary gear **only up to the first archived dungeon entry**. There are no dungeon XP awards, dungeon drops or mastery benefits before that entry. The existing inventory-selection rule remains fixed; earned growth does not grant replacement equipment. The new artifact prepares the equipped inventory while retaining the full original personal ledger in the frozen archive. Historical party growth and exact floor-10→11 stronger-gear carry remain preserved; this early-entry qualification does not newly measure their leveling costs.

The supplied Monday calendar, daily reset visits, ten-second continuous cadence, immediate claims and checkpoint-based assembly remain assumptions. Before the first dungeon there is no dungeon time to reconcile. A later full journey must add dungeon activity and claims to a declared clock policy and account for their XP, Essence training, source progress and mastery. Engine combat seconds and cadence equivalents must remain separate from observed player time.

## Prepared entry results

| Original first-entry checkpoint | Histories | Earned character level | Earned Essence levels |
| --- | ---: | --- | --- |
| 24 cadence hours | 4 | Two at 31; two at 32 | All four at 2 |
| 72 cadence hours | 15 | Seven at 34; eight at 35 | Thirteen with all four at 4; two with the fourth at 3 |
| 240 cadence hours | 13 | Seven at 42; six at 44 | All four at the unascended cap of 10 |

The first character level-up occurs at encounter **3,808–5,083**, depending on the supplied path, victory pattern and prophecy awards. The first Essence level-up occurs at **6,335–8,107**. These are simulated event ordinals, not player-time measurements. The earlier quest-attunement event is tracked separately and is not mislabeled as a newly discovered growth difference.

Across the 32 truncated personal ledgers, the model applies **29,062,594 idle character XP** and **988,649 prophecy character XP**. Their sizes differ with the stopping horizon; this sum is not a party farming bill or population mean. Thirteen histories reach the Essence training cap. The independent auditor verifies level curves, remainder conservation, per-slot training, delayed fourth-Essence ownership, caps, generated offer level/rewards, exact claims, milestone resets, cache stock and assembly debits. Native preparation is audited for character level and retained equipment/Essence descriptor and identity parity; the audit is not a separate reimplementation of every combat attribute calculation.

## Separate mastery dependency replay

The [qualification runner](../LL/tools/BalanceHarness/TowerGrowthStudy.cs) also reconstructs the frozen terminal run state from production layout and resolved actions and calls `DungeonMasteryService`. Production `choose_route` resolves the selected combat/rest room immediately; those rooms must be counted. Combat-readiness losses exclude the terminal lost room from earned room XP, while Vigor attrition after victory includes it. Completion counts, dungeon-family state, owner isolation and duplicate award rejection are checked.

This branch holds character/Essence growth at the archived values to isolate mastery. It accepts old outcomes only while the entry's mastery level remains zero. A completion that first reaches mastery 1 still used mastery 0 at entry; its next same-family entry has changed visibility and equipment-drop benefits and cannot reuse the archived outcome. No modeled mastery reward is fed into the separate grown-character projection.

The audited prefix contains **185 historical runs**. Nineteen histories stop before zero-based attempt 5, and two before attempt 6. Eleven exhaust the existing history without a changed mastery entry; reaching mastery 1 only after the last relevant entry does not create another outcome. The first changed benefits are **one additional visibility row and five percentage points of equipment-drop chance**. Rest-site, Vigor-cost and currency bonuses remain zero at that level. No later historical loot roll or combat result is transferred beyond the cutoff. These replay counts are not new combat samples.

## Implementation and verification

Target: primary LL game balance harness and tests. Seven source files changed:

- Extended the [entry-source adapter](../LL/tools/BalanceHarness/TowerEntrySources.cs) with an explicit optional native progression boundary and earned Essence-XP progress events.
- Added [growth progression](../LL/tools/BalanceHarness/TowerGrowthProgression.cs), [qualification runner](../LL/tools/BalanceHarness/TowerGrowthStudy.cs), [declaration](../LL/tools/BalanceHarness/Fixtures/tower-growth.json) and [tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessGrowthTests.cs).
- Added the [bounded owner](analysis/run-tower-growth.py) and [independent verifier](analysis/verify-tower-growth.py). The previous offer auditor is reused unchanged for common definitions; the new auditor independently handles current-level selection and reward contexts.

**499 regression tests passed, with eleven intentional opt-in skips.** The separately owned qualification passed one test, then its frozen independent auditor passed without amendments or new fights. Focused tests cover character XP boundaries, native Essence distribution, late acquisition, caps and batch invariance, retained items, generated prophecy snapshots, character-only prophecy XP, production preparation, resolved-route mastery and failed-room credit. The regression retains earlier acquisition, carrying, source, dungeon, prophecy and supply tests. No frontend code changed, so frontend tests were not repeated.

Initial sandbox NuGet-config access failed; the repository wrapper succeeded with escalation. Missing harness imports were corrected before the passing build. No required command remains blocked. Commands executed; do not reuse these completed output directories:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-growth-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessGrowthTests|FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~BalanceHarnessProphecyOfferTests|FullyQualifiedName~CharacterExperienceProgressionTests|FullyQualifiedName~DungeonMastery|FullyQualifiedName~EssenceProgressionServiceTests'
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/tower-growth-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessGrowthTests|FullyQualifiedName~BalanceHarnessProphecyOfferTests|FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~BalanceHarnessDungeonLootTests|FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon|FullyQualifiedName~Prophecy|FullyQualifiedName~SigilFragment|FullyQualifiedName~CharacterExperienceProgressionTests|FullyQualifiedName~EssenceProgressionServiceTests'
$env:PYTHONDONTWRITEBYTECODE = '1'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python 'Balance Harness/analysis/run-tower-growth.py' --owner TestResults/tower-growth-owner-20260929 --output TestResults/tower-growth-study-20260929 --artifacts TestResults/tower-growth-build-20260929
& $python 'TestResults/tower-growth-owner-20260929/inputs/Balance Harness/analysis/verify-tower-growth.py' --owner TestResults/tower-growth-owner-20260929 --manifest-pin c2e4d73bf3268d99217d00e52287b41e7240ae3cd4df984b87d9f1763ca00355 --receipt TestResults/tower-growth-owner-20260929/independent-audit.json
```

## Evidence and next work

Frozen output: `TestResults/tower-growth-study-20260929`. The [manifest](../TestResults/tower-growth-study-20260929/files.json) is SHA-256 **`c2e4d73bf3268d99217d00e52287b41e7240ae3cd4df984b87d9f1763ca00355`**; the [result](../TestResults/tower-growth-study-20260929/result.json) is **`d58d41e33d1b97792c36b3d3c92b2e3dfa24e8d60d7b2dd6cdd6a3e8b6873048`**. The [independent audit](../TestResults/tower-growth-owner-20260929/independent-audit.json) checked **2,126 input hashes, 32 histories, 186 claims, 185 mastery-prefix runs and 64 preparations**. The [derived table](../TestResults/tower-growth-owner-20260929/derived-summary.json) preserves per-history findings.

The owner retains the [request](../TestResults/tower-growth-owner-20260929/request.json), [declaration](../TestResults/tower-growth-owner-20260929/declaration.json), frozen source/runtime and [process receipt](../TestResults/tower-growth-owner-20260929/process.json): 8.235 seconds, exit zero, no timeout and zero active children. Bounds were 300 process seconds, 240 native seconds, 32 MiB output and 1 MiB log. See the [regression TRX](../TestResults/tower-growth-build-20260929/regression-tests.trx), [regression log](../TestResults/tower-growth-regression-20260929.log), [qualification TRX](../TestResults/tower-growth-owner-20260929/export-tests.trx) and [final checks](../TestResults/tower-growth-owner-20260929/final-checks.json). `TestResults` is ignored and local.

No new combat was run: these are qualification projections with explicit cutoffs, not a completed growing-character acquisition comparison. The next combat study should integrate the qualified XP/Essence state with native dungeon XP claiming, persistent mastery, source events and a declared activity clock, then freeze a fresh paired bounded run. Its baseline must also be fresh; the earlier 16/16 is conditional fixed-state evidence. Do not extend archived runs or apply mastery to a previously rolled layout/reward stream after the fact.

The historical seed exclusion union remains **870,695**, including all **5,824** unconsumed native-offer reservations and all older unused values. Latest ledger remains SHA-256 `021a8ab1b4527222e738d4e61994e63e7ca21d439e6423e79292e2c223102cc3`. All archives and unrelated concurrent work remain preserved. No production source, deployment configuration, dependency, migration, database, seeding, API startup or deployment changed. The fixture affects only the local harness. Original supply rollout requirements still apply; bosses, the repeating gear curve, stronger owned gear and supported search remain unchanged.

## Subsequent integration

The [fresh growing-activity comparison](Tower-Growing-Activity-Progression-20260929.md) now integrates dungeon XP claiming, persistent mastery, supported source events and a serial clock. Fixed and grown arms both complete 32/32 histories at the same coarse checkpoints; grown final levels are 35–45. It uses new combat panels and does not extend this seed-free archive. Its report records the passing regression, bounded execution, independently audited result and separately preserved auditor-only amendment.
