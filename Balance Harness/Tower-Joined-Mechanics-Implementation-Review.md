# Joined mechanic groups: implemented and verified without combat

15 September 2026. **ImplementedVerifiedNoCombat.** The new opt-in `independent-joined-mechanics-v1` / `joined-mechanics-joint` policy can assemble larger groups from overlapping content-derived mechanics. **32 zero-combat tests passed**. Captured content produces **214 legal joined groups**, independently verified, including the previously missing Howler–Spiderling–Royal Venom triple. This is construction reachability evidence, not a measured improvement in wins.

## What changed

`TowerJoinedMechanics.cs` joins two groups sharing an Essence when their union adds a member to each. Members, evidence and source group IDs are canonical; equivalent member sets get one sampling seat. It rejects unavailable/zero-owned members, repeated source families, and unions exceeding the character budget or five members. The catalogue considers at most 128 distinct base recipes (8,128 pairs), retains at most 256 distinct unions and records truncation. Stable source-ID order chooses a representative evidence path; stable union-ID order determines retention. There is no recursive closure or strength weighting.

The guided fresh-construction route still reserves category coverage first. On sampled owners it chooses a compatible joined group, then fills spare slots using the existing construction. It falls back to compatible small cores when no joined group fits. The one-in-eight uniform route remains. Atomic insertion checks the whole union and current ownership across all owners before changing anything. Existing mutation and whole-loadout reuse can explore the resulting compositions.

Every fresh proposal records the catalogue hash, retained count/truncation, uniform/guided route, owner sampling, selected group with source/evidence IDs, compatible count, outcome and before/after membership. Coverage reservations also record the provider ID and requested/placed counts. A small-core fallback is distinguishable from a joined insertion. When all groups fail, one representative failed joined insertion is recorded; the trace does not enumerate every rejected alternative. Filler choices remain outside this trace.

Registration in generation, scenario/provenance validation, mechanics requirements and loadout coordination makes this a complete opt-in kernel policy. Ordinal Essence order, order-operator exclusion, canonical identity, candidate/proposal limits, ranking and cancellation semantics are retained. Historical streams are unchanged. New trace fields are omitted when null, preserving historical serialization. No control recipe, named Essence or combat outcome enters the joining implementation. Names in the audit describe the predeclared diagnostic target only.

Changed source: the new joining file plus `TowerCompositionSearch.cs`, `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerPartyCoverage.cs`, `TowerLoadoutComposition.cs` and `TowerBossDiscoveryContract.cs`. New tests are `BalanceHarnessJoinedMechanicsFixture.cs` and `BalanceHarnessJoinedMechanicsTests.cs`; existing composition tests are unchanged. Active search plans and the harness README now point here. The exact [patch](../TestResults/balance/tower-joined-mechanics-implementation-20260915/implementation.patch), copied source and compiler projects are retained. Unrelated dirty checkout work was preserved.

## Measured verification

| Check | Result |
| --- | --- |
| Existing composition regression cases | 17 passed |
| New joined-mechanics cases | 15 passed |
| Backend wrapper invocation | 32 passed, 0 failed, 0 skipped; 3.953 seconds wall time |
| Prior composition-only synthetic hashes | Both exact; empty metadata and overlapping-core/coverage metadata |
| Older v1/v13/deep synthetic goldens | All three exact in existing tests |
| Captured base catalogue | 48 groups, 1,128 pairs examined |
| New joined catalogue | 214 distinct groups: 109 triples, 94 four-member, 11 five-member |
| Independent complete pair-union enumeration | Exact set equality; no truncation |
| Howler–Spiderling–Royal Venom group | Absent from old 48-group catalogue; present in joined catalogue |
| Catalogue construction, one instrumented call | 11.092 ms |
| Prior indexed evidence preserved | 1,197 files verified before and after |
| Additional diagnostic workload | 4.907 / 300 seconds |
| Cumulative pilot and follow-up diagnostics | 263.389 / 1,800 seconds |
| New output before publication | 71.31 / 512 MiB |
| Fights / combat replays / preparations / new balance seeds / retries | 0 / 0 / 0 / 0 / 0 |

The new cases exercise canonical/deduplicated joins, disjoint/subset/illegal unions, bounded deterministic truncation, atomic failure, guided/uniform routes, full synthetic generation and ownership-constrained reuse, policy/order boundaries, serialized trace roundtrip, cancellation checkpoints and charged proposal exhaustion. All use a combat-entry guard and a synthetic evaluator. They do not estimate fitness, win probability or search quality. The 256 synthetic reference evaluations (128 with each metadata fixture) likewise are not fights.

The retained [reference hashes](../TestResults/balance/tower-joined-mechanics-implementation-20260915/reference.json) were computed using the sealed previous executable before candidate tests. Candidate builds use copied harness source against the captured gameplay dependency DLLs. Builds passed with zero warnings/errors. Compilation wall times were 1.609 seconds for the reference driver, 3.765 for the candidate harness and 1.500 for tests; these are engineering time, separate from diagnostic workload.

## Reproducible commands and evidence

The [protocol](Tower-Joined-Mechanics-Implementation-Protocol.md) froze the scope before diagnostic execution. Each command below ran once; source, scripts, dependency DLLs and test binaries were hashed before use. The evidence package refuses reuse of phase receipts. To reproduce, first copy the retained scripts/source to a separate new package location and retain the pinned dependencies; do not rerun into this sealed directory.

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-joined-mechanics-implementation-20260915'
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

The `tests` phase invokes the required repository wrapper:

```powershell
./build/run-tests.ps1 -NoBuild `
  -Filter 'FullyQualifiedName~BalanceHarnessCompositionSearchTests|FullyQualifiedName~BalanceHarnessJoinedMechanicsTests' `
  -ArtifactsPath 'TestResults/balance/tower-joined-mechanics-implementation-20260915/tests'
```

Saved evidence: [TRX](../TestResults/balance/tower-joined-mechanics-implementation-20260915/tests.trx), [test log](../TestResults/balance/tower-joined-mechanics-implementation-20260915/tests.log), [catalogue and detailed timing](../TestResults/balance/tower-joined-mechanics-implementation-20260915/content-catalogue.json), [independent content check](../TestResults/balance/tower-joined-mechanics-implementation-20260915/content-verification.json), [completion](../TestResults/balance/tower-joined-mechanics-implementation-20260915/completion.json), [file seal](../TestResults/balance/tower-joined-mechanics-implementation-20260915/files.json). Scoped diff/whitespace verification is retained with publication.

## Limits and next step

No live combat study or combat archive was prepared or verified for the new policy. Native archive/cap behavior was not exercised anew; the implementation leaves those systems untouched and verifies kernel attempt/cancellation behavior. The full backend suite and dirty gameplay build were intentionally not run; this is an isolated targeted build and test run. No required command was blocked or failed.

Only one catalogue timing was taken, including first-call costs and instrumentation; it is not a performance comparison. Catalogue availability does not establish how often real guided construction inserts a particular group after coverage/ownership constraints, how adaptive search retains it, or whether it improves combat. Larger groups consume more slots. Structural overlaps remain hypotheses about timing and compatibility, not proven synergies. The fixed catalogue caps can omit groups in larger content sets, though no truncation occurred here.

Next: freeze a small search-quality comparison for the new policy, with identical gameplay inputs and fixed order, before allocating any further combat budget. The completed pilot already spent its 512 authorized fights and 85 new values; this implementation authorizes none beyond them. All **482,371 reservations** remain preserved. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold are unchanged. No gameplay/content tuning, configuration changes, migrations or deployment.
