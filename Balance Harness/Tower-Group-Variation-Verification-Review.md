# Same-group variation: corrected zero-combat verification passed

**Publication accounting corrected:** the primary report writer recorded an invalid negative duration after reusing a timing variable. Verification results remain passed. The [accounting correction](Tower-Group-Variation-Verification-Accounting-Correction.md) preserves that receipt and provides positive cumulative totals with a conservative one-second publication charge. No diagnostic was rerun.

15 September 2026. **VerifiedNoCombat. All 63 targeted tests passed**, and the independent captured-content audit verified **1,956 schedule positions and 19/19 distinct legal complete requests**. All 17 guided requests retained their exact requested owner counts. Four complete group bundles each produced a different second filler recipe at the same five-owner count and reserved placement. **Zero fights, combat replays, preparations or fresh balance values. All 482,461 reservations preserved. Adoption Hold.**

## What is now verified

The opt-in `independent-group-variation-v1` / `group-variation-joint` policy spends existing fresh requests on the same selected catalogue group at several counts and filler draws. For ten characters, its first sweep uses **1, 5, 10 and 5 owners**, with filler draws **0, 0, 0 and 1**. Every eighth fresh request remains uniform. The second sweep rotates those counts to **2, 6, 1 and 6**. Each guided sweep visits all 214 catalogue groups once in complete four-request bundles. The two sweeps comprise **1,712 guided choices plus 244 uniform positions**; these schedule records are not 1,712 filled parties or fights.

The first 19 captured-input constructions produced 17 guided recipes and two uniform recipes, all legal and all distinct. The guided prefix covers four complete bundles and the first request of a fifth group. Names below describe groups the generic schedule selected; no Essence-specific rules or control recipes were added.

| Selected group | Request indexes | Actual complete-group owners | Second filler recipe differs at fixed count/placement |
|---|---|---|---|
| Pack Howler, Venomous Snake, Wind Harpy | 0, 1, 2, 3 | 1, 5, 10, 5 | Yes |
| Cave Bat, Grave Hound, Web Weaver Spider | 4, 5, 6, 8 | 1, 5, 10, 5 | Yes |
| Blackjaw Spider, Frost Imp, Pack Howler | 9, 10, 11, 12 | 1, 5, 10, 5 | Yes |
| Grave Wisp, Pack Howler, Venomous Spiderling, Web Weaver Spider | 13, 14, 16, 17 | 1, 5, 10, 5 | Yes |
| Bloodfang Wolf, Cinder Beetle, Smolder Rat | 18 | 1 | Incomplete bundle |

The independent audit agrees with every requested, placed and final group count. Reserved targets are nested across count variants; the two five-owner draws have identical target lists and different final recipe hashes in all four completed bundles. It also verifies canonical ordering, known Essence IDs, character/slot/family/ownership limits, complete recipe hashes and the exact previously sealed catalogue identity/hash. All recipes and traces are retained in [content.log](../TestResults/balance/tower-group-variation-verification-20260915/content.log); the [readable construction table](../TestResults/balance/tower-group-variation-verification-20260915/constructions.md) and [independent result](../TestResults/balance/tower-group-variation-verification-20260915/content-verification.json) summarize them.

This demonstrates reachable count/filler variation with unchanged budgets. It does **not** establish stronger builds. A 19-request prefix now spends its 17 guided requests on five groups rather than the preceding schedule's 17 distinct groups. More variation within a group costs catalogue breadth. The captured fixture observed no rejection or duplicate, but other inventories, groups and filler interactions can still reject or duplicate a request, consuming proposal budget normally. Shared filler-stream labels across count variants do not guarantee otherwise identical filler choices when reserved counts change. No causal combat effect is claimed.

## Corrected fixture and historical parity

The [previous attempt](Tower-Group-Variation-Implementation-Review.md) remains sealed with **56 passed / 7 failed**. Its shared fixture supplied only one base core, which cannot form a joined group. This separate pass uses the prepared two-overlapping-core correction and an exactly-one-group precondition. The complete-search test now additionally requires an **evaluated guided variation proposal**, so success cannot come entirely from the uniform fallback. The [fixture patch](../TestResults/balance/tower-group-variation-verification-20260915/fixture-changes.patch) retains both changes relative to the failed test source.

All **17 composition, 15 joined, 16 group/count and 15 variation cases passed** through `build/run-tests.ps1`. The corrected cases cover real guided construction, deterministic search, fixed/nested targets and filler variation, one/two/ten-owner schedule boundaries, ownership rollback, count-drift rejection, rejected/duplicate ordinal consumption, per-arm reset, existing caps, cancellation/checkpoint retention, metadata/method validation and the fixed-order boundary. The original-policy parity regressions also passed.

The old group/count result matches the previously captured reference exactly: **128 synthetic evaluations / 171 proposals**, full-result SHA-256 `b91e655af93f09f35a1d2d9a8373c3814eced62ce91aa4f21e1b102753aae554`. The earlier reference is retained by hash and was not executed again. Synthetic observation labels are fixture identifiers, not new seed allocations. All test classes and the construction driver guard combat entry.

No harness policy source changed in this verification pass. Its source hashes match the implementation snapshot, and the reused candidate files match their captured binary hashes. Only the corrected test assembly was rebuilt; captured gameplay dependencies were reused. No unrelated dirty gameplay build was performed.

## Measured work and limits

| Phase | Measured time | Result |
|---|---:|---|
| Initial frozen-input/package preservation | 1.250 s | Passed |
| Corrected test assembly build, separately measured | 2.703 s | Passed |
| Required backend test wrapper | 4.922 s | 63 passed, 0 failed, 0 skipped |
| Captured construction command/check phase | 0.312 s | Complete |
| Independent recount/check phase | 0.125 s | Verified |
| Final input/binary/package preservation | 1.219 s | Passed |

The construction driver's internal stopwatch measured **106.726 ms**. Its top-level trace phases were **11.429 ms** for catalogue construction, **17.390 ms** for the schedule and **77.887 ms** for the 19 complete requests including generator initialization. Nested hash timing is retained but must not be added again. Command/check wall time also includes process startup, input/output and hash checks; these are single-call construction measurements, not a controlled performance speedup or combat-throughput estimate.

Before publication, this pass used **7.828 / 120 additional diagnostic seconds**, cumulative **270.894 / 1,800 seconds**. The failed attempt's **8.625 diagnostic seconds** remain charged; they are not replaced by this successful pass. Build time is recorded separately. Final publication time, new-output bytes and cumulative totals are in [completion.json](../TestResults/balance/tower-group-variation-verification-accounting-20260915/completion.json). The additional output cap is 128 MiB, combined with the failed implementation inside its 512-MiB envelope and preceding evidence inside 4 GiB. No cap increased, retry or resume occurred.

Initial and final checks preserved **6,520 indexed files across 16 sealed packages**, including the failed implementation and its one-core fixture/binaries/TRX. The previous shared test TRX was saved before this wrapper invocation. There is one unchanged xUnit2031 style warning in the older composition test; compilation and all tests pass. Source snapshots and package inventories isolate the result from unrelated checkout work.

## Reproducible command record

The [protocol](Tower-Group-Variation-Verification-Protocol.md), source, workflow/audit scripts, captured inputs/catalogue and executable hashes froze before execution. Test binaries froze after their build and before testing. Executed once from the repository root:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-variation-verification-20260915'
& $py -B "$w/freeze.py"
& $py -B "$w/workflow.py" bootstrap
& $py -B "$w/workflow.py" build
& $py -B "$w/workflow.py" tests
& $py -B "$w/workflow.py" content
& $py -B "$w/workflow.py" audit
& $py -B "$w/workflow.py" preserve
& $py -B "$w/freeze-publication.py"
& $py -B "$w/publish.py"
```

The tests phase used:

```powershell
./build/run-tests.ps1 -NoBuild `
  -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessGroupCountTests|FullyQualifiedName~BalanceHarnessGroupVariationTests' `
  -ArtifactsPath 'TestResults/balance/tower-group-variation-verification-20260915/tests'
```

These are archived commands for this exact frozen package. Reproduction needs a separate output directory and explicit freeze; do not overwrite or rerun this sealed package. Per-command receipts retain actual arguments, limits, exit codes and logs. [TRX](../TestResults/balance/tower-group-variation-verification-20260915/tests.trx), [test outcomes](../TestResults/balance/tower-group-variation-verification-20260915/test-summary.json), [build log](../TestResults/balance/tower-group-variation-verification-20260915/build.log), [independent audit log](../TestResults/balance/tower-group-variation-verification-20260915/audit.log), [preservation](../TestResults/balance/tower-group-variation-verification-20260915/preservation.json), [binary hashes](../TestResults/balance/tower-group-variation-verification-20260915/binaries.json), [file seal](../TestResults/balance/tower-group-variation-verification-20260915/files.json).

## Changed files and next step

This pass adds the explicit guided-evaluation assertion to `BalanceHarnessGroupVariationTests.cs`, verifies the already applied two-core fixture correction, and adds this report, the corrected verification protocol and evidence package. Six active documents now mark construction verification complete: search strategy reset, automatic discovery plan/implementation, coverage replication plan, acceptance policy and harness README. Historical reports and sealed snapshots remain unchanged.

Next prepare a small **equal-input, equal-budget comparison against the preceding group/count policy**, freezing selection, controls, limits and measurements before execution. The comparison should determine whether spending fewer group selections on more count/filler variants improves search outcomes; these construction checks cannot answer that. Keep controls outside generation, fixed ability order and unchanged gameplay. No new balance study or fresh-seed allocation is launched or authorized by this report; existing fresh-value exceptions are exhausted, so any new values require a specific exception.

No required command was blocked or omitted. The full backend suite and combat/archive execution were deliberately outside this zero-combat scope. No configuration changes, migrations, deployment, Kharad tuning, ability-order optimization, old-cap increase, sealed-v19 change or large confirmation. All **482,461 reservations**, including the unused original 512, remain preserved. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, and adoption **Hold** are unchanged.
