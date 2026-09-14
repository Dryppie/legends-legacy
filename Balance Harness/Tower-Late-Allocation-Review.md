# Late-allocation experiment review

Completed **14 September 2026** under the [frozen plan](Tower-Late-Allocation-Plan.md), after the [implementation checks](Tower-Late-Allocation-Implementation-Review.md). Target: offline BalanceHarness. **reliability Fail 1/3; ordinary/joint family Pass / Pass**.

All **106,496 fights** completed exactly once in **103.82 minutes**, retaining **2379.9 MiB**. This includes 36,864 discovery fights, 12,288 screening fights and 112 × 512 = 57,344 confirmation fights. The campaign stayed within 114,688 fights, 10,800 execution seconds and 4 GiB. The ledger contains **480,120 reservations**: 479,533 historical exclusions and 587 fresh values.

## What was tested

V18 changes the allocation of the isolated pair: each component reaches 256 evaluated candidates, then the better current component receives another 256, ending at 512+256. Both retain 96 initial fresh parties and separate random state, counters, parents and module libraries. Deep v13 retains 768 candidates and 192 initial fresh parties. The complete top-32 screen, original/screened nominations, all controls and 2/3 reliability rule remain unchanged.

This tests the candidate against deep v13 on three fresh paired roots. It does not establish superiority over a fresh equal 384+384 allocation, which was not a comparator in this experiment. The cutoff was informed by earlier trajectories and frozen before these outcomes.

## Allocation decisions

| Root | Selected component | A prefix wins / 8 | A guardian health | B prefix wins / 8 | B guardian health | Final A / B evaluations |
| ---: | --- | ---: | ---: | ---: | ---: | --- |
| 1923607779 | A | 0 | 40.61% | 0 | 54.75% | 512 / 256 |
| 774534297 | B | 0 | 57.18% | 0 | 52.89% | 256 / 512 |
| 1136496908 | B | 0 | 55.14% | 0 | 46.05% | 256 / 512 |

Both complete prefix rankings, attempt counts and chosen components are retained in [allocation decisions](../TestResults/balance/tower-late-allocation-work-20260914/allocation-decisions.json). Prefix scores are adaptive discovery measurements, not held-out success probabilities.

## Fresh confirmation

| Restart / root | Deep primary / 512 | Candidate primary / 512 | Candidate joint interval | Paired difference vs deep (percentage points) | Viable | Improved | Anchor recovered | Reliability |
| --- | ---: | ---: | --- | --- | --- | --- | --- | --- |
| 1 / 1923607779 | 44 | 103 | 14.39%–27.39% | 11.52 [2.35, 20.24] | Yes | Yes | Yes | Yes |
| 2 / 774534297 | 139 | 0 | 0.00%–2.59% | -27.15 [-33.83, -19.40] | No | No | Yes | No |
| 3 / 1136496908 | 21 | 0 | 0.00%–2.59% | -4.10 [-7.92, -0.13] | No | No | Yes | No |

The fixed anchor won **0/512** and the fixed stronger prior control won **149/512**. Strong-control comparisons remain separate from the reliability gate. All nine paired differences and every ordinary/joint interval are retained in the [complete results](../TestResults/balance/tower-late-allocation-work-20260914/results.json).

The full family contains **112 recipes**: 94 prior controls and 18 newly selected recipes. **15** have a joint lower bound of at least 10%: **11** prior controls and **4** new recipes. Observed confirmation rates above 50% occurred in **0** recipes. Ordinary family assessment is **Pass**; joint family assessment is **Pass**. These family results are separate from independent search reliability.

The four new viable recipes came from two different roots:

| Recipe | Source / root | Evaluated candidate | Fresh wins / 512 |
| --- | --- | ---: | ---: |
| [team-9bd9b19bdb225cdd8d08ea3a9df97414](../TestResults/balance/tower-late-allocation-work-20260914/exports/team-9bd9b19bdb225cdd8d08ea3a9df97414.json) | Deep / 774534297 | 646 | 139 |
| [team-016b0821ebf39811646f8fd9bede973f](../TestResults/balance/tower-late-allocation-work-20260914/exports/team-016b0821ebf39811646f8fd9bede973f.json) | Deep / 774534297 | 747 | 131 |
| [team-86b2249a50e46157fc75ab7e6f61be7e](../TestResults/balance/tower-late-allocation-work-20260914/exports/team-86b2249a50e46157fc75ab7e6f61be7e.json) | Isolated A / 1923607779 | 437 | 103 |
| [team-7015124c828ba6272cb758f01b275ed9](../TestResults/balance/tower-late-allocation-work-20260914/exports/team-7015124c828ba6272cb758f01b275ed9.json) | Deep / 774534297 | 583 | 87 |

Candidate 437 lies beyond the old 384-evaluation component limit and was discovered during the allocated continuation. Its discovery rank was 27; the unchanged top-32 screen retained it and selected it as primary. This identifies a useful observed outcome of the extra depth. It does not establish general reliability, superiority over a matched equal-allocation experiment, or a retrospective result from combining the two methods' best confirmation outcomes.

Every original and screened nominee, control and required ceiling breach is retained. No nominee was replaced after confirmation and no previous outcomes were pooled. The [complete recipe inventory](../TestResults/balance/tower-late-allocation-work-20260914/exports/saved-builds.md) includes exact ordered builds, origins, zero-win recipes and fresh intervals. The [family export](../TestResults/balance/tower-late-allocation-work-20260914/family.json) can be explicitly imported as controls in a later study. Recipe exports do not imply catalog promotion or practical acquisition.

## Decision and next boundary

Keep v18 experimental with adoption Hold: it passed only 1/3 reliability restarts. Retain all 112 confirmed recipes, including the four new viable builds (three from deep search and one from late allocation). The next proposal should test a portfolio that uses both search methods and chooses its nominees before confirmation, against a comparator given the same total evaluation and trial budget. The differing successful roots motivate that hypothesis; this run does not establish a portfolio result, a retrospective 2/3 pass, or superiority over equal 384+384 allocation. Count the cost of both methods, retain the numerical 2/3 gate and all 112 controls, and freeze any new design before fresh seeds or fights. No further cutoff variant or portfolio experiment is prepared by this result. Strongest-control coverage, practical acquisition and floors 6–11 remain open.

## Verification and preservation

- 349 distinct backend tests passed through `build/run-tests.ps1`, including 39 new-policy cases. Historical generation and shortlists matched across 13,632 saved evaluations and 48 feedback probes, with unchanged gameplay assemblies.
- The captured executable reconstructed the complete current discovery, both prefix rankings and continuation, six shortlists/screens, nominations, required family, all saved combat evidence, statistics, attempt journal and package inventory with zero new fights.
- Independent Python checks reproduced all allocation decisions, the complete required family, all ordinary/joint rates and nine paired differences within 1e-8. Reconstruction reproduced the analysis outputs exactly.
- All source packages and prior sealed manifests remain preserved. The [final receipt](../TestResults/balance/tower-late-allocation-work-20260914/final-verification.json) records source hashes, documentation snapshots, links and the final package seal.

Initial sandbox builds could not read the user NuGet configuration; retrying the required runner and audit build with the necessary filesystem access succeeded. No required verification command remains blocked. Six unrelated existing build warnings remain. No game content, configuration, migration, catalog, default optimizer or deployment changed. Kharad remains Health **3.5366243328** / Power **4.4702934848**. Practical acquisition and floors 6–11 remain separate milestones.
