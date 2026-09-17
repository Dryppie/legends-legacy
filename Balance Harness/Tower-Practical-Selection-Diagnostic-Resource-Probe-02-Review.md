# Practical Tower selection diagnostic: completed resource probe

**Historical snapshot:** this document records its original scope. Current disposition is in the [execution review](Tower-Practical-Selection-Diagnostic-Execution-Review.md); the earlier launch restrictions below have been superseded for that one completed run.

17 September 2026. Target: the offline BalanceHarness test host. **The fresh resource probe completed in 125.776 seconds with 176,759,620 retained bytes (168.57 MiB), within its 180-second /256-MiB allowance.** This qualifies the declared non-combat workload in the observed environment. Combat cost, full diagnostic completion probability and stronger-team discovery remain unestablished; gameplay launch is **NoGo** pending a concrete admission package and its remaining resource decision.

The [JSON observation](Tower-Practical-Selection-Diagnostic-Resource-Probe-02.json) is reproduced by the [read-only verification script](analysis/practical-selection-resource-probe-02.py). The [frozen scope](../TestResults/selection-diagnostic-probe-02-checks/scope.json) records the fresh authorization, fixed output, caps, source/build pins and previous closed charges. The [first failed probe](Tower-Practical-Selection-Diagnostic-Resource-Probe-Review.md) remains intact. This scope ran once, with no retry, combat, new reservations, entropy draw or candidate generation.

**Subsequent execution:** The [completed diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md) completed all **4,640 fights** and both built-in audits in **248.562 seconds /152.36 MiB**, returning **NoSelectionMissDemonstrated**. The frozen primary won **712/1,000**, versus **632/1,000** and **628/1,000** for the anchors and **598/1,000** for the other challenger. This is encouraging fixed-team evidence, while adoption remains **Hold** and both anchors remain recommended. The scope is closed; **2,089** new reservations bring the permanent union to **486,374**, with no new Pending reservation. No further experiment is queued. The analysis, fixture/probe measurements and JSON below retain their original historical scopes.

## Measured work

The current producing runtime admitted **202 historical files and 484,285 excluded values**, validated live settings/content, copied all **26 runtime assets /19,111,561 bytes**, prepared native inventory/mechanics, and wrote full-history resource payloads. It then processed **512 discovery, 128 selection and 4,000 confirmation report occurrences**. All 4,640 resulting compressed files are byte-identical to their authenticated retained sources.

The two audit read workloads completed **4,640 +4,000 report reads**, with two exact payload-inventory hash passes. The native trace records **9,280 input materializations**, covering report writing and the first audit read workload. The confirmation scenarios carried 1,000 existing historical labels solely to exercise input serialization size. Three retained confirmation recipes supplied repeated reports; these are not new evaluation evidence.

| Probe phase | Measured seconds | Retained growth | Earlier candidate phase ceiling |
| --- | ---: | ---: | --- |
| Admission | 38.744 | 77.49 MiB | 180 s /96 MiB |
| Search report workload | 17.059 | 12.31 MiB | 120 s /64 MiB |
| Confirmation report workload | 37.410 | 77.80 MiB | 660 s /256 MiB |
| Audit read workload and final history check | 31.827 | 0.46 MiB | 360 s /16 MiB |

The phase table uses worker boundary receipts. Another **0.559 seconds** of parent-measured time covers startup, receipts and sealing outside those phase durations. Parent completion including publication took **125.598 seconds**; external timing through process exit was **125.776 seconds**. Phase-growth entries exclude later receipt/manifest writes: final audit/publication growth from the audit's starting size was about **0.97 MiB**. The final file size and combined sampled high water were both **168.57 MiB**; sampling does not establish an exact transient maximum, RAM use or filesystem allocation.

The largest measured cost was history admission and its three rechecks: **75.747 seconds**, about 60% of total parent time. Initial admission took 37.485 seconds; rechecks before search, before confirmation and after audits took 13.072, 12.662 and 12.528 seconds. The two audit read workloads themselves took **16.408 and 2.537 seconds**. This finding does not call for another filesystem refactor: every measured phase fits its proposed ceiling, and combat remains the consequential unknown.

## What the measurement establishes

The earlier [resource sizing review](Tower-Practical-Selection-Diagnostic-Resource-Review.md) lacked current native history/copy/input/report-processing measurements. This observation now supplies those measurements for its declared workload and pins the producing identity. The original **1,380-second /512-MiB** gameplay envelope remains a conditional proposal; it is not a granted allowance or a completion guarantee.

The observed admission growth of **77.49 MiB** exceeds the earlier approximately 60-MiB model because the probe's representation differs. Its history payload contains three full arrays (23,660,651 bytes), whereas the model allowed one historical ledger plus search positions (6,919,080 bytes). Wrapped definitions have extra indentation, and the probe stores recipes during admission. The probe therefore measures full-sized native operations with representative payloads; its archive is not a byte-for-byte model of the diagnostic archive. Both the original model and this observed representation fit the proposed 96-MiB admission growth cap, without proving a worst-case bound.

The probe does not execute the candidate generator, combat preparation/simulation, entropy sampler, statistical assessment or either complete diagnostic decision verifier. Its audit workloads cover native input reconstruction, saved-report deserialization and inventory hashing. They do not establish the cost of the omitted state-machine and decision work. Reading source reports and recording probe telemetry also adds work that a new gameplay run would organize differently. A fourth nominee's report distribution and longer future battles remain unmeasured. One successful timing observation is not a runtime distribution or operational-success probability.

## Scope and accounting

The fresh scope's full **180 seconds /256 MiB** are charged and closed; unused headroom is not refunded. Together with the first failed scope, resource-probe charges are **360 seconds /512 MiB**. These are charged ceilings, not measured cumulative consumption. The first attempt's evidence was not overwritten, resumed or rebound.

The next step is to prepare the diagnostic admission package using this evidence: freeze the native request, source/runtime/content/history identities, phase ceilings and explicit cumulative/prior accounting. It must resolve combat cost through compatible evidence or an explicit acceptance of capped operational-failure risk. The original candidate envelope leaves only 58 seconds /76 MiB for prior costs and setup after its phase caps and closeout reserve; those margins cannot silently absorb the two closed probe charges. This observation grants no gameplay allowance or additional probe.

Both anchors remain recommended. All **484,285 exclusions**, V19's **512 unused values /253 required recipes /Unresolved**, and adoption **Hold** are preserved. No new quality result or search change follows from resource success.

## Implementation and verification

The [probe host](../LL/tests/BalanceHarness.ProcessFixture/ResourceProbeHost.cs) now fixes the fresh output to `selection-diagnostic-resource-probe-20260917-02`. The [workload helper](../LL/tests/BalanceHarness.ProcessFixture/ResourceProbeWork.cs) canonicalizes constructed source paths as well as comparisons, covering history-dictionary keys that still used mixed separators after the first fix. The [probe tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessResourceProbeTests.cs) add that regression and rejection of the first attempt's location. All **14 probe tests passed** before launch, including combat rejection, timeout, storage overflow, parent death, output reuse and historical-only labels. No backend harness or gameplay implementation changed.

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/selection-diagnostic-build -Filter 'FullyQualifiedName~BalanceHarnessResourceProbeTests'
python -B "Balance Harness/analysis/practical-selection-resource-probe-02.py"
```

The reader authenticates the frozen source/build/test pins; the preserved first-scope evidence; both native source inventories (**1,617 entries**); the exact completed probe inventory (**4,717 entries**, plus its manifest and receipt); the payload inventory (**4,686 entries**); all 202 admitted history-file hashes; report repetition order and byte identity; existing-only labels; native trace counts; elapsed/storage limits; and passing TRX results. A second invocation reproduced the JSON byte-for-byte without running a benchmark or replay. All **429 local Markdown links** and whitespace checks passed. Of 249 baseline files, ten received the intended code/test/Markdown edits and the other **239 remained byte-identical**. The pre-probe build succeeded with existing test-project warnings; no required command was blocked.

Changed files are limited to the host's new output scope, source-path helper, two regression tests, this review/JSON/reader and current Markdown pointers. The previous failure JSON, analysis calculations and native archives retain their original bytes. No migrations, application configuration changes or deployments are required.
