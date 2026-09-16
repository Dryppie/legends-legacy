# Group/count construction implemented and verified without combat

15 September 2026. **ImplementedVerifiedNoCombat.** The new opt-in `independent-group-count-v1` / `group-count-joint` policy deliberately selects a content-derived joined group and varies its number of owners. **48 targeted tests pass**, including all previous 32 composition/joined cases and 16 new group/count cases. A separate captured-content check independently verifies **2,140 distinct group/count reservations** and **32/32 legal complete fresh requests**. No new fights, preparations, replays, balance seed values or combat outcomes. All **482,416 reservations** remain preserved; adoption stays **Hold**.

## What changed and why

The [saved diagnosis](Tower-Joined-Mechanics-Combat-Diagnosis-V2-Review.md) found sparse group repetition and no catalogue group in the joined finalist. The new route reserves a chosen group on an explicit number of characters before adding coverage providers and filling remaining slots. It operates on the full content-derived catalogue; no control recipes, outcomes, Essence names or item-specific strength weights enter the implementation. Ten owners are ten characters arranged in two five-player parties.

Each arm has a fresh-request ordinal. Every eighth fresh request takes the existing uniform composition route. Otherwise it visits catalogue groups in stable ID order, rotated by the generation seed. A sweep visits each group once; later sweeps advance its requested count. A diagonal count schedule gives varied counts during the first sweep instead of making every early request a single-owner team. With G groups and N owners, G×N guided requests cover each group at every count 1..N exactly once. Empty catalogues use uniform construction. Rejected/duplicate fresh requests still advance the schedule; every generation arm starts its own ordinal at zero.

Group placement is a transaction: targets are distinct shuffled owner slots, and family, slot and complete-party ownership constraints are checked using existing insertion rules. Any failed placement rolls back the entire reservation. Traces distinguish tentative insertions from committed placement. Existing coverage/filler then fills unused slots without another per-owner core draw. If that filling accidentally completes the group on extra owners, the proposal is rejected as `group-count-drift`; it does not receive a mislabeled combat measurement or a silent repair. Requested, placed and actual final counts are retained.

New optional traces record catalogue hash/bounds, scheduled group/index, source/evidence IDs, requested count, placements, final count and rejection. Null fields are omitted for historical policies. Fixed ordinal Essence order, existing fitness/ranking, mutations, nomination rules, proposal/candidate caps and cancellation/attempt behavior remain in use. Old policy output is verified against frozen synthetic result hashes. No storage, combat accounting or archive-verification implementation was changed.

Changed harness source: `TowerGroupCountSearch.cs` (new), `TowerCompositionSearch.cs`, `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerPartyCoverage.cs`, `TowerBossDiscoveryContract.cs` and `TowerLoadoutComposition.cs`. New tests: `BalanceHarnessGroupCountTests.cs`. This report, the [frozen protocol](Tower-Group-Count-Implementation-Protocol.md), four active Markdown handoffs and a separate evidence package are added/updated. The [exact source patch](../TestResults/balance/tower-group-count-implementation-20260915/implementation.patch) compares this implementation against the dirty checkout at task start, preserving unrelated work.

## Measured checks

| Check | Result |
| --- | --- |
| Existing composition cases | 17 passed |
| Existing joined-mechanics cases | 15 passed |
| New group/count cases | 16 passed |
| Required backend test wrapper | 48 passed, 0 failed, 0 skipped; 4.797 s |
| Previous joined full-result parity | Exact: 128 synthetic evaluations / 159 proposals |
| Existing composition and older policy goldens | Exact in retained regression tests |
| Captured catalogue | 214 groups; no truncation |
| Complete reservation cycle | 2,140 unique group/count pairs over 2,445 fresh positions |
| Uniform positions within that cycle | 305 |
| Howler–Royal Venom–Spiderling reservation counts | Every count 1–10 |
| Complete fresh requests, separate fixed prefix | 32 accepted / 32 requested; 28 scheduled + 4 uniform |
| Complete-request rejection/count drift | 0 observed in these 32 |
| Native construction diagnostic | 176.769 ms within 0.516 s command wall time |
| Independent construction recount | 0.266 s |
| Indexed historical evidence preserved before/after | 3,557 files across nine packages |
| Additional diagnostic workload | 7.923 / 300 s |
| Cumulative comparison/diagnosis/implementation workload | 261.953 / 1,800 s |
| Fights / preparations / replays / new seeds / retries | 0 / 0 / 0 / 0 / 0 |

The 2,140 checks verify **reserved prefixes**, not 2,140 filled combat teams or fights. All catalogue groups/counts are covered in that reservation fixture. Only the separately frozen first 32 requests are filled into complete parties, with all recipes and traces saved, and none is scored. The native diagnostic includes one explicit catalogue audit and the full generator's normal cached catalogue construction during the 32-request check. Its detailed phase timings are retained; this single-call timing is not a controlled speedup claim.

The new tests cover complete pair coverage, periodic uniform construction and empty catalogues, catalogue-order invariance/large indexes, exact owner counts, atomic ownership/family/slot failure, reserved-prefix preservation, deliberate count-drift rejection, complete deterministic synthetic search, per-arm reset, cancellation/checkpoint charging, proposal exhaustion, method/metadata/order boundaries and exact old joined output. Every test class and the native diagnostic install an engine-entry guard. Synthetic evaluator labels are fixtures, not new balance seed reservations.

The isolated candidate and reference builds pass with no errors. The test build emits one existing xUnit2031 style warning in unchanged `BalanceHarnessCompositionSearchTests.cs:188`; all 48 cases execute successfully. Total compilation wall time is **6.812 seconds**, separately measured engineering work. The old shared test TRX was preserved before the repository wrapper wrote its current result.

## Reproduction and evidence

The protocol, scripts, source, tests, fixture input and captured dependency DLLs were frozen before execution; binaries were frozen before tests. The new code was compiled from copied source against the joined-comparison preparation's captured gameplay dependencies. No unrelated dirty gameplay build was performed. Each diagnostic ran once and all passed; no command was blocked.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-count-implementation-20260915'
& $py -B "$w/workflow.py" bootstrap
& $py -B "$w/workflow.py" reference-build
& $py -B "$w/workflow.py" reference
& $py -B "$w/workflow.py" candidate-build
& $py -B "$w/workflow.py" tests-build
& $py -B "$w/workflow.py" tests
& $py -B "$w/workflow.py" audit
& $py -B "$w/workflow.py" preserve
& $py -B "$w/publish.py"
```

The test phase invokes:

```powershell
./build/run-tests.ps1 -NoBuild `
  -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests|FullyQualifiedName~BalanceHarnessGroupCountTests' `
  -ArtifactsPath 'TestResults/balance/tower-group-count-implementation-20260915/tests'
```

These are archived single-use commands. Reproduction requires a separate output location and the pinned inputs; do not overwrite this package or any sealed experiment. [TRX](../TestResults/balance/tower-group-count-implementation-20260915/tests.trx), [test log](../TestResults/balance/tower-group-count-implementation-20260915/tests.log), [old joined reference hash](../TestResults/balance/tower-group-count-implementation-20260915/group-reference.json), [all reservation/recipe traces and performance](../TestResults/balance/tower-group-count-implementation-20260915/content.json), [independent recount](../TestResults/balance/tower-group-count-implementation-20260915/content-verification.json), [completion/output limits](../TestResults/balance/tower-group-count-implementation-20260915/completion.json), [file seal](../TestResults/balance/tower-group-count-implementation-20260915/files.json).

## Limits and next step

This establishes deterministic construction reachability and preservation of historical behavior, **not stronger builds**. A shallow study runs only the start of the schedule; it cannot visit every group/count pair. The full 2,445-position fixture is not a recommendation to fund that many fresh combat teams. Concentrating a group across owners trades away some of the former per-owner mixture diversity. Limited inventory, filler interactions and catalogue truncation can reject or omit choices; rejected requests consume proposal budget. Subsequent mutations and team-level ranking can still remove groups, and there is no independent module fitness or guarantee of good target/timing interactions.

Next: prepare a bounded equal-input comparison of the new policy against the current joined policy, freeze selection and seed/fight accounting first, and obtain a separate fresh-seed exception if required. Existing combat approvals are exhausted. No new combat study has been launched or authorized by this implementation, and the broad catalogue sweep must not become an automatic large experiment. Continue fixed ability order, unchanged gameplay and explicit uncertainty reporting.

The full backend suite and live combat/archive execution were not rerun; targeted zero-combat checks and retained native verification provide the stated evidence only. No configuration changes, migrations, deployment, Kharad tuning, old-cap increases or sealed-v19 changes. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.
