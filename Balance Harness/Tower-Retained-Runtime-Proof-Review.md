# Captured-v19 runtime comparator — regression corrected; full proof pending

Completed **14 September 2026** under the [frozen zero-combat protocol](Tower-Retained-Runtime-Proof-Protocol.md). The new comparison evidence identified the exact same-input discrepancy: **90 equipment acquisition timestamps**. The corrected `tower-runtime-fields-v2` helper passes **59 tests**, and independent projection of the two saved states leaves **22,873 retained entries with zero differences**. The once-only native v1 proof stopped at its same-input control after **two preparations / 3.188 seconds**. No offset pair or complete confirmation binding is certified.

The preceding complete audit still retains **43,879 recipes / 47,834 origins**, all **253 midpoint recipes** and every one of the **90 distinct offset-group recipes**. All **481,603 seed reservations**, including the original unused 512, remain excluded. **Zero fights, new reservations, runtime retries or resumes** occurred. Reliability **Fail 1/3**, adoption **Hold** and all preceding scoped balance results remain unchanged.

## Implemented comparator and durable evidence

`LL/tools/BalanceHarness/TowerRuntimeComparison.cs` captures prepared combatant instance fields, including private health/barrier and mutable counters. It preserves absent versus null values and returns every differing path with both values. It records separated bookkeeping values independently; it never mutates actors or preparation.

The producing v1 comparator excluded exactly the two `PlayerEssence` timestamps and the two modifier ID fields established by the preceding captured-IL inspection. The corrected v2 additionally separates exactly `ItemInstance.AcquiredAtUtc`. All other fields remain included, including actor IDs, equipped-Essence IDs, equipment IDs, definitions, levels, ascension/evolution, modifier amounts, attributes and counters. No broad timestamp, GUID or name-pattern exclusion was introduced.

The captured diagnostic now durably writes a complete compressed comparison record and verifies its stored hash **before asserting equality**. Records contain both state maps, separated metadata, original/normalized input and plan hashes, saved participant checks and every difference. The failure handler also retains CPU, allocations, peak memory and detailed timings. This closes the evidence gap in the earlier diagnostic.

The frozen workload allowed 188 preparations and 97 comparisons, including two same-input controls. Its first control deliberately prepared the previously affected recipe twice without changing its timestamp. It saved `native/comparisons/0001.json.gz`, then stopped because equality failed. The stored record establishes:

| Component | Same-input result |
| --- | --- |
| Original and normalized input hashes | Match |
| Normalized plan and UTC context | Match |
| Participant hashes and both saved-reference checks | Match |
| Retained v1 fields | Exactly **90 differences**, all `ItemInstance.AcquiredAtUtc` |
| Already-separated bookkeeping | **100 differences**, the two timestamps on 50 equipped Essences |

The 90 acquisition paths include equipment collection entries and hand-reference aliases. They are field paths, not 90 different items or recipes. The machine-generated acquisition dates are distinct from the scenario's frozen year-2000 start instant.

The native run retained **54 completed mapping rows**, one verified comparison record and the failure. It did not prepare an offset pair or the extra midpoint cases. Its frozen executable and source remain v1; the current v2 implementation and tests are captured separately under `candidate-source`. The old failed run was neither edited nor repeated, and its partial archive has an explicit `native-failed-files.json` seal.

## Evidence-driven correction and saved regression

A separately frozen read-only inspection of the **captured** assemblies confirms that `ItemInstance` initializes `AcquiredAtUtc` from the current clock, snapshot equipment rehydration does not restore it, and the captured executor does not reference it. This supports separating that exact acquisition timestamp in a diagnostic comparison; it does not justify changing gameplay objects or dropping other fields.

The v2 helper adds that single exclusion. A synthetic regression verifies that acquisition-time changes are separated while equipment-ID changes still alter the retained-state hash. The full scoped suite passed **59/59**, including prior context and audit tests. The earlier v1 suite also passed its 58 cases; both producing logs/TRX and source versions remain available. These were correctness tests, not retries of runtime preparation.

An independently frozen saved-record check recomputed both original state hashes and both complete difference lists. It verified that all 90 retained differences were the exact declared acquisition field, then moved those 90 values and their 90 type markers into bookkeeping. Both projected v2 states retain **22,873 entries**, have **968 bookkeeping entries** and share hash:

`25e1627ca0092b720f861dcfa1d6fa1839f52290d8bfc5000070444ddaebca4a`

That check took **0.567 seconds**, with zero new preparations. It verifies the correction against the saved same-input regression. It does **not** claim that v2 has prepared the real recipes, that an offset pair passed, or that the complete family is bound.

## Measurements and producing commands

| Stage | Measurement |
| --- | ---: |
| Initial input/source/executable freeze | **0.688 seconds** |
| Once-only native command including orchestration | **3.188 seconds**, stopped |
| Native elapsed time at failure | **3.050 seconds** |
| Native preparations / comparisons | **2 / 1** |
| Native CPU | **3.250 seconds** |
| Native cumulative managed allocation | **1,071,642,320 bytes** |
| Native peak working set | **512,774,144 bytes / 489.02 MiB** |
| Independent saved-state regression | **0.567 seconds**, passed |

CPU may exceed wall time; cumulative allocation is not retained memory. Detailed nested timings remain in `native/failure.json`. This stopped check establishes no new combat-throughput improvement or balance measurement. Build/correctness-test time is separate from diagnostic workload; final resource accounting appears below.

Commands ran from the repository root:

```powershell
$proof = 'TestResults/balance/tower-retained-runtime-proof-20260914'
./build/run-tests.ps1 `
  -Filter 'FullyQualifiedName~BalanceHarnessTowerRuntimeComparisonTests|FullyQualifiedName~BalanceHarnessTowerContextTests|FullyQualifiedName~BalanceHarnessTowerRetainedAuditTests' `
  -ArtifactsPath "$proof/test-build-2"
# The earlier v1 test build used "$proof/test-build".

dotnet build "$proof/compiler/CapturedProof.csproj" -c Release -o "$proof/executable"
dotnet "$proof/executable/BalanceHarness.dll" proof "$proof/request.json" "$proof/native"
# workflow.py invoked this v1 proof exactly once and retained exit code 1.

dotnet build "$proof/inspector/Inspector.csproj" -c Release -o "$proof/inspector-bin"
dotnet "$proof/inspector-bin/Inspector.dll" "$proof/executable" "$proof/captured-acquisition-il.txt"
```

The saved regression's exact `saved_regression.py freeze` / `run` commands and input hashes are retained in the [new evidence package](../TestResults/balance/tower-retained-runtime-proof-20260914). Commands above describe producing actions, not permission to overwrite or resume once-only evidence. There are no remaining permission blocks. Both scoped test builds passed; the native proof's nonzero exit is the preserved verification stop.

## Remaining boundary

Next, freeze the v2 comparator and its exact captured executable for a new bounded proof, retaining the same-input controls and persisted differences. Complete all required offset pairs and full identity/anchor verification before binding the conditional confirmation design. The current correction already passes the saved regression, so no further field exclusions should be inferred without specific evidence and tests.

No complete UTC mapping, anchor binding, interval table, fresh schedule or executable confirmation controller was produced. The proposed 560-anchor / 2,434,784-attempt study remains conditional and unlaunched. Existing 20,000-cell/500,000-fight caps remain unchanged. No gameplay, migration, package dependency, shared configuration, catalog promotion or deployment change accompanies this work.

## Final preservation and accounting

The final **21.108-second** preservation/history pass checked **4,192 baseline checkout files**, **199 frozen input bindings**, **158 producing-file bindings** and all **six partial native files**. All bindings match, and all **114 prior reviews** remain unchanged. The frozen v1 source still matches its producing executable; the corrected v2 source/tests match their separate candidate capture. The complete preceding audit and both earlier failures remain intact.

The current seed-history registry still has **145 files / 102 distinct hashes**. Reconstructing its complete array union gives exactly **481,603 reservations**, with zero additions. The original unused 512-value v19 reservation remains excluded.

Fourteen unrelated frontend/UI-verification files changed and two appeared during this work. Their paths are recorded in `preservation.json` and were left intact. This task adds the comparator, its tests, the frozen protocol and this review; it updates seven active Markdown handoffs. `Program.cs`, the context-identity helper, original audit implementation and gameplay are unchanged.

Both isolated correctness-test builds passed: **29.875 seconds / 58 tests** for v1 and **12.296 seconds / 59 tests** for v2. They reported 34 existing warnings outside the new files. The captured producing build took **2.081 seconds**, and the read-only inspector build **0.878 seconds**, both with zero warnings/errors. The inspector executed in **0.056 seconds** without preparing objects or fighting.

The complete diagnostic charge is **under 206 seconds / 3.44 minutes**, including measured phases and preservation, **60 seconds** conservatively charged for short untimed static reads/metadata, and the full **120-second final-seal allowance**. Those allowances are accounting charges, not measured durations. Output is conservatively charged at **under 725 MiB**, including both isolated test builds, producing/candidate captures, whole changed files, shared TRX and 2 MiB for final metadata. Both charges remain inside the **1,800-second / 4-GiB** envelope.

`final-verification.json` records exact resource charges, test receipts, successful saved-regression results and the incomplete runtime-proof boundary. Scoped `git diff --check` and new Markdown links are checked before sealing. `updated-files.json` binds the final implementation and handoffs; `evidence-files.json` seals the full package, including the failure, durable comparison and separate v2 candidate. `seal-timing.json` records actual sealing time. No stopped runtime workload was retried or resumed.
