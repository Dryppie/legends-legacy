# Kharad checked local application — 12 September 2026

**Applied locally and verified.** Floor 5 now uses the linked **+4% Health/Power** setting that passed the [complete 2,918-party staged confirmation](Tower-Staged-Confirmation-Review.md). All six selected detailed local battle reports match the confirmed candidate exactly. **Eight diagnostic fights** took **16.42 seconds** across the two measured phases; **80 relevant Tower tests passed**. The recorded family passes the 10–50% policy, while competitive search quality remains **Fail** and near-optimality is unestablished.

## Content change and evidence scope

The target is the primary game's local [Tower definitions](../LL/src/API/API.LL/Data/world-tower/tower-floors.json). Only these two numeric values changed:

| Floor-5 field | Previous | Applied | Factor |
| --- | ---: | ---: | ---: |
| `guardianScaling.health` | 2.931552 | **3.04881408** | 1.04 |
| `guardianScaling.offense` (Power) | 3.705482 | **3.85370128** | 1.04 |

The common factor preserves the existing Health/Power ratio. Defense, resistance, penetration, regeneration, mechanics, required party slots and every other floor remain unchanged. No exception for Health-only or Power-only scaling was needed. This increment changes no combat or search code.

The complete recorded family already passed the frozen `tower-staged-bonferroni-wilson-95-v1` policy at these settings. Its strongest observed second-stage team won **84/256 (32.81%)**, with an adjusted **22.63–44.91%** interval. Across every final family cell, the largest adjusted upper bound was **48.53%**, and **53** cells had supported lower bounds of at least 10%. No completed final observation or final adjusted upper bound exceeded 50%. These remain scoped, approximate statistical bounds from the original experiment; applying the content does not prove an absolute ceiling over all possible builds or turn a historical sample into fresh evidence.

This resolves local application of that family's Pass. The previous setting's expanded-benchmark Fail and older portfolio Pass remain correct historical findings. The party budget remains ten characters with five Essences each at level 40, tier 1, rank 2, Standard quality, fixed equipment and level-1 unascended/unevolved Essences, without styles or contributions. Independent discovery still has not reliably recovered the best saved builds. Broader-floor competitive acceptance and practical acquisition coverage remain open.

## Checked application

The [frozen application protocol](../TestResults/balance/tower-kharad-application-20260912/protocol.json) was written before combat or application. It selected the four previous confirmed controls and the first two entries in the saved top-build export, using the first existing second-stage seed. It reserved exactly **2 archived candidate replays + 6 local replays**, with **no retries or automatic resume**, a **600-second** aggregate diagnostic-phase limit and **512 MiB** output limit. Preparation, builds, backend tests, documentation and integrity scans are outside that diagnostic time/fight accounting. Cancellation and storage checks are cooperative.

The [preparation receipt](../TestResults/balance/tower-kharad-application-20260912/preparation-verification.json) rechecked **21,879 staged artifacts**, all eight then-current documentation hashes, **5,146 protected historical files**, and the earlier pilot, challenger-confirmation and linked-screen seals. It checked all 16 relevant content files, current harness sources, producing assemblies and both retained catalogs before editing.

An initial preflight rejected raw byte equality before any fight or edit: the archived candidate uses LF, while the local Tower file already contained 15 CRLF endings. The final frozen protocol preserves those local endings. Independent structural comparison proves exactly two changed values, and normalized LF bytes match the archived candidate exactly. The other 15 content files match byte for byte. Consequently the local floor-file hash differs from the candidate solely because of those preserved existing line endings, which is recorded explicitly rather than silently treating different hashes as identical.

The [diagnostic driver](../TestResults/balance/tower-kharad-application-20260912/driver/Program.cs) uses the exact producing combat/harness binaries and checks the live threat/checkpoint settings. Before application, it fully verifies and replays the two leading builds from their compact archives. The other four controls already have sealed detailed reports. The [application script](../TestResults/balance/tower-kharad-application-20260912/apply.py) checks the frozen inputs and successful candidate receipt, replaces only the two numeric values, verifies the result and writes an [application receipt](../TestResults/balance/tower-kharad-application-20260912/application-receipt.json).

After application, the driver runs all six recipes through the local content and compares the entire detailed reports, including participants, outcome, duration and event logs. The [comparison receipt](../TestResults/balance/tower-kharad-application-20260912/after-receipt.json) records matching canonical hashes. An independent Python structural comparison also checks all six retained report pairs. This repeats old confirmation seeds for deterministic parity; there is no new search, statistical acceptance sample, winner selection or retuning.

| Compared recipe | Source | Original second-stage result |
| --- | --- | ---: |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | Previous confirmed control | 78/256 |
| `team-5b7f0a2637ddc8253f7e191d25e9d560` | Previous confirmed control | 50/256 |
| `team-ac1b1cac3fd315d19ad1606ab43ac036` | Previous confirmed control | 57/256 |
| `team-693ffa8ec0b654154a06722aba06a968` | Previous confirmed control / old leader | 68/256 |
| `team-1abe76ca1891d97a91d484f0a3662048` | Highest observed saved candidate | 84/256 |
| `team-3a69c759178064021dc5cf7124d7f4f5` | Next saved candidate; earlier 5/8 discovery | 79/256 |

The two candidate replays and archive verification took **13.53 seconds**; six local comparisons took **2.89 seconds**. All eight starts completed, with no retries or lost attempts. Total measured CPU was **19.25 seconds**; cumulative GC allocation was **5.40 GiB**, which is allocation traffic rather than disk usage. The package retains approximately **98 MiB**, chiefly detailed diagnostic reports and the small producing driver. The final receipt records its exact size. No historical evidence was deleted or rewritten.

## Saved builds and future use

The [top 20 recipes](../TestResults/balance/tower-staged-confirmation-20260912/top-confirmed-recipes.json) and [all 2,918 family recipes](../TestResults/balance/tower-staged-confirmation-20260912/family.json) remain reusable with their measurements and ancestry. Loading these recipes costs no battles. New experiments should explicitly import this expanded family and the strongest saved controls, preserving their source identities and excluding previously used seeds. Fitness for new content or fresh studies must be measured again under the new frozen experiment.

The normal local and versioned retained catalogs remain unchanged in this application increment. Therefore these separately saved new leaders are **not yet automatically added to ordinary Tower Lab searches**. Existing catalog controls still work; the application receipt does not imply catalog promotion or a universal hardcoded team. Prior sealed reviews retain their historical status, including their then-unapplied descriptions.

## Verification and changed files

The diagnostic driver built with **zero warnings and errors**, without rebuilding the producing harness. Its empty-source restore initially lacked sandbox access to the local NuGet configuration and then succeeded with approved SDK access; no package or repository dependency was added.

The required backend wrapper ran successfully:

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerTests'
```

All **80 tests passed**, with no failures or skips. These include normal persisted Tower preparation/playback/outcome parity, floor-5 coverage and the other floors, legal/invalid recipe handling, replay/evidence checks and cancellation. The [test receipt](../TestResults/balance/tower-kharad-application-20260912/tower-tests.trx) is retained. Existing Release binaries were used because only content changed. Backend test simulations are separate from the eight application diagnostics. No new mirror test was added for two literal content values.

The [final verification](../TestResults/balance/tower-kharad-application-20260912/final-verification.json) seals this package and review, independently checks the two-field change and every report pair, and rechecks prior evidence, current content, producing binaries, source files and catalogs. `git -c core.safecrlf=false diff --check` passed. No required command remains blocked.

Changed tracked scope: the Tower definitions JSON, six active discovery/loadout/harness/policy Markdown files and the harness README. This review is new. The ignored output package contains the application protocol, baseline file, diagnostic driver, reports, scripts, attempt log and receipts. Other preexisting working-tree changes were preserved. There are no migrations, deployments, shared-database changes or dependency changes. No running service was restarted; this is a local content edit and a running service must reload that content before using it.

## Next work

1. Profile archive bookkeeping, accumulated storage-tree scans, durable attempt journaling, shared-file handling and repeated historical-exclusion validation in a bounded matched workload. The staged campaign's uninstrumented archive time identifies a measurement target, not proof that one function caused all of it. Preserve authoritative storage enforcement, full evidence checks, cancellation/resume and charged-attempt accounting when testing optimizations.
2. Improve independent restart reliability and explicit saved-team improvement against the latest saved leaders at this applied setting. Include those recipes with their provenance; do not rerun the full search simply to recover already saved teams. Freeze fresh search/validation schedules and resource limits before any new campaign.
3. Resolve search quality before treating the floor as a server-wide competitive benchmark, then continue floors 6–11 at their intended Essence budgets. Floors 2–4 still need challenges against their applied settings; practical acquisition remains separate coverage.

None of those next campaigns ran in this application increment.
