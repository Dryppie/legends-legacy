# Reference exploration with root-dependent owner offsets

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **Implemented and mechanically verified; strength improvement remains untested.** The new opt-in policy `retained-composition-three-reference-exploration-offset-v1` varies each reference's initial owner position by construction root. This addresses the systematic short-search omission found in the [saved-search diagnosis](Tower-Practical-Reference-Exploration-Diagnosis.md). The original exploration policy and current search defaults remain intact. No new scientific comparison or reservation ran.

## Behavior and version boundary

For each supplied reference, the new schedule derives a deterministic seed from the new policy version, the construction root formatted with invariant culture, the exact reference ID and the `owner-offset` namespace. A separate seeded `Random` draws one initial cursor in `[0, ownerCount)`. Sorting the declared starts differently does not change a reference's offset; changing another reference's ID does not advance its stream.

The schedule then follows the existing sorted owner cycle. Reference order, alternating radii two and three, every-fourth opportunity cadence, first three supplied proposals, first six fresh proposals, 32-check construction cap, rejection advancement and duplicate handling are unchanged. Each scheduled owner still receives exactly one Essence replacement. The first exploration opportunity remains zero-based proposal 12.

The new offset draws consume none of the existing fresh, mutation or exploration-construction streams. Both exploration policies deliberately retain the original exploration-construction namespace. Later trajectories can differ because changed owners produce different recipes, legality outcomes and adaptive parents; identical later mutations are not promised across policies.

The existing `retained-composition-three-reference-exploration-v1` passes no offset seed and retains its zero-initialized cursors. The trace schema is unchanged: policy version, construction root, reference ID, visit, radius and scheduled slots suffice to reconstruct the schedule. Old serialized traces gain no fields. The protected three-reference nomination, two challenger slots, selection policy/designation, one selected output and ten-quantity practical interval family remain unchanged.

## Verified coverage

The declared mechanical fixture bank uses integer roots **0–255**, three literal references and exactly three visits per reference. It draws no scientific values and executes no combat. All possible initial two-owner patterns and every owner slot appeared for every reference:

| Party size | Distinct initial patterns per reference | Minimum visits to any owner, across the fixture bank | Maximum visits |
| --- | ---: | ---: | ---: |
| 3 | 3 | 585 | 609 |
| 5 | 5 | 352 | 367 |
| 10 | 10 | 167 | 196 |
| 15 | 15 | 107 | 132 |

For ten owners, the third fixture reference received **173, 174 and 180 visits** to slots 8, 9 and 10 respectively. The original zero-cursor test still reproduces its first-three-visit sequence over slots 1–7. These fixture results demonstrate the removal of that hardwired starting-position omission; they do not guarantee full coverage in every finite sample or establish an increase in win rate.

Additional tests cover 96-opportunity schedules at party sizes 3, 5, 10 and 15, including negative and extreme integer roots. Every radius uses distinct sorted owners, and cumulative scheduled-owner counts within each reference differ by at most one. Construction exhaustion and duplicate proposals consume their visits, and a finite-space search still stops at its attempt cap without refilling or nominating an incomplete batch.

## Integration and explicit use

Generation validation, composition-only classification, direct supplied ancestry, protected nomination and the three-reference practical request grouping recognize the new version. Allocated request reconstruction, publication and abandoned-prefix recovery pass under it. The existing three-reference reuse preset still emits `retained-composition-three-references-v1`.

For a newly declared valid independent schema-3 definition containing all three references, opt in explicitly:

```text
tower-retained-composition-prepare --definition <new-definition.json> --references <id-a,id-b,id-c> --policy-version retained-composition-three-reference-exploration-offset-v1 --output <new-preparation-directory>
```

This prepares a definition; it does not launch the allocated search. A practical allocation must bind the new generation version and definition hash in its own unscheduled three-reference request. Existing captured requests and executables are not upgraded in place.

The closed `tower-reference-exploration-comparison-v1` controller remains fixed to its original baseline and exploration policies. A regression explicitly rejects an offset-policy template at that boundary. Its frozen plan, captured runtime, primary endpoint, admission and scientific result remain unchanged.

## Verification and evidence

**325 scoped backend tests passed, with zero failures or skips**, through `build/run-tests.ps1`. The suite covers:

- The declared owner-coverage bank, reproducibility, culture independence, reference-local streams, construction-stream independence and bounded rejected opportunities.
- Three full original-exploration fixture trajectories captured before this change and compared byte-for-byte afterward, plus the three existing baseline-policy golden trajectories.
- All three protected controls, nomination costs, selection and practical interval accounting, allocation/publication reconstruction and permanently reserved interrupted prefixes.
- Native study and compact archive reconstruction for both exploration versions. Re-sealing a changed scheduled-owner trace under the new version does not bypass semantic verification; the v1 construction-count tamper check also remains active.
- Unchanged reuse-preset generation and rejection of the new generator by the frozen twelve-pair comparison controller.

The native archive cases use temporary engineering studies, each capped at **420 study fights plus 256 compact-discovery fights**. Verification itself runs with combat forbidden. Other existing regression cases retain their own engineering workloads. These fixtures neither reserve authoritative scientific values nor establish strength improvement.

The initial sandboxed baseline build could not read the existing user NuGet configuration. The approved wrapper retry succeeded and captured all three baselines. The final build had **10 warnings outside the changed files and zero errors**; the 325 tests passed on the first implementation regression run. No required check remains blocked. The full repository test suite was not run.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-offset-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExplorationTests.Only_periodic_fresh_opportunities_change'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-offset-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExploration|FullyQualifiedName~BalanceHarnessThreeReferenceTests|FullyQualifiedName~BalanceHarnessPracticalAllocationRecoveryTests|FullyQualifiedName~BalanceHarnessSupplied|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessTowerBossStudyPolicyTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessPracticalSearchTests|FullyQualifiedName~BalanceHarnessPracticalAllocationTests'
```

The first command records the pre-change baseline capture; its then-current test had three cases. The final source expands that theory to both versions. Available receipts are retained under the [implementation evidence directory](../TestResults/reference-exploration-offset-implementation-20260922): [baseline outputs](../TestResults/reference-exploration-offset-implementation-20260922/before), [baseline log](../TestResults/reference-exploration-offset-implementation-20260922/before-approved.log), [regression log](../TestResults/reference-exploration-offset-implementation-20260922/regression.log), [TRX](../TestResults/reference-exploration-offset-implementation-20260922/regression.trx), [ten-owner coverage](../TestResults/reference-exploration-offset-implementation-20260922/coverage-10.json) and [final verification](../TestResults/reference-exploration-offset-implementation-20260922/verification.json).

The [full history check](../TestResults/reference-exploration-offset-implementation-20260922/history-preservation.json) authenticated the prior admission, scientific archive and diagnosis packages and reproduced **567,789 exclusions across 234 files** with the same inventory and hashes, including the supported abandoned-reservation recovery. It took **110.375 seconds**. Exactly five of the admission's 201 producing harness source documents changed; the other 196 remain identical. Historical scientific and diagnostic packages remain sealed. Scoped whitespace and updated local-link checks passed.

Builds, tests, helper work and documentation remain separately disclosed engineering work under the accepted accounting decision. The retained durations do not establish complete engineering totals. Incomplete older totals remain unknown, and the **18,180-second /13,584-MiB** prior ledger is preserved. No unused scientific allowance is transferred to this implementation.

## Changed files and next step

The five production harness files are [the schedule](../LL/tools/BalanceHarness/TowerReferenceExploration.cs), [supplied search integration](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs), [definition validation](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs), [composition classification](../LL/tools/BalanceHarness/TowerCompositionSearch.cs) and [CLI help](../LL/tools/BalanceHarness/Program.cs). Tests add `BalanceHarnessReferenceExplorationOffsetTests.cs` and extend the exploration, archive, comparison, three-reference and allocation-recovery fixtures. This report and current guide pointers document the new opt-in behavior.

The [prospective design and captured-runtime admission](Tower-Practical-Reference-Exploration-Offset-Comparison-Admission.md) and the [single scientific execution](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) are complete. Both audits passed over 51,672 fights. The result is `DoNotPromoteReferenceExplorationOffset`: eleven identical-output pairs, one positive pair and +0.175 percentage points overall, below every required promotion gate. The mechanical coverage improvement did not establish the required selected-team strength gain. This offset experiment is closed without promotion or extension. The [saved-stage review](Tower-Practical-Search-Stage-Review.md) is complete; the [fresh-screening design and implementation](Tower-Practical-Fresh-Screening-Implementation.md) now preserve that search budget, with [captured-runtime admission](Tower-Practical-Fresh-Screening-Admission.md) and the [single strength comparison](Tower-Practical-Fresh-Screening-Comparison-Execution.md) complete as `DoNotPromoteFreshScreening`. The original 53,672-fight result remains `DoNotPromoteReferenceExploration`; its allocation also stays closed.

No application configuration changes, migrations, database operations or deployments occurred. Search defaults, confirmed-team recommendations and the separate captured-cohort balance failure remain unchanged.
