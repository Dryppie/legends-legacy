# Anchored-neighborhood implementation

17 September 2026. Target: offline `LL/tools/BalanceHarness`. Implements the [approved proposal](Tower-Practical-Anchored-Neighborhood-Proposal.md) as optional `anchored-neighborhood-v1`. The [subsequent paired comparison](Tower-Practical-Anchored-Neighborhood-Comparison.md) completed with `DoNotPromoteAnchored`: +1.70 points on average, below its declared improvement gate. The existing incumbent policy and confirmed team recommendation remain the defaults.

## Behavior and budgets

The definition must explicitly name `primaryReferenceId`, identifying one of its two supplied references. This field belongs to the improvement definition and is excluded from the reference-free generation-input boundary. It is rejected for other policies. The first version requires ten characters, five Essences per character and one context.

Construction sorts the eligible Essence IDs, enumerates each character's legal `(removed, added)` edits, and excludes both supplied recipes. It enforces case-insensitive source-family uniqueness and whole-party owned-copy limits, including zero copies for missing inventory entries. A version/root-derived stream shuffles character order. Independent version/root/character streams shuffle the legal edit lists uniformly without replacement. Four complete passes and four extra positions give every character four or five neighbors. If any quota is impossible, the entire batch is rejected before combat; there is no quota redistribution, fallback operator or retry.

The resulting two supplied teams and 44 unique neighbors are frozen before any evaluation. Every neighbor descends from the designated supplied proposal, regardless of its measured fitness. No discovery outcome changes construction. Character identities, equipment, progression and the other 49 Essence assignments remain fixed.

| Stage | Allocation |
| --- | --- |
| Discovery | 46 unique teams × 8 shared values = 368 fights |
| Nomination | Both supplied teams plus the two highest-ranked challengers |
| Selection | 4 nominees × 32 shared values = 128 fights |
| Finalist | One team, using the existing selection policy |
| Confirmation | Existing practical workflow: 256–1,000 declared values per distinct finalist/control recipe |
| Maximum study fights | 496 + 3 × confirmation sample count; exact recipe convergence can reduce actual confirmation cost |

`maximumAttemptsPerArm` retains the existing bounded contract (46–256 here); this kernel constructs exactly 46 admitted proposals and does not spend extra attempts. `freshEvery: 4` remains a compatibility field and does not introduce fresh teams into this policy.

## Use through the existing workflow

For a seed-free practical allocation template, set:

```json
{
  "mode": "improve-supplied",
  "primaryReferenceId": "your-primary-reference-id",
  "generation": {
    "methods": ["retained-composition"],
    "seeds": [],
    "candidatesPerArm": 46,
    "maximumAttemptsPerArm": 46,
    "freshEvery": 4,
    "policyVersion": "anchored-neighborhood-v1"
  }
}
```

This is a partial field illustration, not a complete request. Retain the existing objective, two canonical supplied references and starts, content/runtime hashes, historical exclusions and resource limits. Leave all stage schedules empty in the allocation template; request eight discovery samples, 32 selection samples and a prospectively chosen confirmation count. The stages require four nominees, one finalist, zero diagnostic candidates, zero replay reserve and `tower-staged-zero-win-health-v1`. The existing practical allocator binds fresh schedules and preserves `primaryReferenceId`; the existing run, audit and recovery commands remain in use. No real allocation request was created or executed for this implementation.

The preparation CLI also accepts a valid independent source definition with already-declared schedules:

```text
dotnet BalanceHarness.dll tower-retained-composition-prepare --definition <source.json> --references <primary-id>,<secondary-id> --policy-version anchored-neighborhood-v1 --primary-reference <primary-id> --output <new-directory> --content-root <API.LL-directory>
```

The input must itself pass validation before conversion. Both supplied recipes are canonicalized; the designation is explicit and never inferred from reference order, historical wins or candidate scores. Omitting the new policy keeps the existing preparation default. Passing `--primary-reference` for a legacy policy fails validation.

## Archives and compatibility

Native study/discovery and compact discovery write `anchored-candidate-batch.json` before the first fight. It contains all 46 proposed recipes, their edit descriptors and lineage, the primary reference, shuffled character order and legal option counts. Reconstruction regenerates and matches this snapshot before consuming saved outcomes. Completed output also records evaluations and nomination in the usual discovery artifact. Cancellation or invalid measurements retain the frozen proposals and completed measurements without producing a shortlist.

The new definition, proposal and arm fields are omitted from JSON when absent, preserving legacy serialized shapes. Existing incumbent construction, nomination, selection, diagnostic contracts and the racing comparison contract are unchanged. The new policy uses the incumbent-preserving nomination helper; it does not use racing's promoted-parent panel.

## Changed files

- `TowerAnchoredNeighborhoodSearch.cs`: deterministic batch construction, quota rejection, fixed-panel evaluation and partial evidence.
- `TowerBossDiscoveryContract.cs`, `TowerBossGeneration.cs`: optional designation and archive records; explicit policy validation.
- `TowerSuppliedCompositionSearch.cs`, `TowerCompositionSearch.cs`, `TowerPracticalSearch.cs`, `Program.cs`: dispatch, preparation, canonical composition and practical-workflow integration.
- `TowerBossStudy.cs`, `TowerBossDiscoveryRun.cs`, `TowerCompactDiscovery.cs`: pre-combat snapshot publication and reconstruction.
- `BalanceHarnessAnchoredNeighborhoodTests.cs`, `BalanceHarnessAnchoredArchiveTests.cs`: synthetic contract/kernel/study checks and native archive/compact integration.
- Proposal, this implementation note, practical guide and harness README: current status and usage.

## Verification

Verification results are recorded in `TestResults/anchored-neighborhood-verification-20260917/`. The required backend wrapper is used with the isolated build directory `TestResults/anchored-neighborhood-build-20260917`.

The synthetic fixtures check legal edit counts, equal character coverage, unique recipes, family and inventory constraints, deterministic reconstruction, explicit alternate-primary ancestry, root-dependent extra slots, independence from evaluation feedback, quota rejection, malformed measurements, cancellation, protected nomination, exact study accounting, allocation binding and absent-field compatibility. The native fixture uses artificial supplied teams and one confirmation value solely to check archive mechanics; practical production requests still require at least 256 confirmation values. It checks native/compact parity, zero-combat reconstruction, pre-fight snapshot existence and rejection of a changed batch even after the file manifest is rehashed.

Final results: **422 distinct passing cases**, including **19 new anchored-policy cases**. The broader run passed 419/420; the existing `Ten_resigned_tampering_cases_still_fail_reconstruction_or_publication` diagnostic fixture hit its phase deadline/storage guard during the concurrent run, then passed alone in 49 seconds. The native anchored archive/compact fixture passed in about 61 seconds and confirmed reconstruction without combat. The final focused build passed all 18 synthetic kernel/workflow cases in three seconds, with zero errors and nine existing test-project warnings.

Commands used:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/anchored-neighborhood-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessAnchored|FullyQualifiedName~BalanceHarnessSupplied|FullyQualifiedName~BalanceHarnessCompositionSearch|FullyQualifiedName~BalanceHarnessIncumbent|FullyQualifiedName~BalanceHarnessPractical|FullyQualifiedName~BalanceHarnessTowerBossStudy|FullyQualifiedName~BalanceHarnessTowerBossDiscovery|FullyQualifiedName~BalanceHarnessAllocationComparison|FullyQualifiedName~BalanceHarnessEvaluationAllocation|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessSelectionDiagnostic'
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/anchored-neighborhood-build-20260917 -Filter 'FullyQualifiedName=EssenceSystem.Tests.BalanceHarnessSelectionDiagnosticTests.Ten_resigned_tampering_cases_still_fail_reconstruction_or_publication'
./build/run-tests.ps1 -ArtifactsPath TestResults/anchored-neighborhood-build-20260917 -Filter 'FullyQualifiedName~BalanceHarnessAnchoredNeighborhoodTests'
git -c core.safecrlf=false diff --check -- LL/tools/BalanceHarness
```

The corresponding `regression`, `isolated-diagnostic` and `final-focused` logs and TRX files are retained in the verification directory. Initial test-only compile/fixture-budget errors were corrected before the broad run; their logs are retained too. No required check remains blocked. The full unrelated backend suite was not run, and the existing diagnostic time limits were not changed.

No gameplay, balance thresholds, database schema or service configuration changed. There are no migrations or deployment steps. The separately versioned `tower-anchored-comparison-v1` has now completed without promotion; its protocol and result are linked above. The [saved-evidence selection tie audit](Tower-Practical-Selection-Tie-Review.md) is complete and defines a separate optional selector implementation as the next step. The historical `tower-allocation-comparison-v1` remains bound to the closed racing experiment.
