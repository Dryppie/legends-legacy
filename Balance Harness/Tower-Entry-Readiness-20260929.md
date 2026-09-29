# Visible equipment coverage before dungeon entry — 29 September 2026

## Finding

Waiting for complete equipped-slot coverage reduced failed entries from **83 to 14** in this paired diagnostic. Both arms spend earned Goblin sigils first, then Catacombs sigils; only the readiness requirement differs.

| Assumed idle outcomes | Entry policy | Completed seven-item target | Attempts | Earned supply items | Failed entries |
| --- | --- | ---: | ---: | ---: | ---: |
| Perfect victories | Immediate | 16/16 | 154 | 112 | 42 |
| Perfect victories | Full slot coverage | 16/16 | 119 | 112 | 7 |
| Four victories, one defeat | Immediate | 14/16 | 149 | 108 | 41 |
| Four victories, one defeat | Full slot coverage | 15/16 | 118 | 111 | 7 |

Across these alternatives, readiness required **66 fewer entries** and earned three more supply items. These are descriptive results for sixteen reused identities, two idle-outcome scenarios and two policy arms, not 64 independent players or population success estimates. The comparison uses its own fresh immediate baseline; the [previous two-source result](Tower-Two-Source-Progression-20260929.md) remains historical.

The one unfinished readiness history, `striker--0--four-of-five`, succeeded in **all six available attempts**, earned six selected items and exhausted both sigil stocks at the final activity checkpoint. Its remaining shortfall is entry supply under the declared sources, not a failed dungeon. Other production entry channels are excluded, so this is not evidence that the complete economy cannot fund a seventh item.

## Fixed rule and retained assumptions

The [fixture](../LL/tools/BalanceHarness/Fixtures/tower-entry-readiness.json) was frozen before combat. `full-slot-ready` requires equipped Head, Chest, Legs, Necklace, Ring and Relic, plus either a two-handed weapon or a one-handed weapon with off-hand. **Any rarity/rank qualifies**. It uses no stat-score threshold, hidden encounter information or past combat outcome to decide readiness. The existing inventory policy still chooses the highest materialized-stat-budget legal equipment; all owned alternatives remain retained. An off-hand is not required with a two-handed weapon.

Both policies wait for the same four-Essence quest gate. At each unchanged 6/24/72/240 idle cadence-hour checkpoint, the immediate arm spends available entries and the readiness arm first checks equipment coverage. After each award, coverage is checked again. A failed coverage check preserves every sigil and waits until the next checkpoint. Every enter/wait/stop decision records its missing slots and reason; the independent auditor reconstructs the decision from the equipment and resource ledger.

The shared [activity runner](../LL/tools/BalanceHarness/TowerActivityStudy.cs) calls the new [readiness policy](../LL/tools/BalanceHarness/TowerEntryReadiness.cs). Starting owner/item identities, random quest armor seeds, ordered Essences, ordinary production reward streams and supply item identities remain matched to the pinned joint-activity histories. New histories start from their initial state. Fresh combat panels are indexed by family and within-family attempt ordinal and shared across arms. The seven-item purchase order and twelve-total-attempt cap are unchanged. Every failure consumes the actual family's sigil.

Inherited assumptions remain substantial: supplied level 30 and earlier quest progress; fixed Essence level 1/ascension 0 and mastery 0; assumed perfect or deterministic four-in-five idle victories; one Catacombs quest sigil and a Goblin sigil earned at the modeled quest gate; production ordinary idle equipment/sigil rewards. Level/mastery growth, ordinary dungeon loot, fragment assembly, caches, purchases, donations and dismantling income remain excluded. See [joint activity](Tower-Joint-Activity-Progression-20260929.md) for the original dependency and reward model. Seven selected supplies are a fixed purchase target, not a minimum useful loadout.

There was one readiness rule, no threshold sweep, no outcome-based adjustment, no new search and no change to bosses or the supply economy.

## What improved, and what remains

The immediate arm entered at the first checkpoint for all 32 scenario histories, spending **74 entries for three clears** there. The readiness arm spent none at that checkpoint. Its first entry occurred at 24 cadence hours for four histories, 72 for fifteen and 240 for thirteen. Those delays are part of the policy's cost; the experiment does not establish the earliest useful entry time.

| Assumed idle outcomes | Immediate completions at 72 / 240 | Readiness completions at 72 / 240 | Matched effect |
| --- | ---: | ---: | --- |
| Perfect | 3 / 13 | 6 / 10 | Three completed earlier; thirteen at the same checkpoint |
| Four-in-five | 3 / 11 | 5 / 10 | Two earlier; twelve same; one newly complete; one still censored |

No matched history lost completion or completed at a later checkpoint in this sample. The auditor checked **73 coverage-wait decisions**. Missing-slot counts across repeated waits were Necklace 52, Relic 43, Ring 41, Legs 24, Head 22, OffHand 20 and Chest 17. These repeated counts describe why this policy waited; they are not independent drop-rate estimates.

| Assumed idle outcomes | Policy | Mines clears / entries | Catacombs clears / entries |
| --- | --- | ---: | ---: |
| Perfect | Immediate | 88/107 | 24/47 |
| Perfect | Coverage | 96/96 | 16/23 |
| Four-in-five | Immediate | 81/100 | 27/49 |
| Four-in-five | Coverage | 91/91 | 20/27 |

All **14 readiness-arm failures were Catacombs Vigor attrition**, despite winning the fights that consumed the remaining Vigor. The supplied Rare controls also completed only **22/32 Catacombs runs**, with ten attrition failures; Mines controls completed 32/32. Controls supplied their entry and equipment and awarded nothing to the earned histories. Coverage is therefore not a sufficient guarantee of dungeon completion. The 187/187 earned Mines clears are conditional, correlated observations across the small shared panel, not a universal success rate.

Completed-path combat-only minutes ranged from 29.79–121.41 under immediate perfect idle and 21.92–85.96 with coverage; the four-in-five ranges were 34.24–145.12 and 21.92–75.81. These sum engine combat seconds, including failed entries. They omit player actions, navigation, breaks, waiting and earlier leveling. **Neither combat minutes nor the coarse idle checkpoints establish acquisition time. Zero player samples were measured.**

## Bounded evidence

The [owner](analysis/run-tower-entry-readiness.py) first checked 375 relevant production/content/reward sources against the earlier study, then captured source, fixtures, historical personal histories and runtime. It used the existing Windows Job owner and required backend wrapper. The frozen [declaration](../TestResults/tower-entry-readiness-owner-20260929/declaration.json) allowed 64 histories, 768 acquisition attempts, 64 supplied controls, 144 full-run replays, 64,000 room combats, 86,400 idle encounters per history, 900 process seconds, 840 native seconds, 256 MiB output and a 1 MiB log. There was no retry or extension.

Execution completed **540 acquisition attempts, 64 controls, 133 matching full-run replays and 6,160 room combats**, earning 443 supply items across the alternative histories. It processed 4,501,440 modeled idle encounters and replayed 239 reward windows across different batch boundaries without additional combat. Process runtime was about 32.78 seconds with zero remaining children; output totaled 93,589,021 bytes. Computer runtime is not gameplay duration.

The [independent file-only audit](../TestResults/tower-entry-readiness-owner-20260929/independent-audit.json), using the frozen [verifier](analysis/verify-tower-entry-readiness.py), passed **1,912 input hashes**, 779 entry decisions, all 64 historical loot prefixes, 36 identical-input complete-run pairs, 4,562 production-prepared rooms and 64 final preparations. It reconstructed retained ownership, exact entry payments, supply awards, Vigor, failures, time and censoring without new fights. Production reward batch replays do not claim an independent reimplementation of every reward roll.

Local evidence under ignored `TestResults`:

- [Request](../TestResults/tower-entry-readiness-owner-20260929/request.json), [source map](../TestResults/tower-entry-readiness-owner-20260929/source-map.json), [compatibility receipt](../TestResults/tower-entry-readiness-owner-20260929/source-compatibility.json), [process receipt](../TestResults/tower-entry-readiness-owner-20260929/process.json), [derived tables](../TestResults/tower-entry-readiness-owner-20260929/derived-summary.json).
- [Results](../TestResults/tower-entry-readiness-study-20260929/result.json), SHA-256 `cb58ac8f3e4c67cd0b08e7e830afb3aecb5b40cd419db618ee4b43d8d8a60df0`.
- [Manifest](../TestResults/tower-entry-readiness-study-20260929/files.json), trusted pin **`d70b1c4d85231a1b30518bd5f6a89ba3316eac2e3c2790be6b7aa1833335ad65`**.
- [Seed ledger](../TestResults/tower-entry-readiness-owner-20260929/seed-ledger.json), SHA-256 **`bc54eae43e081231eb28fdbfa9d2f676a2f2a7f66b6034cd31bc7b8f6c8c0e96`**.
- [Study TRX](../TestResults/tower-entry-readiness-owner-20260929/study-tests.trx) and [study log](../TestResults/tower-entry-readiness-owner-20260929/study.log).

The owner reserved **6,240 fresh dungeon values**, extending the prior 851,975-value exclusion union to **858,215**. Of these, 520 were used and **5,720 unconsumed reservations remain excluded**, together with every older reservation. The sixteen historical owner/armor seeds were intentionally reused for pairing; production idle reward identities remain a separate owner/generation/timestamp stream. Preserve the new ledger and its predecessors. Do not overwrite archives or repin historical source/runtime receipts to this build.

## Verification and changed files

Extended `TowerActivityStudy.cs`; added `TowerEntryReadiness.cs`, the entry-readiness fixture, [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessEntryReadinessTests.cs), owner and verifier. Added this report and updated the handoff, two-source report and supply implementation report. All changes in this continuation are harness/test/documentation work.

**385 backend tests passed**, with five intentional opt-in study skips; the owned study separately passed one test. [Regression log](../TestResults/tower-entry-readiness-regression-complete-20260929.log), [preserved regression TRX](../TestResults/tower-entry-readiness-build-20260929/regression-tests.trx). The initial sandbox run could not read local NuGet configuration; the same wrapper succeeded with escalation. No required command remains blocked. No frontend code changed or required frontend testing.

Executed commands; these historical output directories must not be reused:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-entry-readiness-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon' *> TestResults/tower-entry-readiness-regression-complete-20260929.log
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/run-tower-entry-readiness.py' --owner TestResults/tower-entry-readiness-owner-20260929 --output TestResults/tower-entry-readiness-study-20260929 --artifacts TestResults/tower-entry-readiness-build-20260929
& $python 'TestResults/tower-entry-readiness-owner-20260929/inputs/Balance Harness/analysis/verify-tower-entry-readiness.py' --owner TestResults/tower-entry-readiness-owner-20260929 --manifest-pin d70b1c4d85231a1b30518bd5f6a89ba3316eac2e3c2790be6b7aa1833335ad65 --receipt TestResults/tower-entry-readiness-owner-20260929/independent-audit.json
```

Concurrent LiveOps/analytics work and historical archives remain preserved. No production code, configuration, dependency, migration, database, seeding, API startup or deployment changed. The repeating equipment curve, stronger owned-item carryover and supported search `affinity-creation-with-benchmark-validation-v1` remain unchanged. Guardian SHA-256 is still `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`. Nothing was staged or committed.

## Next work

Use this fixed coverage policy as an explicit diagnostic scenario while extending the earned model with **ordinary dungeon rewards and their production eligibility**, then costed entry sources and level/mastery growth. Begin with a seed-free rules/ledger integration and production preparation; reserve another bounded combat study only for changed loadouts or behavior. The remaining six-entry history makes the difference between entry supply and combat readiness concrete. Catacombs Vigor attrition remains a separate observed cost and must not be silently removed.

Do not sweep readiness thresholds or tune bosses to erase these costs. A minimum useful Tower loadout is a different target from purchasing seven selected items; validate that question separately. Measured activity or an explicit desired farming budget is still needed before accepting normal-player pace or changing supply cadence.

## Subsequent ordinary dungeon equipment extension

The [completed loot study](Tower-Dungeon-Loot-Progression-20260929.md) now retains production completion/miniboss equipment and records unspent blueprints under this fixed readiness policy. Its included-loot arm retained 106 equipment items, used dungeon drops in 74 later attempts and changed eleven final loadouts. Completion remained **16/16 perfect-idle and 14/16 four-in-five**, equal to its fresh exclusion baseline. This report's 15/16 observation remains historical on its own panel; it is not a matched comparator for the loot change.

The extension passed 388 regression tests, a zero-fight projection, its bounded study and an independently corrected audit. Its PRNG transcription correction is separately pinned, preserving original inputs and outputs. Next work is explicitly funded entry sources; no gear curve, boss, production setting or player-time conclusion changed.
