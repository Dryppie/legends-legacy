# Periodic reference exploration: implementation

22 September 2026. Target: offline `LL/tools/BalanceHarness`. The opt-in generation policy `retained-composition-three-reference-exploration-v1` implements the bounded intervention proposed by the [saved-trajectory coverage review](Tower-Practical-Three-Reference-Coverage-Review.md). The subsequent [twelve-pair comparison](Tower-Practical-Reference-Exploration-Comparison-Execution.md) completed 53,672 fights and both audits with `DoNotPromoteReferenceExploration`. Eleven pairs selected identical outputs; the mean candidate-minus-baseline difference was −0.142 percentage points. The [frozen plan](Tower-Practical-Reference-Exploration-Comparison-Plan.md), [controller](Tower-Practical-Reference-Exploration-Comparison-Implementation.md) and [admission](Tower-Practical-Reference-Exploration-Comparison-Admission.md) remain preserved. This allocation is closed and defaults remain unchanged.

## Policy contract

The new policy retains exactly three references, one construction root and equipment context, canonical Essence order, fixed actor identities and equipment, the declared pool and optional owned-copy limits. It requires 3–15 character slots so every scheduled radius can be constructed. Existing policies and preparation defaults are preserved.

| Opportunity | Behavior |
| --- | --- |
| First three proposals | Evaluate the three supplied references, in the existing start-ID order. |
| Next six proposals | Existing fresh legal construction and its original random stream. |
| Subsequent mutation proposals | Existing top-four/supplied parent choice, operator cycle, donor handling and random stream. |
| Subsequent every fourth proposal | Perturb one supplied reference using the new exploration constructor. The first such proposal has zero-based index 12. |

Exploration visits the references in ordinal **reference-ID** order, independently of outcomes. Each reference has its own zero-based visit count; radius alternates 2, 3, 2, 3. A separate cyclic character cursor selects that many distinct owners. Cumulative scheduled owner counts within each reference differ by at most one, including rejected opportunities. This balances scheduled opportunities, not necessarily accepted edits: legality and duplicates can still limit accepted coverage.

Each chosen owner loses exactly one Essence and receives one different Essence. All removed copies are freed before replacements are chosen, permitting legal transfers under an owned-copy ceiling. Replacement options are ordered by Essence ID and filtered for case-insensitive family uniqueness and ownership. The completed party is canonicalized and checked through the shared legality validator. Its distance from the reference must be exactly two or three replacements (four or six in the harness's symmetric set-difference convention).

Construction uses at most **32 checks per opportunity**. A legal duplicate is recorded as a duplicate through the existing search deduplication; it is not reconstructed again within that opportunity. Failed construction records `construction-exhausted`. Schedule counters advance before either outcome. The existing evaluated-candidate and attempted-proposal caps govern stopping; inability to reach the candidate target remains `Incomplete`, with no shortlist or hidden refill.

`TowerReferenceExploration` owns the schedule and constructor. It uses a separate random stream derived from its version, construction root and `exploration` namespace. Initial fresh proposals and the baseline mutation stream keep their existing namespaces. Later adaptive parents can differ as the new proposals change rankings; identical later trajectories are not promised across different policies.

## Provenance and downstream decisions

Each `reference-exploration` proposal points directly to its already evaluated supplied proposal and inherits its exact reference ID. The nullable `supplied.exploration` record contains `referenceId`, `visit`, `radius`, `scheduledSlots` and actual `constructionChecks`. Existing `changedSlots` identifies actual edited owners. Rejected construction retains the schedule metadata even when no party exists. The new field is omitted for legacy proposals, preserving legacy serialized bytes.

Validation permits this operator only under the new generation version and rejects ancestry through an intervening mutant. Native study and compact archive verification regenerate the schedule, construction and trace from saved measurements. Re-sealing a changed trace's outer file inventory does not make it valid.

Nomination remains **all three references plus the two highest-ranked eligible challengers**. Selection keeps the declared zero-win policy or explicit incumbent-tie designation; the new policy does not designate a reference. There is one selected output, three or four distinct confirmation recipes, and the unchanged ten-quantity practical interval family. Improvement must still pass against all three controls.

The declared request/result version remains `tower-practical-three-reference-search-v1`; allocation uses `tower-practical-three-reference-allocated-search-v1`. Those versions describe the three-control workflow. The definition's separately pinned generation version declares the constructor. Two-reference request/version mismatches still fail. Allocation, publication and interrupted-allocation recovery validate the new definition while preserving ownership, reserved prefixes and closed-attempt semantics.

## Explicit opt-in

For a newly declared, valid independent schema-3 definition containing the three exact references, preparation accepts:

```text
tower-retained-composition-prepare --definition <new-definition.json> --references <id-a,id-b,id-c> --policy-version retained-composition-three-reference-exploration-v1 --output <new-preparation-directory>
```

This validates and prepares inputs; it does not execute a search. For the allocated practical workflow, declare the new generation version in a **new unscheduled template**, retain the three-reference allocated request version, and bind its new definition hash before admission. The existing three-reference reuse preset still emits `retained-composition-three-references-v1`. Historical definitions, sealed requests and captured executables are not upgraded in place.

## Verification and changed files

**275 scoped backend tests passed**, with zero failures or skips. The [regression log](../TestResults/reference-exploration-after-20260922/regression.log) and [TRX results](../TestResults/reference-exploration-after-20260922/regression.trx) cover:

- Exact reference/radius/owner scheduling for 3, 5, 10 and 15 characters; legal owned-copy transfers; family aliases; canonical distance and reproducibility.
- A finite 27-recipe space that produces both duplicates and construction exhaustion, consumes all 256 proposal opportunities and stops incomplete without extra evaluations or nomination.
- All three protected references, two ranked challengers, three/four confirmation recipes and family-ten intervals.
- Practical request binding, publication reconstruction, request-version tampering and permanently reserved interrupted-allocation prefixes under both generation versions.
- Native study and compact discovery reconstruction, with combat forbidden during verification, plus rejection of a re-sealed exploration construction-count change.
- Three legacy construction roots whose complete result JSON remains byte-identical to outputs captured before the implementation, plus the existing supplied-search, selection, contract and practical regressions.

The successful command was:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExploration|FullyQualifiedName~BalanceHarnessThreeReferenceTests|FullyQualifiedName~BalanceHarnessPracticalAllocationRecoveryTests|FullyQualifiedName~BalanceHarnessSupplied|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessTowerBossStudyPolicyTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessPracticalAllocationTests'
```

The first sandboxed build could not read the existing user NuGet configuration. The approved retry succeeded. The first focused run passed 106/109 tests; its three failures came from a new test incorrectly treating intentionally rejected baseline proposals as legal candidates. The assertion was corrected, the finite-space test was added, and the subsequent 275-test run passed. The final incremental build reported nine existing warnings outside the new exploration files and no errors. No required verification remains blocked; the full repository test suite was not run. Scoped whitespace checks passed. An initial repository-wide whitespace check found a pre-existing trailing-space line in the unrelated `LL/docs/strongholds-mechanical-progression-revision.md`; it was left intact.

The new native archive fixture executes a temporary engineering study of at most 420 fights and a 256-fight compact reconstruction fixture. These are bounded implementation tests, not a scientific policy comparison; other existing regression fixtures retain their own test workloads. No scientific fights or registry values were added. The [full history preservation check](../TestResults/reference-exploration-after-20260922/history-preservation.json) matched all **551,408 exclusions across 232 files** byte-for-byte in **69.094 seconds**. Historical execution and coverage manifests remain pinned and unchanged. The [verification receipt](../TestResults/reference-exploration-after-20260922/verification.json) records source and test evidence hashes.

Source changes are the new `TowerReferenceExploration.cs`, integration in `TowerSuppliedCompositionSearch.cs`, version/ancestry validation in `TowerBossDiscoveryContract.cs`, composition classification in `TowerCompositionSearch.cs`, protected selection in `TowerBossStudyPolicy.cs`, practical version grouping in `TowerPracticalThreeReference.cs`, and CLI help in `Program.cs`.

Tests are in the new `BalanceHarnessReferenceExplorationTests.cs` and `BalanceHarnessReferenceExplorationArchiveTests.cs`, with new policy cases in `BalanceHarnessThreeReferenceTests.cs` and `BalanceHarnessPracticalAllocationRecoveryTests.cs`. Documentation changes are this report and current links in the coverage, execution, three-reference search, state, README and practical-search guides.

## Saved-search diagnosis and next experiment boundary

The [completed read-only diagnosis](Tower-Practical-Reference-Exploration-Diagnosis.md) separates construction, nomination and selection losses. All 122 direct proposals were evaluated; seven were nominated and none selected. Fixed cursor resets also left slots 8–10 of reference `96b94357…` untouched by direct exploration across all twelve roots. The [owner-offset revision](Tower-Practical-Reference-Exploration-Offset-Implementation.md) is now implemented as a new opt-in version, with 325 passing scoped tests and byte compatibility for this original policy. A new prospective comparison and captured-runtime admission remain necessary before testing strength.

The [frozen comparison](Tower-Practical-Reference-Exploration-Comparison-Plan.md) specified twelve paired roots, unchanged nomination and selection, all three shared controls, a five-point mean-gain gate with at least seven positive pairs, and a conditional confidence bound. The separately implemented and admitted comparison completed within its cumulative 10,800-second /6-GiB envelope. Its result does not support promotion. The later diagnosis does not tune against confirmation outcomes or reopen the allocation. The diagnostic root that originally motivated this policy cannot validate it.

Implementation and temporary engineering fixtures do not authorize that experiment or establish stronger-team discovery. The closed study's unused allowance stays closed. Engineering remains separately disclosed under the user's existing accounting decision; this work does not infer missing historical engineering totals or revise the prior-charge ledger. No application configuration, migrations, database changes or deployment is involved.
