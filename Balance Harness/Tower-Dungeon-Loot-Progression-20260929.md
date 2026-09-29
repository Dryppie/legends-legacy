# Retained ordinary dungeon equipment — 29 September 2026

## Finding

Ordinary dungeon equipment is now included in the earned progression model through production acquisition and claim services. Across the 32 included-loot histories, characters retained **106 additional equipment items**, including 54 styled items, and 80 unspent blueprints. Dungeon drops were equipped in **74 subsequent attempts**; thirteen remained equipped at the end, changing eleven matched final loadouts.

That changed equipment did **not** change completion counts, attempted entries or completion checkpoints in this fixed comparison:

| Assumed idle outcomes | Reward policy | Seven-supply completion | Attempts | Supply items | Failed entries | Dungeon equipment retained |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Perfect victories | Prior exclusion | 16/16 | 116 | 112 | 4 | 0 |
| Perfect victories | Include dungeon loot | 16/16 | 116 | 112 | 4 | 54 |
| Four victories, one defeat | Prior exclusion | 14/16 | 117 | 109 | 8 | 0 |
| Four victories, one defeat | Include dungeon loot | 14/16 | 117 | 109 | 8 | 52 |

The fresh four-in-five baseline is **14/16**. The [previous readiness study](Tower-Entry-Readiness-20260929.md) observed 15/16 on its own panel; comparing that older count directly with this study would confound equipment with the changed combat panel. There are sixteen underlying personal identities, two conditional idle scenarios and two paired arms, not 64 independent players.

The two unfinished included-loot histories exhausted both sigil stocks: `striker--0--four-of-five` made six attempts, earned five supply items and two ordinary dungeon items; `controller--3--four-of-five` made nine attempts, earned six supplies and four ordinary dungeon items. The first history still cannot buy seven supplies from six entries, even if every attempt succeeds. Neither result establishes insufficiency of the complete economy, whose other entry channels remain excluded.

## Production rules and implementation

The new [loot adapter](../LL/tools/BalanceHarness/TowerDungeonLoot.cs) applies `EquipmentAcquisitionService` and `DungeonRunRewardClaimer` to eligible boundaries in the production run record, using in-memory repository/inventory boundaries. It reads the production ordinary equipment, blueprint, dungeon and item catalogs. It does not replace the production reward roll with a harness formula.

- At mastery zero, region-1 completion equipment has a **50% chance**, and an eligible miniboss has a **25% chance**. Grade-I rarity weights are 84% Uncommon, 14% Rare and 2% Epic, at tier 1/rank 1. Quality, specialization, attribute rolls and compatible native variants use their production rules.
- A miniboss must complete while the run remains active. A victory that exhausts Vigor fails before the miniboss reward callback. Pending miniboss loot is lost if the run later fails. Only successful terminal runs can claim equipment under this fixed non-retreat route.
- Blueprint rewards use the native per-family 25% chance with the four-completion guarantee, stable reward identities and persisted miss state. Blueprints remain unspent; no free crafting upgrade is credited.
- The entry snapshot stays fixed within each dungeon. Claimed equipment joins the owner's retained inventory only before subsequent attempts; the existing legal highest-materialized-stat-budget selection rule chooses equipment without deleting alternatives.

The fixed route excludes Treasury and retreat. **No eligible miniboss reward callbacks occurred in this combat panel**, so all observed equipment came from completion. Miniboss eligibility, pending-loot loss and Vigor-terminal suppression are covered by focused tests; this study does not claim empirical miniboss yield. No pending item was lost in the observed sample. General room currency, creature loot, monster cores, level/mastery growth, crafting and additional entry sources remain excluded. This is an equipment/blueprint extension, not a complete dungeon economy simulation.

Both arms use the [frozen full-slot-readiness rule](../LL/tools/BalanceHarness/TowerEntryReadiness.cs), Goblin-first-then-Catacombs spending, unchanged quest gate, purchase order, twelve-attempt cap and 6/24/72/240 cadence-hour checkpoints. The [new fixture](../LL/tools/BalanceHarness/Fixtures/tower-dungeon-loot.json) fixes these assumptions. Owners, initial equipment, quest armor seeds, ordered Essences and idle reward identities remain matched to the historical joint-activity study. All earned stronger gear remains owned. The exclusion arm projects the same loot rules but deliberately withholds the equipment from combat inventory, reproducing the earlier modeling exclusion; it is a counterfactual control.

Every reward callback is invoked twice to verify retry idempotence, and the claimed state rejects a duplicate claim. Every full reward projection is replayed from a copy of its preceding blueprint state. Native reward randomness derives from owner/run/layout identities; no independent reward seed is selected for a favorable item.

## Preparation before combat and audit correction

First, a [zero-fight projection](../TestResults/tower-dungeon-loot-build-20260929/first-attempt-projection.json) applied the production rewards to only the first archived attempt in each of the 32 readiness histories. **Eight resulting loadouts changed**, and all 32 prepared successfully through production preparation. Later archived combat outcomes were not reused to evaluate the changed gear. The reward draws and ledger for all 32 first attempts were independently checked before allocating the fresh study panel.

The new bounded comparison then completed and was retained unchanged. Its first independent full audit rejected a reward draw because the Python PRNG transcription omitted unchecked signed 32-bit subtraction and used division rather than multiplication by the reciprocal. This was an **auditor defect**, not a game/runtime change. Five fixed native .NET seeds, including the failing seed and signed integer boundaries, supplied **160 draws that now match exactly**.

The [audit amendment](../TestResults/tower-dungeon-loot-owner-20260929/audit-amendment.json) preserves the original failure reason and original helper hash, and pins the corrected auditor and [native vectors](../TestResults/tower-dungeon-loot-owner-20260929/audit-amendment/reward-rng-native-vectors.json) separately. The frozen original inputs, executable and study manifest were not replaced or repinned. The corrected audit passed without new combat or new seed allocations. Reproduce this archive with the amended auditor path below, not its original helper.

## Combat and progression evidence

In either arm, perfect-idle histories completed seven targets at the 72-hour checkpoint and nine at 240; four-in-five histories completed five at 72 and nine at 240, with two censored. None changed completion checkpoint. These are coarse conditional cadence checkpoints, not earliest completion or calendar-time estimates.

Included-loot histories cleared 181/181 Mines entries and 40/52 Catacombs entries. All twelve failures were Catacombs Vigor attrition. Supplied Rare controls cleared 32/32 Mines and 28/32 Catacombs, with four attrition failures; their entries and rewards are excluded from acquisition accounting. These correlated counts do not establish universal success or failure rates.

Completed-path combat-only minutes ranged from 21.63–81.53 under perfect-idle exclusion and 21.00–81.53 with loot; the four-in-five ranges were 23.07–85.98 and 23.07–86.41. Better inventory score is not guaranteed to improve combat duration or build synergy. The selected rule and ordered Essence recipes were kept fixed rather than optimized after observing outcomes.

**No player acquisition time was measured.** Engine combat seconds omit player actions, navigation, waiting, breaks and earlier leveling. Idle victories, level 30, Essence level 1/ascension 0, mastery 0 and prior quest activity remain assumptions. The seven-selected-supply target is distinct from a minimum useful Tower loadout; ordinary equipment is retained but never relabeled as a purchased supply item.

## Bounded execution and retained evidence

The [owner](analysis/run-tower-dungeon-loot.py) checked 375 production/content/reward sources against the earlier state and froze inputs/runtime before one process through the existing Windows Job owner and repository test wrapper. Limits: 64 histories, 768 acquisition attempts, 64 supplied controls, 144 full-run replays, 64,000 room combats, 86,400 modeled idle encounters per history, 900 process seconds, 840 native seconds, 256 MiB output and a 1 MiB log. No search, retry, sample extension, policy tuning or boss adjustment occurred.

Actual execution: **466 acquisition attempts, 64 controls, 122 matching full-run replays, 6,025 room combats**, 442 supply items and 4,078,080 modeled idle encounters across the alternative histories. Production rewards projected 212 ordinary items and 160 blueprints across both arms; only 106 items and 80 blueprints belong to the included-loot arm. There were 232 reward-window split replays and 884 callback/claim retry checks, plus one full reward-projection replay per attempt. Runtime was about 43.08 seconds with zero remaining children; output totaled 93,933,993 bytes.

The [passing independent audit](../TestResults/tower-dungeon-loot-owner-20260929/independent-audit.json) checked 1,917 input hashes, all 466 dungeon reward ledgers, native eligibility/rarity/archetype/specialization/quality/attribute/variant/blueprint draws, exact pending-loss/claim rules, all 64 historical loot prefixes, 698 entry decisions, 159 identical-input complete-run pairs, 4,247 production-prepared rooms and 64 final preparations. It checked inventory ownership, retained stock, entry costs, awards, Vigor, failures and censoring without fights. It does not independently reimplement the combat engine or every materialized-stat formula; native production preparation and descriptor checks cover those boundaries.

Local evidence under ignored `TestResults`:

- [Request](../TestResults/tower-dungeon-loot-owner-20260929/request.json), [source map](../TestResults/tower-dungeon-loot-owner-20260929/source-map.json), [declaration](../TestResults/tower-dungeon-loot-owner-20260929/declaration.json), [process receipt](../TestResults/tower-dungeon-loot-owner-20260929/process.json), [derived tables](../TestResults/tower-dungeon-loot-owner-20260929/derived-summary.json).
- [Results](../TestResults/tower-dungeon-loot-study-20260929/result.json), SHA-256 `cd9d1a3f6bb1722f2d2ad5dac27002f097e440aaa5c6d42494d53ea472617c31`.
- [Manifest](../TestResults/tower-dungeon-loot-study-20260929/files.json), trusted pin **`3b4f4870dfc8e10f9b0056788eb006b394ad7618dca963cba1d48d204a453fe7`**.
- [Seed ledger](../TestResults/tower-dungeon-loot-owner-20260929/seed-ledger.json), SHA-256 **`c48c5fafa3b176febbf862841425f6fcbffdfd016032ad1ad02f0d459d52f1c9`**.
- [Study TRX](../TestResults/tower-dungeon-loot-owner-20260929/study-tests.trx) and [study log](../TestResults/tower-dungeon-loot-owner-20260929/study.log).

The owner reserved **6,240 fresh dungeon values**, extending the exclusion union from 858,215 to **864,455**. Of these, 443 were used and **5,797 remain unconsumed and excluded**, together with every earlier reservation. Historical owner/armor seeds are reused only for pairing. Native idle and dungeon reward identities are not separately allocated combat samples. Preserve all archives and ledgers.

## Verification and scope

Extended `TowerActivityStudy.cs`; added `TowerDungeonLoot.cs`, the loot fixture, [tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessDungeonLootTests.cs), bounded owner, [progression verifier](analysis/verify-tower-dungeon-loot.py) and [independent reward auditor](analysis/audit-dungeon-equipment-loot.py). Added this report and updated the handoff, readiness report and supply implementation report. No production source changed in this continuation.

**388 backend tests passed**, with seven intentional opt-in skips. The archived first-attempt projection and owned study each separately passed one test. [Regression TRX](../TestResults/tower-dungeon-loot-build-20260929/regression-tests.trx), [regression log](../TestResults/tower-dungeon-loot-regression-complete-20260929.log), [projection TRX](../TestResults/tower-dungeon-loot-build-20260929/projection-tests.trx). Initial sandbox NuGet-config access was denied; the same required wrapper succeeded with escalation. No required command remains blocked. No frontend code changed or required frontend tests.

Executed commands; never reuse these completed output paths:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-dungeon-loot-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessDungeonLootTests|FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon'
$env:LL_TOWER_DUNGEON_LOOT_PROJECTION = [IO.Path]::GetFullPath('TestResults/tower-dungeon-loot-build-20260929/first-attempt-projection.json')
try { ./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/tower-dungeon-loot-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessDungeonLootTests.Project_first_earned_attempts_without_reusing_later_outcomes' } finally { Remove-Item Env:LL_TOWER_DUNGEON_LOOT_PROJECTION }
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/run-tower-dungeon-loot.py' --owner TestResults/tower-dungeon-loot-owner-20260929 --output TestResults/tower-dungeon-loot-study-20260929 --artifacts TestResults/tower-dungeon-loot-build-20260929
& $python 'TestResults/tower-dungeon-loot-owner-20260929/audit-amendment/verify-tower-dungeon-loot.py' --owner TestResults/tower-dungeon-loot-owner-20260929 --manifest-pin 3b4f4870dfc8e10f9b0056788eb006b394ad7618dca963cba1d48d204a453fe7 --receipt TestResults/tower-dungeon-loot-owner-20260929/independent-audit.json
```

No configuration, dependency, migration, shared database operation, API startup, seeding or deployment occurred. Concurrent LiveOps/analytics work remains preserved. The repeating equipment curve, stronger owned-item carryover and supported search remain unchanged. Guardian SHA-256 stays `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`. Nothing was staged or committed.

## Next work

Add **explicitly costed entry sources** to the same personal ledger: fragments/assembly, reachable quest or prophecy/cache rewards, and affordable purchases only when their prerequisites, currencies and acquisition activities are funded. Begin with production rules and seed-free affordability accounting; no additional combat is needed merely to count a source. Preserve blueprint stock without free crafting, and add currency/level/mastery progression as explicit later extensions.

The equipment-loot omission is now measured and did not remove the observed source/attrition limits on this panel. Do not extend this completed sample or retune bosses. An explicit desired farming budget or measured activity remains necessary before accepting normal-player pace or changing supply cadence.

## Subsequent costed entry-source projection

The [costed-source continuation](Tower-Costed-Entry-Sources-20260929.md) now accounts for native prophecy progress/claims, weekly Revelation and whole-fragment assembly without new combat. Conditional daily Common offers fund three additional Mines sigils at the supplied 240-hour endpoint; adding the completed weekly kill prophecy funds four. Each alternative changes four of these 32 histories' first source decisions, including both unfinished paths. All eight projected entry states prepare successfully, but no later old outcome is transferred. Quest sigils are not counted again; caches remain unopened and purchases remain unfunded. This does not change this archive's completion counts or establish player pace.
