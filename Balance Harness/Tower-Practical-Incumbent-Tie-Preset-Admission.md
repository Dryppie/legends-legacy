# Practical incumbent-tie preset: captured-runtime admission

**Execution follow-up:** The authorized [single practical search completed and passed both audits](Tower-Practical-Incumbent-Tie-Preset-Execution.md). It returned `ImprovementNotDemonstrated` after 3,496 fights; permanent exclusions now total 539,367. This admission record describes the inputs before that run. Its intended output now exists and its scope is closed.

22 September 2026. Target: offline BalanceHarness. **ReadyNoReservation.** The public preset command and ordinary allocation admission passed against the captured floor-5 gameplay runtime. The complete live registry contains **538,326 permanent exclusions across 224 ledger files**. Preparation allocated no values, ran no combat and created no scientific output.

This completes the admission handoff from the [verified preset implementation](Tower-Practical-Incumbent-Tie-Preset.md). The [completed selector comparison](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) remains closed. Its result supports the selector for its frozen outputs; this admission establishes compatibility and input validity, not a new improvement result.

## Concrete request and scope

The [prepared request](../TestResults/incumbent-tie-practical-admission-20260922/preset/request.json) and [unscheduled template](../TestResults/incumbent-tie-practical-admission-20260922/preset/template.json) describe one practical search on the captured floor-5 encounter: ten level-40 characters, five Essences each, fixed equipment, actor identities, subgroup membership and ability order, with `OwnedCopies=null`. Both exact supplied recipes and the allowed pool are preserved.

| Choice | Frozen value |
| --- | --- |
| Generator | `retained-composition-incumbents-v1` |
| Selector | `tower-staged-incumbent-tie-v1` |
| Designated reference | `confirmed-399bc7760fb0cf790a5d8ac4` |
| Designated party | `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b` |
| Candidate/proposal limit | 46 evaluated candidates; at most 256 proposals |
| Discovery | 8 trials per candidate; 368 fights |
| Selection | Four nominees, 32 trials each; 128 fights |
| Confirmation | 1,000 trials per distinct frozen team; at most 3,000 fights |
| Maximum total | **3,496 fights**; no diagnostics or replay reserve |
| Future allocation | Master `20260922`; domain `practical-incumbent-tie-pilot-20260922` |
| Future panel size | 1,041 accepted values: 1 construction +8 discovery +32 selection +1,000 confirmation |
| Proposed execution ceiling | 1,200 seconds /1 GiB; one run, zero retries, no resume |
| Intended new output | `TestResults/balance/tower-practical-incumbent-tie-20260922` (absent at admission; subsequently completed) |

The 8/32/1,000 panels preserve the previous comparison's per-search sampling scale, with one prospective root for this practical workflow. This choice does not guarantee enough precision to establish a five-point improvement. If the selected primary is a supplied reference, confirmation deduplicates it and actual fights can be below the conservative maximum. The native check's `declaredValues=1041` counts temporary validation labels; no allocation candidates were derived or reserved.

The new source template changes only the previous unscheduled template's ID, producing execution identity and complete historical union. The public preset then changes only the selector and its explicit primary. The source request's counts, master/domain, limits, history pins, recovery mappings and content binding survive unchanged. No old unused value or experiment allowance funds this proposal.

## Compatibility and admission evidence

The [preparation helper](analysis/prepare-incumbent-tie-preset.py) authenticates the preceding captured package and the preset implementation evidence before copying inputs. It replaces only the four harness runtime files with the tested build. All other runtime dependencies, 16 content files and the effective settings file retain their captured hashes. This is a captured gameplay scope; it makes no compatibility claim about current development gameplay.

Portable symbols authenticate **190 source documents** against the tested DLL. The context helper resolves **538 methods**, including the practical preset and allocation admission paths, without executing their bodies. The subsequent public admission prepares native recipes and audits the recovered historical reservation through the existing production path, with the combat guard active.

- [Preset receipt](../TestResults/incumbent-tie-practical-admission-20260922/preset/preset.json): `PreparedNeedsAdmission`, exact primary, source/output hashes and cost.
- [Native admission](../TestResults/incumbent-tie-practical-admission-20260922/native-check.json): `ReadyNoReservation`, 538,326 exclusions, zero new values and fights.
- [Compatibility receipt](../TestResults/incumbent-tie-practical-admission-20260922/context.json), [preparation accounting](../TestResults/incumbent-tie-practical-admission-20260922/preparation.json) and [external package pin](../TestResults/incumbent-tie-practical-admission-20260922-pin.json).
- [Exact package inventory](../TestResults/incumbent-tie-practical-admission-20260922/files.json) and [preparation log](../TestResults/incumbent-tie-practical-admission-20260922.log).

The runtime DLL SHA-256 is `935d2fa1239d84b588b008a78ad2228f10c123889f416d3c8f8a26655ad3bd82`; the captured execution identity is `ad9c108a778e372c27cedf9a3ffe1ee7be57ab89892709d0a2cbdcbd9e7325a3`. Settings remain `f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74`. The prepared request's file SHA-256 is `9e74fc1d755be81f3b3e02d2fc7a9c5e78742de71a13d2f09edfae69f988ab81`; its native canonical hash is separately recorded in the admission receipt.

The package manifest SHA-256 is **`3f580b3216d1da086e4bdc080c6a883e3f1bceaec4028500ed78ccaa585cf034`**: 275 inventoried files plus the manifest, totaling 46,487,567 bytes. The preparation receipt records 115.235 seconds through admission and the subsequent history recheck; package sealing and verification follow that measurement. The native admission process itself took 21.234 seconds. The final preparation deadline check passed after verification; the 115.235-second figure is not the full enclosing wall time.

Preparation has its own 600-second /512-MiB engineering ceiling, separate from the proposed scientific execution. The context, preset and admission processes were each created suspended, assigned to the existing Windows Job Object before resuming, and verified empty after exit. All three exited successfully without timeout. Repeated read-only history scans check that admission itself changed no ledger or exclusion. No failed scientific claim was created or retried.

## Verification and execution handoff

The helper checks exact package membership and file hashes, runtime/content preservation, compiled source copies, exact template/request changes, receipt costs, completed process ownership and unchanged live history. Its verification route does not call a native command, allocate or fight. Rechecking an admission package is valid only while its intended scientific output is absent and its live history remains unchanged.

```powershell
# Already completed once; prepare refuses an existing package.
python -B 'Balance Harness/analysis/prepare-incumbent-tie-preset.py' prepare

# Read-only check, using the externally recorded SHA-256 value.
python -B 'TestResults/incumbent-tie-practical-admission-20260922/prepare.py' verify --manifest-sha256 3f580b3216d1da086e4bdc080c6a883e3f1bceaec4028500ed78ccaa585cf034
```

The authorized execution step used one owned, bounded `tower-practical-search-allocate-run` with this exact captured runtime and prepared request, followed by archive verification and a saved-evidence review. Its preflight checked the external manifest pin and live history; the public workflow repeated admission under its registry lease. The [execution report](Tower-Practical-Incumbent-Tie-Preset-Execution.md) closes that handoff. Do not refresh this package, change the root, extend confirmation, or rerun its completed claim. A changed input requires a newly reviewed scope.

Successful operational execution means a complete, verified archive with matching attempt accounting, selection, practical decision and team exports inside the declared limits. A novel team demonstrates improvement only under the existing practical gate: supported 10% viability, at least five observed percentage points over **both** references, and positive adjusted paired lower bounds. `IncumbentRetained` and `ImprovementNotDemonstrated` are valid terminal results. They do not authorize another root. Any interruption or integrity/resource failure ends this attempt; preserve its output and all exposed values. This single search cannot establish optimizer reliability or encounter balance.

Changed source is limited to the preparation helper; this review, the preset review, README and practical guide carry the handoff. The harness binary and C# implementation are unchanged from the **180 passing tests** already recorded. No backend rebuild is needed for this orchestration/documentation step. Python syntax, real public preset/admission, process cleanup, package/history authentication and local Markdown links are the relevant checks. No migration, application configuration change or service deployment is involved.
