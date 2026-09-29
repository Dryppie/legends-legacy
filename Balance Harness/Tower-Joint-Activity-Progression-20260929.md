# Joint equipment and sigil activity — 29 September 2026

## Finding

Ordinary gear and dungeon sigils now come from the **same production reward history**. The model no longer waits for exact Common profiles or adds independent gear-farming and sigil-farming expectations. It retains every earned item, equips useful legal combinations under a fixed inventory policy and spends only personally available Goblin sigils.

By the final checkpoint, **13/16 perfect-idle histories** and **9/16 four-in-five-victory histories** had earned all seven selected Rare supply items. All ten unfinished histories had eight occupied equipment slots, zero Goblin sigils and unused Catacombs sigils. Later funded Mines attempts won 173/173; the earliest checkpoint won only 8/40. These observations separate an early equipment-coverage problem from a later **Mines-only source-policy constraint**. They do not establish that the complete economy lacks entry resources.

The study remains conditional: level 30 and earlier quest activity are supplied, idle outcomes are declared scenarios, and only one dungeon family is spent. There are **zero measured player samples**. The next useful comparison is a fixed policy that can spend both personally earned regional sigil families, with their actual dungeon failures and rewards. No boss, supply cadence or gear curve was changed.

## Model and production boundaries

The new [activity inventory model](../LL/tools/BalanceHarness/TowerActivityInventory.cs) calls `CombatAcquisitionRewardProcessor.ProcessAsync` directly. It uses production equipment selection, rarity, quality, random attribute rolls, regional variants, sigil rolls, stable reward identities and item metadata. Failed idle encounters award neither equipment nor sigils. Equipment and both sigil families share the same owner, schedule generation and encounter timestamps. Item metadata is read from the production seed catalog with its normal defaults; no database or application host is involved.

The [fixed plan](../LL/tools/BalanceHarness/Fixtures/tower-activity.json) declares four recipes from the preceding [first-supply study](Tower-First-Supply-Progression-20260928.md), four personal histories per recipe and two idle-outcome scenarios. The scenarios share owner identities and random armor outcomes: perfect victory versus a deterministic four-victories/one-defeat pattern. These are paired sensitivity cases, not 32 independent population samples or measured success rates.

The start is a **conditional level-30 checkpoint at the Restless Dead quest gate**, with earlier leveling and quest victories supplied. Each character owns a quest mace, an unconditioned random Armor Chest outcome, the selected First Hunt/Lumo/Crystal Essences and one unspent Catacombs quest sigil. Earlier ordinary loot and sigil activity receive no credit. This zero-stock boundary is conservative for retained resources, but it is not an assertion that real level-30 characters normally have this inventory.

Armor selection uses the actual uniform 42-definition production pool, with a frozen random seed instead of the live fresh opening identity. It can award any eligible head/chest/legs definition and specialization; it is no longer conditioned on the desired chest piece. This random selection is independently checked in the audit.

The character farms Moonlit Graves until an ordinary drop is earned. That drop is recorded as temporarily equipped for the quest gate; its tier-1 slot is legal with the quest mace, or replaces the mace when two-handed. The model then counts **five subsequent Moonlit victories**, followed by **five Twilight victories**, before granting the selected Twilight Essence and the one Goblin quest sigil. These counts and areas are checked against current quest content. Later activity stays in Twilight. Quest eligibility/rewards are interpreted from content; the persistent quest service is not executed. Level and Essence levels remain fixed at 30 and 1, without ascension, evolution or mastery growth.

Inventory selection is explicitly `highest-materialized-stat-budget-v1`: for each non-hand slot, equip the item with the highest sum of its actual stats multiplied by production materialized point costs; compare the best two-handed item with the best one-handed/off-hand combination. Ties use stable item IDs. All qualities, profiles, armor types and styles remain eligible. All other items stay owned. Production slot validation accepts partial inventories. This is a simple declared inventory policy, **not a combat-strength guarantee, recipe search or replacement for `affinity-creation-with-benchmark-validation-v1`**. It can miss ability synergy and preferable tradeoffs between attributes.

The [sequential study](../LL/tools/BalanceHarness/TowerActivityStudy.cs) uses fixed cumulative encounter checkpoints of 2,160, 8,640, 25,920 and 86,400. At the production ten-second cadence these correspond to **6, 24, 72 and 240 idle activity hours**. At each checkpoint it spends available Goblin sigils on grade-I Mines until stock runs out, seven target supply items have been earned, or twelve cumulative attempts have been made. It never borrows, converts Catacombs sigils or credits future drops. Failed dungeon attempts consume their entry sigil.

Each success awards one selected item through the production supply catalog, in the predeclared weapon/chest/head/legs/necklace/ring/relic order. The target recipe is the prior study's seven-item two-handed reference. Owned higher-budget alternatives can remain equipped; all seven purchased targets remain available. Completion means **seven selected supply items earned**, not proof that every purchase was necessary or the cheapest useful progression route. No extra set is charged at floor 11; the earlier retained-inventory model remains unchanged.

Production dungeon generation, preparation, routes, rest, Vigor, failure and combat are reused from the existing runner. Completion callbacks are recorded. Ordinary dungeon loot, persistent reward/mastery advancement, leveling, reinforcement, donations, dismantling, fragment assembly and purchases are excluded. Already-owned Rare controls use supplied gear and entry and are accounted separately, never as acquisition successes.

## Results

| Idle outcome scenario | Seven-item histories completed | Dungeon attempts | Successful runs / earned items | Histories censored at activity cap |
| --- | ---: | ---: | ---: | ---: |
| Perfect victories | 13/16 | 124 | 106 | 3 |
| Four victories, one defeat | 9/16 | 113 | 96 | 7 |

One perfect-victory history completed at the 72-hour checkpoint; the other twelve completed at 240. All nine completed four-in-five histories finished at 240. These are fixed policy observation times, not estimates of the earliest achievable completion. All ten unfinished histories reached the activity cap; none reached the twelve-attempt cap. They retained 3–11 Catacombs sigils each. Nine had enough in count for their missing selected items **only if future attempts succeeded**; the remaining history had three sigils and four items still to earn. Catacombs outcomes for those changed inventories have not been tested here.

| Checkpoint, idle cadence hours | Perfect: wins / attempts | Four-in-five: wins / attempts | Median ordinary items owned, perfect / four-in-five |
| ---: | ---: | ---: | ---: |
| 6 | 4/21 | 4/19 | 3 / 2 |
| 24 | 11/12 | 10/12 | 11.5 / 9.5 |
| 72 | 28/28 | 22/22 | 31.5 / 26 |
| 240 | 63/63 | 60/60 | 97 / 78 |

The final perfect column contains fifteen still-active histories because one stopped after completion at 72 hours. Counts combine different recipes and increasingly earned gear; they are not estimates of player success probabilities. The later 173/173 wins cannot justify assuming future perfect dungeon success. The 32 already-owned Rare control probes all succeeded and are excluded from the acquisition totals.

The personal reward ledgers contain **2,844 ordinary items and 202 supply items** across the two scenarios. No item was discarded, donated or sold. The following stock accounting keeps resources on their original owners; its totals are descriptive and cannot be pooled between players:

| Scenario | Random Goblin sigils earned | Quest Goblin sigils | Goblin sigils spent | Goblin sigils retained | Catacombs sigils retained, including quest reward |
| --- | ---: | ---: | ---: | ---: | ---: |
| Perfect | 144 | 16 | 124 | 36 | 148 |
| Four-in-five | 122 | 16 | 113 | 25 | 119 |

The retained Goblin stock belongs to completed histories and cannot solve other owners' shortages. The unfinished histories all have zero Goblin stock and eight occupied equipment slots. A policy using only Mines leaves all Catacombs sigils unspent by design; that must not be described as a shortage across every supported acquisition source.

The first supply item was earned at 6/24/72/240 hours by 4/6/4/2 perfect histories and 4/5/5/2 four-in-five histories respectively. The quest gate itself completed after 0.23–5.46 cadence hours in the perfect scenario and 0.24–5.81 in the four-in-five scenario. These are simulated reward/activity outcomes from supplied idle victories, not measured quest completion durations.

Completed paths used 21.59–91.50 engine combat minutes in the perfect scenario and 22.69–102.37 in the four-in-five scenario, including failed attempts. The activity hours and combat seconds cover different activities in the same declared history; gear and sigil rewards are never charged as two independent idle histories. **Do not call 240 cadence hours a measured ten-day acquisition time.** Earlier leveling, real idle success and scheduling, player input, navigation, breaks, offline limits and excluded reward sources remain unmodeled. Fixed level/Essence/mastery budgets over long activity windows are also a limitation.

## Bounded evidence

The [owner](analysis/run-tower-activity.py) snapshots content, source, tests and runtime assemblies before execution and uses the existing suspended Windows Job owner. The [frozen declaration](../TestResults/tower-activity-owner-20260929/declaration.json) limits the run to 32 histories, 86,400 idle encounters per history, 384 acquisition attempts, 32 control probes, 40 combat replays, 30,000 room combats, 900 seconds process time, 840 seconds native time, 256 MiB output and a 1 MiB log. No retries, adaptive extension, recipe search or balance changes occurred after freezing.

Actual execution: **2,704,320 modeled idle encounters**, **237 funded acquisition attempts**, **32 control probes**, **40 matching full-run replays** and **2,465 room combats**. Another **127 reward windows** were replayed through production processing at different batch boundaries, preserving every equipment descriptor and sigil count. These reward replays execute no combat. The owned process took about 20.34 seconds and exited with zero active children; output was 42,721,069 bytes. Computer runtime is unrelated to modeled gameplay time.

The previous exclusion union was 842,599. This owner reserved **3,136 new values**: sixteen owner/Armor Chest identity seeds and 3,120 layout/room seeds. Production idle rewards use their native owner/generation/timestamp identities, not simulated combat RNG; these reward streams are explicitly separate from the reserved combat panel. Of the new values, 360 were used and **2,776 unconsumed reservations remain excluded**. The full union is **845,735**. Preserve the [ledger](../TestResults/tower-activity-owner-20260929/seed-ledger.json), SHA-256 `0e99464e7a7239ea51b22c204eb8eab6681b70a03ac4a98f012d7458a8d60f8f`, and all predecessor ledgers.

Retained local evidence under ignored `TestResults`:

- [Frozen request](../TestResults/tower-activity-owner-20260929/request.json), [source map](../TestResults/tower-activity-owner-20260929/source-map.json), [process receipt](../TestResults/tower-activity-owner-20260929/process.json), [completion receipt](../TestResults/tower-activity-owner-20260929/completion.json).
- [Results](../TestResults/tower-activity-study-20260929/result.json), SHA-256 `d22db867dea16528cd0109eb94a30068cc769c1636d4ece3fd280fffe0d71879`; adjacent history files retain quest gates, ordinary reward windows, sigil balances, checkpoint inventories, attempts, supply items and final production preparation.
- [Manifest](../TestResults/tower-activity-study-20260929/files.json), trusted pin **`aa84a148c2b8c0c008e84eae9d1d45ab8bcdd2999d5f428e23ab61549b84449c`**.
- [Passing independent audit](../TestResults/tower-activity-owner-20260929/independent-audit.json), [derived tables](../TestResults/tower-activity-owner-20260929/derived-summary.json), [study TRX](../TestResults/tower-activity-owner-20260929/study-tests.trx), [study log](../TestResults/tower-activity-owner-20260929/study.log).

The frozen [file-only verifier](analysis/verify-tower-activity.py) checked 1,863 input hashes, manifests, historical exclusions, random Armor Chest draws, chronological quest gates, the loss pattern, inventory selection, personal item retention, funded entry costs, awards, routes, Vigor, failures, combat seconds and censoring. It checked gear/Essence parity in **1,957 production-prepared diagnostic rooms** and all **32 final preparations**, without additional fights. Native reward-window replays verify production batch independence; the file-only audit reconstructs the ledger but does not independently reimplement every ordinary drop roll or full equipment evaluator.

## Verification and changed files

Added two harness classes, the activity fixture, [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessActivityTests.cs), bounded owner, independent verifier and this report. Updated the handoff, first-supply report and supply implementation report. No production code changed in this continuation.

**367 backend tests passed**, with three intentional opt-in combat-study skips; the owned study then separately passed its one test. [Regression log](../TestResults/tower-activity-regression-20260929.log), [preserved regression TRX](../TestResults/tower-activity-build-20260929/regression-tests.trx). Initial compilation was blocked by sandbox NuGet-config access and succeeded through the required wrapper with escalation. Before combat, focused tests exposed item-default and partial-loadout validation errors; both were corrected. One initial build also saw concurrent LiveOps files between edits; the later build passed without changing that work. No required verification remains blocked. No frontend changes or frontend tests were needed.

Recorded commands (historical output directories must not be reused):

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-activity-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon' *> TestResults/tower-activity-regression-20260929.log
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/run-tower-activity.py' --owner TestResults/tower-activity-owner-20260929 --output TestResults/tower-activity-study-20260929 --artifacts TestResults/tower-activity-build-20260929
& $python 'TestResults/tower-activity-owner-20260929/inputs/Balance Harness/analysis/verify-tower-activity.py' --owner TestResults/tower-activity-owner-20260929 --manifest-pin aa84a148c2b8c0c008e84eae9d1d45ab8bcdd2999d5f428e23ab61549b84449c --receipt TestResults/tower-activity-owner-20260929/independent-audit.json
```

The repeating user-defined gear curve, supported search and stronger personal inventory carryover remain intact. Guardian content is unchanged at SHA-256 `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`. No dependencies, application configuration, migration, database access, API startup, seeding or deployment were introduced. All concurrent work and historical archives were preserved; nothing was staged or committed.

## Next work

Compare a predeclared **both-family spending policy** against this Mines-only baseline, using independently reserved dungeon panels and the same underlying personal reward histories where a matched comparison is intended. Preserve actor/item identities and include Catacombs Vigor attrition; its sigils cannot simply be counted as successful Mines clears. Retain this completed archive rather than continuing its censored paths or extending its sample.

Then add other affordable entry sources only with eligibility and costs, and include leveling/mastery/ordinary dungeon rewards if modeling an actual journey rather than this fixed-budget diagnostic. Measured activity or an explicit desired farming budget is still needed before accepting a normal-player pace or changing cadence. Early gear coverage and source policy should be addressed before considering any boss adjustment.

## Subsequent paired source-policy comparison

The [completed two-source study](Tower-Two-Source-Progression-20260929.md) retains these personal identities and reward prefixes while comparing both policies on a fresh, shared dungeon panel. Seven-item completion improved **13/16 → 16/16** under perfect idle victories and **8/16 → 14/16** under four-in-five victories. The fresh paired baseline is 8/16; this report's older 9/16 result remains historical and is not the matched comparator.

Both-family spending charged 95 Catacombs entries, including 36 failures and ten Vigor-attrition failures. Only 1/34 first-checkpoint Catacombs entries succeeded, making an explicit entry-readiness policy the next comparison. Two histories still exhausted both sigil families. These remain conditional activity scenarios with no measured player-time claim. The shared runner was extended, while this study's captured source/runtime, output, receipts and seed exclusions remain preserved. No production settings changed.
