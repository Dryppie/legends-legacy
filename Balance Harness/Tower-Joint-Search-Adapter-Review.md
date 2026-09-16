# Joint structural search adapter: implementation and verification

15 September 2026. **VerifiedJointStructuralAdapter**. The opt-in policy submitted all **16 saved structural rosters**, with **12 tests passing** and exact full-result parity for **seven prior policies**. Zero fights, preparations, replays, fresh seed values and retries. These are structural and accounting checks; no new combat-strength result is claimed.

## Implemented behavior

[`TowerJointStructuralSearch`](../LL/tools/BalanceHarness/TowerJointStructuralSearch.cs) registers `independent-joint-structural-v1` / `joint-structural` in the ordinary discovery generation kernel. Selecting this policy explicitly derives its pool from authored mechanic cores and all five coverage categories, then converts complete allocator rosters into ordinary `PartyChoice` values. It accepts no historical outcomes or supplied winning teams. Fixed ordinal Essence order is retained.

The finite policy supports one generation label, at most 16 candidate evaluations / 16 proposal attempts, ten characters, five slots, 128 providers and 64 cores. Each core receives 256 states / 16 recipes. Full recipes are deduplicated; shorter recipes are excluded without filler. Allocation uses 256 states, 250,000 compatibility checks and 16 retained parties, enforcing one distinct recipe per character and the existing shared inventory. These limits define an experimental policy, not a broader search guarantee.

Construction is cached once per generator. Every unavailable proposal consumes a normal attempt and receives a reason; accepted parties use the existing checkpoint-before-evaluation path. Evaluator cancellation/failure retains the charged proposal and trace. Existing campaign caps, durable fight charging, archive verification and storage accounting are unchanged. New optional trace fields are omitted for old policies.

## Verification and measurements

Exactly 12 new zero-combat Facts cover registration/bounds, complete deterministic generation, empty and exhausted pools, one-time submission, shared copies, cancellation before generation and during evaluation, evaluator failure, immutability, metadata permutation, optional serialization, checkpoints and provenance. Tests run through `build/run-tests.ps1` with an engine-entry failure guard and synthetic measurements. See the [TRX](../TestResults/balance/tower-joint-search-adapter-20260915/control/tests.trx) and [protocol](Tower-Joint-Search-Adapter-Protocol.md).

The before/after harnesses use identical captured gameplay DLLs. Each old-policy fixture uses 32 synthetic evaluations and at most 512 proposal attempts: composition, joined, group-count, variation, diversity, completion and allocation. Full result equality covers proposals, recipe order/identities, traces, synthetic measurements and shortlist nominations. It is bounded parity for these seven fixtures, not exhaustive parity for every historical policy or combat stream.

The captured check regenerates the pool from 48 authored cores and checks it against the earlier sealed 680 full recipes. The independent audit compares all 16 roster builds and structural identities with the preceding unrestricted allocator output and validates every loadout's family/role/diversity constraints, all proposal counters and construction caps. Only ordinary independent discovery inputs/mechanics enter generation. Synthetic losses and 100% remaining boss health are fixture values, not combat observations.

Candidate native checks (seven synthetic parity fixtures plus the captured adapter) took **0.340 seconds**, 0.328 CPU seconds, allocated 107,645,784 bytes and peaked at 77,074,432 bytes working set. The single constructor/allocator pass took **0.056 seconds**. Nested trace timings must not be added together. These measurements do not establish a combat throughput improvement. The original filesystem performance work remains separately completed: 622.54× incremental accounting and 4.50× sixteen-write lifecycle improvements, with whole-run throughput and the broader 5× lifecycle target unestablished; see the [performance review](Tower-Discovery-Performance-Review.md).

| Phase before publication | Seconds | Accounting |
| --- | ---: | --- |
| audit | 0.218 | diagnostic |
| build | 2.828 | engineering build |
| captured | 0.515 | diagnostic |
| freeze | 4.906 | diagnostic |
| reference-build | 3.625 | engineering build |
| reference | 0.453 | diagnostic |
| setup | 0.203 | diagnostic |
| test-build | 1.750 | engineering build |
| tests | 1.891 | diagnostic |

Failure receipts: None.

The prior chain consumed **1780.335 diagnostic seconds**. The [completion receipt](../TestResults/balance/tower-joint-search-adapter-20260915/control/completion.json) carries every new measured diagnostic phase, publication and one conservative closure second against the unchanged 1,800-second allowance. Reference/candidate/test builds are measured separately as engineering, without gameplay rebuild or restore. New output is capped at 192 MiB within cumulative 4 GiB. All **18,363 indexed files across 47 predecessor packages** were verified unchanged before/after.

## Reproduction and next decision

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-search-adapter-20260915'
& $python -B "$work/setup.py" # Capture source before applying the adapter changes.
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" reference-build
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" reference
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The tests command is `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter FullyQualifiedName~BalanceHarnessJointStructuralSearchTests`. Saved command receipts specify exact isolated projects and binaries. Do not rerun this sealed directory: reproduction requires a new frozen output and budget. The full dirty gameplay build/backend suite and combat diagnostics were not run. All three isolated builds and the prescribed checks passed.

Changed source: the new adapter, optional trace fields and dispatch in `TowerBossGeneration` / `TowerBossPartyGenerator`, explicit policy/provenance validation in `TowerBossDiscoveryContract`, and composition/coverage recognition. Added the test class, this protocol/review, six active Markdown handoffs and separate evidence. No configuration changes, migrations, gameplay changes or deployment implications.

The integration step is complete. The next substantive question is whether these structurally complete teams perform better in combat. A comparative pilot would need a concrete budget/protocol and a specific fresh-seed exception; none is authorized by this zero-combat scope. The current diagnostic allowance must not be silently reset. Further small structural layers are not evidence of combat progress. Diversity and all-five-role constraints can exclude useful repeated or specialized loadouts; bounded construction can miss legal solutions. Authored roles do not prove targeting compatibility, uptime or strength. The policy stays opt-in and is not registered as a benchmark comparison design.

Preserve **482,641 reservations**, all **253 v19 recipes** and **512 unused confirmation values**. Reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved**, adoption **Hold**. No Kharad tuning, ability-order optimization, 129,536-fight confirmation, deployment or old-cap increase.
