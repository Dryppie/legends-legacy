# Affinity creation: owned controller and archive audits

**Subsequent execution and diagnosis — 23 September 2026:** The [creation pilot](Tower-Affinity-Creation-Pilot-01-Execution.md) is complete and verified, with decision `Inconclusive`. The [saved-stage review](Tower-Affinity-Creation-Stage-Review.md) now traces all 24 paths and identifies older-primary selection as the source of 40 of 41 net benchmark lost wins. No policy was promoted. The implementation below is historical; the admission and scientific output are consumed.

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

The owned study controller, launcher, native audit and independent Python audit now support `tower-affinity-creation-comparison-v1`. They retain support for the separately identified preservation comparison. This extends the [v3 native comparison implementation](Tower-Affinity-Creation-Native-Implementation.md); it does not change the scientific design or claim stronger teams.

## Version binding

The request and prospective plan must name the same supported design. That design identity now follows the work through the launch envelope, admission receipt, entropy intent, allocation, study binding, all-output freeze, held-out archive algorithm, result, worker receipt and terminal publication records. Unknown versions and mixtures of creation and preservation records fail validation. The preservation constant and omitted resource-envelope behavior remain unchanged for existing callers and archives.

The creation candidate remains the exact v3 creation factory; its control remains the v2 single-edit factory. Relabeling a policy, changing the selected operators, adding preservation or substituting another parent schedule fails the frozen comparison contract. Resource-envelope versions remain independent of scientific design: the original native/audit split is 9,600/1,200 seconds; the existing v2 split is 9,000/1,800. Both retain the 10,800-second and 6-GiB total. This implementation does not enlarge either allowance.

The controller retains the twelve paired roots, 528 trials per search arm, separate arm charges and the durable barrier before held-out observations. All 24 searches must complete before any held-out measurement. Identical physical outputs within a root share measurement. Failed roots are retained without replacement, and interrupted reservations retain pending sentinels and all exposed values. No decision directly adopts a search policy or gameplay change.

## Complementary audits

Native replay reconstructs both generation waves from the bound plan and saved literal observations, including exact random choices, affinity route derivation, creation metadata, selection and the held-out family. It compares the reconstructed allocation, binding, freeze and result to the archive. The production audit additionally authenticates captured content, runtime, prepared-input identities and native reports.

The Python audit selects the correct policy and racing versions for each arm, recounts battle reports and independently recomputes ranking, pruning, selection, paired endpoints and uncertainty. For creation proposals it also checks one-owner locality, exact additions/removals, minimal one- or two-slot replacement, the target pair's identity and missing endpoints, selected route metadata, duplicate rejection and bounded construction. Native replay remains responsible for proving the association between route IDs and the authored inventory and for the exact random trajectory. The Python audit does not reimplement the combat engine or claim to independently derive that graph.

Mixed-version allocation, binding, launch, admission and terminal records are rejected. Both audits still require the unchanged all-root barrier, complete attempt accounting and physical held-out deduplication. Publication requires agreement between the native and independent results and the owned-process receipts; final verification requires an external closeout pin and rechecks the retained inventory.

## Scope and next step

All execution in this implementation step uses separately named synthetic fixtures, literal reports and fixed integer batches. No combat simulator run, production entropy draw, scientific reservation, admission or new efficacy result is implied by those fixtures. Historical study packages and their scientific conclusions are unchanged.

The [earlier prospective plan](Tower-Affinity-Creation-Comparison-Plan.json) remains bound to the earlier qualified binary. It is retained unchanged and cannot admit this new build. Next, add a separately versioned creation admission declaration/preparation path, qualify the final executable and captured dependencies, and bind a new plan to that execution identity. Then measure current resource feasibility and refresh the complete live history before any fresh allocation. Existing preservation admission scripts remain preservation-specific; they must not be repurposed by merely changing a version label.

Changed files:

- `TowerProposalStudy.cs`, `TowerProposalStudyProtocol.cs`, `TowerProposalStudyArchive.cs` and `TowerProposalStudyRun.cs`: request-to-publication design identity, native reconstruction and rejection checks.
- `build/run-proposal-affinity-study.py` and `Balance Harness/analysis/audit-proposal-affinity-study.py`: matching launch/admission identities and version-aware independent audit, including structural creation checks.
- `BalanceHarnessAffinityCreationStudyTests.cs`, `BalanceHarnessProposalStudyTests.cs`, `BalanceHarnessAffinityCreationNativeTests.cs` and `ProposalStudyFixtureHost.cs`: cross-version rejection, reservation interruption, all-root replay and owned fixture coverage.
- `build/test-proposal-affinity-study.py`, `build/test-proposal-affinity-study-owned.py` and `build/test-proposal-resource-envelope.py`: both designs, creation metadata tampering and process/resource regression checks.
- This report and current-status links in the harness guides and earlier implementation reports.

There are no migrations, application configuration changes, deployments or gameplay-default changes.

## Verification

The required backend wrapper passed **140 tests, zero failures and zero skips**. The build had zero errors and 42 existing warnings. Both full native fixtures completed all 24 searches, froze every output before held-out callbacks, reconstructed all 18,816 literal observations and rejected a resealed version change or reordered freeze. The retained [TRX](../TestResults/affinity-creation-study-tests-20260923.trx) and [test log](../TestResults/affinity-creation-study-tests-20260923.log) identify the exact run.

Python verification passed **39 tests**: eleven unit/process cases, five resource cases, eleven preservation archive cases and twelve creation archive cases. One creation-only metadata test is intentionally skipped for the preservation archive. Its creation counterpart passes and rejects altered metadata even when both saved batch copies and their manifest are resealed. Other cases reject changed battle reports, pruning, held-out order, allocation/binding versions, summary arithmetic, lost reservation tails and unmatched charges. Logs: [unit/process](../TestResults/affinity-creation-study-python-unit-final-20260923.log), [resources](../TestResults/affinity-creation-study-resource-tests-20260923.log), [preservation archive](../TestResults/affinity-creation-study-python-preservation-20260923.log), [creation archive](../TestResults/affinity-creation-study-python-creation-20260923.log).

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityCreation|FullyQualifiedName~BalanceHarnessProposalStudyTests|FullyQualifiedName~BalanceHarnessProposalNativeTests|FullyQualifiedName~BalanceHarnessProposalResourceTests|FullyQualifiedName~BalanceHarnessProposalRuntimeRetentionTests|FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessDamageAffinityTests' -ArtifactsPath 'TestResults/affinity-creation-study-build-20260923'

python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-resource-envelope.py
python -B -X utf8 build/test-proposal-affinity-study.py --fixture TestResults/proposal-study-versioned-fixture-20260923 ArchiveTests -v
python -B -X utf8 build/test-proposal-affinity-study.py --fixture TestResults/creation-study-versioned-fixture-20260923 ArchiveTests -v
```

The backend run exported its fixtures through `TOWER_PROPOSAL_STUDY_FIXTURE_EXPORT` and `TOWER_CREATION_STUDY_FIXTURE_EXPORT`, both pointing to new directories outside the scientific registry. Python commands used the installed workspace Python runtime. [Producing-symbol checks](../TestResults/affinity-creation-study-verification-20260923/compiled-source-files.json) bind all eight changed C# files to the built harness, tests and separate fixture executable. Both public comparison-plan checks retain their earlier hashes and return `ValidDesignNotAdmitted`: [creation](../TestResults/affinity-creation-study-verification-20260923/creation-plan-check.json), [preservation](../TestResults/affinity-creation-study-verification-20260923/preservation-plan-check.json).

Three separately named Windows-owned fixtures passed:

| Fixture | Verified behavior | Elapsed / retained bytes |
| --- | --- | --- |
| [Preservation completion](../TestResults/tower-proposal-owned-fixture-preservation-versioned-20260923/verification.json) | Original resource partition; 18,816 literal reports; both audits, publication and native final verification | 405.265 s / 644,331,482 |
| [Creation completion](../TestResults/tower-proposal-owned-fixture-creation-versioned-20260923/verification.json) | Existing v2 resource partition; 18,816 literal reports; both audits, publication and native final verification | 348.718 s / 656,498,908 |
| [Creation failure](../TestResults/tower-proposal-owned-fixture-creation-failure-versioned-20260923/verification.json) | One started attempt charged; all 16,384 exposed fixture values retained; no result, completion or closeout | 6.313 s / 45,023,735 |

The fixture executable replaces only the native content/materialization, entropy and combat-report boundaries. The real launcher, leases, admission pinning, one-shot allocation, searches, freezing, arithmetic, audits, publication and final verification remain active. All report zero actual combat and zero production entropy draws. Timings include fixture preparation and verification, with other tests running concurrently; they are engineering observations, not feasibility estimates for the captured scientific scenario.

```text
python -B -X utf8 build/test-proposal-affinity-study-owned.py --fixture-host TestResults/affinity-creation-study-build-20260923/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll --source-fixture TestResults/creation-study-versioned-fixture-20260923 --output TestResults/tower-proposal-owned-fixture-creation-versioned-20260923 --resource-envelope tower-proposal-resource-envelope-v2
```

The preservation run used the retained earlier literal source fixture, its own new output and the omitted legacy resource-envelope field. The failure run used `--mode attempt-failure` and another new output. These commands refuse output reuse.

The creation fixture's external closeout pin is `1460576dbb2870b8dc73c9de5731e9955bcd32f58fc48ab84dfacf49e259f5b7`; its archive manifest pin is `9334cb659e5615490c1e957199264e1fee081851e9415c3f71d5e4c1f7076c06`. The [independent published-archive audit](../TestResults/affinity-creation-study-verification-20260923/independent-published-creation.json) also verifies the final external-pin path. The final harness DLL is `bf2e5c1f283ea566184099fe1686ed8e9de1774f6ea0afdd5dd8c79a65a77cf4`.

The [completion receipt](../TestResults/affinity-creation-study-verification-20260923/completion.json) binds the final test results, producing sources, Python tools, owned receipts and unchanged prospective plan. No required verification command remains blocked. The required backend wrapper used approved access to installed NuGet configuration. No production runtime qualification or admission was attempted for this final binary.
