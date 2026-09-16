# Refinement comparison: storage profile review

16 September 2026. **VerifiedRefinementStorageProfile**. All 32 backend facts and the complete synthetic binding/launch/reconstruction passed. Independent verification confirms one shared executable bundle, four bound stage references, all historical values and unchanged schedules, nominations, quality and 240 synthetic attempt pairs.

The counted component floor fell from **165,458,836 bytes (157.79 MiB)** to **76,555,348 bytes (73.01 MiB)**. This includes fourteen complete historical arrays, one current executable bundle and four content-data copies. A representative full-history JSON payload fell from **6,893,377** to **3,996,440 bytes**, with exact decoded values and canonical hash parity. Headroom before publication was 118,900,307 bytes; the component floor fits: **True**. Reports, other metadata and temporary writes are excluded; this is not a complete run-size guarantee.

Native fixture and history serialization: 2.9379 seconds, 1.7969 CPU seconds, 305,657,368 allocated bytes and 232,800,256 peak working-set bytes. The runtime uses fabricated observations and a three-byte executable fixture, not the real combat adapter.

## Implementation and verification

Added opt-in `shared-executable-compact-json-v1` to the refinement request and bound it to authorization and archived execution. Default requests/options omit the new fields and retain legacy behavior. Compact JSON is async-context-local; all seed lists remain ordinary complete arrays. Canonical hashes and semantic values do not depend on indentation.

The comparison owns one bounded executable bundle. Schema-2 stages reference the fixed sibling path and manifest hash. Each verification checks exact membership, file hashes and execution identity, rejecting hidden extras, missing/changed files, path escapes and reparse points. The complete outer inventory owns the bundle once; the full comparison is relocatable. A stage depends on its shared sibling bundle. Copy limits are checked before writes; cancellation and failure retain evidence and forbid retry. Durable attempt charging, Pending history, selection and combat code remain unchanged.

Changed implementation: `HarnessJson.cs`, `TowerSharedExecutable.cs`, `TowerBossStudyArchive.cs`, `TowerBulkCampaign.cs`, `TowerRefinementComparisonLaunch.cs` and `TowerRefinementComparisonRun.cs`. Updated two existing synthetic fixtures, routed controller-test output through the counted temporary directory, and added `BalanceHarnessSharedExecutableTests.cs`. Added this frozen diagnostic package and updated six active Markdown handoffs. Builds share one isolated output directory; captured gameplay DLL hashes match exactly when the independent audit completes.

Backend result: **32/32 facts passed** through `build/run-tests.ps1 -NoBuild`: ten launcher, twelve controller/model and ten new profile/shared-bundle facts. Synthetic legacy/profile parity checks schedules, nominees, finalists, family, quality and attempt bytes. New tests cover JSON context isolation, unknown profiles, bounded copying/cancellation/no overwrite, cap/no retry, tampering/membership/identity, reference escape, relocation, returned-label preservation and profile downgrade rejection. Existing traversal reparse tests remain prior evidence; no new junction test or full backend suite ran. The independent ZIP audit additionally matches the preceding sealed launcher's saved recipes/quality.

| Completed phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.313 |
| build | 3.594 |
| captured | 3.141 |
| freeze | 0.750 |
| test-build | 1.984 |
| tests | 19.422 |

Uncompleted phases: **none**. Failure detail:

```
None.
```

The [protocol](Tower-Refinement-Storage-Profile-Protocol.md) caps this scope at 60 seconds / 48 MiB, including retained temporary fixtures and shared-test allowance, under unchanged cumulative 3,240-second / 4 GiB limits. The [completion receipt](../TestResults/balance/tower-refinement-storage-profile-20260916/completion.json) carries exact added/remaining time and output. New tests/builds/audits are charged; no cap increase or budget reset. No production allocation, combat, runtime preparation, real registry traversal, replay or retry.

Prepare the exact real comparison request and a complete output/time allowance using the opt-in profile. Actual compact-runtime integration and a full-run output upper bound remain unmeasured. Production allocation and combat still require specific authorization. The preceding minimum-size failure remains sealed. No search-strength, runtime-throughput or balance-adoption claim follows from these storage measurements.

Executed commands, each at most once; missing receipts identify unrun phases. Never rerun this path:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-storage-profile-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Preserve all **482,821 reservations**, v19's unused 512 values and all 253 v19 recipes. V19 remains Unresolved; later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain. Fixed ability order and gameplay are unchanged. No configuration, migration or deployment changes. Historical archives and unrelated dirty files remain intact.
