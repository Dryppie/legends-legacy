# Stagger-reservation pilot review

Completed **13 September 2026**. The separately frozen opt-in `independent-stagger-reservation-v10` pilot completed **9,224 fights** with **zero retries or lost attempts**. Reliability is **Fail: 0/3 passing restarts, with 2 required**. All **twelve generated finalists won 0/256** on fresh held-out seeds. The strongest saved control won **125/256 (48.828%)**, joint-adjusted interval **39.08–58.67%**. The ordinary and joint-adjusted 18-cell assessments are both **Inconclusive**.

This experiment did not establish reliable competitive discovery from the count-only change. No new validation observation exceeded 50%, but the strongest control's uncertainty still crosses the ceiling. Earlier observed breaches remain separate and are not erased. The result does not establish complete Tower-family acceptance, near-optimality or practical acquisition feasibility.

## Frozen comparison and budget

The [protocol](../TestResults/balance/tower-stagger-reservation-20260913/protocol.json), [precombat plan snapshot](../TestResults/balance/tower-stagger-reservation-20260913/implementation-plan.md), [experiment design](../TestResults/balance/tower-stagger-reservation-20260913/experiment-design.json) and [preflight receipt](../TestResults/balance/tower-stagger-reservation-20260913/precombat-verification.json) froze three paired restarts of unchanged `compatible-defense-joint` versus `stagger-reservation-joint`. Each arm evaluated 96 complete parties on eight shared discovery seeds, with at most 2,048 proposals. The driver froze each arm's rank-one primary and rank-two exploratory finalist before validation, then measured all twelve finalists and six controls on 256 shared disjoint seeds.

The new arm changes only the supported recurring-control count draw during guided fresh construction: uniform `ceil(firstThreshold / authoredStaggerPower)` through party size, after unchanged provider selection. Unsupported/unattainable routes retain the original range. Conditional providers stay eligible; mutations, category membership, core skip, fillers, ranking and defaults remain unchanged. Nominal authored power is not predicted uptime, accepted contribution or combat strength. The [implementation review](Tower-Stagger-Reservation-Implementation-Review.md) retains its 243 passing tests and exact historical/comparator checks.

Kharad remains **Health 3.04881408 / Power 3.85370128**. The cohort remains ten level-40, tier-1, rank-2 Standard characters with five level-1 unascended/unevolved Essences each, exact fixed gear and no styles/contributions. All 80 eligible Essences, including Rare, use hypothetical ownership. No saved recipe, count, ancestry or held-out measurement entered generation; all six controls remained outside it. Removing or reversing references produced the same generation input hash.

| Phase | Actual fights |
| --- | ---: |
| Six discovery arms × 96 parties × eight seeds | 4,608 |
| Eighteen frozen recipes × 256 validation seeds | 4,608 |
| Four historical detailed parity checks | 4 |
| Three new primaries and fixed anchor on first validation seed | 4 |
| Total | **9,224** |

The measured phases totaled **246.02 seconds**; complete execution took **249.74 seconds**, below the 600-second cap. Approximately **343.4 MiB** was retained before final documentation and verification, below 1 GiB per package and 512 MiB per campaign. The eight repeated checks were not fresh win-rate samples. No sample extension, replay selection, retry or automatic resume occurred.

## Held-out decision

All 576 discovery parties had zero wins on their eight-seed discovery samples. All twelve generated finalists then had zero wins on the separately frozen validation schedule. Each generated finalist's joint-adjusted Wilson interval is approximately **0–3.84%**; none meets the supported ≥10% viability requirement.

| Restart seed | New-arm primary recipe | Primary wins | Comparator primary wins | Reliability |
| --- | --- | ---: | ---: | --- |
| `-76005071` | `team-8af59401a6c41b112700d27eb3f60603` | 0/256 | 0/256 | Fail |
| `-129820605` | `team-0e67c243c93bd8b0af96881c705406df` | 0/256 | 0/256 | Fail |
| `-127588879` | `team-88ffb0cd0eb95ac618b0734e1db0cdac` | 0/256 | 0/256 | Fail |

Every primary's paired difference against its same-restart comparator was **0 percentage points**, joint-adjusted interval **−3.57 to +3.57 points**. Against the fixed anchor `team-1abe76ca1891d97a91d484f0a3662048`, each difference was **−29.30 points**, interval **−38.67 to −17.84 points**. All three components of the predeclared primary gate failed in every restart. Secondary candidates did not replace primaries.

| Saved control | Wins | Observed rate | Joint-adjusted rate interval |
| --- | ---: | ---: | --- |
| `team-1a924a7cfff12298633bee909cdea4ad` | 125/256 | 48.83% | 39.08–58.67% |
| `team-38248d838d1db9634fd82536c177df0a` | 101/256 | 39.45% | 30.27–49.44% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 84/256 | 32.81% | 24.25–42.70% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 75/256 | 29.30% | 21.14–39.04% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 61/256 | 23.83% | 16.43–33.24% |
| `team-693ffa8ec0b654154a06722aba06a968` | 48/256 | 18.75% | 12.21–27.69% |

The joint nominal alpha .05 remains split .025 over all 18 rate cells and .025 over six paired comparisons, with component .025/12 for discordance bounds. The ordinary evaluator separately uses .05/full-family. Approximate Wilson intervals are not a lifetime repeated-study guarantee. No results were pooled across recipes or closed studies.

## Saved construction and replay observations

All **588 proposals**, including twelve rejected/duplicate proposals, remain saved with ancestry and full accepted recipes. The new arm made **127 fresh proposals**: 22 took the uniform route and 105 made guided control nominations. Every guided request obeyed its source-derived minimum; **104 of 105** requests were fully satisfied. One requested ten placements and satisfied six under unchanged placement legality. All four eligible control providers were nominated. These counts describe placements, not additional occupied slots or accepted stagger applications. Comparator traces do not invent provider identities.

The [nomination findings](../TestResults/balance/tower-stagger-reservation-20260913/reservation-findings.json), [finalist ancestry and named recipes](../TestResults/balance/tower-stagger-reservation-20260913/trace-findings.json) and [replay diagnostics](../TestResults/balance/tower-stagger-reservation-20260913/replay-diagnostics.json) retain the observations. The [post hoc event summary](../TestResults/balance/tower-stagger-reservation-20260913/stagger-replay-summary.json) reads only the four predeclared replay files:

| Fixed replay | Guardian break ticks | First friendly death tick |
| --- | --- | ---: |
| First new primary | 400, 800 | 429 |
| Second new primary | 200 | 420 |
| Third new primary | 600 | 420 |
| Fixed anchor | 600, 1000 | 729 |

All four replays were defeats on the first validation seed, `-1840085499`. These fixed observations do not establish a causal remedy, a timing cutoff or provider-level efficacy. They do show why nominal count capacity and a first-break timestamp cannot be treated as a complete explanation of competitive performance. No additional replays or constructor probes were run.

## Verification, files and next boundary

The [analysis](../TestResults/balance/tower-stagger-reservation-20260913/analysis.json) verifies manifests, every compact record and seed schedule, durable attempt journals, frozen selections, provider substitutions and all six exact control recipes. Both complete campaigns reconstructed without combat. Four historical detailed reports matched exactly, and four new detailed reports matched their stored compact summaries. The [final receipt](../TestResults/balance/tower-stagger-reservation-20260913/final-verification.json) independently checks adjusted intervals and primary gates, all 16 prior packages, 74 sealed reviews, 2,237 unchanged C# files, five unchanged producing assemblies, all 16 content files, both catalogs and active Markdown links.

The source-free local driver built with zero warnings. Its first restore was denied access to the user's NuGet configuration by the sandbox; the scoped approved restore succeeded. No command remains blocked. The sealed implementation's **243/243 passing backend tests**, run through `build/run-tests.ps1`, were reverified rather than rerun because source and producing assemblies were unchanged. This pilot's additional verification consists of the fresh campaigns, replay checks, reconstruction and independent saved-record analysis. Commands and timing are retained in the [verification record](../TestResults/balance/tower-stagger-reservation-20260913/verification-commands.json).

Changed files are the new pilot's local driver/scripts/evidence, this review, the active stagger-reservation plan, shared handoff/status plans and harness README. The [validated builds](../TestResults/balance/tower-stagger-reservation-20260913/validated-builds.json) retain all eighteen seed-free recipes, including every zero-win finalist, for reuse without rerunning discovery. No application source or gameplay content changed; there are **no migrations, configuration changes, deployments or catalog/default promotions**.

The latest [seed ledger](../TestResults/balance/tower-stagger-reservation-20260913/seed-ledger.json) excludes all 471,656 preceding reservations and retains all 269 new reservations, including two unused stage seeds: **471,925 distinct seeds** across every array. Future work must exclude that whole union.

The next scoped step is a separately bounded **zero-combat diagnosis** of the saved nominations, ancestry, validation summaries and four fixed replays. Distinguish requested capacity from placement, inheritance and observed contributions before choosing another independent hypothesis. No next policy, provider filter, timing margin or campaign is selected. Do not repeat this pilot, strengthen gear, retune Kharad or expand floors on this failed reliability result.
