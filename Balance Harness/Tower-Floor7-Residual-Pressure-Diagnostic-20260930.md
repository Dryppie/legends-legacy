# Floor 7: residual Health and recovery pressure — 30 September 2026

**Latest floor-7 target-Health trial (30 September):** The [target-Health Springtide trial](Tower-Floor7-Health-Scaled-Springtide-20260930.md) closed **`NoEligibleHealthSpringtide`** after **46,080 fresh fights / 384 reservations**, retaining **120 recipes / eight compositions**. The 25% candidate’s best recipe won **3/128** (minimum 25); all other recipes at 25%, and every recipe at 27.5% and 30%, won **0/128**. **No confirmation or gameplay edit.** The independent audit checked every outcome and native preparation. **100 Python checks and 148 backend tests passed**, with four intentional skips, plus three native study fixtures. The new test assembly was verified against the exact archived combat DLLs after recovering the NuGet build-access issue. Exclusions: **920,956**. A descriptive review of **1,024 existing reports** shows one-healer slot-1 early-death counts increasing **40→82** and **39→102**. Next is a frozen **64-historical-replay diagnostic**, not allocated, to explain that survival/output loss and check condition interactions before another candidate. Target-MaxHealth base damage bypasses source-Power adjustments such as Weaken; whether that explains these failures remains unverified. Keep the original gates and expected progression. Floor 4 remains the latest applied change; no dungeon or acquisition work.

Target: primary LL World Tower (`LL/src/API/API.LL`) and offline Balance Harness. Continue the [closed penetration/Power trial](Tower-Floor7-Penetration-Pressure-20260930.md), preserving current live catalogs and all previous accepted changes.

## Prospective protocol

Replay exactly the six recipes and first sixteen saved seeds frozen in `TestResults/tower-floor7-residual-pressure-proposal-20260930.json`: **96 historical replays maximum**, with no replacement, retry, fresh acceptance fight or new seed. For compositions A/B, compare baseline equipment, six specialized items on one healer, and full Health + Regeneration. Preserve identities, ordered Essences, positions and level-40 / five-Essence / Unique-Exceptional-rank-4 tier-1 budgets. These profiles were chosen after observing screening results; the diagnostic remains descriptive.

Use the rejected archived **offense 0.60 / penetration 50** candidate, effective penetration 40, with original Springtide **1.0 / 0.20 per Abundance**. Authenticate the complete original 120-recipe family, isolated candidate delta, saved inputs, previous publication, compiled runtime and seed ledgers. Native replay loads the archive's content; never copy rejected content to live catalogs.

Require native input/result parity and exact equality of the complete saved report after removing only its event log. Reconcile damage, healing, regeneration and deaths against native statistics. Record Springtide's native integer target Health and MaxHealth **after** each hit. Do not infer pre-hit Health from damage or cumulative Abundance stacks from logged applications.

Keep the existing fixed before/after-40-second windows for comparison. Also partition events at the first original-party Death event: the killing hit precedes it, and later events at the same tick remain after it. Report exposure and original-slot damage output on each side; zero-exposure rates are null. Distinguish no-death fights and condition first-death medians on observed deaths. Diagnose survival loss, healing/regeneration and guardian recovery before choosing a separate finite gameplay candidate.

Run fresh diagnostic and penetration safeguards; authenticate/reuse unchanged **138 backend passes / four intentional skips**. Before execution, project twice the preceding 96-replay diagnostic's measured time and archive bytes. Require projections below 80% of **1,200 seconds / 2 GiB**. Bound each replay to 60 seconds and 64 MiB; keep attempts and process receipts. Monitor only supervisor output while native logs are active. Independently audit all closed outputs and event totals.

The existing complete-family acceptance requirements remain unchanged: two distinct compositions with at most eight specialized items on two characters, simultaneous lower win-rate bounds of at least 10%, and upper bounds no higher than 50% for every recipe. These selected historical replays do not satisfy or replace that acceptance test. Exclusions start and must end at **920,572**. No search, dungeon, acquisition, supply, migration, configuration, database or deployment work.

## Completed diagnostic

**All 96 native replays matched their exact saved inputs and complete combat/Tower results.** The independent collector verified **160,923 events**, including recipient damage/healing/regeneration totals and first-death times. It reproduced the event-order partitions and native Health snapshots. The six selected recipes each retained all 128 saved outcomes for the initial recount (**768 existing reports**); the event diagnostic used only the first sixteen declared seeds. **Zero new acceptance fights, seeds or gameplay edits.**

Springtide accounts for **83.71–85.21%** of original-party health damage before 40 seconds across these six profiles. One-healer specialization improves recovery but leaves first-death medians at **48s**. Health + Regeneration buys more survival: light-character Max Health increases **2,359 → 3,307** (+40.2%) and prepared regeneration **99.44 → 128.67** (+29.4%), while Resistance stays **23.401081**. Tenacity falls **116.92 → 0**, so this remains a multi-attribute equipment comparison. The healer's specialization instead supplies **409.22998 Restoration** without increasing its Max Health.

| Composition / equipment | Saved diagnostic wins / 16 | Median first death, when observed | No original-party death | Output before / after first death (damage/s) |
| --- | ---: | ---: | ---: | ---: |
| A / baseline | 0 | 48.00s | 0 | 192.2 / 134.0 |
| A / one healer | 2 | 48.00s | 2 | 174.4 / 139.3 |
| A / Health + Regeneration | 5 | 60.00s | 3 | 180.3 / 102.0 |
| B / baseline | 0 | 43.85s | 0 | 190.7 / 133.8 |
| B / one healer | 2 | 48.00s | 2 | 172.0 / 127.4 |
| B / Health + Regeneration | 6 | 48.60s | 4 | 177.6 / 136.2 |

These 16-seed counts are selected historical descriptions, **not new win-rate estimates or acceptance evidence**. The complete 128-seed screen remains authoritative for rejecting the candidate: one-healer A/B won **12/128 and 18/128**, while Health + Regeneration won **62/128 and 66/128**. First-death medians exclude no-death fights, shown separately. Output rates pool damage divided by exposure only within fights that have a death; changing fight phases, conditions and remaining actors prevent a causal interpretation of the rate difference.

The native per-hit Health snapshots show why survival around the fourth Springtide matters:

| Slot-3 equipment | A: remaining Health after 48s Springtide | B: remaining Health after 48s Springtide |
| --- | ---: | ---: |
| Baseline | 0.31% (13 hits) | 0.51% (9 hits) |
| One healer | 9.32% (16 hits) | 6.31% (15 hits) |
| Health + Regeneration | 15.21% (16 hits) | 16.74% (16 hits) |

These are means over actual slot-3 damage hits at 48s, including fatal hits. Characters already dead contribute no hit, and fights may end earlier, so the hit counts differ. This is not a matched-survivor or single-stat experiment. The full Health + Regeneration parties also regenerate roughly **4,038 / 4,062** Health before 40s, versus **2,645 / 2,611** for one-healer specialization. In both gear profiles, guardian healing before 40s is **584**; the stronger profile does not win by reducing that early healing. Its longer survival preserves party output further into the fight. Healing, condition effects and post-death damage changes remain part of the explanation.

## Health-snapshot reporting correction

The first diagnostic reducer selected every event carrying Springtide's source ID. That included Death notifications emitted by lethal hits, which added duplicate zero-Health samples to per-hit means. The closed replay archive and initial audit remain intact. The helper now selects only native **Damage / DamageCrit** events for per-hit Health reporting, including fully absorbed damage events. A regression test proves a Death notification cannot add another hit.

The separate read-only correction reprocessed the same 96 closed logs: **2,474 source-associated snapshots → 2,132 actual damage snapshots**, excluding exactly **342 Death notifications**. Independent extraction using positive incoming raw damage reproduced the corrected selection and Health values. Every other analysis field remained byte-for-data equal, and every full saved native report still matched. No new replay, attempt, seed, outcome or acceptance decision was generated. Frozen helper/test versions were preserved under `TestResults/tower-floor7-residual-health-correction-20260930/before`; the corrected review supersedes only the initial audit's per-hit Health aggregates.

## Proposed next balance trial

**Test target-MaxHealth scaling for Springtide's base damage**, while retaining its authored **0.20 source Power per Abundance stack**. Use exactly three candidate base fractions: **25%, 27.5% and 30%**. Keep guardian offense at **0.60 of the original source**, penetration at **50** (effective 40), and all other settings unchanged. Build from the original current-live 120-recipe source; do not multiply these factors into the already reduced rejected archive.

This tests whether the base attack can exert more comparable pressure across Health pools, easing the smaller baseline/one-healer pools while applying more damage to the larger Health + Regeneration pools. It uses the existing `tower-health-pressure-candidate-v1` representation; no new shared combat or regeneration rule is required. All three plans passed read-only contract validation against both live and original archived catalogs. **No candidate catalog was materialized and no candidate fight was run.**

At the saved guardian Power **747.93256**, simple pre-mitigation arithmetic gives the following changes relative to the rejected 0.60 candidate, assuming four Abundance stacks:

| Target-Health base | Light baseline: raw damage change at four assumed stacks | Light Health + Regeneration: raw damage change |
| ---: | ---: | ---: |
| 25% | -11.75% | +5.85% |
| 27.5% | -7.37% | +12.00% |
| 30% | -2.99% | +18.14% |

These ratios are **static arithmetic, not measured damage effects or win-rate predictions**. They omit crits, barriers, block, mitigation changes, dynamic attributes, healing and altered fight duration. Tank/controller Health is higher too, so the same change can increase pressure on those characters. Retain every original gear profile, high-armor control, limited-healer route and whole-family ceiling. Regeneration remains advantageous; this proposal does not claim to isolate or eliminate that advantage.

The frozen next proposal permits three independent **128-seed screens** and at most one **160-seed confirmation**, with the unchanged **25–44/128** and **30–57/160** simultaneous gates over **120 recipes / eight actual compositions**. Require at least two actual compositions with at most eight specialized items on two characters, plus every recipe's upper bound at or below 50%. Select highest second-best limited-composition lower bound, then smallest maximum upper bound, then lower target-Health fraction. Maximum future scope: **65,280 fights / 544 reservations**; measured resource admission precedes each allocation. No fourth candidate, retry, extension or pooling. Require native target-Health/source-Power checks and isolated input/full-replay parity before any confirmed local application. **Status: proposed, not materialized or allocated.**

## Verification and scope

**64 distinct Python safeguards pass:** five base diagnostic checks, 20 mixed-equipment checks, 16 original resistance-diagnostic checks, 12 penetration checks and the corrected 11-case residual-pressure suite. The initial 10-case residual suite also passed and remains preserved, for **74 total test executions** across the two runs. All 96 native CLI replays succeeded; no new backend fixture run was necessary because C# and compiled runtime assemblies were unchanged. The previous **138 backend passes / four intentional opt-in skips** were authenticated and reused.

Owned replay processes took **136.288 seconds in total**; the closed diagnostic archive is **280,910,616 bytes**. The prospective two-times projections were **273.438 seconds / 505,202,078 bytes**, below the declared admission limits. Every owned process exited with no timeout or remaining child. **Zero retries**, no blocked verification command, and exclusions remain **920,572**. The reporting correction and static candidate review added no combat or seeds.

Changed maintained files: new `analysis/diagnose-tower-residual-pressure.py`, its regression tests, this report, the preceding penetration-trial follow-up notice, continuation handoff, balance status, gear coverage and both harness guides. Current live catalogs and compiled runtime remain unchanged. No migration, configuration, database or deployment changes. Floor 4 remains the latest applied balance change. Search, expected progression gear, Essence budgets and acquisition assumptions are preserved.

Commands used bundled Python with `-B -X utf8`:

```text
TestResults/tower-floor7-residual-pressure-prepare-20260930.py
TestResults/tower-floor7-residual-pressure-tests-20260930.py
Balance Harness/analysis/diagnose-tower-residual-pressure.py --plan TestResults/tower-floor7-residual-pressure-proposal-20260930.json --entry TestResults/tower-floor7-residual-pressure-entry-20260930/entry.json --artifacts TestResults/tower-floor4-health-scaled-hall-precision-runtime-20260930 --protocol Balance Harness/Tower-Floor7-Residual-Pressure-Diagnostic-20260930.md --output TestResults/tower-floor7-residual-pressure-diagnostic-20260930
TestResults/tower-floor7-residual-pressure-collect-20260930.py
Balance Harness/analysis/test-tower-residual-pressure-diagnostic.py
TestResults/tower-floor7-residual-pressure-review-20260930.py
TestResults/tower-floor7-residual-pressure-publish-20260930.py
```

Publication checks local Markdown links, `git diff --check`, source/runtime/catalog bindings and the unchanged seed union. Native commands and process receipts remain in the immutable diagnostic archive.

## Evidence

- Closed diagnostic: `TestResults/tower-floor7-residual-pressure-diagnostic-20260930`, manifest **`5e1f0a5f9bd50a8418d6f936bd1cad6a7e501645cbc9f689dbd4cb0012739a9a`**.
- Independent event audit: `TestResults/tower-floor7-residual-pressure-evidence-20260930.json`, SHA **`667008449f3410ff3dd711e989c1c14f36932155a21ca216264deb1c28f6388b`**.
- Corrected per-hit review and static arithmetic: `TestResults/tower-floor7-residual-pressure-review-20260930.json`, SHA **`8a2b478c8c0b9b73ef4fd61df0b62660bee181f06a7b6139b83408d8737265b8`**.
- Next unallocated proposal: `TestResults/tower-floor7-health-scaled-springtide-proposal-20260930.json`, SHA **`908268fe8684ac893ffa531e84ac818be7d3a833dfd85393c3b9235da8aa106d`**.
- Entry snapshots: `TestResults/tower-floor7-residual-pressure-entry-20260930`.
- Publication: `TestResults/tower-floor7-residual-pressure-publication-check-20260930.json`.
