# Reference exploration comparison: implementation

22 September 2026. Target: offline `LL/tools/BalanceHarness`. **Implemented, admitted and scientifically tested; comparison closed.** The [single twelve-pair execution](Tower-Practical-Reference-Exploration-Comparison-Execution.md) completed 53,672 fights and both audits with `DoNotPromoteReferenceExploration`: eleven pairs selected identical outputs, the remaining pair favored the baseline, and the mean difference was −0.142 percentage points. The separately versioned controller, native verifier, independent saved-row auditor and owned launcher implement the [frozen protocol](Tower-Practical-Reference-Exploration-Comparison-Plan.md). The implementation evidence below retains its original pre-admission status; later admission and execution are documented separately. Defaults remain unchanged.

## Behavior and decisions

`tower-reference-exploration-comparison-v1` runs two independent searches for each of twelve paired roots. Both receive the same eight discovery and 32 selection values, three references, 46-candidate budget, five nominees and incumbent-tie selector. Only the generator changes. Separate archive namespaces prevent cross-arm search caching.

All 24 outputs and all physical confirmation families are durably frozen after exactly **12,672 search fights**. Each pair then measures the union of its three controls and both selected outputs on 1,000 common values. Physical deduplication produces three, four or five recipes per pair: **48,672–72,672 total fights**. Identical outputs still measure their controls and absolute wins; their primary difference is exactly zero. Different selected controls can share a three-member union.

The primary denominator stays 12,000. Support requires at least 600 net wins, seven positive pairs and a positive conditional lower bound. The bound conditions on the twelve frozen output pairs; it does not estimate reliability over future construction roots. Each arm also receives its descriptive family-ten rates and paired contrasts. Those views do not adopt a team or add promotion endpoints.

Reservation uses one 16,384-word batch. The first 12,492 fresh values are assigned, with all 492 search values preceding confirmation. Every remaining fresh value is permanently reserved. Pending intent and the complete entropy bytes precede classification. Exhaustion or interruption cannot refill, retry, resume or silently release values. An unresolved Pending record remains a blocker requiring a separately supported reconciliation.

The request binds the exact plan JSON, independent auditor, reconciled source capture, closeout receipt, template, full history and runtime dependencies. The existing gameplay assemblies, settings and 16 content files must survive the new harness capture. A development build is not evidence of that compatibility.

The proposed cumulative allowance remains **10,800 seconds /6 GiB**, with **600 seconds /512 MiB** charged to admission. The launcher permits **10,200 seconds /5.5 GiB** for execution, audits and publication; its native child has **10,080 seconds /5.25 GiB**. Both audits use the same remaining deadline. The owner starts processes suspended inside a Windows Job, waits for the entire tree to empty, and publishes only after both audits pass. Completion and manifest bytes are included in the retained storage charge. Failed work retains its evidence and reservations.

## Public interface

```text
dotnet BalanceHarness.dll tower-reference-exploration-comparison-check <request.json>
python -B -X utf8 build/run-reference-exploration-comparison.py --request <request.json> --harness <captured-BalanceHarness.dll>
dotnet BalanceHarness.dll tower-reference-exploration-comparison-verify <completed-archive>
python -B -X utf8 "Balance Harness/analysis/audit-reference-exploration-comparison.py" <completed-archive> --manifest-sha256 <pin>
```

`check` validates inputs and native preparation with a combat-entry guard, returning `ReadyNoReservation`. It neither captures an executable nor creates a complete admission package. The launcher's `-run` and prepublication `-audit` commands are plumbing for that owned workflow. Direct scientific invocation, existing output directories and altered envelopes are rejected.

The request is `ExplorationRequest`: absolute capture, closeout, content, template, registry, output, plan and auditor paths; template/plan/auditor hashes; required-history pins; supported Pending recoveries and receipt hashes; and the exact cumulative/prior allowances. The frozen plan hash is `6a0e8fb68d7a20e66740795d17278e5bf7e95c8e77c6b10f9127e8c3e77468a4`. The subsequent admission linked above supplies the concrete captured request.

Native verification replays generation and selection using saved outcomes, reconstructs prepared inputs, validates archive membership and provenance, reconciles every attempt, and recomputes the result under a combat-entry guard. It reuses prepared parties across seeds while validating each seed-specific input and cache key. Public verification requires the producing runtime.

The Python auditor independently recounts direct gzip battle reports, search fitness, the common selector, nominations, paired panels, physical confirmation roles, all primary counts and all 24 descriptive views. It authenticates capture, plan, content, runtime and reservation bindings. Native reconstruction owns the .NET proposal-trajectory and prepared-input checks. Primary arithmetic is compared at `1e-12`; descriptive intervals allow `2e-9` for the different inverse-normal implementations. The auditor returns the native result only after every independently computed quantity agrees.

## Verification and evidence

Evidence is retained in [the implementation directory](../TestResults/reference-exploration-comparison-implementation-20260922). The final receipt and source/build hashes are in [verification.json](../TestResults/reference-exploration-comparison-implementation-20260922/verification.json).

- **98 scoped backend regression cases passed**, including the comparison, both generator policies, three-reference integration and earlier incumbent-tie comparison. All **24 controller cases** passed again on the final revision.
- The new controller suite has **24 cases**, covering panel mapping, both policies, protected nominees, three/four/five-member unions, convergence, decision boundaries, entropy failures, interrupted search/freeze/confirmation and altered archives.
- A **60,672-row literal archive** passed native reconstruction and the independent Python recount. Both returned the same synthetic endpoint and all 24 descriptive views. This is an engineering fixture, not a search-quality finding.
- **14 Python fixtures passed**, covering decision arithmetic, reservation tails, native Job ownership, timeout/storage failures, shared audit deadlines, retained-byte publication accounting, complete synthetic audit bindings and source tampering.
- The read-only live-history scan reproduced **232 files and 551,408 exclusions** exactly; no scientific values or fights were added.

Commands used:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-comparison-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExploration|FullyQualifiedName~BalanceHarnessThreeReferenceTests|FullyQualifiedName~BalanceHarnessIncumbentTieComparisonTests'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/reference-exploration-comparison-build-20260922' -Filter 'FullyQualifiedName~BalanceHarnessReferenceExplorationComparisonTests'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'build/test-reference-exploration-comparison.py' --fixture 'TestResults/reference-exploration-comparison-implementation-20260922/literal-archive'
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'TestResults/reference-exploration-comparison-implementation-20260922/check-history.py'
```

Earlier failed builds and the fixture settings mismatch remain disclosed in the logs; they were corrected before the passing runs. The new comparison tests use literal outcomes with combat-entry guards. The existing native generator archive regression also exercised temporary engineering battles outside the scientific registry. None of these fixtures admits the new executable against the historical gameplay capture or measures the full scientific run's resource cost.

Engineering work remains separately disclosed under the user's decision. Incomplete older engineering totals are not reconstructed or reset; the previous 18,180-second /13,584-MiB ledger remains historical. This implementation does not consume a scientific allocation or pretend that proposed admission has completed.

## Changed files and next step

Four new C# files hold comparison execution/statistics, reservation and capture validation, saved archive reconstruction, and owned-run publication checks. `Program.cs` adds command dispatch and help. The Python launcher, independent auditor and C#/Python fixtures are new files. This report and current guide pointers document their scope; the frozen plan JSON and sealed planning evidence remain unchanged.

The subsequent captured-runtime admission completed the source/runtime freeze, validated both native policies and all three references without combat, refreshed the full registry, and produced the immutable request described in the plan. The single comparison and both audits completed within the admitted envelope. The [execution report](Tower-Practical-Reference-Exploration-Comparison-Execution.md) records the result; the [diagnosis](Tower-Practical-Reference-Exploration-Diagnosis.md) motivated the separately [implemented owner-offset policy](Tower-Practical-Reference-Exploration-Offset-Implementation.md). This controller still binds its original policies and rejects the offset policy. Any new strength comparison requires a new contract; this comparison has no retry, resume or extension.

No required implementation check remains unrun. Later admission and scientific execution are documented separately. There are no application configuration changes, migrations, database actions or deployment implications. Defaults, confirmed-team recommendations and the captured cohort's separate balance failure remain unchanged.
