# Kharad linked Health/Power screen — 12 September 2026

The frozen screen selected **1.04 × current Health and Power** for fresh confirmation. At this setting the four previously confirmed teams won **29/100, 29/100, 30/100 and 28/100**. The candidate values are **Health 3.04881408 / Power 3.85370128**, derived directly from the current **2.931552 / 3.705482** baseline.

All **2,004 fights** completed in **38.64 seconds**, including four matching detailed replays. The retained package is about **190 MiB** after analysis and the next-family inventory. Only isolated content copies were changed. **The candidate is not applied**: current Kharad still fails the expanded benchmark established by [fresh challenger confirmation](Tower-Challenger-Confirmation-Review.md). This screen does not replace complete-family confirmation or establish search optimality.

## Frozen scope

The [protocol](../TestResults/balance/tower-kharad-linked-screen-20260912/protocol.json), SHA-256 `e6eb3c7e5ada54360d073ea5183853215359917294e106590deb067153fcb599`, froze the following before combat:

- Linked multipliers **1.00, 1.02, 1.04, 1.06 and 1.08**, each applied to the same baseline without compounding or rounding the resulting decimal inputs.
- All four recipes from the preceding confirmation, with identical actual actor identities, equipment, Essence order and encounter scenario ID. The budget remains ten level-40 characters, Standard tier-1 rank-2 fixed gear and five level-1 unascended/unevolved Essences each. Full-pool hypothetical ownership remains an assumption.
- **100 new paired seeds** shared across all 20 setting/team cells. **467,195 historical values** are excluded, using 97 recorded sources. Shared seeds support controlled comparisons; they do not create 2,000 independent random environments. External unregistered jobs remain outside this history audit.
- Exactly **2,000 screen fights**, four detailed replays and 32 retry reservations, for **2,036 maximum attempts**. The five archives reserve retries as 8/6/6/6/6; unused reservations do not enlarge samples. All attempts are durable and globally counted.
- A 300-second cooperative driver cancellation budget, 2 GiB logical output limit, sequential prepared execution and 32-record chunks. Checks are not hard OS quotas; setup or an in-flight write can cross a check boundary. No automatic resume or cap extension was used.
- The complete producing build/source/content/settings identities and driver inputs. Preparation materialized every one of the 20 cells before the first fight.

The [input preparation script](../TestResults/balance/tower-kharad-linked-screen-20260912/prepare-inputs.py) copies only the 16 allowlisted data files. In non-baseline copies it changes only floor 5's `guardianScaling.health` and `guardianScaling.offense`. Python verification checks both exact decimal products and complete JSON equality after restoring those two fields. All other files retain their original hashes, and the baseline copy is byte-identical. Offense is the existing scaling input for guardian Power; defense, resistance, penetration, regeneration, mechanics and recommended power rating stay outside this linked adjustment.

## Selection and results

The predeclared rule was: eligible when the strongest observed team wins **10–40%**, choose the maximum closest to **30%**, and prefer the smaller multiplier if tied. No eligible setting would mean unresolved at the fixed cap. These interior selection targets do not change the final **10–50% acceptance policy**. Four replays use the selected setting, or the baseline if no setting is eligible.

Each entry below is wins out of 100. The first three columns retain the pilot's challenger order; the last is its historical leader.

| Linked factor | Two Essence changes | Web Weaver Spider change | One Essence change | Saved leader | Maximum |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1.00 | 61 | 55 | 68 | 43 | **68%** |
| 1.02 | 35 | 46 | 43 | 33 | **46%** |
| **1.04** | **29** | **29** | **30** | **28** | **30%** |
| 1.06 | 21 | 14 | 24 | 11 | **24%** |
| 1.08 | 4 | 6 | 11 | 3 | **11%** |

The [complete results](../TestResults/balance/tower-kharad-linked-screen-20260912/results.json), [seed observations](../TestResults/balance/tower-kharad-linked-screen-20260912/observations.json) and [selection receipt](../TestResults/balance/tower-kharad-linked-screen-20260912/selection.json) retain every cell, including baseline breaches and settings that were not selected.

The screen reports ordinary pointwise Wilson intervals and approximately simultaneous 95% Bonferroni-Wilson intervals over **all 20 cells**, not just the selected four. At +4%, the strongest observed team's adjusted interval is **18.31–45.04%**; every screened team's upper bound is below 50% and every lower bound is above 10%. This supports the four-team screen, while the frozen workflow still requires fresh complete-family confirmation. The +2% setting lacks supported upper bounds for every team; +8% lacks a supported 10% team in this sample. +6% is viable for screening but farther from the predeclared 30% preference.

Team ordering changed at +2%, where the Web Weaver Spider candidate became strongest in this sample. Neither ranking nor monotonic response is assumed for untested teams. No historical or preceding confirmation samples are pooled here, and there is no formal paired-difference or lifetime repeated-testing claim. The two teams at 29% are not established as weaker than the team at 30%.

## Saved candidate and complete-family inventory

The [candidate for confirmation](../TestResults/balance/tower-kharad-linked-screen-20260912/candidate-for-confirmation.json) records the exact +4% content hashes, resolved scaling inputs and all four reusable recipes. The recipes' seed arrays are empty intentionally; future evidence needs a separately frozen fresh schedule. The selected frozen content is at `TestResults/balance/tower-kharad-linked-screen-20260912/variants/scale-104/`. The normal retained catalogs and live content remain unchanged.

A read-only [next-family inventory](../TestResults/balance/tower-kharad-linked-screen-20260912/next-family-inventory.json) combines:

- The historical **2,438-recipe** calibration portfolio.
- **All 128 independent and 128 supplied-team evaluations** from the bounded pilot, including candidates outside its shortlists and any discovery-stage breaches.
- All **65** pilot validation recipes, which include the 49 compatible saved controls, and the four fresh confirmed candidates.

Normalizing party/equipment order and equivalent default/explicit identity vectors yields **2,678 distinct recorded parties**, including **240 additions** to the historical portfolio. Actual identities and ordered Essences are preserved. The inventory stores source locators and hashes, not another copy of the 58 MB historical portfolio. Its Python semantic fingerprints are inventory keys rather than the harness's serialized `RecipeHash`; strict harness normalization, full budget/context review and preparation must precede a frozen confirmation definition. Confirmation must not silently drop unshortlisted evaluated teams or earlier breaches to fit a smaller budget.

## Performance and verification

| Work | Fights | Seconds |
| --- | ---: | ---: |
| Five archive creation/combat phases | 2,000 | 34.96 |
| Five complete streaming verifications | 0 | 1.71 |
| Four fresh detailed replays at +4% | 4 | 1.42 |
| Timed phases | **2,004** | **38.09** |

The enclosing driver measured **38.64 seconds**, including operations between phases. CPU totaled **36.95 seconds**, cumulative allocation **18.21 GiB**, and process-lifetime peak working set **146.01 MiB**. Allocation is garbage-collected traffic, not concurrent memory or saved disk space. The initial package was about 188.7 MiB, becoming about 190 MiB after analysis/inventory receipts. The separate CLI verification and later review/integrity work are outside the driver runtime. Five small archives intentionally retain their own producing executables and content; their fixed storage cost is included. This is not a matched speed comparison with earlier campaigns.

Verification completed successfully:

- The diagnostic driver restored with an empty package-source configuration and built with zero warnings/errors. SDK access to local NuGet configuration was approved. There were no harness implementation changes.
- Every archive fully verified against the frozen recipes, exact seeds, expected content/settings and complete results. At most **32 full reports** were buffered. All **2,004** started/completed combats match journal accounting, with zero retry/lost attempts.
- All four detailed replays matched. The selected archive also passed the retained executable's `tower-compact-verify --run .../runs/scale-104` command, returning 400 trials in four cases without executing combat.
- [Python analysis](../TestResults/balance/tower-kharad-linked-screen-20260912/analyze.py) independently reconstructed every rate interval with `statistics.NormalDist`, reproduced selection, checked seed/recipe/attempt counts and verified the two-field-only changes in both input copies and archived content.
- The [final verification receipt](../TestResults/balance/tower-kharad-linked-screen-20260912/final-verification.json) checks protected history, prior pilot/confirmation artifacts, producing assemblies, current content, source hashes, catalogs, inventory source hashes and documentation links. `git -c core.safecrlf=false diff --check` passed.

The producing build matches the [prior 270-test regression run](Tower-Resumable-Bulk-Integration-Review.md#verification); backend tests were not repeated for this workload/documentation-only increment. New files are this review and the ignored driver, variant content, evidence and analysis/inventory files. Active discovery/loadout/harness plans, the acceptance policy and harness README are updated. No required command remains blocked. There are no live game configuration changes, migrations, deployments or external environment effects; the empty-source NuGet configuration belongs only to the diagnostic driver.

## Next step

Prepare complete-family confirmation at **+4%**, beginning with strict materialization of the **2,678-recipe inventory**, candidate/context deduplication and a runtime/storage estimate. Retain the known recipes and obtain bounded challenger coverage of the candidate setting before freezing the final family. Any newly discovered breach must remain represented.

The remaining work should use the [planned versioned staged allocation policy](Tower-Balance-Acceptance-Policy.md#planned-staged-sampling-and-compact-evidence) rather than silently repeating a 250- or 1,000-fight allocation for every weak team. Implement and independently test its family-wide uncertainty allocation, fixed stage schedules, stopping rules, strong-control inclusion, complete evidence coverage and hard resource stop before using it for acceptance. Ordinary fixed-sample intervals cannot be repeatedly inspected until a setting passes. Freeze the actual battle reservation before execution; an exhausted cap or excess unresolved family remains incomplete/inconclusive, never a smaller accepted family.

No further combats or application were part of this 2,036-attempt screen. Full-family confirmation and checked local application remain pending. The four-team result does not establish broader-floor balance, practical acquisition or near-optimal builds.
