# Practical search evaluation allocation: implementation

17 September 2026. Target: the offline BalanceHarness. Implements the [evaluation-allocation proposal](Tower-Practical-Search-Evaluation-Allocation-Proposal.md) as the optional `retained-composition-racing-v1` policy. The existing `retained-composition-incumbents-v1` remains the default practical policy.

This document records implementation and fixture verification. The subsequent [paired comparison protocol and result](Tower-Practical-Search-Allocation-Comparison.md) missed the declared strength endpoint: -0.73 percentage points across three paired restarts. The policy remains optional and is not promoted; the incumbent policy stays the default.

## Frozen policy behavior

The policy requires the existing supplied-reference scope: exactly two distinct canonical starts, one context, one construction root, four nominees, one finalist, and no diagnostic or replay allowance. The starting recipes are explicit inputs; the harness does not silently import the confirmed team or historical outcomes. For a future practical comparison, supply the exact confirmed candidate and the same second reference to both methods.

Each round admits eight new distinct legal recipes. The first round includes the two supplied teams and six fresh construction opportunities. Subsequent proposals retain the baseline attempt-based fresh schedule, single-neighborhood traversal, five mutation/recombination operators, and occasional supplied-parent selection. Invalid and duplicate proposals consume the existing attempt budget without combat. A round that cannot fill its eight recipes stops the search as Incomplete without evaluating that partial batch.

1. Freeze the batch before reading any of its outcomes. New recipes cannot parent another proposal in that batch.
2. Screen the batch, previous four parents and both supplied teams on eight common trials, deduplicating identical recipes within the round.
3. Freeze the four screening leaders plus both supplied teams for promotion. This is four to six distinct recipes.
4. Evaluate every promoted recipe on 16 common fresh trials. Those trials are separate from screening and every other round.
5. Rank only the current promotion measurements to select the next four parents. Historical or differently sampled measurements are never mixed into this ranking. Both supplied teams also retain the baseline's explicit parent access.
6. After the final round, nominate both supplied teams and the two highest-ranked distinct challengers from that round's promotion panel. Freeze their order before the unchanged selection and confirmation stages.

Ranking retains the existing win-rate, remaining guardian health, survival, winning duration and stable-ID order. These training tournaments do not apply a statistical strength gate. An eight-trial screen can still discard a good candidate; the additional precision costs search breadth. Neither tradeoff has yet earned promotion over the comparator.

## Cost and schedules

The existing `Discovery` schedule now means the concatenation of each round's eight screening and 16 promotion trial values for this policy only. Every declared value remains subject to the existing uniqueness, historical exclusion, stage separation and durable reservation rules.

| CandidatesPerArm | Rounds | Discovery schedule length | Maximum discovery fights |
| ---: | ---: | ---: | ---: |
| 16 | 2 | 48 | 368 |
| 24 | 3 | 72 | 576 |
| 32 | 4 | 96 | 784 |

The first round reserves at most `8 * 8 + 6 * 16 = 160` fights. Each subsequent round reserves at most `14 * 8 + 6 * 16 = 208`. Actual consumption can be lower when the supplied teams already occupy parent or promotion positions. Unspent capacity is reported; it does not buy extra candidates or samples. All parent and supplied-team reevaluations count toward the reservation.

The 2–4-round range fits the existing 100-value discovery schedule limit. A different batch size, sampling ratio, operator schedule or retention rule requires a different version. The candidate count selects a bounded number of these same rounds.

Selection and confirmation costs are added by the existing contract. For example, two rounds, 32 selection trials for each of four nominees, and up to three distinct confirmation teams at 256 trials reserve at most `368 + 128 + 768 = 1,264` fights. This arithmetic is not a precision assessment or a granted experiment budget.

## Integration and use

- [TowerEvaluationAllocationSearch.cs](../LL/tools/BalanceHarness/TowerEvaluationAllocationSearch.cs) implements batching, fresh-panel promotion, parent retention and final nomination.
- [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs) exposes the unchanged single-edit neighborhood and operator schedule. Its legacy fixed-panel entry point rejects racing rather than silently evaluating on the wrong schedule.
- Discovery contracts, canonical composition checks and dispatch recognize the new version. [TowerBossStudy.cs](../LL/tools/BalanceHarness/TowerBossStudy.cs), ordinary discovery and compact discovery supply the requested panels through the real measurement path.
- The existing practical run, allocation, recovery, attempt accounting, verification and seed-free export paths accept the explicit racing policy. Defaults and the final practical strength gate remain unchanged.

For the preferred allocation workflow, use a new unscheduled supplied-definition template with `Generation.PolicyVersion = "retained-composition-racing-v1"`, `Methods = ["retained-composition"]`, and `CandidatesPerArm = 16`, `24` or `32`. Set `Allocation.DiscoverySamples` to `48`, `72` or `96` respectively. Keep all template schedules empty, supply the complete exclusion history, and use the existing request/ownership/resource fields described in the [practical guide](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md). Template shape validation uses local labels only; it does not reserve real values.

The retained-composition preparation command also accepts `--policy-version retained-composition-racing-v1` when given a compatible independent source definition with explicitly supplied schedules and exactly the declared two references. As before, it validates the source before conversion and performs native content checks. Ordinary `tower-boss-discover`, `tower-boss-study`, their verification routes, and compact discovery recognize the new policy. Use the practical runner for complete history checks and owned execution.

No runnable request or real schedules were generated in this implementation. Reuse the existing commands only with a separately prepared prospective experiment definition and matching runtime/content identities.

## Retained evidence and failure behavior

Each arm saves `evaluationRounds`, including parent membership, new candidate IDs, both panels, all screening/promotion measurements, frozen promotion membership and resulting parents. Its existing `evaluations` list retains each new recipe's first screen. Every later measurement remains in the round trace and contributes to accounting and earlier balance-breach reporting.

Native archive reconstruction reruns the same decisions from recorded battles and matches recipes, arms, stages and trial values. Compact reconstruction consumes the same round batches. Shortlist exports and displayed discovery scores use the final promotion panel, not an invented full-schedule measurement. Existing serialized reports omit the new field when unused, preserving historical policy output shapes.

Incomplete measurements, cancellation, invalid evidence or exhausted proposals cannot nominate teams. A partially completed native measurement may have charged attempts before a complete scalar measurement exists; the outer attempt ledger remains authoritative for such failures. The existing scientific strength and balance decisions remain separate from execution success.

## First comparison

These recommendations are now instantiated in the [prospective three-pair comparison](Tower-Practical-Search-Allocation-Comparison.md), which owns its separate schedules, execution allowance and decision.

Use the two-round policy against the unchanged incumbent policy configured for 46 candidates on eight discovery trials: both reserve 368 discovery fights. Hold supplied teams, gameplay context, operator availability, selection rule and downstream evaluation budget constant. Include several predeclared construction restarts and report actual costs as well as the equal ceilings.

The policy differences include fewer proposals, frozen batches, refreshed panels and additional parent evaluation. This tests that complete allocation policy; it cannot isolate a benefit from any one component. Confirm final outputs on an untouched paired panel and predeclare the useful improvement threshold, comparison family, precision and overall stopping rule. The current single-run practical gate does not by itself establish superiority of one search method across multiple runs.

If the prospective comparison misses its endpoint, close this version rather than extending samples or tuning ratios after observing the result. The larger historical evaluation proposal and its decisions are not reopened by this implementation.

## Verification

**304 distinct tests passed, zero failed or skipped.** The [303-case regression result](../TestResults/evaluation-allocation-verification-20260917/search-regressions.trx) includes all **17 new policy tests** plus practical execution/allocation/recovery, incumbent selection, retained search parity, discovery contracts and study verification. The separate [compact archive parity test](../TestResults/evaluation-allocation-verification-20260917/compact-parity.trx) also passed.

The new fixtures use fabricated complete-party measurements and an engine guard. They cover fresh paired panels, exact reevaluation accounting, delayed parent changes after a lucky screen, deterministic reconstruction, schedule tampering, invalid contracts, incomplete promotion, cancellation, proposal exhaustion, allocator binding, unchanged operator scheduling, and practical stage/export integration. Existing native regression fixtures exercise their own temporary test scenarios; they are not a search-quality experiment. A full native racing workload and a measured old/new algorithm comparison were unperformed at this implementation checkpoint; the subsequent comparison is linked above.

Commands, through the repository test wrapper:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/evaluation-allocation-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessEvaluationAllocationTests|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessRetainedStandaloneTests|FullyQualifiedName~BalanceHarnessSuppliedSchedulingParityTests|FullyQualifiedName~BalanceHarnessPractical|FullyQualifiedName~BalanceHarnessTowerBossStudy|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryRunTests'
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/evaluation-allocation-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessTowerBulkTests.Explicit_supplied_improvement_keeps_its_provenance_and_scores_on_the_compact_path'
```

The initial sandboxed build could not read the user NuGet configuration; the approved retry resolved that access issue. The final build succeeded with nine existing warnings and no errors. Scoped whitespace and local-link checks passed. No required verification remains blocked.

Changed files comprise the new search kernel and tests; the existing contract, composition, dispatch, ordinary/compact discovery, study accounting and practical entry points; and this implementation document, the proposal and two usage guides. Sealed historical experiment packages and gameplay source were not edited. No production deployment, database migration or application configuration change is required. Search request configuration changes only when explicitly opting into the new policy.
