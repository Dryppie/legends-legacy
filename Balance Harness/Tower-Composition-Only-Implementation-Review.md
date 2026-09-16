# Composition-only search implemented and verified

15 September 2026. **All three requested changes are complete:** ability order is fixed, equivalent permutations share a build identity, and legality/determinism/duplicate handling pass focused zero-combat verification. Target: offline BalanceHarness. **17/17 tests passed; zero fights, preparation calls or newly allocated balance seeds.** No build-strength or combat-speed improvement is claimed.

## Implemented behavior

The new explicit policy is **`independent-composition-only-v1`**, with the single method **`composition-only-joint`**. It reuses the established loadout-composition construction, coverage, parent selection and module-library mechanisms. Every proposed party sorts Essence IDs **ordinally within each character** before identity hashing, duplicate detection, measurement, module retention and nomination. The fixed ordering convention is not selected using the order diagnostic or a ranking of ability strength.

Both sources of order-only mutation are disabled: the generation operator list omits `order`, and loadout refinement chooses only one- or two-Essence membership replacements. Direct order mutation, noncanonical parent/module inputs and noncanonical generated scenarios are rejected. Owned-copy, eligible-pool and source-family checks remain active; canonicalization does not remove duplicates or repair an illegal loadout.

Character placement stays meaningful: swapping two different character loadouts or changing an ingredient still produces a distinct build. All 576 permutations of two four-Essence loadouts collapse to one build identity. Modules from canonical measured parties therefore cannot use separate library slots for equivalent permutations. A duplicate still consumes a proposal attempt and is recorded as `duplicate`; it does not consume another candidate evaluation. Deduplication remains per independent arm, with the existing cross-arm shortlist deduplication. Replicating the same composition in independently restarted arms is not a new build identity.

Historical policy semantics, streams and limits are unchanged. The new policy uses its own deterministic stream and the existing generic limits (at most 1,000 candidates and 10,000 attempts per arm); it does not inherit or increase the closed deep experiment's exceptions. Existing defaults and sealed study definitions remain historical. Future composition-only definitions must explicitly select these generation fields:

```json
"policyVersion": "independent-composition-only-v1",
"methods": ["composition-only-joint"]
```

These are fields inside an otherwise valid, separately frozen discovery definition, not a runnable study or a seed-allocation request. Existing `tower-boss-discover` definition validation and execution consume the new policy. No new CLI, permissive cap override or automatic promotion is introduced.

## Measured verification

| Check | Result |
| --- | --- |
| Pre-edit harness compilation | Passed, 3.547 seconds |
| Frozen pre-edit synthetic reference generation | Passed, 2.719 seconds; 1,792 evaluations across three policies |
| Current harness compilation | Passed, 2.719 seconds |
| Focused test assembly compilation | Passed, 1.734 seconds |
| `build/run-tests.ps1` focused suite | **17/17 passed**, 4.735 seconds wrapper elapsed |
| Combined reference/test diagnostic command time | **7.454 / 1,800 seconds** |
| New output before report publication | **76.38 MiB / 1 GiB** |

Tests cover all **15 combinations of six Essences taken four at a time**, each evaluated once. Requesting 16 candidates exhausts all 1,024 allowed proposals and reports Incomplete without inventing a sixteenth composition. A four-Essence pool similarly produces one candidate, not 24 permutation candidates. The complete 128-candidate fixture repeats exactly, covers every new-policy operator, validates module ancestry and canonical shortlisting. Further cases cover 576 permutation equivalences, distinct placements/membership, ordinal comparison, preserved illegal duplicates, family and owned-copy restrictions, both refinement replacement branches, required mechanics, definition/scenario/provenance boundaries and cancellation before the first or fortieth synthetic measurement.

Historical output hashes were produced by a separate compilation of the pre-edit checkout snapshot, then asserted by the candidate tests:

| Historical policy | Evaluations | Proposals | Complete result SHA-256 |
| --- | ---: | ---: | --- |
| independent-teams-v1 | 128 | 136 | `95c8f96387dd82450e9d7a8f267630fa583bead714a1c0715c9bde4d014ae777` |
| independent-loadout-composition-v13 | 128 | 165 | `8f03c21739624236c43c56847dda6698f5f7b36126d17fb0995e5bcbdb643a3b` |
| independent-deep-challenger-v1 | 1536 | 2158 | `bcaf02e23ec3bf6b363114284362117c349af6fa022376e043953fa0b1dd85c0` |

The fixture uses explicit integer labels and a synthetic hash-based score, never a balance allocator or combat engine. A combat-entry guard encloses execution. No real gameplay acceptance can be inferred from these results. All producing inputs and executable hashes were pinned before their respective checks. There were no failed build/test/diagnostic attempts in this scope.

Both harness builds compile all snapshotted/current harness source against the same captured gameplay dependencies. Already restored SDK metadata and `--no-restore` avoid the previously documented sandbox NuGet-profile access issue. Backend tests ran through the required repository wrapper with the new focused assembly. The full backend suite and a rebuild of the dirty gameplay projects were not run; no gameplay code changed. This verifies the harness changes at the captured dependency boundary, not unrelated checkout work.

## Reproduction and preservation

Recorded once-only commands, from the repository root:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-composition-only-implementation-20260915'
& $py -B "$w/workflow.py" reference-build
& $py -B "$w/workflow.py" reference-run
& $py -B "$w/workflow.py" candidate-build
& $py -B "$w/workflow.py" test-build
& $py -B "$w/workflow.py" tests
& $py -B "$w/publish.py"
```

The test phase invokes `build/run-tests.ps1 -NoBuild -Filter FullyQualifiedName~BalanceHarnessCompositionSearchTests -ArtifactsPath "$w/tests"`. Do not rerun the sealed directory. Reproduction requires a new workspace containing the pinned `before-source`, fixture and scripts; reference hashes must come from that snapshot, not the modified source. Source-only fixture integers do not reserve or transfer balance seeds.

[Test results](../TestResults/balance/tower-composition-only-implementation-20260915/control/tests.trx), [verification receipt](../TestResults/balance/tower-composition-only-implementation-20260915/control/verification.json), [incremental source diff](../TestResults/balance/tower-composition-only-implementation-20260915/implementation.patch), [protocol](../TestResults/balance/tower-composition-only-implementation-20260915/protocol.md), [completion](../TestResults/balance/tower-composition-only-implementation-20260915/control/completion.json), [sealed files](../TestResults/balance/tower-composition-only-implementation-20260915/files.json). Source changes are the new `TowerCompositionSearch.cs`, the existing generation/party/loadout/coverage/discovery-contract integrations, and two focused fixture/test files. Seven active handoffs link this review.

All **386 indexed files** in the prior composition diagnostic's preparation/execution/study packages match their seals. Its ledger preserves **482,286 reservations**, including the previously reserved unused values. Unrelated dirty changes were recorded and left intact. No migrations, application configuration changes, gameplay tuning or deployment occurred.

The implementation is ready for a separately frozen, small search-quality comparison. None was run or authorized here. Keep ability order fixed, compare legal composition/placement exploration, retain all outcomes and choose inputs/budgets before measuring. Whether this finds stronger teams or improves time to discovery remains unmeasured. Historical portfolio reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved** and adoption **Hold** remain unchanged.
