# Proposal-affinity resource envelope: implemented and admitted

**Subsequent status — 23 September 2026:** Separately declared [pilot 02 completed and passed all audits](Tower-Proposal-Affinity-Pilot-02-Execution.md), reaching `AbandonThisConfiguration`. Its runtime retained all 27 admitted files, including the producing PDB. Every consumed admission and earlier failure described below remains closed and fully charged.

**Subsequent execution — 23 September 2026:** The [single admitted launch](Tower-Proposal-Affinity-Pilot-01-Execution.md) failed before allocation because its executable archive omitted the producing PDB. That failure is preserved, and retention is repaired with regression coverage. The admission and launch command described below have been consumed and cannot be reused; the remainder records the earlier admission state.

23 September 2026. Target: the offline Balance Harness.

**Resource envelope v2 is implemented, qualified and admitted.** The [sealed admission](../TestResults/proposal-affinity-resource-admission-repaired-20260923/admission.json) is ready for one owned scientific launch. No scientific combat, entropy draw or reservation occurred in this work. The complete live history remains **672,220 values across 246 files**. Gameplay defaults and recommendations are unchanged.

The scientific version remains `tower-proposal-affinity-comparison-v1`. A separately bound request field, `resourceEnvelope: tower-proposal-resource-envelope-v2`, selects the amended partition. The admitted request's hash binds that choice through the launcher, native worker, audits and publication. Missing resource metadata retains the original v1 interpretation; explicit v1 is also supported. Unknown versions and mismatched launch limits fail closed.

| Allowance | Original v1 | Admitted v2 |
| --- | ---: | ---: |
| Native execution | 9,600 seconds | **9,000 seconds** |
| Both audits and publication | 1,200 seconds | **1,800 seconds** |
| Scientific total | 10,800 seconds | **10,800 seconds** |
| Native storage | 5.5 GiB | **5.5 GiB** |
| Audit/publication storage | 0.5 GiB | **0.5 GiB** |

Both audits, publication and sealing share one absolute deadline. Native underspend cannot enlarge the audit partition. Native cancellation, enclosing Windows Job cleanup, retained-byte accounting and terminal verification enforce the selected envelope. The independent Python auditor also checks version, launch limits and completed resource accounting. Existing v1 archives retain their original bounds.

All scientific choices remain unchanged: twelve roots, the two frozen policies, 528 search trials per arm, 256 held-out values per root, 3,948 assigned values from one fixed entropy batch, the all-output freeze barrier, 21,888-fight maximum, endpoint thresholds and no replacement/refill/resume. The original plan file remains SHA-256 `f7840ded410c34a3283fd3424ec18af98cfc2057cee76181b02dfddf51666188`; runtime qualification changes only its scope binding.

The qualification used all captured gameplay dependencies and replaced only the four tested harness files. It compared **72 historical cases in each runtime**, with exact input and prepared-participant agreement, reproduced all three saved preview arms, resolved native entry points, and authenticated **222 producing source documents**. Only `TowerProposalStudyProtocol.cs`, `TowerProposalStudyRun.cs` and the new `TowerProposalStudyResources.cs` differ from the previously qualified production sources. Harness SHA-256 is `568158581ffb5e5f76f6fc2ea77121b73a862bcba4e9d1c28a39f09f12a74ea5`; execution hash is `c709aac9a7ef184fec2083e4ca1c1591fecae22801e89e94a95000792271a56b`.

The [admitted forecast](../TestResults/proposal-affinity-resource-admission-repaired-20260923/resource-forecast.json) is **4,239.253 native seconds /1,301.007 audit seconds**, with approximately **3.195 GB** projected total storage. It retains the previous native estimate as a floor, the single native cost probe, its doubled timing margin and 120-second publication reserve. Archive growth can only increase the hashing projection. These estimates are not guaranteed upper bounds; the hard limits still invalidate incomplete work.

One new technical failure is preserved. The first v2 admission qualified successfully, then referenced the previous forecast at a nonexistent path. The forecast was already authenticated inside the probe as `previous-resource-forecast.json`. The [failed attempt](../TestResults/proposal-affinity-resource-admission-20260923/failure.json) completed in 103.766 seconds and remains charged its full 900-second /1-GiB allowance. Its [closeout](../TestResults/proposal-resource-admission-closeout-20260923/verification.json) pins the complete failed package, qualified runtime and frozen measurements. The corrected lookup has a regression test.

The repaired admission used a new declaration, package and full 900-second /1-GiB charge. It copied the immutable qualification evidence, rebound only source locations, refreshed the complete history before and after admission, and repeated the native input check. **It did not repeat preparation or audit timing measurements.** The failed directory was neither resumed nor admitted. The successful admission took **159.672 seconds** and retained **93,920,731 bytes**.

Recorded admission/probe/review allowances through the final closeout total **4,440 seconds /4,630,511,616 bytes**. This includes all four admission attempts, the native probe and four 60-second /16-MiB reviews. Development builds and literal test runs remain separate engineering work, as in the preceding ledger. Including the untouched scientific allowance, the corresponding cumulative maximum is **15,240 seconds /11,072,962,560 bytes**. No failed allowance was refunded or transferred.

**Verification: 184 backend tests, 41 Python tests, owned completion and owned failure fixtures all pass.** The complete fixture produced 18,816 literal reports and passed native reconstruction, independent audit, publication and final native verification. The failure fixture retained its charged attempt and all 16,384 exposed literal values without publishing a result. Process receipts demonstrate a shared audit/publication deadline. The updated independent auditor also passed against the original externally pinned v1 fixture. Literal outcomes are engineering evidence only.

The [backend log](../TestResults/proposal-resource-backend-approved-20260923.log), [TRX](../TestResults/proposal-resource-backend-20260923.trx), [complete fixture](../TestResults/tower-proposal-owned-fixture-resource-v2-20260923/verification.json), [failure fixture](../TestResults/tower-proposal-owned-fixture-resource-v2-failure-20260923/verification.json) and [final closeout](../TestResults/proposal-resource-admitted-closeout-20260923/verification.json) are retained. Backend verification used:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessProposal|FullyQualifiedName~BalanceHarnessDamageAffinity|FullyQualifiedName~BalanceHarnessAdaptiveRacing|FullyQualifiedName~BalanceHarnessBatchRacing' -ArtifactsPath 'TestResults/proposal-resource-build-20260923'
```

Python verification covered `build/test-proposal-resource-envelope.py`, `build/test-proposal-affinity-study.py`, both owned-fixture modes, and the three admission test scripts under `Balance Harness/analysis`. The sandbox initially denied the existing NuGet configuration; the approved wrapper rerun succeeded. Existing unrelated warnings remain. The first admission's lookup failure was corrected and separately closed. No required verification remains blocked.

Changes comprise the three production C# resource files and `BalanceHarnessProposalResourceTests.cs`; the Python launcher, auditor and owned-fixture selector; resource/deadline/admission regression tests; shared qualification helpers with an explicit harness pin; the new resource admission and qualified-admission scripts; two immutable declarations; this report and current-status links. There are no database migrations, application configuration changes or deployment actions.

The current admission [manifest](../TestResults/proposal-affinity-resource-admission-repaired-20260923/files.json) has external SHA-256 **`4737c0bc455f3e7f7ce82149072564ec2f5431567ed45f8e5d10499c3235c711`**. Its request hash is `afed0ef3ad02f999cccecd195f806965d05a96994063071bc63c2feac68ec33c`. The final closeout manifest is `71ac98305d0ac781d1a03fdbfb37db4277c5cb8755bb6864cb1f45481ba1685c`. Historical reports, the original prospective amendment and all failed artifacts remain unchanged.

The next action is the single scientific study through the retained owner below. The output must remain absent; the launcher rechecks live history and refuses retry/resume. This command has **not** been run:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 TestResults/proposal-affinity-resource-admission-repaired-20260923/run-proposal-affinity-study.py --request TestResults/proposal-affinity-resource-admission-repaired-20260923/request.json --harness TestResults/proposal-affinity-resource-admission-repaired-20260923/runtime/BalanceHarness.dll --admission-pin 4737c0bc455f3e7f7ce82149072564ec2f5431567ed45f8e5d10499c3235c711
```
