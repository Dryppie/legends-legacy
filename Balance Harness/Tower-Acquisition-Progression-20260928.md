# Tower acquisition and earned progression — 28 September 2026

**Implemented and verified a reproducible, seed-free acquisition model.** The authored floor-1→11 path earns **280 individual bound items** across 15 characters, retaining inventory through party contraction and expansion. Its ten returning floor-11 members reuse all **70** equipped floor-10 items. The strongest retained floor-10/11 equipment is also obtainable through the production supply choices. **Normal-player acquisition pace remains unvalidated:** the study has zero measured player samples and uses explicit success-rate, activity and duration scenarios.

This is primary LL game and offline Balance Harness work. The user-defined repeating equipment curve, supported `affinity-creation-with-benchmark-validation-v1` search and boss content are unchanged. Start subsequent work from the [handoff](Tower-Continuation-Handoff-20260928.md); the [supply implementation report](Tower-Equipment-Supplies-Implementation-20260928.md) retains its feature verification.

## Reproducible model and evidence boundary

- [TowerAcquisitionStudy.cs](../LL/tools/BalanceHarness/TowerAcquisitionStudy.cs) loads production dungeon definitions through their validator/materializer/provider, including retired-dungeon filtering; production ordinary-drop, supply and upgrade catalogs; the declared floor budgets; and local acquisition switches. It authenticates supplied historical reference cells using external manifest pins. It cannot execute combat.
- [TowerAcquisitionInventory.cs](../LL/tools/BalanceHarness/TowerAcquisitionInventory.cs) awards actual `EquipmentData` through `TowerEquipmentSupplyCatalog.Award`, with personal owners, item identities and successful-completion ordinals. Stronger owned items are reused only when they have the requested archetype/specialization, legal level/tier and at least the requested stats. Every old item stays owned; no donations or dismantling credits are assumed.
- [TowerAcquisitionEconomy.cs](../LL/tools/BalanceHarness/TowerAcquisitionEconomy.cs) calculates attempts, failures, entry items, assembly fragments and separate activity requirements. [The pace fixture](../LL/tools/BalanceHarness/Fixtures/tower-acquisition-pace.json) declares all behavioral assumptions. New unsupported entry currencies cause an error rather than receiving free income.
- Production `TowerEquipmentSupplyService` and the model now share the catalog's candidate ordering by source region and entry level. This is a behavior-preserving extraction; server progress and released-floor gates still apply. No issuance cadence or reward price changed.
- Earned equipment is converted to snapshots and prepared by `WorldTowerCombatRuntimeFactory` / `CombatPreparationPipeline`. Slot legality, tier eligibility and personal ownership are checked. Historical reference preparation preserves original positions, actor/item identities and ordered Essence recipes. The new cohort uses its own stable identities; historical RNG results are not attributed to those identities.

The final [native report](../TestResults/tower-acquisition-20260928/report-v2.json) contains the ledger, all floor loadouts, two independent inventory paths, newcomer loadouts, 102 effort rows, current execution identity and **406 current source hashes**. Report SHA-256: `6df2d6196bf51a024860d34e00a5cb4b347398de3378a8dbcd2222a2a3e63dc5`.

The separate [PowerShell verifier](analysis/verify-tower-acquisition.ps1) independently reads production JSON and checks source hashes, personal ownership, completion counts, cumulative bills, carried item IDs, acquisition arithmetic and dismantling returns. Its [receipt](../TestResults/tower-acquisition-20260928/independent-audit.json) is `VerifiedAcquisitionAccounting`. Native equipment-descriptor/preparation parity remains separate evidence; this is not a second combat implementation or a full reconstruction of old archives.

A second export to a fresh file reproduced the native report **byte-for-byte**. All five recorded runtime assembly hashes match the final verified build. An incorrect report pin was rejected before creating any audit receipt. All 57 local links in the three changed reports and scoped whitespace checks passed.

## Production constraints, not measured player behavior

Region-1 Goblin Mines I and Forgotten Catacombs I each cost **one matching sigil per attempt**, with no preceding dungeon or Tower-floor requirement. Grade II requires completing I; III requires II. Region-2 dungeons require server floor 10. Being accessible does not establish a character's ability to complete them.

The production regional pool gives a sigil chance of **1/4,320 per victorious eligible idle encounter**, then uniformly chooses one of the two region-1 families. The configured idle scheduling cadence is **10 seconds**. With perfect victory and retained/resolved activity, that is one random regional sigil per **12 expected idle hours**, or one particular family's sigil per **24 expected hours**. Idle combat animation length does not replace the scheduler's cadence. The configured 24-hour offline retention cap still requires activity to be resolved; the scenarios count eligible retained activity rather than arbitrary time logged out.

Assembly costs **10 fragments per sigil** and retains normal progression access requirements. Entry costs are consumed on every started run. Failed/expired runs lose pending loot and award no supply. Retreat can retain pending loot but earns no supply. Successful completion awards one bound chest regardless of dungeon grade, alongside ordinary rewards; opening costs no additional Cinders or Parts. First-completion monster cores are recorded separately and are not repeat sigil income. Ordinary equipment/blueprint rolls are not credited toward a guaranteed exact set.

Production Goblin Mines I routes have **10–13 rooms** and Catacombs I **11–14**, including entrance and boss. Completion requires sequential route/action choices and boss victory. The **48-hour expiry is not a completion duration**. No live run timings or observed dungeon/idle success rates were available in this local study. All 5/15/30-minute successful-run and 3/8/20-minute failed-run durations below are assumptions; room counts were not converted into invented timings.

Other sigil sources exist and are deliberately not treated as free recurring income:

| Source | Production quantity/constraint | Model treatment |
| --- | --- | --- |
| Region-1 quests | One Goblin sigil from *Between Day and Night*, one Catacombs sigil from *Crystal Currents* | No starting quest stock credited; these rewards are not repeatable income |
| Daily prophecy flat rewards | Common/Uncommon/Rare/Epic: 2/3/4/5 fragments per completed claim | Illustrative and constrained scenarios credit exactly one completed `Daily.Common` claim per day; completion activity itself is unmeasured |
| Weekly prophecy flat rewards | Uncommon/Rare/Epic: 8/10/12 fragments; additional caches and revelation milestones exist | Not credited or double-counted as daily income |
| Champion Market | 20 fragments for 140 Glory, at most twice weekly, Bronze rank required | No assumed Glory supply or purchases |
| Guild shop | 10 for 200 Favor, twice weekly at Market Office 2; 30 for 350 Favor, once weekly at Office 3 | No assumed guild unlocks, Favor or purchases |
| Tournament/event rewards | Placement, eligibility and event conditions apply | No expected placement or event availability inferred |

These sources can reduce random-sigil demand for actual players. Consequently, the random-only figures below are **conditional channel estimates, not unavoidable total acquisition times or estimates of the complete player economy**.

## Failures and pace sensitivity

For `N` successful completions and assumed independent stationary dungeon success probability `p`, expected attempts are `N/p`, with `N(1-p)/p` failures. Each attempt spends its entry sigil. Expected active dungeon hours are `(N × successful minutes + failures × failed minutes) / 60`.

For one seven-item two-handed set, accepting both regional sigil families with identical assumed run performance:

| Explicit scenario | Dungeon / idle victory probability | Success / failure minutes | Expected attempts / failures | Assembly-only fragments | Random-sigil-only idle hours | Active dungeon hours |
| --- | --- | --- | --- | ---: | ---: | ---: |
| Optimistic | 100% / 100% | 5 / 3 | 7 / 0 | 70 | 84 | 0.58 |
| Illustrative | 80% / 80% | 15 / 8 | 8.75 / 1.75 | 87.5 | 131.25 | 1.98 |
| Constrained | 50% / 50% | 30 / 20 | 14 / 7 | 140 | 336 | 5.83 |

An eight-item one-handed/off-hand set requires respectively **8/10/16 expected attempts**, **80/100/160 assembly fragments**, **96/150/384 random-only idle hours** and **0.67/2.27/6.67 active dungeon hours**. Restricting random sigils to Goblin Mines doubles idle-hour demand; it does not double assembly cost. Fractional counts are expectations, not actual fractional entry transactions.

The report also exposes fragment-only and combined **rate-budget days**. With one Common daily claim, a seven-item set's fragment-only budgets are 43.75 days at 80% dungeon success and 70 days at 50%. Combining assumed 8 eligible idle hours/day and that claim gives 11.93 rate-budget days in the illustrative case; the constrained 4-hour case gives 38.18. These divide expected demand by average income. **They are not expected first-passage times, observed calendar durations or a proposed desired farming target.** Discrete daily claims, variance, stock, simultaneous activities, account availability and the slowest party member prevent that interpretation. Active dungeon hours stay separate.

## Individually earned progression

The explicit cohort assumption is that an absolute party slot identifies the same person when present. Slots 6–10 join at floor 5, retain their items while absent at floors 6–7, and return at floor 8. Slots 11–15 join at floor 10. All newcomers begin without gear or sigil stock; all members have the separately declared level and ordered level-1 Essences. This models equipment acquisition, not leveling, Essence ownership or training.

| Floor | Active characters | New individually earned items | Reused equipped items | Cumulative owned items |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 5 | 35 | 0 | 35 |
| 2 | 5 | 0 | 35 | 35 |
| 3 | 5 | 0 | 35 | 35 |
| 4 | 5 | 35 | 0 | 70 |
| 5 | 10 | 35 | 35 | 105 |
| 6 | 5 | 0 | 35 | 105 |
| 7 | 5 | 35 | 0 | 140 |
| 8 | 10 | 35 | 35 | 175 |
| 9 | 10 | 0 | 70 | 175 |
| 10 | 15 | 105 | 0 | 280 |
| 11 | 10 | 0 | 70 | 280 |

The first five people each earn 28 items over four bands; the next five earn 21 over three bands; the last five earn seven. At assumed 80% dungeon success this is **350 expected attempts across all characters**, not 350 shared expedition clears or a party calendar time. Each first-cohort person needs 35 expected attempts and 525 random-only idle hours under the illustrative 80% idle-victory assumption. Leveling or Essence activities may also generate sigils; no double-counted activity savings are inferred.

The tier-1→2 replacement occurs before floor 10, after floor 9 clears and at level 50, using region 1. Floor 11's Rare reference is a repeating minimum evaluation band, not a forced downgrade. In the separate **all-new floor-11 party**, ten newcomers earn **70 personal Legendary tier-2 items from region 1**; no original member donates anything.

## Retained references and combat decision

Checked the strongest retained reference from each pinned confirmation:

| Reference | Exact items | Extra chests if adapting after buying authored gear | Reused authored items |
| --- | ---: | ---: | ---: |
| Floor 10: `floor10-retained-2/armor-and-health` | 105 | 60 | 45 |
| Floor 11: `floor11-six-1/armor/add/essence.shadow_imp` | 70 | 40 | 30 |

Those are **alternative adaptation paths**, not mandatory progression bills. The second ledger instead selects the exact floor-10 reference gear up front: it still earns **105 items before floor 10 and 280 cumulatively**, then carries all **70 equipped items into the exact floor-11 reference with zero new items**. Changing Essence recipes between these parties is an explicit, unfunded ownership/training assumption.

All 175 independently checked reference item descriptors match the current production supply output, including definition, tier, rarity, quality, rank, roll, style, behavior, stat allocation and stats. Both earned parties and unchanged historical recipes prepare through the current production Tower path. The native report retains historical recipe hashes and current prepared-participant hashes separately.

**No new combat was necessary to investigate a gear-descriptor difference: none was found.** No fights or combat seeds were allocated, no search was launched, and no boss was retuned. This is not current-build combat qualification or a new win-rate result. Historical 55/256 and 93/256 results remain evidence for their captured identities/builds and declared inventory assumptions. Do not apply them directly to the new cohort or claim acquisition acceptance from them.

Any later changed loadout, lower-Essence claim or combat-behavior change must use the existing bounded owner: freeze the plan and current hashes, preserve strong controls and qualification parity, consult the retained 835,319-value exclusion union before allocation, enforce time/output limits, retain incomplete evidence, and independently audit. No new combat launch boundary was introduced here.

## Repeat farming and dismantling

The oldest eligible region is economically dominant **for this supply channel** after floor 9: region-1 grade I yields tier-2 Legendary/Masterpiece/rank-5 equipment while region 2 begins with Rare/Standard/rank 2, at the same one-chest cadence. Higher grades have other rewards, but do not increase supply-chest count. This establishes reward-value dominance, not measured clear-time or win-rate dominance.

Production dismantling returns **157 Parts for a one-slot floor-10 supply item or 314 for a two-handed item**, compared with **17/34 Parts** for floor-11 region-2 supply gear. Selecting a two-handed item for surplus chests therefore maximizes this channel's Parts per successful clear. There is no Cinders refund, and entry sigils still cost resources. These values include the intrinsic rank value of freely awarded reinforcement; they are not refunds of money paid by the modeled owner.

The main ledger retains 210 items not equipped by the active floor-11 ten-person party, including 35 current items owned by its five absent members. Their total potential dismantling value is 11,080 Parts; **none is treated as disposable or credited as income**. No cadence, eligibility, dismantling or other economy change was made. A correction would need an explicit design decision informed by actual activity and the intended role of older dungeons.

## Verification, commands and changed files

**146 focused backend tests passed**, through the required wrapper, with zero failures/skips. Coverage includes acquisition arithmetic and invalid rates, production supply eligibility/retries/ownership, seven-item and eight-item loadouts, party growth and carryover, isolated newcomers, pinned references and tamper rejection, output preservation, dungeon access, ordinary acquisition, upgrade rules and Tower preparation. The build finished with 16 existing warnings and zero errors. The final [log](../TestResults/tower-acquisition-verification4-20260928.log) and [preserved TRX](../TestResults/tower-acquisition-20260928/backend-tests.trx) record the result.

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~DungeonAccessPolicyTests|FullyQualifiedName~CombatAcquisitionTests|FullyQualifiedName~EquipmentUpgrade|FullyQualifiedName~BalanceHarnessTowerPreparedTests' -ArtifactsPath 'TestResults/tower-acquisition-build-20260928'
dotnet 'TestResults/tower-acquisition-build-20260928/bin/BalanceHarness/release/BalanceHarness.dll' tower-acquisition-study 'LL/src/API/API.LL' 'LL/tools/BalanceHarness/Fixtures' 'LL/tools/BalanceHarness/Fixtures/tower-acquisition-pace.json' 'TestResults/tower-acquisition-20260928/report-v2.json' 'TestResults/tower-acquisition-20260928/references.json'
& 'Balance Harness/analysis/verify-tower-acquisition.ps1' -Report 'TestResults/tower-acquisition-20260928/report-v2.json' -ReportPin '6df2d6196bf51a024860d34e00a5cb4b347398de3378a8dbcd2222a2a3e63dc5' -Output 'TestResults/tower-acquisition-20260928/independent-audit.json'
```

These are historical commands; choose fresh output files on rerun. The optional [reference list](../TestResults/tower-acquisition-20260928/references.json) supplies the original handoff pins; omitting it runs the portable authored/newcomer study without requiring ignored archives. Initial local runs exposed duplicate normalized hash paths and two historical cell schemas; both were corrected and tested before the final report. The first wrapper attempt hit sandbox NuGet-configuration access; escalation succeeded. No required verification remains blocked. No frontend verification was needed because this continuation changed no frontend code.

Changed files: three new harness classes, its pace fixture and CLI dispatch; the new backend test file and independent audit script; the shared candidate method in the existing uncommitted supply catalog/service; this report and the implementation/handoff updates. Unrelated concurrent LiveOps/analytics edits and all old archives were preserved. No commits or staging were performed.

No migration, package, application configuration, database operation, deployment, seeding or API startup changed. The existing supply feature's future catalog/item-seed rollout requirements still apply. Guardian file SHA-256 remains `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`.

## Subsequent combat diagnostic

The [full-dungeon qualification](Tower-Dungeon-Acquisition-Qualification-20260928.md) adds fixed-loadout simulated outcomes and combat-only duration observations: 416 attempts, 52 matching full-run replays and 3,485 room combats. Previously earned gear supported later supply farming in these inputs, including the tier transition before floor 10 and stronger gear retained at floor 11. Starter-only controls failed; obtaining the first Rare set remains unresolved. Gear and Essence differences are confounded in that comparison and require matched controls before diagnosis.

This does not turn this report's assumed activity or duration scenarios into measured acquisition times. The eight-layout cells do not establish population success probabilities; the native diagnostic deliberately leaves seven-item expected attempts unset. There are still zero measured player samples. No inventory was reset, no new supply cadence accepted and no boss retuned. The new report and handoff contain the producing runtime, independent audit, 360-test regression result, latest seed exclusions and subsequent unrelated source-drift limitations.

## Subsequent first-supply inventory trajectories

The [first-supply continuation](Tower-First-Supply-Progression-20260928.md) adds matched four-Essence pre-dungeon recipes and follows individually earned items after each full run. Full Common gear completed **16/16 Goblin Mines acquisition paths** in seven attempts each; sparse quest armor plus an ordinary weapon completed **0/16**. All displaced equipment remains owned. This conditional first-set route complements this report's party growth and floor-10→11 carryover model; it does not change those ledgers or historical pins.

Starting gear remains conditional on specific ordinary drops and a random Armor Chest outcome. The latest targeted-family scenario credits one unspent quest sigil: seven no-failure attempts then need six additional matching sigils, equivalent to 60 fragments through assembly OR 144 expected eligible idle hours through random targeted drops at perfect idle success. This differs from this report's either-family, zero-stock 84-hour scenario. Neither is a normal-player forecast; gear and sigils can be earned in the same activity, so their separate expectations must not be added as independent costs. The new report retains 2,374 attempts, 18,472 room combats, 80 matching replays, independent audit and 364 passing regression tests. Zero measured player samples and no economy or boss changes.
