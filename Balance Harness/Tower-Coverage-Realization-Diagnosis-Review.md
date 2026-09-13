# Coverage realization: saved v9 construction and replay diagnosis

The zero-combat diagnosis is complete. It checked **576 saved discovery parties, all 9,216 saved trial records, 18 validation cells and exactly four existing detailed replays**. V9 reliability remains **Fail (0/3)**; all twelve generated finalists remain **0/256**, and the tested-family assessment remains **Inconclusive**. This analysis adds **no fights, seeds, constructor calls or confidence update**.

The key finding is a gap between coverage labels and realized support. The constructor usually placed its requested nominations, but those counts do not describe activation timing, recipient reach, effect magnitude or whether control crosses a boss's stagger threshold. No new search policy, Essence filter or copy quota is selected from this diagnosis. The [next reporting plan](Tower-Coverage-Diagnostics-Plan.md) proposes reusable explanatory diagnostics before another behavioral experiment.

## Preserved scope

Target: the offline BalanceHarness and the closed [v9 protection-compatibility study](Tower-Protection-Compatibility-Review.md). All **eleven preceding scientific packages plus the v9 Markdown verification package** were checked against their receipts and exact inventories. All **70 earlier sealed reviews, 2,231 C# sources, five producing assemblies, sixteen content files and both catalogs** were preserved. Current documentation hashes matched the latest Markdown receipt before editing.

The retrospective [protocol](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/protocol.json) acknowledges the already known zero-win results and replay summaries. It limits the diagnosis to saved content, trials and four previously selected first-validation-seed replays, with zero new simulations, constructor calls or seeds; 120 seconds of measured analysis and 64 MiB of output. It does not blind or confirm a new hypothesis. Every generated recipe, all six controls, both methods and all zero-win outcomes remain included.

Kharad remains **Health 3.04881408 / Power 3.85370128**. The ten level-40, tier-1, rank-2 Standard characters, fixed gear, five level-1 unascended/unevolved Essences each, no styles/contributions and 80-Essence hypothetical pool remain unchanged. Rare acquisition and near-optimality are unverified. The **131/256, 136/256 and 134/256** observed ceiling breaches and the separate **479/1,000 Inconclusive** confirmation remain separate sealed studies; 126/256 in v9 does not erase them.

## Construction and ranking

All **576 discovery candidates had zero wins across 4,608 saved fights**. Refinement therefore improved existing tie-break proxies, chiefly remaining guardian health, without finding a winning starting point. The three new-arm primaries improved guardian-health remaining by **10.79–16.29 percentage points** over their best fresh candidates. These are adaptive comparisons, not causal operator effects or new paired tests.

| New-arm generation seed | Fresh parties | Median fresh characters with a core | Best fresh → final guardian-health improvement | Refinements better than their best parent |
| ---: | ---: | ---: | ---: | ---: |
| -988509643 | 42 | 5.0 | 16.29 pp | 19/54 |
| 1383973713 | 42 | 5.0 | 13.92 pp | 19/54 |
| -1693253229 | 43 | 5 | 10.79 pp | 17/53 |

The six arms contain **253 fresh parties**, of which **220** use guided coverage. Their saved intents record **5,435/5,443 requested placements**. All requested recovery, protection and recurring-control placements succeeded; the eight missing placements belong to enemy-pressure in one arm. This gives no basis for blaming general placement failure or imposing more copies of a named provider. Intents record category counts, not nominated provider IDs; overlapping roles and later cores/filler prevent exact nomination attribution from the final recipe alone.

Fresh parties typically equip recovery providers on **8–9 characters**, and have a median **five characters with at least one content-derived core**. Those structural counts do not establish sufficient healing or effective timing. Two new-arm fresh parties already contained cores on all ten characters and still won no discovery fights. [Search structure](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/search-structure.json), [coverage exposure](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/coverage-exposure.json) and [all cell metrics](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/saved-cell-metrics.json) retain every party and validation cell.

## Static categories cover different mechanics

The audit resolves every effect evidence key in the full 71-entry v8 collection and labels the two nominations excluded by v9. The table shows v9's 69 retained features:

| Category | Providers | Effect routes | Providers whose category routes target only Self | Providers whose category routes require a death event |
| --- | ---: | ---: | ---: | ---: |
| attack-enabler | 5 | 6 | 3 | 0 |
| enemy-pressure | 29 | 34 | 0 | 0 |
| protection | 14 | 20 | 11 | 0 |
| recovery | 17 | 18 | 13 | 2 |
| recurring-control | 4 | 4 | 0 | 0 |

Recovery includes party healing, single-ally healing, self-healing on damage, one-use emergency heals, death-triggered self-healing and regeneration changes. **13 of 17 recovery providers are self-only**, but this is not evidence that self-recovery is bad: the strongest saved control's recovery providers are also self-only. No self-heal removal rule follows from these observations.

The four recurring-control providers also differ. Three have unconditional effect predicates, while one requires its current target below 30% health. Their ability kinds, first activation, cooldowns, target selectors, chance and stagger power differ. Being in one category does not make them interchangeable. The full [authored route audit](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/coverage-audit.json) retains targets, operation/value fields, selected triggers, health predicates and bounded uses, without learned weights.

## Fixed replays: control timing and actual restored health

These are the three discovery-selected new primaries and the original fixed anchor on the same old seed **759537949**. Validation medians use all 256 saved trials per cell. The last two columns use the **same first 40 seconds** for all four replays, rather than comparing unequal lifetimes.

| Fixed replay | Median validation first death | First guardian stagger break in this replay | First death in this replay | Health restored by healing before 40 s | Health restored by regeneration before 40 s |
| --- | ---: | ---: | ---: | ---: | ---: |
| New primary 1 | 42.9 s | 40.0 s | 42.9 s | 267 | 2253 |
| New primary 2 | 42.9 s | 40.0 s | 42.9 s | 312 | 2435 |
| New primary 3 | 42.0 s | 40.0 s | 42.9 s | 899 | 2128 |
| Fixed anchor | 70.9 s | 20.0 s | 69.0 s | 842 | 2350 |

The three primaries first broke Kharad at **40 seconds**; the anchor broke him at **20 seconds**. At 20 seconds, the new primaries' logged accumulated stagger was **240, 120 and 170** against the initial **250** threshold. The anchor reached 250. Contributions are capped at the threshold, so the final application can log less than its authored power.

All three primaries show `StaggerRecovered` at tick **429**, then fatal **Physical Crushing Verdict** damage on that same tick. The respective first-death slots are **6, 1 and 1**, with logged health damage **891, 1,573 and 772**. The anchor's first death occurs at tick **690**, with a Physical basic attack. This establishes recorded timing and source, not that another stagger schedule would prevent death or produce a viable build. Whole-fight damage shares cannot explain an individual fatal hit; overkill remains possible in damage telemetry.

Recovery is also insufficiently described by equipped counts. Before their own first deaths, the primaries restored **301, 334 and 938** health through heal events; the anchor restored **2,511**, over a longer pre-death period. At the equal 40-second cutoff, the third primary actually restored more through healing than the anchor (**899 versus 842**), yet still died earlier. This counterexample prevents treating raw healing volume as a sufficient explanation or objective.

The [corrected replay analysis](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/replay-coverage.json) matches every observed category application to owner and source, records recipients and timestamps, and retains all ten-second windows and complete guardian stagger-event sequences. Heal/HealCrit and regeneration sums independently reconcile to saved combat statistics. Regeneration modifiers have no separate per-provider causal credit. Nonzero application is not proof of efficacy, and absent logs are not proof of inactivity.

## Runtime semantics and reporting limitation

Source review confirmed that the authored effect chance and the runtime Stun/Freeze application gate are separate. Ward, Unstoppable, conditions, stagger recovery immunity and the maximum break count can further prevent effective control. Stun/Freeze against a stagger-enabled boss contributes stagger instead of directly applying ordinary Stun/Freeze. Kharad uses a **250 initial threshold, 30-tick break, 20-tick recovery, 35% threshold growth and four-break maximum**. Counting a control provider or reading only its authored chance cannot establish a reliable break schedule.

These rules are linked to frozen source hashes in [runtime semantics](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/runtime-semantics.json). `RestoreHealth` records the actual health change; its event sums match healing statistics. Coverage construction only records category placement intent. It does not record the timing, magnitude or recipient effect of that placement.

An initial diagnostic decoder matched authored effect IDs and condition identities but missed stagger logs, which use ability names. Source review corrected that mapping and checked that each matched ability has one relevant control route. The superseded `*-draft.json` files remain labeled drafts; the corrected report and independent verifier account for every stagger contribution/break in all four replays. No inactivity conclusion uses the draft omission.

## Decision, verification and handoff

**No new behavioral hypothesis is selected.** The evidence supports improving diagnostic visibility, not replacing all self-recovery, forcing a copied control composition, learning provider weights, changing the ranking objective or assuming earlier control guarantees wins. The [proposed diagnostics increment](Tower-Coverage-Diagnostics-Plan.md) is a zero-new-combat reporting task; it is not v10 and does not authorize another pilot.

Verification independently recomputed all **9,216 records**, the **576-party ranking/provenance and coverage summaries**, all **18 validation cells**, effect definitions and selected triggers, source/recipient matches, stagger sequences and all four fixed replays. It checked prior package inventories/hashes, unchanged sources/assemblies/content/catalogs, Markdown files/anchors and `git diff --check`. Measured analysis took about **5.56 seconds**, with about **19.1 MiB** retained before documentation and final receipt, inside the frozen limits. The [final receipt](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/final-verification.json) records exact totals and hashes.

Commands: bundled Python with `-B` for `prepare.py`, `analyze-records.py`, `analyze-coverage.py`, `analyze-semantics.py`, documentation generation and independent `verify-final.py`; source reads and `git diff --check`. No backend code changed, so no backend test or combat was rerun. The prior **196/196 passing** regression receipt was verified as historical evidence, not reported as a new test run. No required command remains blocked.

Changed files are the new diagnostic scripts/artifacts, this review, the reporting plan and active Markdown handoffs/plans/README. There are no gameplay, generator, configuration, migration, database or deployment changes. All **18 v9 seed-free recipes** remain in the original package. The [copied ledger](../TestResults/balance/tower-coverage-realization-diagnosis-20260913/seed-ledger.json) adds no seeds and retains **471,656 distinct exclusions across every array**, including unused and constructor-only reservations. Retuning, default/catalog promotion, larger campaigns and floors 6–11 remain deferred.
