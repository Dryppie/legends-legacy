# Equal-budget search portfolio: verified capacity stop

Completed **14 September 2026** under the [frozen plan](Tower-Search-Portfolio-Plan.md), after the [implementation verification](Tower-Search-Portfolio-Implementation-Review.md). Target: offline BalanceHarness.

**86,016 fights** completed exactly once in **292.69 minutes**, retaining **3101.6 MiB**. Discovery and all six screens completed. The required family contains **253 recipes**, exceeding the frozen capacity of **144**; all recipes are retained and **zero confirmation fights** ran. **129 recipes** exceeded 50% in discovery, and **46** also exceeded 50% in fresh screening. Reliability remains **Unresolved**, adoption **Hold**, and confirmation-family acceptance is **NotRun**. The current ledger contains **480,707 reservations**.

## What changed and what was tested

The opt-in v19 policy compares one 1,536-candidate deep search with a portfolio of deep 768 plus the v18 isolated 512+256 pair. Both units receive 1,536 evaluated candidates, 384 initial fresh evaluations and up to 16,384 proposals per root. All four components retain separate streams, parents, counters and module libraries. The portfolio merges before screening, keeping every duplicate charge and origin. Both units use the same top-32/64-trial screen and pre-confirmation nomination rule.

All **9,216 generated recipes are distinct**. Discovery completed **73,728 fights in 283.63 minutes**. The six screens completed **12,288 fights in 8.92 minutes**. Total recorded execution was **292.69 minutes**, within the six-hour and 8 GiB limits. The capacity stop is the planned preservation behavior, with no retry, resume, cap extension or partial confirmation. Exit code 1 indicates the capacity boundary; completed reconstruction returned 0.

## Why confirmation did not run

| Required family contribution | Distinct recipes |
| --- | ---: |
| Prior controls | 112 |
| Original and screened nominees | 22 |
| Controls plus ordinary nominees | 134 |
| Additional recipes required solely by the ceiling rule | 119 |
| **Complete required family** | **253** |
| Frozen capacity | 144 |

There are **129 unique discovery ceiling observations** (>4 wins of 8). **46** of those recipes also exceed 32 wins of 64 in screening; screening adds no new breach recipe outside the discovery set. Ten breach recipes were already ordinary nominees, leaving 119 additional required cells. The complete family is 112 controls plus **141 new recipes**. None was dropped to fit capacity.

## Screening results — unconfirmed

| Root | Unit | Discovery recipes above 50% | Screen recipes above 50% | Primary wins / 64 | Primary discovery rank |
| ---: | --- | ---: | ---: | ---: | ---: |
| -1038588442 | Deep 1,536 | 90 | 32 | [47/64](../TestResults/balance/tower-search-portfolio-work-20260914/exports/team-77416926dedbf2bf6abc7ccb7f782f89.json) | 27 |
| -1038588442 | Portfolio | 0 | 0 | [8/64](../TestResults/balance/tower-search-portfolio-work-20260914/exports/team-1b3d503fe18c185829cf1d18a135ff29.json) | 25 |
| -867718476 | Deep 1,536 | 13 | 0 | [30/64](../TestResults/balance/tower-search-portfolio-work-20260914/exports/team-42385f7b20113d3bf961905843438a72.json) | 25 |
| -867718476 | Portfolio | 26 | 14 | [43/64](../TestResults/balance/tower-search-portfolio-work-20260914/exports/team-6d05d55b31fc7d2092c447ccd19fd5a2.json) | 2 |
| -242256866 | Deep 1,536 | 0 | 0 | [15/64](../TestResults/balance/tower-search-portfolio-work-20260914/exports/team-3037558287f9005eeb6ae60dd59587d4.json) | 2 |
| -242256866 | Portfolio | 0 | 0 | [20/64](../TestResults/balance/tower-search-portfolio-work-20260914/exports/team-697e8a1863114d6c92a757db294776f0.json) | 5 |

The 47/64 (73.44%) deep primary and 43/64 (67.19%) portfolio primary are screening observations used for selection. They are not confirmed rates or multiplicity-adjusted ceiling findings. The fixed anchor and stronger control were retained but not measured in this experiment. No confirmation intervals, nine paired confirmation differences, family acceptance or 2/3 reliability result were computed.

## Allocation and discovery sources

| Root | Selected component | A prefix guardian health | B prefix guardian health | Final A / B |
| ---: | --- | ---: | ---: | --- |
| -1038588442 | B | 54.43% | 23.29% | 256 / 512 |
| -867718476 | A | 56.56% | 64.23% | 512 / 256 |
| -242256866 | B | 57.61% | 45.96% | 256 / 512 |

All six prefix leaders had zero wins in their eight discovery trials. Both full 256-prefix rankings, attempts and chosen continuations are retained in [allocation decisions](../TestResults/balance/tower-search-portfolio-work-20260914/allocation-decisions.json). Full component counts, first breach ordinals and nominee sources are retained in [component origins](../TestResults/balance/tower-search-portfolio-work-20260914/component-origins.json).

This comparison produced substantially stronger unconfirmed challengers and exposed an undersized confirmation-family envelope. All 129 discovery observations above 50% came from deep components: 103 from the 1,536-candidate comparators and 26 from the portfolio's 768-candidate deep search. None came from the isolated pair. The first comparator breaches occurred at evaluated candidates 1,036 and 1,345, beyond the previous 768-candidate limit; the portfolio deep component's first was at 634. These are observed positions within new trajectories, not proof that extra depth alone caused the gain: initial construction horizons and roots also differ from earlier experiments. Forty-six recipes also exceeded 50% during fresh screening. Confirmation is necessary to assess the ceiling and the frozen reliability comparison; no reliability Pass/Fail or new balance acceptance is established here.

## Retained evidence and next boundary

The [complete family export](../TestResults/balance/tower-search-portfolio-work-20260914/family.json) contains every exact ordered recipe and origin. The [build inventory](../TestResults/balance/tower-search-portfolio-work-20260914/exports/saved-builds.md) includes individual recipe links and all discovery/screening observations, with confirmation explicitly null. The [results](../TestResults/balance/tower-search-portfolio-work-20260914/results.json) retain counts, phase timings, six nomination groups, allocation decisions and the unresolved decision. Exports do not imply catalog promotion or practical acquisition.

First audit the version boundary between the captured v19 experiment and the concurrently changed checkout. Ten captured content files and five workspace executable files now differ, including gameplay assemblies. The existing results and 388 passing tests describe the frozen producing version, not a verification of those later edits. To complete the original captured-scope comparison, prepare a separate confirmation-only study for all 253 retained recipes and the already frozen six screened primaries, using 512 fresh shared trials per recipe (129,536 fights). Bind the complete v19 campaign/work manifests, retain the same anchor, stronger control, numerical 2/3 gate and ordinary/joint family rules, and exclude all 480,707 reserved values. Implement and verify the larger confirmation contract, then freeze its own time/storage limits and fresh schedule before execution. Do not resume v19, increase its capacity, drop breach-only recipes or rerun discovery/screening. Confirm these challengers before further search variants or Kharad tuning. Reliability remains Unresolved and adoption Hold; no default/content promotion. A supported ceiling breach would require a separate recalibration and complete relevant-family confirmation. Practical acquisition and floors 6–11 remain separate milestones. If confirmation instead targets updated gameplay, explicitly freeze that scope and establish the necessary baseline; do not transfer the original generation-reliability claim automatically.

No separate confirmation package or fresh schedule has been prepared by this work. The proposed 129,536-fight confirmation is a new study, not an extension or reuse of v19's reserved confirmation schedule. All 512 unused confirmation values remain excluded in the current ledger.

## Verification and preservation

- 388 distinct relevant backend tests passed through `build/run-tests.ps1`, including 39 new-policy cases. All six earlier studies reproduced exactly across 18,240 saved evaluations and 48 feedback probes, with unchanged gameplay assemblies/runtime.
- Nine prepared-package checks passed, binding all 112 controls, content/settings/executable, resource limits and disjoint schedules, and rejecting modified inputs or repeated execution.
- The captured executable fully reconstructed the current discovery, allocation, merged rankings, screens, nominations, 253-recipe family, saved combat evidence, attempt journal and package inventory. Verification took **251.04 seconds** and started zero new fights.
- Independent Python analysis rebuilt all six full rankings, 9,216 evaluation charges, three allocation decisions, screening counts, nominations and complete family recipes and origins. Read-only reproduction matched every derived output exactly. See the [independent selection check](../TestResults/balance/tower-search-portfolio-work-20260914/independent-selection-check.json). No confirmation statistics were computed.
- Earlier source packages and current evidence remain preserved. The [final receipt](../TestResults/balance/tower-search-portfolio-work-20260914/final-verification.json) binds source/document snapshots, manifests, links and the work-package seal.

Use `analyze-capacity.py verify` for read-only reproduction. The prewritten `analyze.py` and `write-completion.py` target the unexecuted confirmation path; they were not run.

Initial sandbox builds could not read the user NuGet configuration; required tests and the audit build succeeded with the necessary filesystem access. The first focused tests exposed the legacy control-import cap; the v19-only fix passed all relevant checks before preparation. No required verification command remains blocked. Six unrelated existing warnings remain. This task changed no game content, configuration, migration, deployment, catalog or default optimizer. Unrelated concurrent changes are recorded below. Kharad remains Health **3.5366243328** / Power **4.4702934848**.

## Concurrent checkout boundary

The final preservation check detected edits outside this task made to the shared checkout during the approximately five-hour run. A point-in-time [drift receipt](../TestResults/balance/tower-search-portfolio-work-20260914/concurrent-checkout-changes.json) records 24 changed baseline files, 39 newly observed Git changes and 39 preserved source/content snapshots. The checkout may continue changing; those observations are not a claim that concurrent work has stopped.

Ten files differ from the campaign's frozen content: abilities, creature abilities, statuses, summons, Essences, items, regional combat balance, creature loot tables, creatures and regions. Five workspace binaries also differ from the captured executable inventory: Application, BalanceHarness, Common, Domain and Services.LL. The observed source edits include additional ability triggers/effects and combat-engine behavior. They were neither made nor reverted by this task.

The campaign ran entirely from its retained content and executable. Its complete captured reconstruction passed, and its sealed evidence is unaffected. The 388-test result and historical replay describe the producing version tested before these later changes. This task has not retested or established gameplay equivalence for the concurrently updated checkout.

The immediate handoff therefore includes a version-compatibility audit. Complete the original 253-recipe confirmation only against the captured scope, or explicitly register the updated scope and establish the necessary baseline. Do not interpret this experiment or earlier accepted families as acceptance of unverified gameplay changes. No further fights or seeds were added to investigate the drift.
