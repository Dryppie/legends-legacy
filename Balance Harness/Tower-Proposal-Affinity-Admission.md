# Proposal-affinity runtime qualification and admission

Current status (2026-09-25): The twelve-root placement comparison completed 16,000 actual fights and all audits. Frozen decision: Inconclusive; no demonstrated improvement and no default promotion. See [completed comparison and diagnosis](<Tower-Loadout-Placement-Plain-Cost-Reconciliation.md>).

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**Runtime qualification passed; resource admission was rejected. No campaign, combat, entropy draw or reservation occurred.** The next step is a separately bounded native audit-cost probe. Both admission attempts are closed and retained; no package is admitted for launch.

## What passed

The latest test build contains changed gameplay assembly hashes. Admission therefore retained all 23 captured dependency files and substituted only the four verified harness files: DLL, portable symbols, dependency manifest and runtime configuration. The harness DLL remains SHA-256 `015b8e06535543414d497ccdfb35f647b534f24fe655cc4732a845e082aa4a9b`. No current gameplay DLL was substituted into the capture.

Separate owned processes compared the captured and proposed runtimes on all 36 team entries in the retained three-arm preview. Each recipe used the first and last already-reserved values from the declared legacy schedules. Preview recipes are seedless; these deterministic projections supply historical values solely for preparation. They do not preview prospective roots or generate observations.

All **72 input hashes and 72 prepared participant hashes per runtime matched exactly**, covering 144 native preparations in total, with combat guards active. The current harness also reproduced the complete typed inventory and the entire retained first-wave export. Its 221 source documents matched their producing portable symbols, and 647 methods were resolved without running their search, allocation or combat bodies. Nine native source files—including the combat runner, content loader, inventory builder and archive implementation—also match the captured source byte-for-byte.

The captured execution hash is `41385143ac5c20eada2a6ac1e11cafae637cebf3ed49b7fc2448f541ee017789`; the qualified execution hash is `c0657716db5c6303edf6fbe14f5b8853095cb57f12be1b9428580c71dec4a513`. This is an explicit compatibility check and new binding. It does not relabel the old runtime.

The independently scanned historical union remains **672,220 values across 246 ledger files**. The native request check reconstructed that union and returned `InputsVerifiedNoReservation`. The independent Python auditor also passed against the externally pinned, previously completed literal-report fixture. All five owned processes exited successfully, without timeout or active descendants. The post-failure review did not repeat the live registry scan; a future admission must refresh it again.

The original prospective plan remains byte-for-byte unchanged, with file SHA-256 `f7840ded410c34a3283fd3424ec18af98cfc2057cee76181b02dfddf51666188`. A [separate runtime-bound plan](../TestResults/proposal-affinity-admission-repaired-20260923/plan.json) was generated, with canonical hash `9fe4254fb1ede131fad179f094f71d83203504ac5eb96806076c632acf91db21`. Only its scope binding differs; policies, affinities, benchmark, twelve roots, endpoints, thresholds and resource limits are unchanged. **This plan is not admitted.**

## Why admission stopped

The planning formula was fixed before the measurements: twice the scaled historical whole-phase cost plus the owned literal-study phase, with additional measured input/preparation work. It charges the first sampled call once and twice the empirical 95th percentile of subsequent calls across 21,888 possible trials. Storage includes explicit per-arm evidence caps, duplicated contexts and content, runtime retention and historical archive scaling. These estimates are not guaranteed bounds.

| Partition | Planning estimate | Fixed cap | Result |
| --- | ---: | ---: | --- |
| Native execution | 4,158.750 seconds | 9,600 seconds | Fits |
| Both audits and publication | **1,361.463 seconds** | **1,200 seconds** | **Rejected** |
| Native retained storage | 3,159,678,639 bytes | 5,905,580,032 bytes | Fits |
| Audit/publication storage | 34,989,534 bytes | 536,870,912 bytes | Fits |

The total estimated time is below 10,800 seconds, but the separate audit partition still fails. No native time was transferred into it, no margin was reduced after seeing the result, and no further admission was attempted to obtain a faster sample.

The candidate process's subsequent-call input-materialization p95 was 21.4072 ms, contributing 937.198 seconds to the conservative audit estimate. This measurement includes PowerShell-to-native invocation and hashing. It does **not** demonstrate that the native audit will take 1,361 seconds. The missing evidence is a representative measurement through the actual native loop, including full-history serialization, archive reading, input reconstruction and publication work.

The first resource rejection retained all raw measurements but did not write the derived estimate before throwing. The follow-up implementation now saves failed estimates before enforcing admission. The [read-only closeout](../TestResults/proposal-affinity-admission-review-20260923/reconstructed-resource-forecast.json) independently reconstructs the same arithmetic from the original retained measurements; neither failed package was modified.

## Preserved attempts and charges

| Work | Outcome | Measured seconds | Fully charged allowance |
| --- | --- | ---: | --- |
| First admission | Probe builder assumed nonempty preview seeds; stopped before preparation | 93.328 | 900 seconds /1 GiB |
| Separate corrected admission | Runtime/checks passed; audit resource estimate rejected | 99.984 | 900 seconds /1 GiB |
| Read-only failure closeout | Both attempts authenticated and sealed externally | 2.437 | 60 seconds /16 MiB |

These charges total **1,860 seconds and 2 GiB +16 MiB**, separately from the untouched scientific allowance. They do not purport to include every earlier implementation/test cost. The two admission directories retain 165,843,364 bytes; the closeout retains another 111,616 bytes. Both failures remain closed, with no admission receipt, scientific output or completion publication.

The first [failure receipt](../TestResults/proposal-affinity-admission-20260923/failure.json) has SHA-256 `5924ebc3e5ced8162d1d1cd4b764d1eef04113b5c827d8325eda89c04d897d3a`; the second [failure receipt](../TestResults/proposal-affinity-admission-repaired-20260923/failure.json) has SHA-256 `9a03f31ce2e1d9e38a9a04220d4deb80fffb33bfbb7fc5abb2660052bbb70e81`.

The [closeout manifest](../TestResults/proposal-affinity-admission-review-20260923/files.json), including exact inventories of both failed attempts, has external SHA-256 **`b2cd5249373b58069af6000579aa2d73c971999adc74b5d57e19a0369b8c28d2`**. Its [review](../TestResults/proposal-affinity-admission-review-20260923/review.json) records `RuntimeQualifiedResourceAdmissionRejected`. This is a failure/qualification closeout, not a launch admission pin.

## Verification and next step

**12 admission tests pass.** They cover seedless historical probes, legacy exclusions, immutable physical/design inputs, runtime dependency substitution, exact input/prepared snapshot parity, owned-process completion, manifest tampering, nonfinite timings, separate resource caps, retaining rejected estimates, and closing admission before another charge. The final [test log](../TestResults/proposal-affinity-admission-tests-closed-20260923.log) is retained. Earlier 9-, 10- and 11-case runs are retained as well.

```text
python -B -X utf8 "Balance Harness/analysis/test-proposal-affinity-admission.py"
```

The native qualification helper passed PowerShell parsing and completed its baseline, candidate and plan-binding modes. The public native request check and independent fixture audit passed under the owned process launcher. No C# source or producing binary changed, so the preceding 184-case backend suite and 20-case final focused verification were authenticated rather than rebuilt. No verification command is blocked by access or missing dependencies; admission itself failed its resource criterion.

Next, implement one prospectively bounded, combat-free native audit-cost probe using the qualified runtime and retained historical inputs. Measure materialization/hashing within C#, full-history context handling and representative archive/audit costs. Retain its own charge and failure receipts, do not draw values, and do not keep resampling until a timing fits. Use that evidence to decide whether the unchanged scientific partitions are feasible or require an explicitly revised resource design. Any later admission must preserve both failed charges and refresh the registry. The current admission CLI refuses another attempt.

Changed repository files: `analysis/prepare-proposal-affinity-admission.py`, `analysis/proposal-affinity-admission-context.ps1`, `analysis/test-proposal-affinity-admission.py`, this report, and current-status links in the harness guides and preceding reports. There are no gameplay/default changes, database migrations, application configuration changes, deployments or infrastructure changes.
