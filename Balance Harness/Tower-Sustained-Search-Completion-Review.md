# Sustained Tower search: completed validation and decision

13 September 2026. Target: the offline `LL/tools/BalanceHarness` evidence workflow. The user authorized the [prepared continuation](../TestResults/balance/tower-search-benchmark-verification-20260913/continuation-proposal.json) after the original run exhausted its execution allowance. **All 24 original cells now have all 256 validation outcomes.** Both candidate methods fail the combined independent-search reliability rule at **0/3 passing restarts**. Ordinary and joint-adjusted tested-family balance assessments are **Inconclusive / Inconclusive**.

This closes the comparison proposed in the [strategy reassessment](Tower-Search-Strategy-Reset.md). Greater search budget found useful independent parties and supported improvement over the small baseline in two restarts, but did not recover fixed-anchor competitiveness. The measured-behavior archive supplied no supported improvement over the deep baseline. Neither method earns default promotion or a return to boss calibration.

## Complete results

Each number is wins out of **256 fresh validation trials**, using the original frozen family and seed order. Rank-one primaries remained fixed before screening; secondaries remain exploratory and cannot replace a primary. The three restart seeds, in order, are 1233403840, -7800881 and -226472186.

| Method | Primary wins by restart | Secondary wins by restart | Combined reliability |
| --- | --- | --- | --- |
| A: v4, 96 candidates per restart | 0, 0, 0 | 0, 0, 0 | Comparator; no candidate-method claim |
| B: v4, 384 candidates per restart | **43, 0, 37** | 43, 0, 37 | **Fail, 0/3** |
| C: behavior archive, 384 candidates per restart | **36, 0, 0** | 37, 0, 0 | **Fail, 0/3** |

The original 2,592-party discovery and 64-seed screen remain unchanged. Six of the 18 generated finalists recorded validation wins. Two generated recipes have a jointly adjusted supported rate above 10%, but both are finalists from **the same B restart**. They do not establish independent restart reliability.

| Candidate primary | Validation rate | Joint rate interval | Supported ≥10% viability | Supported improvement over comparator | Anchor non-inferiority |
| --- | ---: | --- | --- | --- | --- |
| B, restart 1233403840 | 43/256 = **16.80%** | 10.51–25.76% | Yes | Yes | No |
| B, restart -7800881 | 0/256 | 0–4.03% | No | No | No |
| B, restart -226472186 | 37/256 = **14.45%** | 8.68–23.09% | No | Yes | No |
| C, restart 1233403840 | 36/256 = **14.06%** | 8.38–22.64% | No | No | No |
| C, restart -7800881 | 0/256 | 0–4.03% | No | No | No |
| C, restart -226472186 | 0/256 | 0–4.03% | No | No | No |

B's successful comparisons against the small baseline have adjusted paired lower differences of **+6.48 and +4.65 percentage points**. These are real gains under the frozen rule. However, the fixed anchor won **95/256 (37.11%)**. B's promising primaries have observed anchor gaps of **−20.31 and −22.66 points**, with adjusted paired intervals **−34.91 to −4.08** and **−37.47 to −6.02** points. Both fail the required lower bound of at least −10 points. The result is supported baseline improvement with inadequate reference competitiveness, not a successful overall search method.

| External control | Validation wins | Observed rate |
| --- | ---: | ---: |
| `team-1a924a7cfff12298633bee909cdea4ad` | 111/256 | 43.36% |
| `team-1abe76ca1891d97a91d484f0a3662048` — fixed anchor | 95/256 | 37.11% |
| `team-38248d838d1db9634fd82536c177df0a` | 83/256 | 32.42% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 66/256 | 25.78% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 66/256 | 25.78% |
| `team-693ffa8ec0b654154a06722aba06a968` | 46/256 | 17.97% |

No cell exceeds 50% in this validation, but the strongest control's joint interval is **33.67–53.58%** and its ordinary adjusted upper bound is **52.96%**. Therefore this tested family cannot establish the universal upper ceiling. The same control's earlier **131/256 (51.17%)** observation remains an unresolved breach. This comparison does not pool studies, erase earlier breaches, prove near-optimality or certify practical acquisition.

Read [all 24 builds and ordered Essence loadouts](../TestResults/balance/tower-search-validation-continuation-20260913/saved-builds.md), the [complete recipe/measurement export](../TestResults/balance/tower-search-validation-continuation-20260913/saved-builds.json), [all trial outcomes](../TestResults/balance/tower-search-validation-continuation-20260913/evidence.json), the [ordinary assessment](../TestResults/balance/tower-search-validation-continuation-20260913/assessment.json), and [joint rates and all 12 paired comparisons](../TestResults/balance/tower-search-validation-continuation-20260913/quality.json). Every zero-win finalist and all six external controls are included. None entered independent generation or was promoted to a catalog.

## Explicit execution continuation

The [original interrupted review](Tower-Sustained-Search-Review.md), original package and its verification package remain unchanged. The [accepted amendment](../TestResults/balance/tower-search-validation-continuation-20260913/amendment.json), SHA-256 `4708dde743450b74273d028830531932efc0ecd3bbdcc86e1c7554efff9ddfe5`, allowed exactly **3,456 additional attempts**, **180 additional execute seconds** and at most **512 MiB** of new output. Its exact missing suffix was prepared before this turn and checked against the retained original prefix before execution.

Fourteen completed results had been lost before their chunk committed, and one original attempt was cancelled. These **15 previously charged attempts were explicitly permitted repeats**. All other continuation fights were previously unstarted. There were no retries beyond those 15, no new seeds and no new diagnostic fights. No attempt journal, original limit or historical receipt was reset.

| Accounting | Result |
| --- | ---: |
| Original retained validation outcomes | 2,688 |
| Additional attempts / completed fights | **3,456 / 3,456** |
| Complete validation family | **6,144 outcomes; 24 × 256** |
| Additional execute time | **56.36 seconds** |
| Cumulative actual attempts / completed fights | **28,439 / 28,438** |
| Original planned schedule now covered, including eight fixed diagnostic slots | 28,424 |
| Previously charged attempts repeated | 15 |
| Further retries / fresh seeds | **0 / 0** |

The cumulative completed-fight count includes the 14 earlier completed-but-uncommitted executions. Those are not extra statistical samples: every original cell/seed appears exactly once in the combined 6,144-outcome validation evidence. The cancelled attempt remains part of actual resource accounting.

The continuation used the original captured executable and content. Before its first fight, zero-combat preparation checks confirmed identical prepared participants for **all 14 suffix cases**, including the case restarted halfway through its seed schedule. Each combined cell was checked against the original full scenario, settings, content, execution identity and exact ordered 256 seeds. Composite source digests identify both retained sources. The original rate allocation, comparator/anchor IDs, primaries, screen decision and multiplicity rules were unchanged.

## Verification and changed files

The [continuation driver](../TestResults/balance/tower-search-validation-continuation-20260913/driver/Program.cs) provides preparation, bounded execution and reconstruction using the captured harness APIs. Its Release build passed without warnings. Before freezing the amendment, a compile-time internal-API access issue and a receipt property-casing issue were corrected; neither executed combat or changed the original packages. An export command initially used the wrong working directory and was rerun correctly. No command remains blocked.

Verification reconstructed **all 6,144 validation outcomes**, all 24 cells, the ordinary assessment and the original joint-quality calculation. Five in-memory rejection checks passed: missing trial, reordered schedule, duplicate/replaced seed, changed scenario and missing suffix case. The original **20,736 discovery outcomes**, nomination and **1,536 screening outcomes** also reconstructed. This verification took **71.53 seconds**, separate from execution, and enforced **zero new fights**. All 29,152 original-package files and the prior verification artifacts matched their sealed hashes.

A [read-only checker](../TestResults/balance/tower-search-validation-continuation-20260913/check-driver/Program.cs) verifies the final package, both sealed original sources, exact exported recipes and the joined assessment without writing output files or running fights. Run from the repository root:

```powershell
dotnet TestResults/balance/tower-search-validation-continuation-20260913/check-driver/bin/Release/net10.0/Check.dll
```

The full original `tower-search-benchmark-verify` command is not the verifier for this composite artifact: its source package intentionally remains interrupted. The continuation's [verification record](../TestResults/balance/tower-search-validation-continuation-20260913/verification.json) and [final receipt](../TestResults/balance/tower-search-validation-continuation-20260913/final-verification.json) record the joined completion, file identities and actual limits.

This turn adds retained continuation/check/export artifacts and this review, and updates the active handoff, strategy status, discovery plan/implementation status and harness README. It does **not** change the generator, simulator, evaluator, game application or tests. The previous **121 distinct passing backend tests** remain implementation evidence; they are not claimed as a new run. The captured source/binary identity, preparation parity, complete reconstruction, malformed-join checks, read-only artifact check, Markdown links and `git diff --check` are the relevant verification for this continuation.

Kharad remains **Health 3.04881408 / Power 3.85370128**, with fixed gear and untrained/unevolved Essences. The [unchanged all-array ledger](../TestResults/balance/tower-search-validation-continuation-20260913/seed-ledger.json) contains **472,525 distinct reservations**. No migration, gameplay configuration change, database change, deployment, infrastructure change, catalog promotion or new floor coverage occurred.

## Decision and next substantial boundary

Close the depth/behavior comparison as **complete with a failed search-reliability result**. The deeper v4 method is a useful experimental comparator because it found supported improvements; it is not a reliable replacement for the reference portfolio. The behavior archive has not earned promotion. Do not repeat the same comparison with fresh seeds or add another provider-category rule on the assumption that the failure is merely missing coverage.

The next substantial work should address **how complete party combinations are represented and proposed**, as called for in the strategic reset when the remedy fails. Prepare one concrete independent-search design that can make and retain coordinated changes across complete ordered character loadouts and their distribution across the party. Derive any construction rules from source mechanics and independently generated candidates; saved control recipes, copy counts and ancestry remain excluded. Its benchmark must distinguish supported viability, improvement over the deeper v4 comparator and competitiveness against the fixed reference portfolio.

The large drop from the deeper finalists' discovery scores of 4/8 to validation rates around 14–17% also motivates reviewing discovery-sample allocation. This is a hypothesis about search generalization, not proof that more samples would find better parties. The retained shortlist cannot reveal whether stronger unselected candidates existed. Settle representation and sampling tradeoffs in that design before allocating another combat campaign; do not bundle several unmeasured changes into a new pilot.

No next policy or campaign is frozen by this review. Calibration, an independent challenge to the strongest controls, practical acquisition and floors 6–11 remain downstream work. A deliberately supplied-build improvement workflow would be a separate explicit choice, not independent discovery relabeled as success.
