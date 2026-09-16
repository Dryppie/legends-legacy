# Standalone retained-composition: native preparation

Both historical teams passed the actual `tower-retained-composition-prepare` branch using the verified standalone harness and current gameplay/content. The command returned zero and saved the expected four preparation artifacts. An independent audit verified those artifacts. **The surrounding native test failed on an incorrect expected cost; it is not reported as passing.** The corrected assertion is prepared, not executed. No production code changed and no search or combat ran.

This closes the evidence gap about whether the standalone preparation branch can admit these two teams. It does not close the failed test or establish team strength, player inventory availability, fresh schedules or readiness to launch a practical study. The failed block-search proposal remains closed, adoption Hold and V19 reliability Unresolved.

## What ran

The [new package](../TestResults/balance/tower-retained-native-admission-20260916) verifies the prior source and assembly pins, including all 1,337 gameplay files. It reuses the newly compiled standalone `BalanceHarness.dll` with matching gameplay dependencies, rather than the withdrawn controller's older harness assembly. Current content and only the required non-secret combat settings are copied into an isolated directory. The already tested settings-writing method is reused without changing production readers or live configuration.

One isolated native test calls production `Program.Main` with the real preparation arguments under a guard that throws if combat tracing starts. The branch performs production equipment materialization for ten character templates and both reference inputs before writing output. It admits:

- `team-040e60d3dbc5c127321653c47ed3a9d3`
- `team-49f6979895354870c89362d4abf214bb`

Each remains a floor-5 party of ten level-40 characters with five Essences, tier 1/rank 2, fixed equipment, neutral identities and ordinal Essence order. The eligible pool remains 85 Essences in 82 families. Recipes and evidence hashes match the prior admitted references exactly. No current combat performance is inferred from their historical labels or results.

The input is a distinct admission-only copy of the old definition. It uses the current harness identity and a valid 64-candidate /256-attempt standalone-compatible allocation. Existing generation values, stage schedules and exclusions remain unchanged. The prepared definition has exactly one `retained-composition` method and two explicit starts. Its historical schedules are an admission fixture, **not a fresh study or an executable experiment approval**.

## Failures and independent verification

The first build failed with CS0017 because the reused helper's `Main` conflicted with the test SDK's generated entry point. A [recorded mechanical amendment](../TestResults/balance/tower-retained-native-admission-20260916/build-packaging-amendment.md) retained the original inputs and compiled only the unchanged settings-writing method. The second build completed with zero warnings/errors. This changed the originally frozen one-build limit to two attempts within the same time/storage ceiling; it did not change the native test, search rules or outcome criteria.

The native test then reached the cost assertion after the CLI had successfully prepared both teams. Its expected literal assumed 1,000 confirmation values, while the original saved definition actually contains 256. That was an error in this verification fixture and protocol, not a change to the retained schedule or an undercount by the harness.

| Component | Count from unchanged input | Cost |
| --- | --- | ---: |
| Discovery | 1 method ×3 roots ×64 candidates ×8 values | 1,536 |
| Selection | 12 shortlisted teams ×64 values | 768 |
| Generated confirmation | 5 finalists ×256 values | 1,280 |
| Reference confirmation | 2 references ×256 values | 512 |
| **Definition total, unexecuted** | No diagnostics/replays | **4,096** |

The [independent artifact audit](../TestResults/balance/tower-retained-native-admission-20260916/audit.json) recomputes every component from the original schedules. It also checks all four saved artifacts, the exact generation-input boundary, reference recipes, canonical starts, contexts, content hashes, standalone executable identity and the reservation ledger. All 328 retained stage values were already reserved; no new values were derived or bound. This independently covers the saved-output assertions after the failed cost assertion, without rerunning preparation.

The original test remains **0 passed /1 failed**. Its [corrected source candidate](../TestResults/balance/tower-retained-native-admission-20260916/RetainedNativeAdmissionTests.corrected.cs.txt) changes only the expected cost tuple to `(1536, 768, 1280, 512, 0, 0, 4096)`. It has not been compiled or executed. The first build failure, original wrong assertion, failed TRX, original protocol, successful prepared output, corrected candidate and audit are preserved. The prior standalone 13-case parity result remains unchanged.

Backend verification used the required runner:

```powershell
./build/run-tests.ps1 -NoBuild `
  -ArtifactsPath TestResults/balance/tower-retained-native-admission-20260916/tests `
  -Filter 'FullyQualifiedName=EssenceSystem.Tests.RetainedNativeAdmissionTests.Standalone_cli_admits_both_current_anchors_without_combat'
```

The build commands and failed test command ran; none were blocked by the environment. Independent verification ran `close-admission.py audit`, followed by package preservation and scoped `git diff --check`. A corrected test rerun is outstanding and was outside the retained one-native-invocation scope. No broad backend suite, gameplay rebuild, candidate generation, combat or seed allocation ran. Do not call this a fully green native regression gate.

## Files, budget and handoff

New helper/test sources, frozen content, prepared artifacts, failed build/test records and audits live only in `TestResults/balance/tower-retained-native-admission-20260916`. This review, the harness README and five current handoff notices are updated. Production files and the active test project are unchanged; the failed isolated test is not added to the repository test project's compilation.

The scope is capped at **22 diagnostic seconds /24 MiB** from remaining engineering allowance, including eight seconds for static work and three for closure. The [completion receipt](../TestResults/balance/tower-retained-native-admission-20260916/completion.json) records all charges, including failures, and the remaining balances. No cap was increased or funding transferred. All **483,046 reservations**, prior sealed packages and unrelated dirty files remain preserved. The closed comparison's 891 values and up to 6,912 fights remain unused.

The next verification action is the prepared assertion correction and a separately bounded rerun; no search-method change is needed. A later practical search still requires its own current-target protocol, fresh independent evaluation schedules and applicable resource/seed authorization. Do not launch the admission-only output or treat the failed block-search allocation as available funding for that study.

There are no migrations, persistent configuration changes, gameplay-content edits or deployment implications.
