# Paired anchored-neighborhood quality comparison

17 September 2026. **Prospective protocol, frozen before allocation or combat.** Target: offline BalanceHarness supplied-team refinement using the captured gameplay/content of the confirmed fixed-team study. This follows the [anchored-policy implementation](Tower-Practical-Anchored-Neighborhood-Implementation.md) and keeps the closed racing comparison and current-family calibration separate.

**Completed and verified: `DoNotPromoteAnchored`.** The mean difference was +1.70 percentage points, below the declared five-point requirement, with a one-sided 95% lower bound of −2.78 points. The incumbent default and confirmed team remain unchanged. See the outcome and selection follow-up below.

## Question and scope

Does `anchored-neighborhood-v1` select stronger teams than `retained-composition-incumbents-v1` with the same supplied knowledge and search budget? Both methods receive the exact confirmed `399bc776…` recipe and Anchor040e (`8287f779…` canonical recipe). The confirmed recipe is explicitly designated as the anchored method's primary parent. Neither method receives historical fitness. This compares the whole supplied-refinement policy, not the isolated effect of character balancing.

The ten level-40 characters, five Essences each, two subgroups, equipment, canonical ability order, content and gameplay assemblies stay fixed. `OwnedCopies=null` preserves the existing acquisition assumption. Only the new harness executable changes; gameplay DLL hashes must match the retained capture. The primary reference and both complete starting recipes are pinned in the seed-free template before drawing values.

Three restart pairs are declared. Within each pair, both methods share one construction root, eight discovery values, 32 selection values and 1,000 confirmation values. Pairs have disjoint values. Each method evaluates 46 candidates, with a 256-attempt ceiling for the incumbent and the anchored method's fixed batch of two supplied teams plus 44 legal neighbors. Both reserve **368 discovery fights**, **128 selection fights**, four nominees and one finalist. Both supplied teams remain confirmation controls. Exact recipe convergence is deduplicated by the existing study machinery; unused capacity is not reassigned. The overall ceiling is **20,976 fights**.

## Allocation and stopping

The separately versioned command is `tower-anchored-comparison-run`, with request version `tower-anchored-comparison-v1`. The historical `tower-allocation-comparison-v1` remains racing-only.

One 4,096-word cryptographic draw supplies 3,123 distinct fresh assigned values: 3 × (1 + 8 + 32 + 1,000). Previously reserved and duplicate values are skipped. Every fresh unused tail value is also permanently excluded. The initial historical union is **501,467**; live-registry checks must agree. A durable Pending marker precedes entropy generation; the complete batch is retained before the reservation can become Complete. There is no refill, retry, resume, replacement restart or sample extension. Any failed search, missing freeze, incomplete confirmation, failed reconstruction or resource overrun ends the experiment without a positive decision; planned pairs cannot be discarded.

All six selected outputs and confirmation families are frozen before the first confirmation fight. Each anchored candidate batch is separately persisted before its first discovery fight. Combats are serialized and every attempt is journaled. The unchanged native study verifier reconstructs all six archives without added combat. An independent saved-evidence audit checks assignments, paired outcomes, candidate quotas/edits, accounting, freeze records and endpoint arithmetic.

## Endpoint and precision

The primary endpoint is the equally weighted mean of three paired confirmation win-rate differences, anchored minus incumbent. Draws and defeats count as non-wins. Report all three selected recipe pairs, wins, gained/lost wins, actual fight costs and the mean. Confirmation cannot change the chosen outputs.

The declared useful-improvement threshold remains **five percentage points**. Support requires at least 150 net gained wins across 3,000 paired trials, a positive one-sided 95% conditional lower bound, and positive point differences in at least two of three pairs. The decisions are `SupportsAnchoredForFrozenOutputs` or `DoNotPromoteAnchored`. Failure closes this version's evaluation without changing the endpoint, extending the sample or substituting a coverage/efficiency claim. A positive comparison does not itself replace the existing fixed-team adoption or balance gates.

For the conservative conditional bound, each paired difference is in [-1,1]. Conditional on the search observations, set `M = 2^32 - historicalCount - 123`, `n = 3000`, `delta = n(n-1)/(2M)` and `alpha = 0.05 - delta`. Coupling distinct uniform draws to independent draws charges the birthday union bound `delta`. The reported lower bound is `max(-1, mean - sqrt(2 log(1/alpha)/n))`. Its margin is about 4.49 points at this historical size, so the five-point point-estimate requirement is stronger than the positive-bound requirement.

This bounds the average strength difference of these frozen outputs under this scope. It does not quantify variation over future search roots. Three restarts do not establish general reliability, and the comparison is not powered to settle small improvements. The distribution-free lower bound on passing the aggregate five-point threshold, at a true conditional effect above five points, is `1 - exp(-n*(effect-.05)^2/2)` before the extra two-positive-pairs condition; that condition can further reduce power. No precision claim is made by pooling previous experimental panels.

## Resource envelope and retained evidence

One new owned run has a **2,400-second /2-GiB** total ceiling. The native command receives 2,380 seconds and 1,920 MiB, retaining headroom for the enclosing owner and logs. The Windows Job owner bounds the process tree; a hard native deadline also applies. This allowance is separate from all closed prior experiments. Builds, tests, seed-free preparation, preflight, independent saved-row review and documentation are engineering work outside the operational allowance and will be disclosed separately.

Preparation and launch script: [practical-anchored-comparison.py](analysis/practical-anchored-comparison.py). Independent audit: [audit-anchored-comparison.py](analysis/audit-anchored-comparison.py). Package: `TestResults/anchored-comparison-20260917/`. Native output: `TestResults/balance/tower-anchored-comparison-20260917/`. The create-new launch receipt prevents a second invocation. The package retains this prospective text before outcomes exist.

No application configuration, migration or deployment is involved. The incumbent search default and confirmed team recommendation stay unchanged unless a later valid adoption decision changes them.

## Implementation and preflight

`TowerAnchoredComparison.cs` supplies the explicit anchored binding and result schema. `TowerAllocationComparison.cs` shares its execution, durable reservation, barrier and conditional-bound calculation while retaining the old racing defaults and public result schema. `Program.cs` exposes the new command. `BalanceHarnessAnchoredComparisonTests.cs` checks paired binding, designation scope, distinct reservations, the real six-study freeze, exact five-point threshold, the two-positive-pairs condition, missing/reused panels and cross-version rejection.

**111 relevant tests passed**, including the four new comparison cases, old racing comparison cases, anchored and racing kernels, practical search and native study regressions. The final build had zero errors and nine existing test warnings. The initial test fixture mistakenly used the anchored fixture's 46-attempt cap for the incumbent; duplicate proposals prevented one synthetic study reaching the barrier. That test run was stopped, the fixture was changed to the declared 256 attempts and given a cancellation deadline, then the full targeted set passed. No experimental allocation had occurred. The stalled log is retained. A Python syntax error in the new launcher was also corrected before preparation.

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/anchored-comparison-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessAnchoredComparisonTests|FullyQualifiedName~BalanceHarnessAllocationComparisonTests|FullyQualifiedName~BalanceHarnessAnchoredNeighborhoodTests|FullyQualifiedName~BalanceHarnessEvaluationAllocationTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessTowerBossStudy'
```

The native allocation-shape check returned `ReadyNoReservation`, with **501,467 historical values**, zero new values and zero fights. It ran against the copied gameplay/content, both exact recipes and the explicit primary designation. The packaged harness SHA-256 matched the tested build. Seed-free preparation took **1.44 seconds**; tests, preflight and documentation are outside the owned experimental allowance. Logs and TRX: `TestResults/anchored-comparison-verification-20260917/`. No required preflight remains blocked.

The original prospective text is retained in `TestResults/anchored-comparison-20260917/prospective-design.md`. It predates the one owned launch and is unchanged.

## Verified outcome

The [native result](../TestResults/balance/tower-anchored-comparison-20260917/result.json) and [separate saved-evidence audit](../TestResults/anchored-comparison-20260917/independent-audit.json) agree.

| Restart | Incumbent search wins | Anchored search wins | Anchored minus incumbent |
| ---: | ---: | ---: | ---: |
| 1 | 613 / 1,000 | 692 / 1,000 | +7.90 points |
| 2 | 727 / 1,000 | 699 / 1,000 | −2.80 points |
| 3 | 739 / 1,000 | 739 / 1,000 | 0.00 points |

The equally weighted mean is **+1.70 points**, with the predeclared conditional lower bound **−2.78 points**. Only one pair has a positive difference. The comparison fails the declared mean, bound and two-positive-pairs requirements. This does not demonstrate the required search improvement, and it does not establish general equivalence or a population-level regression. Close this version's evaluation without extending samples, changing the endpoint or promoting a coverage claim into a strength result.

In restart one, the incumbent selected `37918562…`; the anchored method retained the confirmed `399bc776…` team. Its advantage therefore came from avoiding a weaker selected challenger, not finding a stronger replacement. In restart two, the anchored method selected `8d87215b…`, which recorded 699/1,000 versus the confirmed team's 727/1,000 on the same panel. Both methods selected the confirmed team in restart three. None of the selected challengers demonstrated an advantage over the confirmed team on its corresponding panel. Unselected candidates were not independently confirmed, so their true strength remains unknown.

Every anchored run constructed its 44 unique legal edits with the declared four/five-character quotas. The audit verified each frozen batch, primary ancestry, edit descriptors, canonical recipes, legal option counts and the unchanged evaluated batch. This establishes implementation coverage; it does not change the negative quality decision.

All **16,976 attempted fights completed**: 2,208 discovery, 768 selection and 14,000 confirmation. Both methods spent exactly 368 discovery and 128 selection fights per restart. All six outputs were frozen after 2,976 fights, before confirmation. Six native reconstructions added zero fights, and the separate audit agreed in **2.98 seconds**, also with zero fights.

The owned run completed in **845.36 seconds (14.09 minutes)**, including native verification, with a sampled combined storage high-water of **851,211,591 bytes (811.78 MiB)**. The owner reported exit 0, no timeout and zero remaining processes. These values are within the declared envelope; they are not a per-method speed comparison. There was one launch, no retry, no refill and no extension.

The entropy batch contained one historical collision and no duplicate fresh words. It reserved **4,095 fresh values**: 3,123 assigned and 972 unused permanent exclusions. The complete historical union is now **505,562**. Prior allowances and historical decisions remain closed.

## Completed follow-up: selection ties

The saved selection rows identify a concrete follow-up. In incumbent restart one, the selected challenger and confirmed team both won **22/32** selection fights. In anchored restart two, they both won **23/32**. The unchanged selector breaks these nonzero-win ties by the earlier discovery ranking, which favored the challenger in both cases. Those challengers then recorded worse confirmation results than the confirmed team.

Audit this tie behavior across retained runs before proposing another generator. A future rule that retains a designated incumbent when selection shows no measured advantage would need its own explicit contract and fresh validation. These two observed ties are diagnostic evidence, not a prospective test of that rule. No selector, threshold, default or endpoint was changed after seeing these outcomes, and no additional experiment was launched.

The subsequent [selection tie audit](Tower-Practical-Selection-Tie-Review.md) reconstructed all 12 outputs from both comparisons plus the earlier diagnostic primary. Three comparison outputs had top ties; a positive-win tie rule retaining the designated primary would change the two cases above and preserve all strict leaders. Nine analyzer tests passed, with zero new fights or values. The review defines the optional versioned selector and archive-verification contract as the next engineering step. This retrospective finding does not change this comparison's closed decision.
