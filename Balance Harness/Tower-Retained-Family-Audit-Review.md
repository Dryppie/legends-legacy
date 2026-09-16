# Captured-v19 retained-family audit — complete and independently verified

Completed **14 September 2026**: the offline `LL/tools/BalanceHarness` materialized **all 43,879 retained entries**, with **zero invalid entries and zero native aliases**, against the sealed captured-v19 +10% inputs. All **253 midpoint recipes** reproduce their saved prepared participant hashes. Both the native audit and independent full-row verifier passed. Result: **MaterializedCompleteFamily**, with two serialized context keys. The subsequent confirmation binding check **stopped on missing context-anchor coverage**; follow-up evidence identifies different offset representations of the same instant. Materialization/provenance verification is complete; context equivalence for a runnable confirmation remains unresolved.

The scope is the [frozen zero-combat audit](Tower-Retained-Family-Audit-Protocol.md) and [subsequent independent confirmation design](Tower-Retained-Family-Confirmation-Design.md). No fight, fresh balance seed reservation, retry, replay, gameplay application, catalog promotion or deployment occurred. The current ledger remains **481,603 reservations**, including all **512 unused original v19 confirmation values**. Reliability **Fail 1/3**, adoption **Hold** and the previous confirmation **Fail / Fail** remain historical.

## Inventory, provenance and contexts

The source-union verification reproduced the original ten-source inventory **exactly**, preserving all **43,879 ordered entries and 47,834 origin records**. It checked every source hash and its sealed manifest binding, including the stopped allocation archive's external binding. No source row was selected or removed based on outcomes. The 253 midpoint recipes were already represented; no new entry was needed to retain them.

| Native context hash | Entries |
| --- | ---: |
| `a0563ef3a5ed3c8d0c7cb7d57f5a71e17f132f38d6aaa38dd87e72a575c6eead` | **43,789** |
| `d92cb15541190400b28e45a018661b2b5a28ba12181ea11be9e31d9c062cf402` | **90** |

The native key preserves scenario identity, floor, serialized starting time, preparation and equipment budget as context, plus complete character/Essence identity in its recipe hash. All descriptive entries mapped to distinct native cells, so none were merged. The two groups differ only in their timestamp offset representation: **00:00 +00:00** versus **01:00 +01:00**, both on 1 January 2000. Their actual instant, scenario ID, floor, preparation and equipment budget match. Do not infer two distinct gameplay conditions from these literal hash keys. A verified equivalence policy is required before defining confirmation cohorts; the audit does not establish win rates, practical ownership or global search coverage.

The historical audit checked **145 registered files / 102 distinct hashes**. Their complete array union is **481,603 values**, with **zero additions** outside the current midpoint ledger. Every registered path/hash remained bound. Source scenarios are seed-free; the materializer temporarily reused the midpoint's first already-reserved value without executing combat or allocating a schedule. The original unused 512-value v19 reservation remains excluded.

## Implementation and verification decisions

`TowerRetainedFamilyAudit.cs` adds strict scenario parsing, native recipe/context identities and a bounded streaming materialization loop. Unknown fields, changed floor/time/preparation, nonempty schedules and incompatible budgets are rejected. Production preparation checks real Essence/equipment legality and duplicate monster families. Invalid entries remain as explicit rows and block complete-family status. Exact aliases must have identical prepared participants. The same loop powers synthetic tests and the captured native run.

`Program.cs` adds `tower-retained-audit <frozen-request.json> <new-output>`. The command exposes no combat, allocator or resume path and refuses an existing output directory. The frozen request binds the input inventory, captured midpoint and gameplay hashes. A guard rejects combat entry; cancellation, a 1,500-second native deadline and 1-GiB native output cap cover preparation and final verification. Storage checks examine the active compressed output; there is no growing-tree scan per entry. The final native inventory is independently hash-verified.

The producing build copied the preceding captured harness source and added only the audit class, referencing the four sealed gameplay DLLs. No live game assembly or content was changed. Full participant descriptions and input/participant hashes are saved in `native/rows.jsonl.gz`; the original compressed scenario/origin inventory is retained alongside them.

`BalanceHarnessTowerRetainedAuditTests.cs` verifies identity/order equivalence, context preservation, strict unknown-field handling, incompatible budgets, invalid-entry retention, alias conflicts, required-recipe coverage, incomplete/extra/duplicate sources, combat guards, cancellation and resource stops. **All 20 tests pass**, with zero combat. The first build was denied access to NuGet.Config by the sandbox; the approved retry compiled but exposed six faulty fixture tests. Their five-character floor-1 fixture was corrected to a synthetic ten-character floor-5 fixture, after which the same scoped command passed. Every failed log and TRX is retained. The final incremental build reported six existing warnings in other tests; the captured producing build had zero warnings/errors.

Before freezing the primary pass, a Python canonical compatibility check found that .NET hashes the timestamp's plus sign as `\u002B`. The initial one-pair check matched participants but failed input hashing. That failed receipt was retained, the lexical encoder was corrected, and the pair then matched. A separate one-pair recipe/equipment check also matched. The frozen primary pass subsequently verified **all 253 saved input hashes and 253 participant hashes** before materializing the broader family. These were zero-combat engineering checks, not balance trials or retries of the primary audit.

## Recorded measurements

| Stage | Seconds | Result |
| --- | ---: | --- |
| Exact ten-source union reconstruction and source checks | **56.297** | 43,879 entries / 47,834 origins |
| Complete registered-history audit | **4.422** | 481,603 reservations |
| Independent saved midpoint canonical compatibility | **10.031** | 253 input + 253 participant matches |
| Complete primary preparation | **71.656** | Zero fights/seeds |
| Whole native command | **362.765 / 6.05 minutes** | All 43,879 materialized |
| Independent all-row, identity, participant and inventory verification | **628.328 / 10.47 minutes** | Verified |
| Complete primary diagnostic workflow | **1,063.359 / 17.72 minutes** | Inside the 30-minute limit |
| Native streaming materialization loop | **360.508** | Zero invalid entries or aliases |
| Native performance snapshot before final metadata | **362.246** | Detailed trace persisted |
| Native CPU | **323.109** | Process measurement |
| Native peak working set | **431.02 MiB** | Process measurement |
| Native cumulative managed allocation | **550,337,497,592 bytes** | Cumulative allocation, not retained RAM |
| Compressed participant-row archive | **407,633,123 bytes** | Complete per-entry descriptions |
| Entire audit package at independent verification | **818,202,085 bytes / 780.30 MiB** | Includes builds, captures and retained history metadata |

The independent verifier recomputed every native recipe/equipment/context key from its retained scenario, every prepared participant digest, all alias/context totals, all 253 midpoint matches and the exact native inventory/hash seal. It rechecked producing files and all original input/history paths and hashes. Both contexts and every origin remain present. The Python verification is the largest measured audit phase; it was neither sampled nor rerun to obtain a different timing.

Final output and diagnostic accounting, including the attempted confirmation binding and engineering closure, are recorded below. The audit uses a separate **1,800-second / 4-GiB** envelope; build/correctness-test time is recorded separately. No old study limit was increased. This is preparation/verification performance, not a paired combat speedup measurement. The original accounting and parity results retain their prior scope and limitations.

## Reproducible commands and bindings

Commands ran from the repository root. The primary workflow is once-only provenance; do not repeat it on this completed or started package. Any later diagnostic repetition needs separate scope and accounting.

```powershell
./build/run-tests.ps1 `
  -Filter 'FullyQualifiedName~BalanceHarnessTowerRetainedAuditTests' `
  -ArtifactsPath TestResults/balance/tower-retained-family-audit-20260914/test-build

dotnet build TestResults/balance/tower-retained-family-audit-20260914/compiler/CapturedAudit.csproj `
  -c Release -o TestResults/balance/tower-retained-family-audit-20260914/executable

$audit = 'TestResults/balance/tower-retained-family-audit-20260914'
dotnet "$audit/executable/BalanceHarness.dll" tower-retained-audit "$audit/request.json" "$audit/native"
# The frozen workflow.py invoked this native command exactly once,
# surrounded by source/history preparation and independent verification.
```

| Binding | SHA-256 |
| --- | --- |
| Frozen primary protocol | `91e2ee87e568c280f0df9cbff6030d69a67d1ab16b2465d41711ab6a6553ad88` |
| Producing audit BalanceHarness.dll | `0587a1f3ea797878fd70f6d6e3d3894d017082f222105f2934dcfbbcf2e12f17` |
| Retained scenario/origin inventory | `8173a6535d12dacdfd6e71eda939520dfeabb7e4399eae7db7d98837feb21fcf` |
| Source midpoint final inventory | `8a23325bea8fe71f62be5f313b6e49cf29814c276572122e1970c2412d0a2300` |
| Completed native files.json | `1af34e1e4b3787c390537fdf1e4fc9886323aa73fe5120f73ab4abaeda34e81f` |

The complete executable/source hashes, exact commands, old source/manifest bindings, registered histories and limits are retained under the [audit package](../TestResults/balance/tower-retained-family-audit-20260914). The current authoritative seed ledger remains the sealed midpoint ledger. No migration, package dependency, shared configuration, live gameplay or deployment change accompanies this work.

## Confirmation design guard and remaining work

The fixed anchor union resolved **327 historical anchors + all 253 midpoint recipes − 20 overlaps = 560 anchors**. Every one belongs to the larger serialized context key. The guard requiring an anchor in each context therefore stopped `design.py` with exit **1**, before writing a bound confirmation definition or the planned interval tables. This is a preserved failed design check, not an invalid materialization result. The design was **not retried**.

The [failure diagnosis](../TestResults/balance/tower-retained-family-audit-20260914/design-failure-analysis.json) retained the exact anchor counts and representative contexts in **4.766 seconds**. The failed design command did not retain an instrumented wall timer; its file timestamps span **23.618 seconds**, explicitly a timestamp observation. Its initial unexecuted `nextDesignOption` suggested forcing all 90 entries into stage two. That preliminary option predates the subsequent offset review; the current recommendation is the explicit context-equivalence check below.

A separately frozen saved-record inspection read the already verified native rows once in **16.172 seconds**, without re-materialization, combat, allocation or binding. It checked all 90 offset-group recipes against the larger group's native recipe hashes: **zero duplicates and zero matching prepared pairs**. All 90 remain necessary unique recipes. The [offset evidence](../TestResults/balance/tower-retained-family-audit-20260914/offset-review.json) retains every unmatched key. Their timestamp strings represent the same UTC instant, but that fact alone does not replace a captured-runtime compatibility check or a new binding contract.

The [confirmation design](Tower-Retained-Family-Confirmation-Design.md) now marks its blocker explicitly. Next, verify UTC-instant equivalence in the captured runtime and introduce that rule in a separately versioned context/binding path, preserving original scenarios and all recipes. Test equivalent offsets and truly different instants, then freeze the revised binding. If established, the complete family remains **43,879**, the anchor union **560**, the first stage **43,319 × 32 = 1,386,208 fights**, and the maximum second stage **4,096 × 256 = 1,048,576 fights**: **2,434,784 maximum attempts**. The proposed 24-hour / 64-GiB envelope and 288 fresh values are design quantities only. **Zero values have been reserved and no confirmation has started.** Existing 20,000-cell/500,000-fight caps remain unchanged.

No interval table or executable confirmation package is claimed. Reliability **Fail 1/3**, adoption **Hold**, current-gameplay application and the earlier sealed results remain unchanged.

## Engineering closeout and preservation

The final preservation pass checked **4,150 baseline checkout files**, **172 frozen input bindings**, **127 captured source bindings**, **27 producing files** and all **five native inventory members** in **1.274 seconds**. Every input/source/executable/native binding matched; all **112 preceding reviews** and the frozen audit protocol remain unchanged. The new audit class still matches its producing source copy. The original v19 seal, the completed midpoint seal and the authoritative 481,603-value ledger retain their frozen hashes.

The checkout comparison also identified **17 modified and 22 added unrelated frontend/UI-verification paths** since the initial snapshot. These are outside this task's edits and were left intact. The original `preservation.json` retains its `DifferencesRequireReview` status and exact paths; `concurrent-work-review.json` records the scoped classification without rewriting that receipt or rerunning the preservation pass. No claim is made that the entire concurrent checkout stayed byte-identical.

This task adds the audit class and its test file, the frozen audit protocol, this review and the conditional confirmation design. It changes `Program.cs` only to add the audit dispatch, and updates six active handoffs: both automatic-discovery documents, acceptance policy, coverage/replication plan, search-strategy reset and the harness README. Previous midpoint source/tests and unrelated frontend work are retained. There are **five added and seven modified task-owned files**, with no migrations, dependency/shared-configuration changes, live gameplay edits or deployment implications.

The retained test receipts record **1.349 seconds** for the initial NuGet.Config sandbox denial, **30.337 seconds** for the fixture-failing test run, and **5.608 seconds** for the final **20/20 passing** run. The captured producing build took **1.649 seconds**. No command remains blocked by sandbox access. The scientific design guard failure remains unresolved, with its evidence preserved; it was not a test retry or a combat run.

Diagnostic accounting charges **1,385.727 seconds / 23.10 minutes** against the 1,800-second envelope. This includes measured primary work, canonical engineering checks, saved-record diagnosis and preservation, plus **60 seconds each** for the uninstrumented initial freeze/input hashing and failed design command, and the full **180-second final-seal allowance**. These allowances are conservative bookkeeping, not measured durations; the failed design's 23.618-second timestamp span is the only retained timing observation for that command. Compilation, correctness tests and engineering editing/review are reported separately. The final receipt also conservatively counts the whole changed files, shared TRX and **2 MiB** of final metadata; the total is **under 784 MiB**, inside 4 GiB.

`final-verification.json` records scoped whitespace/link checks, exact output accounting, commands, test receipts, changed-file hashes and the unresolved confirmation boundary. `evidence-files.json` seals the complete new engineering package, including failures and the initial design. `seal-timing.json` records the actual final sealing time separately from its conservative charge. The completed package is once-only evidence and must not be resumed or overwritten.
