# Optional incumbent tie selection

22 September 2026. Target: offline BalanceHarness. Implements the opt-in `tower-staged-incumbent-tie-v1` policy proposed by the [saved selection tie audit](Tower-Practical-Selection-Tie-Review.md). This review documents the engineering change; the subsequent [prospective comparison](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) supplies separate quality evidence and returned **SupportsIncumbentTieForFrozenOutputs**. The existing `tower-staged-zero-win-health-v1` remains the default for practical supplied search. The confirmed team recommendation and closed racing/anchored comparison decisions remain unchanged.

## Behavior

The selector keeps a predesignated supplied primary when it ties another nominee for the **positive maximum selection win count**. A unique leader wins, including a challenger. A tie above the designated primary uses the existing frozen discovery order and stable ID. When every nominee has zero wins, mean guardian health remains the first tie-breaker, followed by frozen discovery order and stable ID.

The new version requires the practical supplied-team scope: an incumbent, racing or anchored generator; two distinct supplied references; one construction root and equipment context; four nominees including both supplied teams; one finalist; and no diagnostics or replays. Existing party legality, fixed composition ordering, budgets and measurement validation still apply. Candidate generation and nomination are unchanged.

## Explicit configuration

Use an existing valid `improve-supplied` definition with one of those generators. Amend its `stages` object with these two fields, retaining its other stage settings:

```json
{
  "selectionPolicyVersion": "tower-staged-incumbent-tie-v1",
  "selectionPrimaryReferenceId": "anchor-0"
}
```

`anchor-0` is an illustrative existing reference ID; use the exact ID from that definition's `starts[].referenceId` and `references[].id`. This is a definition fragment, not a complete executable request. The practical request continues to bind the definition/template through `definitionHash`. Freeze that file and its hash before allocation. The existing practical run and allocated-run commands accept the new selection contract; no new CLI command or allocation algorithm is needed.

The selection designation is **separate from** the top-level `primaryReferenceId` used by anchored candidate generation. They may identify different supplied teams. An anchored parent alone does not opt into incumbent tie selection. A missing, unknown or ambiguous selection designation fails validation; an explicit designation with an older selector also fails rather than being silently ignored. Independent-source conversion does not infer a selection incumbent.

Allocation-template validation binds temporary local labels and validates the complete definition before deriving real values. The bound definition retains both selector fields. The existing request/template hashes and allocation reconstruction protect them against substitution.

## Saved evidence and compatibility

`TowerBossStudyPolicy.Select(definition, mechanics, shortlist, selection)` validates the definition and resolves the canonical supplied party ID before applying the rule. The input-only overload remains available for old selectors; it cannot activate the new policy without the validated designation. Both the live study and native saved-evidence reconstruction call the definition-aware route.

`finalists.json` records why the actual primary won: a retained positive tie, a unique selection leader, a tie above the incumbent, or the zero-win fallback. `confirmation-freeze.json` additionally records `incumbentSelection` containing the designated reference ID, canonical party ID and reason. That record remains about the designated incumbent even when a challenger is selected. The study report displays the rule, designation and actual reason.

Both added JSON properties are omitted when null. Old definitions and confirmation freezes retain their previous serialization, selector ordering and finalist reasons. No archive format version changes. The native verifier reconstructs the designation and choice from the saved definition and measurements and compares the freeze, finalist and report artifacts; updating an outer file hash cannot conceal a changed designation.

The fixed `tower-allocation-comparison-v1` and `tower-anchored-comparison-v1` commands reject the new selector before writing a Pending reservation or drawing entropy, and their bindings repeat the check. Their shared assessment also rejects a freeze using it. These closed generator contracts cannot become tests of a different selector through a changed template.

## Changed files

| File | Purpose |
| --- | --- |
| `TowerBossDiscoveryContract.cs` | Optional stage designation and validation of the new version's scope. |
| `TowerBossStudyPolicy.cs` | Definition-aware selection, positive-tie behavior, exact selection reasons and frozen designation. |
| `TowerBossStudy.cs` | Shared live/reconstruction call to the definition-aware selector. |
| `TowerBossStudyMarkdown.cs` | New-policy reporting while retaining old report output. |
| `TowerAllocationComparison.cs`, `TowerAnchoredComparison.cs` | Preserve the original selector in closed comparison contracts. |
| `BalanceHarnessIncumbentTieTests.cs` | Behavioral, binding, compatibility and native reconstruction/tampering fixtures. |
| This review, the audit review, harness README and practical guide | Usage, current status and next evaluation step. |

## Verification

**206 tests passed, zero failures or skips**, including **20 new selector cases**, using the repository wrapper. The final build had zero errors and 38 existing warnings across the referenced projects. The targeted suite includes existing incumbent, racing, anchored, practical allocation, comparison, discovery-contract and study regressions. Native integration fixtures use test schedules; no scientific comparison, historical reservation or adoption evaluation was launched.

The new cases cover positive two-way and multi-way ties, strict leaders, ties above the primary, zero-win health/order, absent nominees, missing/unknown/ambiguous designations, unsupported scopes, all three practical generators, allocation binding, original comparison rejection, identical nomination/legacy outputs and omission of new fields in old JSON. A small native archive reconstructs successfully with a combat guard active, then rejects changed frozen party identity and changed definition designation even after the affected outer inventory hashes are recomputed. Confirmation evidence never enters selection.

The first successful 206-test run preceded the final reservation-boundary refinement; the final source was rebuilt and the same 206 tests passed again. Final build time was 96.98 seconds and test time about 100 seconds. Test evidence: [TRX](../TestResults/incumbent-tie-verification-20260922/tests.trx), [final build/test log](../TestResults/incumbent-tie-tests-final-20260922.log), and [verification receipt](../TestResults/incumbent-tie-verification-20260922/verification.json).

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/incumbent-tie-build-20260922 -Filter 'FullyQualifiedName~BalanceHarnessIncumbentTieTests|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessAnchoredNeighborhoodTests|FullyQualifiedName~BalanceHarnessEvaluationAllocationTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessPracticalAllocationTests|FullyQualifiedName~BalanceHarnessAllocationComparisonTests|FullyQualifiedName~BalanceHarnessAnchoredComparisonTests|FullyQualifiedName~BalanceHarnessTowerBossStudy|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
```

The first sandboxed build could not read the user's NuGet configuration. The same repository wrapper was restarted with approved access and completed; no required verification remains blocked. The earlier audit's 111 consumed-input hashes and final artifact inventory were rechecked without another scientific analysis or combat run. Local Markdown links and scoped whitespace checks passed. Unrelated concurrent checkout edits were preserved. No deployment or database operation is involved.

## Next step

The [single prospective comparison completed](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) under the frozen 24-restart design. Both native and independent audits passed: four differing outputs, four positive restarts, +2.575 points over the fixed 24,000 denominator and a +1.930-point conditional lower bound. The result supports the optional rule for the captured scope and frozen outputs. The subsequent [practical-search preset](Tower-Practical-Incumbent-Tie-Preset.md) now prepares explicitly designated inputs while preserving legacy definitions and archive replay. For a separately scoped future search, prepare current inputs and pass no-allocation admission. Default promotion remains a separate reviewed decision. The experiment is closed; the old five-point gates and retrospective audit findings remain unchanged.

No gameplay logic, database schema or application configuration changed. There are no migrations or deployment steps. The new settings affect only definitions that explicitly opt into this offline selector.
