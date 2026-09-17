# Practical Tower selection diagnostic: resource probe disposition

17 September 2026. Target: the offline BalanceHarness test host. **The single authorized 180-second /256-MiB resource probe failed before live-history admission or report processing. Native resource qualification remains missing; gameplay launch is NoGo.** The failure was a path-comparison defect in this probe, not evidence that the resource ceiling is too small. The defect is corrected and regression tested. The native attempt was not repeated.

This follows the [resource sizing review](Tower-Practical-Selection-Diagnostic-Resource-Review.md). The [JSON record](Tower-Practical-Selection-Diagnostic-Resource-Probe.json) pins the failure evidence, producing source/binary snapshot, and test results. The probe has a separate resource-only version and output outside the allocation registry. It cannot publish a diagnostic decision, reserve values, or reuse an existing output directory.

**Later separately authorized scope:** the [corrected second probe](Tower-Practical-Selection-Diagnostic-Resource-Probe-02-Review.md) completed in 125.776 seconds /168.57 MiB. The first-attempt disposition and JSON below remain historical evidence; its failed output and charge were preserved.

## What happened

The producing worker authenticated both retained native inventories: **53 and 1,564 entries**, all with their expected hashes. That step recorded **0.162728 seconds**. It then compared normalized Windows source paths with paths assembled using forward slashes. The strings described the same locations, but the comparison rejected them as `Changed native source location`.

The parent closed the attempt and retained its failure record. No live-history reconciliation, content/runtime copying, full-history payload serialization, report processing or audit workload ran. There were **zero new fights, reservations, entropy calls or candidates**, and no diagnostic publication.

| Observation | Recorded value | Interpretation |
| --- | ---: | --- |
| External launch through process exit | 0.652288 s | Includes process startup and failure closeout |
| Parent operation through failure closeout | 0.474872 s | Failure-path measurement, not successful publication |
| Final probe files | 25,214 bytes | Seven failure/protocol files |
| Parent sampled high water before failure marker | 24,795 bytes | Excludes the later 419-byte failure marker |
| Charged allowance | 180 s /268,435,456 bytes | Full 256-MiB allowance remains charged; no refund or retry |

These small numbers do not qualify any of the intended workloads or support lowering future limits. No successful resource receipt or payload directory exists. The partial output remains intact at `TestResults/selection-diagnostic-resource-probe-20260917`; the ordinary command rejects that existing output.

## Implementation and correction

The [probe host](../LL/tests/BalanceHarness.ProcessFixture/ResourceProbeHost.cs) owns the output and registry leases, starts a hidden child, samples storage, enforces a deadline with a closeout reserve, and watches child completion. The child independently watches the parent's PID/start time and deadline. The combat trace rejects entry before the engine runs. Storage limits are sampled/boundary checks, not an OS quota; byte counts describe files, not RAM or filesystem allocation.

The [resource workload](../LL/tests/BalanceHarness.ProcessFixture/ResourceProbeWork.cs) was implemented to authenticate retained inputs; reconcile the 484,285-value history; copy current native content/runtime; write wrapped history payloads; and process 512 discovery, 128 selection and 4,000 confirmation report occurrences. Confirmation repetitions cycle through the 768 retained reports. Their input scenarios use 1,000 already-excluded labels to exercise serialization size, without creating an evaluation panel. The two planned audit workloads read 4,640 and 4,000 reports and hash the payload inventory twice.

Even a complete run of this probe would measure only that declared resource workload. It does not execute candidate generation, combat preparation/simulation, entropy sampling, statistical assessment or either complete diagnostic decision verifier. Three retained confirmation recipes cannot establish a fourth nominee's report distribution. Reading source reports, authenticating pins and recording probe measurements add overhead that differs from a gameplay run. These limits remain relevant to any future proposal.

The correction normalizes **both** compared paths and uses the platform's casing rules. Its regression test accepts alternate separators and Windows casing, while rejecting a different registry or content location. No production harness, gameplay code, candidate generation or statistical gate was changed. The failed attempt's three host source files, project file and producing DLL were copied to `TestResults/selection-diagnostic-probe-checks/failed-attempt-source` before rebuilding. Their hashes agree with the attempt's source and assembly pins; the corrected workload source is recorded separately.

## Verification

The required backend runner passed **63 cases before the attempt**: 11 new probe cases, 20 practical process cases and 32 diagnostic cases. After the path correction, **all 12 probe cases passed**, including the new path regression. That is **64 distinct cases** across these runs, not 75 distinct cases. Coverage includes combat rejection, timeout, storage overflow, parent death, output reuse, forbidden reservation filenames, historical-only labels and report repetition counts.

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/selection-diagnostic-build -Filter 'FullyQualifiedName~BalanceHarnessResourceProbeTests|FullyQualifiedName~BalanceHarnessPracticalProcessTests|FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests'
./build/run-tests.ps1 -ArtifactsPath TestResults/selection-diagnostic-build -Filter 'FullyQualifiedName~BalanceHarnessResourceProbeTests'
```

An initial build caught an ambiguous `Version` reference; it was fixed before the native attempt. The successful builds report existing test-project warnings. The final source correction was tested only with synthetic fixtures; it has no successful native measurement yet. An optional `Win32_Process` inventory query was denied by the sandbox; the owned-process tests and exit records supplied the needed lifecycle verification. No required verification command remains blocked.

Post-attempt verification reauthenticated all **1,617 retained inventory entries**, checked the exact seven-file failure output and preserved producing source/binary, and confirmed the passing TRX counts. The separate [verification inventory](../TestResults/selection-diagnostic-probe-checks/failed-attempt-inventory.json) pins 18 evidence files and is itself pinned by the JSON record. It is an external evidence inventory, not a diagnostic completion seal. All **419 local Markdown links** and whitespace checks passed. Of 225 baseline files, seven received the intended host/Markdown edits and the other **218 remained byte-identical**.

## Remaining decision

The proposed **1,380-second /512-MiB gameplay envelope remains a conditional sizing model**. This failed attempt provides no native history, copy, report or audit-cost qualification and changes none of its projections. Combat cost and completion probability also remain unestablished.

The next useful observation would be one fresh, separately authorized resource probe with the corrected source, frozen runtime/input pins and explicit limits. The closed allowance cannot be recycled, the fixed output cannot be resumed, and no replacement attempt is queued. A later gameplay request would still need its own frozen cumulative/prior accounting and compatible combat-cost evidence or explicit acceptance of capped operational-failure risk. Neither resource measurements nor unused allowance transfer silently between scopes.

Both anchors remain recommended. The retained **484,285 exclusions**, V19's **512 unused values /253 required recipes /Unresolved**, and adoption **Hold** are unchanged. There are no migrations, application configuration changes or deployment implications.

Changed files: added the two test-host probe files and their test class; added a dispatch branch to `FixtureHost.cs`; added this review and its JSON record; and updated the resource/implementation reviews, readiness review, main assessment, README and practical guide to point to this disposition. Historical analysis scripts, JSON calculations and sealed native records remain unchanged.
