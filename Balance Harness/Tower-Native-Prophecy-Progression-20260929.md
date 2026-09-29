# Native prophecy offers and Tower progression — 29 September 2026

## Finding

Production offer generation, daily acceptance, objective progress, claims and fragment assembly now fund a reproducible personal entry policy. The fresh paired comparison completes seven selected supplies in **16/16 perfect-idle histories in both arms**, and **15/16 → 16/16 four-in-five histories** when the earned prophecy sigils are available. The previously short character buys the seventh item. No already-completing history reaches an earlier activity checkpoint.

This closes one conditional entry-resource shortage. It does not establish expected player acquisition time: **zero measured player samples**. Sixteen identities under two supplied idle outcomes are not 32 independent players. Seven supplies remain an authored purchase target, not a measured minimum Tower loadout. No boss, supply cadence, repeating gear curve or supported search changed.

## Production offers and fixed choice

The [offer adapter](../LL/tools/BalanceHarness/TowerProphecyOffers.cs) calls production `ProphecyService.GetOverviewAsync` and `AcceptAsync`, followed by the existing native progress, claim and assembly services. In-memory repository boundaries preserve production definition order, persisted offers, one daily choice, automatic weekly acceptance, UTC expiry and weekly Favor reset. Reads cannot reroll offers; a second daily acceptance and duplicate claims are checked for rejection.

The [declaration](../LL/tools/BalanceHarness/Fixtures/tower-prophecy-offers.json) fixes one visible policy before combat: choose the offered daily `KillCreatures` objective with the most fragments, then lowest target, then ordinal definition ID. Abstain when none is offered. There are no rerolls or policy sweeps. Eligible daily kill targets/rewards are 300/2, 600/3 and 900/4. Native weekly kill or unfiltered win objectives can also receive the supplied idle events; weekly dungeon, Essence and treasure objectives receive zero fabricated progress. This remains an intentionally restricted activity channel, including during the combat comparison: actual dungeon events are not fed back into prophecy progress.

The same assumed idle victories fund both random rewards and prophecy progress. Each victory supplies one killed creature and one enemy defeated; defeats supply neither. Activity is continuous at ten seconds per encounter from Monday 28 September 2026, 00:00 UTC. Daily selection occurs at reset, claims occur immediately at the objective threshold, and whole Mines sigils are assembled at the existing 6/24/72/240 cadence-hour checkpoints. Ten fragments buy one sigil. These calendar inputs and attendance are assumptions, not observed behavior. Dungeon combat time is accounted separately and does not advance this source calendar.

Native Soulstones, Fate Echo, caches and XP remain recorded. Caches are unopened, currencies unspent and XP unapplied at the fixed level-30 boundary; no leveling benefit enters combat. Previously counted quest sigils are not duplicated, and unfunded guild/arena purchases contribute nothing. Unsupported weekly objectives are not silently replaced with favorable ones.

The independent [offer auditor](analysis/audit-prophecy-offers.py) reconstructs production eligibility, weighted SHA-256 selection, slot fallback and unique definition/category preferences. It checks the exact owner/period/slot/scope/initial hash inputs, choice, acceptance, calendar, progress, claim timestamps, rewards and integer assembly. In-memory instance GUIDs are normalized for reproducible logs; they are not inputs to offer selection. Owner identities and calendar seeds are scenario inputs, not newly allocated combat samples.

## Seed-free preparation gate

The first [projection](analysis/run-tower-prophecy-projection.py) used the 32 archived included-loot histories, stopped outcome transfer at the first changed entry and prepared the exact retained loadout through production. It generated **760 offers across 236 character-days**, abstained on 66 days and claimed 222 objectives/milestones. All **five** changed entry states passed preparation. No new equipment, combat outcomes or combat seeds were supplied by this projection.

At the archived 72-hour endpoints, eleven histories assembled zero additional sigils and one assembled one. At the 240-hour endpoints, three assembled one, eleven assembled two and six assembled three: **44 additional sigils across the 32 source ledgers**. This replaces the older guaranteed-offer assumptions of three or four sigils per ten-day history; neither result is a population income estimate. The five first differences switch Catacombs to Mines: controller path 3 in both idle scenarios, guardian path 2 four-in-five, striker path 0 four-in-five, and restorer path 2 perfect. Only the restorer change occurs at 72 hours; the others occur at 240.

## Fresh paired combat

The [combat declaration](../LL/tools/BalanceHarness/Fixtures/tower-prophecy-combat.json) keeps full-slot readiness, both dungeon families, Mines-first spending, earned ordinary equipment, blueprint stock, personal identities, purchase order and the original attempt/activity bounds in both arms. `no-prophecy-credit` projects the identical source ledger for auditing but withholds its sigils from entry stock. `offered-kills-no-reroll` credits only newly assembled whole sigils. No other prophecy reward improves either loadout.

New dungeon panels are shared across arms by family and within-family attempt ordinal. The baseline is this fresh study's baseline, not the older loot study's 14/16 result. Historical reward prefixes and exact inventory ownership remain checked. Every earned item is retained; the existing selection policy decides what is equipped. The earlier party-growth and floor-10→11 carried-inventory evidence remains historical and was not rerun or replaced.

| Supplied idle victories | Policy | Seven-supply completions | Attempts | Failed entries | Supplies earned | Retained dungeon equipment |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Perfect | No prophecy credit | 16/16 | 112 | 0 | 112 | 49 |
| Perfect | Native offered kills | 16/16 | 112 | 0 | 112 | 51 |
| Four-in-five | No prophecy credit | 15/16 | 112 | 1 | 111 | 50 |
| Four-in-five | Native offered kills | 16/16 | 113 | 1 | 112 | 50 |

Both arms complete eight perfect and six four-in-five histories at 72 cadence hours. At 240, the baseline completes another eight perfect and nine four-in-five; the funded arm completes eight and ten. No completion moves earlier or later; one censored history becomes complete. The funded arm assembles **40 sigils** by its actual stopping horizons: 18 perfect and 22 four-in-five. The audit's 80 total includes the baseline's withheld source projections and must not be described as 80 funded entries.

`striker--0--four-of-five` wins all six baseline entries but has no sigils left. Its three earned prophecy sigils permit six Mines entries plus one Catacombs entry, all successful, earning the seventh supply at the final checkpoint. Four other paired paths substitute Mines entries and retain their previous completion checkpoint. Total spending changes from 176 Mines / 48 Catacombs to 183 / 42. Extra stock can remain unused; every assembled sigil is not necessarily spent.

The only acquisition failure per arm is the identical first Mines entry for `restorer--0--four-of-five`, classified by production as `Combat Readiness`; that history later completes. No Catacombs attrition failure occurs in this fresh panel. All 32 Mines and 32 Catacombs already-owned controls complete. These counts do not invalidate earlier panels with attrition, prove universal readiness or establish population success rates. The study does not tune after observing these results.

Execution completed **449 acquisition attempts, 64 controls, 120 full-run replays and 5,408 room combats**, awarding 447 selected items. The independent audit reconstructed 677 entry decisions, 146 coverage waits, 449 ordinary-loot ledgers, 894 reward retry checks, 64 retained historical reward prefixes, 215 identical-input run pairs, 3,858 prepared rooms and all 64 final preparations. Its 200 retained dungeon items and 125 blueprint items are personally owned; there are no donations. The 228 rebatched idle reward windows preserve outputs. No audit executes new fights.

## Implementation and verification

Target: the primary LL game's balance harness and tests. Twelve source files changed:

- Extended [entry-source adapter](../LL/tools/BalanceHarness/TowerEntrySources.cs), [entry projection](../LL/tools/BalanceHarness/TowerEntrySourceStudy.cs) and [activity study](../LL/tools/BalanceHarness/TowerActivityStudy.cs). Legacy modes remain available. Native-offer output stops at each history's actual horizon; an early-stop schedule is rebuilt and its checkpoint prefix verified to prevent future-day progress leakage.
- Added [offer policy](../LL/tools/BalanceHarness/TowerProphecyOffers.cs), [offer fixture](../LL/tools/BalanceHarness/Fixtures/tower-prophecy-offers.json), [combat fixture](../LL/tools/BalanceHarness/Fixtures/tower-prophecy-combat.json) and [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessProphecyOfferTests.cs).
- Added the [offer auditor](analysis/audit-prophecy-offers.py), [projection owner](analysis/run-tower-prophecy-projection.py), [projection verifier](analysis/verify-tower-prophecy-projection.py), [combat owner](analysis/run-tower-prophecy-combat.py) and [combat verifier](analysis/verify-tower-prophecy-combat.py). The existing corrected dungeon reward PRNG auditor is reused unchanged.

This report, the handoff, implementation report and a historical source-report forward link are the documentation changes. **459 backend regression tests passed; ten opt-in studies intentionally skipped.** The owned projection and combat study each passed their separate test and independent audit. Initial sandbox NuGet-config access failed; the required wrapper succeeded with escalation. No required command remains blocked. No frontend code changed, so frontend tests were not repeated.

Commands executed; preserve these completed directories rather than rerunning into them:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-prophecy-offers-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessProphecyOfferTests|FullyQualifiedName~BalanceHarnessEntrySourceTests|FullyQualifiedName~BalanceHarnessDungeonLootTests|FullyQualifiedName~BalanceHarnessEntryReadinessTests|FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon|FullyQualifiedName~Prophecy|FullyQualifiedName~SigilFragment'
$env:PYTHONDONTWRITEBYTECODE = '1'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python 'Balance Harness/analysis/run-tower-prophecy-projection.py' --owner TestResults/tower-prophecy-projection-owner-20260929 --output TestResults/tower-prophecy-projection-study-20260929 --artifacts TestResults/tower-prophecy-offers-build-20260929
& $python 'TestResults/tower-prophecy-projection-owner-20260929/inputs/Balance Harness/analysis/verify-tower-prophecy-projection.py' --owner TestResults/tower-prophecy-projection-owner-20260929 --manifest-pin de4780eb3ab37cf08e6531b981ee492330e81893f13c385315c59ab57fd62c1b --receipt TestResults/tower-prophecy-projection-owner-20260929/independent-audit.json
& $python 'Balance Harness/analysis/run-tower-prophecy-combat.py' --owner TestResults/tower-prophecy-combat-owner-20260929 --output TestResults/tower-prophecy-combat-study-20260929 --artifacts TestResults/tower-prophecy-offers-build-20260929 --projection-owner TestResults/tower-prophecy-projection-owner-20260929 --projection-pin de4780eb3ab37cf08e6531b981ee492330e81893f13c385315c59ab57fd62c1b
& $python 'TestResults/tower-prophecy-combat-owner-20260929/inputs/Balance Harness/analysis/verify-tower-prophecy-combat.py' --owner TestResults/tower-prophecy-combat-owner-20260929 --manifest-pin 820ecd26646eb4d1b00b59c14db489054dee86d629b2d83427dc47064027e3c5 --receipt TestResults/tower-prophecy-combat-owner-20260929/independent-audit.json
```

## Frozen evidence and reservations

The projection audit checked 1,898 hashes; the combat audit checked 1,960. Both owners freeze source, content, runtime, declarations and logs before execution. The combat owner requires the passing projection and compatible live adapter/plan/auditor hashes. Both processes exited zero, did not time out and left zero active children. Projection ran for 3.078 seconds within its 180-second / 32-MiB bound. Combat ran for 40.422 seconds within its 900-second process, 840-second native and 256-MiB output bounds; no retry or sample extension occurred.

| Artifact | Trusted SHA-256 |
| --- | --- |
| [Projection manifest](../TestResults/tower-prophecy-projection-study-20260929/files.json) | `de4780eb3ab37cf08e6531b981ee492330e81893f13c385315c59ab57fd62c1b` |
| [Projection result](../TestResults/tower-prophecy-projection-study-20260929/result.json) | `fce6c9f4935d88b4c5b447b8d000a8c75fc3727e220dd57471e653b48072dce6` |
| [Combat manifest](../TestResults/tower-prophecy-combat-study-20260929/files.json) | `820ecd26646eb4d1b00b59c14db489054dee86d629b2d83427dc47064027e3c5` |
| [Combat result](../TestResults/tower-prophecy-combat-study-20260929/result.json) | `def0dbea0d16a39b12f857a61f0f3563e683e816bf301a9d3206e5ccba1fb5ed` |
| [Combat seed ledger](../TestResults/tower-prophecy-combat-owner-20260929/seed-ledger.json) | `021a8ab1b4527222e738d4e61994e63e7ca21d439e6423e79292e2c223102cc3` |

See the [projection audit](../TestResults/tower-prophecy-projection-owner-20260929/independent-audit.json), [combat audit](../TestResults/tower-prophecy-combat-owner-20260929/independent-audit.json), [derived comparison](../TestResults/tower-prophecy-combat-owner-20260929/derived-summary.json), [regression TRX](../TestResults/tower-prophecy-offers-build-20260929/regression-tests.trx), [projection TRX](../TestResults/tower-prophecy-projection-owner-20260929/export-tests.trx), [combat TRX](../TestResults/tower-prophecy-combat-owner-20260929/study-tests.trx) and [final checks](../TestResults/tower-prophecy-combat-owner-20260929/final-checks.json). `TestResults` remains ignored and local.

The seed exclusion union increases from 864,455 to **870,695**. All **6,240** new values remain reserved, including **5,824 unconsumed** values; only 416 distinct allocated values were used. Every prior consumed and unconsumed reservation remains excluded. Sixteen historical owner/Armor Chest identities remain intentionally paired, not fresh independent combat evidence. Preserve old archives, original pins and the prior loot auditor amendment.

No production source, deployment configuration, dependency, migration, shared database, seeding, API startup or deployment changed. The original supply item-seed rollout requirements remain. The guardian content remains `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`. All unrelated concurrent work is preserved; nothing was staged or committed.

## Next work

The offer-availability gap is now implemented and verified. The next substantial model gap is character growth: apply earned XP and mastery through production progression, while explicitly carrying required Essence ownership/training and resulting objective eligibility. Begin with a seed-free state/preparation audit and reuse retained inventories, including the stronger floor-10 gear carried through floor 11. Additional cache choices or guild/arena income require their own declared funded policy. A complete journey must reconcile dungeon activity with the source calendar rather than treating the supplied idle clock as measured attendance.

Separately, compare personally earned loadouts with the existing floor requirements before assuming every character needs seven new supplies. Prepare changed states first and use fresh bounded combat only when warranted; preserve the supported search and all reservations. Measured activity data and an explicit progression-time goal are still required for normal-player pace or supply-cadence decisions. This result provides no reason to retune bosses.

## Subsequent earned-growth qualification

The [growth qualification](Tower-Earned-Growth-Qualification-20260929.md) now applies native character and Essence XP up to the first archived dungeon entry and audits a separate mastery-only prefix. All 32 grown states differ and all 64 baseline/grown preparations pass; mastery changes a later entry in 21 histories. This is zero-fight qualification with explicit outcome cutoffs, not a replacement combat study. The frozen 16/16 result above remains conditional on fixed character/Essence levels. The next fresh comparison must integrate dungeon XP claiming, mastery and a declared clock. Historical pins and all reservations above are unchanged.
