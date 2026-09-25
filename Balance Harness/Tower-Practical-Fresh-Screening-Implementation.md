# Practical fresh-screening implementation

22 September 2026. Target: offline `LL/tools/BalanceHarness`. The [prospective comparison](Tower-Practical-Fresh-Screening-Comparison-Plan.md) is implemented under `tower-practical-fresh-screening-comparison-v1`. It compares the existing direct-nomination pipeline with **46 ×4 discovery, 23 ×8 fresh screening and 5 ×32 final selection**, retaining **528 search fights per arm**. The [captured-runtime admission](Tower-Practical-Fresh-Screening-Admission.md) and [single scientific execution](Tower-Practical-Fresh-Screening-Comparison-Execution.md) are now complete. Both audits passed; the result is `DoNotPromoteFreshScreening`. This implementation report preserves the preceding engineering work.

## Implementation and decisions

[TowerPracticalScreening.cs](../LL/tools/BalanceHarness/TowerPracticalScreening.cs) validates the fixed candidate stage contract and complete measurements. It freezes three controls plus the top twenty discovery-ranked challengers before screening, then uses fresh screening fitness alone to freeze three controls plus the top two challengers before selection. Each freeze binds the definition, discovery record, membership, order, panel and stage count. Incomplete discovery, screen measurements or checkpoint writes terminate the comparison without a fallback.

[TowerReferenceExplorationComparison.cs](../LL/tools/BalanceHarness/TowerReferenceExplorationComparison.cs) coordinates the new stage through the existing battle archive and native reconstruction. All 24 outputs still freeze before confirmation. The ordinary generator's five-member discovery shortlist remains as a reconstructed checkpoint; the screened arm passes its explicit five screening nominees to the unchanged incumbent-tie selector. Both generators remain `retained-composition-three-references-v1`, so outputs and descriptive views also identify their pipeline. New nullable fields are omitted from legacy JSON. The two earlier comparison versions retain their generator, plan, panels and decision labels.

[TowerReferenceExplorationReservation.cs](../LL/tools/BalanceHarness/TowerReferenceExplorationReservation.cs) binds version-specific allocation sizes. The new version assigns **588 search values /12,588 total**, reserving every fresh value from its one 16,384-word batch. The previous versions keep **492 /12,492**. Within each 49-value search block, candidate discovery uses the first four of baseline's eight values, screening uses eight separate values, and final selection shares 32 values. Confirmation remains twelve disjoint 1,000-value panels. Search fights remain 12,672; total physical fights remain 48,672–72,672.

The [independent auditor](analysis/audit-reference-exploration-comparison.py) recounts all three stages from literal saved battles, reconstructs protected screen membership and nomination, and checks ordering, freshness, selection, confirmation, descriptive views and the primary endpoint. Native reconstruction additionally verifies deterministic proposals and exact .NET hashes, including exponent-form fitness serialization. The new conditional-bound population subtracts 588 search values. Both audits and the [owned launcher](../build/run-reference-exploration-comparison.py) bind the explicit new version and its frozen plan.

No generator operator, offset schedule, selector rule, default or gameplay assembly source changed for this work. Coarser discovery can change adaptive parents, so this is a comparison of complete pipelines. Equal fight counts alone do not establish runtime feasibility or strength improvement.

## Verification

**154 scoped backend tests passed, with zero failures or skips**, through `build/run-tests.ps1`. [The base comparison fixtures](../LL/tests/EssenceSystem.Tests/BalanceHarnessReferenceExplorationComparisonTests.cs) now exercise original, offset and screening protocols. [The new screening fixtures](../LL/tests/EssenceSystem.Tests/BalanceHarnessReferenceExplorationScreeningComparisonTests.cs) cover protected membership, a screening winner outside the discovery top two, zero-win controls, fresh-only ranking, incomplete/duplicate/reordered measurements, changed freeze bindings, overlapping panels, changed pipeline labels and failure at both screening checkpoint boundaries. Native saved-battle reconstruction passes for all three versions and rejects resealed trajectory changes. Legacy JSON omits screening/pipeline fields.

Backend fixture archives each contain **60,672 literal reports** and a verified result. Their synthetic outcomes are engineering inputs; a combat guard rejects entry into the game engine. They do not count as scientific evidence or reserve values in the scientific registry. Synthetic reports favor the screened candidate deliberately to exercise the support decision, without claiming a measured benefit.

**41 independent Python tests passed.** They cover all three native fixture archives, matching primary and descriptive results, resealed screening membership/nomination/panel changes, missing controls, protocol substitution, allocation arithmetic, source/plan binding, cumulative resource limits and owned-process cleanup. The three complete launch-envelope fixtures also passed. These checks operate on literal reports and do not launch scientific combat.

The read-only integrity check authenticated **seven sealed predecessor packages** and preserved **584,171 historical exclusions across 236 files**, including abandoned-reservation recovery. Of the 201 previously captured producing documents, only the comparison controller and its reservation code changed; the new screening file is added separately. All three frozen plan hashes remain valid. Final syntax, source, link and publication checks are retained with the engineering evidence.

The initial sandboxed build could not read the existing user NuGet configuration. The same repository wrapper then completed with the necessary access: zero build errors and 38 existing warnings. No gameplay or infrastructure configuration was changed to bypass the failure.

Commands:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/practical-fresh-screening-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExploration|FullyQualifiedName~BalanceHarnessThreeReferenceTests'
python -B -X utf8 build/test-reference-exploration-comparison.py --fixture TestResults/practical-fresh-screening-implementation-20260922/fixtures/tower-reference-exploration-comparison-v1 --offset-fixture TestResults/practical-fresh-screening-implementation-20260922/fixtures/tower-reference-exploration-offset-comparison-v1 --screening-fixture TestResults/practical-fresh-screening-implementation-20260922/fixtures/tower-practical-fresh-screening-comparison-v1
```

The backend command used `BALANCE_HARNESS_EXPLORATION_FIXTURE_EXPORT` pointing to the package's `fixtures` directory. Python used the bundled runtime. Logs, TRX, source snapshots, fixtures and final checks are retained in [the engineering evidence](../TestResults/practical-fresh-screening-implementation-20260922/completion.json), with its [manifest](../TestResults/practical-fresh-screening-implementation-20260922/files.json).

Engineering remains separately disclosed. These local build, test and archive costs are not charged as a scientific comparison. Complete historical engineering totals remain unknown; preserve the reconciled **18,180-second /13,584-MiB** prior-charge ledger. No unused scientific allowance transfers here.

## Next step

The [new captured-runtime admission](Tower-Practical-Fresh-Screening-Admission.md) completed the following requirements: retain the original gameplay dependencies, content, settings and exact controls; capture the tested harness and auditor; verify preparation and screening against that retained runtime; rescan historical exclusions; and seal the new request within the frozen admission allowance. The existing launcher supports the new version, but earlier admission packages cannot authorize or supply its allocation. The [single comparison](Tower-Practical-Fresh-Screening-Comparison-Execution.md) has since completed and closed as `DoNotPromoteFreshScreening`. The [saved-stage diagnosis](Tower-Practical-Fresh-Screening-Stage-Review.md) is also complete. It recommends separately evaluating positive-tie preference for every retained reference on the direct-nomination baseline. This screening allocation cannot be resumed or extended.

The original and offset comparisons remain closed without promotion. No team was adopted. No migrations, application configuration changes, production deployment or infrastructure changes are involved.
