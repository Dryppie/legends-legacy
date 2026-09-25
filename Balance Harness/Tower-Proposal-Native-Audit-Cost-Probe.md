# Native proposal audit-cost probe

23 September 2026. Target: the offline Balance Harness and its separate test executable.

**The single bounded probe completed, but the predeclared planning estimate still exceeds the existing audit partition: 1,301.007 seconds versus 1,200. No admission, scientific campaign, new combat, entropy or reservation occurred.** The next implementation is the [proposed resource-envelope amendment](Tower-Proposal-Affinity-Resource-Amendment.json): 9,000 seconds for native execution and 1,800 for both audits/publication, preserving the existing 10,800-second /6-GiB scientific total. The proposal is not implemented or admitted.

## Measured work

The probe ran once in a suspended Windows Job with a fixed **600-second /256-MiB** allowance. Its native worker had 480 seconds; both independent audits and final sealing shared the remaining enclosing allowance. The output is outside `TestResults/balance`. Existing output cannot be reused, and there is no native retry, warm-up run or resampling after the result.

The new helper lives in `BalanceHarness.ProcessFixture`. It ran alongside the **unchanged qualified harness and captured gameplay dependencies**, not the gameplay assemblies produced by the new test build. Harness SHA-256 remains `015b8e06535543414d497ccdfb35f647b534f24fe655cc4732a845e082aa4a9b`; execution hash remains `c0657716db5c6303edf6fbe14f5b8853095cb57f12be1b9428580c71dec4a513`. Its twelve test-host source documents were checked against the producing symbols and retained.

The historical native phase used the existing fixed-family verifier to reconstruct **all 27,648 previously saved inputs, cache keys, trial/report bindings, freezes, journals and the saved study**. It preserved the old archive's execution identity. The probe supplied only the previously qualified input factory after checking matching gameplay assemblies, platform, settings and content. Combat guards remained active. This read existing observations; it did not execute their fights again.

For full-history proposal work, the helper selected the first 876 sorted already-reserved values outside the declared legacy/reference schedules. It built twelve resource-only blocks, each shared by the two frozen policies. Every arm retained **671,851 exclusions**; each serialized plan was approximately 17.28 MB. All 24 native policy trajectories and their reconstruction passed with the same fixed literal outcome on every request: defeat, guardian health 100, survival 0 and duration 1. These 12,672 literal rows are workload fixtures, not observed outcomes or search-quality evidence. They cannot serve as a scientific allocation.

| Phase | Seconds | Work |
| --- | ---: | --- |
| Runtime, source and capture authentication | 0.123 | Qualified runtime and source-symbol bindings |
| Historical outer inventory | 38.903 | 27,951 pinned files |
| Historical native reconstruction | 96.014 | 27,648 saved inputs and reports |
| Full-history workload construction and hashing | 1.857 | 24 plans |
| Full-history proposal generation and replay | 35.630 | 24 complete 528-row trajectories |
| Entire owned native process | **173.062** | Includes startup and enclosing overhead |
| Independent historical Python audit | 34.375 | Retained recognition observations |
| Independent proposal Python audit | 15.063 | Externally pinned complete literal study |
| Entire probe through sealing | **224.796** | **24,660,615 retained bytes** |

The C# input-materialization submeasurement was **28.473 seconds**, approximately **1.03 ms per input**. It is contained in the 96.014-second verifier phase and must not be added to it. The earlier PowerShell p95 included materialization and input hashing; this C# submeasurement includes materialization alone and reports a mean over a different workload. Their ratio is not a like-for-like speedup estimate.

## Frozen planning result

Before execution, the owner fixed this formula:

```text
2 × (whole native worker + both independent audits
     + 2 × historical inventory time × max(1, projected bytes / historical bytes))
+ 120 seconds publication reserve
```

It intentionally retains the entire worker cost, including synthetic trajectory creation, even though a future audit only reconstructs saved trajectories. It also adds two projected outer inventory passes. The timing margin remains two, and the publication reserve remains 120 seconds. No measured phase was discarded after seeing its cost.

The prior storage projection is 4.7297 times the historical archive size. The two extra inventory passes therefore contribute 368.003 seconds before the timing margin. The resulting [assessment](../TestResults/proposal-native-audit-cost-probe-20260923/resource-assessment.json) is **`AuditPartitionStillNotSupported`**, at **1,301.007 seconds**. The existing 1,200-second partition remains binding. The probe does not prove that a future audit would exceed it; the archive-byte extrapolation is conservative, and neither the retained historical teams nor fixed literal feedback cover every future trajectory.

This measurement removes the need to use PowerShell per-call timing as native-loop throughput. It does not justify repeatedly measuring until one sample fits, reducing the predeclared margin, or silently borrowing native time. An explicit resource-envelope revision is the concrete next step.

The [prospective amendment](Tower-Proposal-Affinity-Resource-Amendment.json) moves 600 seconds from native execution to audit/publication. The unchanged prior native planning estimate of 4,158.750 seconds fits the proposed 9,000-second native cap; the probe's estimate fits the proposed 1,800-second audit cap. This remains planning evidence, not a completion guarantee. All scientific policies, roots, allocations, endpoints, fight limits and storage partitions remain unchanged. A future implementation must bind the envelope explicitly, preserve v1 verification, qualify its producing runtime and complete another separately charged admission with refreshed history.

## Evidence and verification

The [probe manifest](../TestResults/proposal-native-audit-cost-probe-20260923/files.json) has external SHA-256 **`8929c5b94a95e717e8f7ec2ffb4d94ee01d0cbc9083e4b31873f635a5cc85cf7`**. It includes the frozen declaration, qualified source inventory, exact retained runtime, source-symbol proof, every phase and arm receipt, both independent audits, process receipts, completion and producing owner scripts. All three owned processes exited zero, without timeout or active descendants. The qualification and historical source pins were preserved.

The full probe allowance remains charged at 600 seconds /256 MiB despite completing sooner. Together with the two closed admissions and their earlier review, the recorded allowances through probe completion total **2,460 seconds /2,432,696,320 bytes**. They are separate from the untouched scientific allowance and are not a claim to total all earlier implementation/test work. Both failed admission packages remain unchanged; the live allocation registry was not rescanned or modified by this resource probe.

**Five backend tests and eight Python/owned-process tests pass.** Coverage includes complete historical-only workload construction, exact native replay with literal outcomes, gameplay/platform/settings compatibility, rejection of changed output/limits, resource arithmetic and incomplete workload rejection, plus real Windows Job success, timeout cleanup, storage failure and combat-guard failure. The [backend log](../TestResults/proposal-audit-probe-backend-final-20260923.log), [TRX](../TestResults/proposal-audit-probe-backend-final-20260923.trx) and [Python log](../TestResults/proposal-audit-probe-python-20260923.log) are retained.

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessProposalAuditCostProbeTests' -ArtifactsPath 'TestResults/proposal-audit-probe-build-final-20260923'
```

```text
python -B -X utf8 build/test-proposal-audit-cost-probe.py --host TestResults/proposal-audit-probe-build-final-20260923/bin/BalanceHarness.ProcessFixture/release/BalanceHarness.ProcessFixture.dll
```

The wrapper required approved access to the existing NuGet configuration. The first compiled test run exposed mixed Windows path separators in the native output comparison; normalized full-path comparison fixed it. Both original logs and the 4/5 test result are preserved. The corrected suite passed 5/5. Existing unrelated build warnings remain. No required verification command remains blocked.

Changed files are `ProposalAuditCostProbe.cs`, its exact dispatcher route in `FixtureHost.cs`, `BalanceHarnessProposalAuditCostProbeTests.cs`, the two Python owner/test scripts, this report, the proposed amendment JSON and current-status links. No production harness code, gameplay defaults, database migrations, application configuration or deployment changed.
