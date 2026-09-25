# Confirmation admission: verification improvement and resource amendment

23 September 2026. Target: offline confirmation preparation and evidence tooling. **Complete package verification is implemented and measurably faster; the prospective accounting amendment is frozen for versioned implementation.** The [original failed admission](Tower-Practical-Three-Reference-Confirmation-Admission.md) remains closed and charged. No new native admission, preparation, scientific request, seed reservation or combat was performed.

The [amendment JSON](Tower-Practical-Three-Reference-Confirmation-Resource-Amendment.json) has SHA-256 **`5fdc1ba623a8be75bc32215e5375c3cbc86de64029b0025b071185f4f1e0c920`** and status `ProspectiveRequiresVersionedImplementation`. It binds the [original scientific plan](Tower-Practical-Three-Reference-Confirmation-Plan.json), SHA-256 `c6544a63a569de5197c4e2a35749c01a14388478e6a9986509d991c64bd6a74f`, without modifying it. Proposed native version: `tower-practical-three-reference-confirmation-v2`. This artifact is not an executable request.

## Verification improvement

The full original-verifier profile authenticated the implementation's **157,773 manifest-listed files** in **69.250 seconds with profiling enabled**. It recorded **315,546 path resolutions**, consuming **30.532 seconds**, and substantial repeated `Path.relative_to` work. The full tree scan itself took approximately 2.55 seconds. The smaller earlier sample did not include enough of this work to explain it.

The new [complete evidence verifier](analysis/confirmation-evidence-verification.py) carries relative names while scanning, checks the root and its ancestors once, and avoids rebuilding/resolving every path repeatedly. It still reads and hashes **every file** against the externally pinned manifest. It rejects missing/extra members, duplicate or unsafe manifest paths, case aliases, Windows device names, symlinks and reparse points. It checks file metadata before reads, compares path and opened-handle identities, then repeats the complete membership/metadata scan and manifest hash. Metadata is never a substitute for byte hashing or a cache across calls.

The [preparation helper](analysis/prepare-three-reference-confirmation-admission.py) now uses and copies this verifier, includes it in helper provenance, and passes its existing admission deadline/storage checks into authentication. The repaired PowerShell framework host remains in place. All complete historical-ledger checks remain unchanged: Python before and after native admission, plus the native history reconstruction. The old shared helper and every sealed package remain unchanged.

One sequential, **unprofiled** comparison authenticated the same three complete input archives with both implementations:

| Input archive | Manifest-listed files | Original verifier | New verifier |
| --- | ---: | ---: | ---: |
| Retained capture | 297 | 0.141 s | 0.078 s |
| Tested implementation | 157,773 | 50.843 s | 16.672 s |
| Closed selector comparison | 14,022 | 4.954 s | 1.656 s |
| Total | **172,092** | **55.938 s** | **18.406 s** |

Both implementations returned identical full manifests and checked every listed digest. The reduction in this comparison was approximately **67%**. [Comparison receipt](../TestResults/three-reference-confirmation-admission-overhead-20260923/comparison.json), [process receipt](../TestResults/three-reference-confirmation-admission-overhead-20260923/comparison-process.json), [baseline profile](../TestResults/three-reference-confirmation-admission-overhead-20260923/baseline-profile.txt) and [comparison log](../TestResults/three-reference-confirmation-admission-overhead-20260923/comparison.log) retain the observations.

This is a measured verifier improvement on one machine and filesystem cache. It does **not** fully explain the earlier 499.765-second failed admission, establish cold-cache timing, or measure the complete admission/native workflow. The second verifier ran after the first; cache/order effects are not controlled. The profiled 69.250-second result is not used as the speedup denominator. Actual captured-runtime compatibility, native preparation and complete-workflow feasibility remain unestablished.

## Explicit cumulative accounting

| Scope | Time ceiling | Storage ceiling |
| --- | ---: | ---: |
| Preserved failed admission, fully charged | 600 seconds | 512 MiB |
| One proposed new admission, fully charged | 600 seconds | 512 MiB |
| Execution owner, including audits/publication | 7,200 seconds | 3.5 GiB |
| **Proposed cumulative total** | **8,400 seconds** | **4.5 GiB** |

The increase over the original 7,800-second /4-GiB plan is exactly **600 seconds /512 MiB**. Before execution, the request must carry **two distinct pinned admission-charge receipts totaling 1,200 seconds /1 GiB**. The failed attempt's measured 499.765 seconds and 55,649,229 retained bytes do not refund its full allowance. Its original charge, failure and external inventory are pinned in the amendment.

The scientific owner remains 7,200 seconds /3.5 GiB. Native work remains 6,000 seconds /3 GiB; both audits and publication remain 1,200 seconds /512 MiB within the original owner deadline. There is no transfer between phases or automatic further attempt after another admission failure. The old v1 request, launcher and auditor still enforce their original limits; no wider allowance was silently enabled.

The five challengers, all three references, exact scenarios/content/settings, 6,500 shared values per team, 52,000 fights, family 38, all fifteen contrasts, qualification thresholds, sampling and full-tail reservation rules are unchanged. The amendment records hashes of every original top-level field except `resources` and `proposedNativeVersion`. Current recommendations and defaults are unchanged.

The accepted **18,180-second /13,584-MiB historical engineering ledger remains separately disclosed**, with complete historical engineering totals unknown. Profiling, helper fixtures and this planning/closeout are engineering work; none is charged as successful scientific admission. The initial scan/sample profile had a 300-second /128-MiB process cap; the full baseline and parity comparison each had 600-second /128-MiB caps. Their owned processes closed empty, with measured durations of 18.344, 69.390 and 74.515 seconds respectively. These are individual process observations, not a complete project-cost total.

## Verification and implementation handoff

**30 tests pass:** 12 evidence-verifier tests, 12 admission-helper regression tests and six resource-amendment tests. Coverage includes altered content with restored timestamps, real Windows junction rejection, changed files during verification, unsafe/duplicate paths, interruption, no cross-call digest cache, unchanged exact native family, host selection, full failed-charge retention, cumulative arithmetic and unchanged scientific fields. The amendment was generated and reproduced from the same inputs.

The [read-only closeout](../TestResults/three-reference-confirmation-admission-overhead-20260923/closeout.json) verified the failed package unchanged and rehashed all 240 previously recorded history files against their retained pins. It did not perform another full registry enumeration. [Source snapshots and receipts](../TestResults/three-reference-confirmation-admission-overhead-20260923/files.json) are sealed under manifest SHA-256 **`09026265fbd8b140687e0d7fcfad6fa4ceb1c93238b0bd321bab27592477a2bc`**. No scientific output exists.

The initial verifier tests exposed Windows differences between `DirEntry.stat`, `lstat` and `fstat`: directory enumeration omitted device/inode IDs, and path/handle `ctime` semantics differed after writes. The implementation now compares directory-scan metadata separately from opened-handle device/inode, size and modification time. Full byte hashing remains mandatory. Initial failed logs are preserved. A privileged symlink fixture was replaced with a real junction fixture, which passed without elevated access. The first profiling script also shadowed Python's standard `profile` module; it was renamed before the bounded profile ran.

Relevant verification commands:

```text
python -B -X utf8 "Balance Harness/analysis/test-confirmation-evidence-verification.py"
python -B -X utf8 "Balance Harness/analysis/test-three-reference-confirmation-admission.py"
python -B -X utf8 "Balance Harness/analysis/test-three-reference-confirmation-resource-amendment.py"
python -B -X utf8 "Balance Harness/analysis/three-reference-confirmation-resource-amendment.py"
```

**Next: implement the explicit v2 resource/accounting profile and its tests before another admission.** Pin both the unchanged scientific plan and this amendment; require both charge receipts; carry the cumulative totals through native validation, the Python owner, independent audit, completion and terminal receipt. Preserve v1 serialization and replay behavior. The current enforcement points include [native request validation](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationRun.cs), [protocol profiles](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationProtocol.cs), [owned native execution](../LL/tools/BalanceHarness/TowerThreeReferenceConfirmationRun.cs), [Python owner](../build/run-three-reference-confirmation.py) and [independent auditor](analysis/audit-three-reference-confirmation.py).

After that implementation has producing-runtime evidence, prepare a newly identified admission package within the proposed 600-second /512-MiB allowance. A second failure is terminal under this amendment. Never rename, repair in place or resume the original failed package, and do not start the 52,000-fight study before successful admission.

No backend/gameplay code, application configuration, migrations, shared database, deployment or infrastructure changed in this step. Backend tests were not rerun because only Python evidence/planning tooling and Markdown changed; the prior implementation verification remains historical evidence.
