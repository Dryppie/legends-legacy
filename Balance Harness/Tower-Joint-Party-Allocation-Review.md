# Bounded joint party allocation: implementation and verification

15 September 2026. **VerifiedStructuralPartyAllocator**. Zero fights, fresh seed values, battle preparations or replays. The bounded allocator retained **16 complete ten-character parties (16 distinct)** across the three inventory fixtures, using **680 full loadouts** from the sealed pool. Shared copy limits, character slots and explicit recipe-diversity constraints verified.

## Implementation

[`TowerJointPartyAllocator`](../LL/tools/BalanceHarness/TowerJointPartyAllocator.cs) assigns complete loadouts to numbered character slots while charging every Essence against one shared inventory for the full roster. It validates exact Essence-slot counts and same-character family uniqueness, canonicalizes IDs, and removes duplicate recipes within each slot. Slot-specific candidate pools are supported.

The allocator fills the most constrained remaining character slot first, then prefers less-used recipes and new Essence coverage with stable hash ties. Minimum distinct recipes and maximum uses per recipe are explicit hard constraints. This is deterministic structural traversal, not strength ranking. Every branch restores inventory and recipe counts on backtracking; every returned party includes all slots, used-copy totals, recipe identities and diversity counts.

State, candidate-check and retained-party limits are separate. Candidate checks are charged before assessing compatibility, bounding large-pool scans even at one state. Capped results are incomplete, and an empty capped result is unresolved. Exhaustion covers only the supplied candidate pools and constraints. The allocator never pads a partial loadout, reorders abilities as a search variable, invokes combat or accepts reference outcomes.

This is a standalone API. Existing search-policy selection and gameplay are unchanged. It connects the verified saved loadouts to complete structural rosters but does not register a new search policy or establish combat strength, targeting compatibility or useful uptime.

## Captured three-scenario check

The previous constructor retained 768 records / 740 distinct recipes. This scope used every distinct recipe with exactly five Essence IDs; **60 shorter recipes were excluded** and retained in [pool provenance](../TestResults/balance/tower-joint-party-allocation-20260915/pool-proof.json). No constructor call or combat replay was repeated. Each returned roster contains ten characters, corresponding to two five-player parties.

Frozen cases: unlimited copies with ten distinct loadouts and at most one use each; five copies per Essence with at least five distinct loadouts and at most two uses each; and one copy per Essence with ten distinct loadouts and at most one use each. These inventory assumptions are metadata fixtures, not content changes. Each call was capped at 256 states, 250,000 compatibility checks and 16 retained parties.

| Fixture | Complete parties | Minimum observed distinct recipes | Maximum observed Essence copies | States | Candidate checks | Termination | Negative-result status |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
| unrestricted-diverse | 16 | 10 | 8 | 27 | 37400 | party-limit | not-applicable |
| five-copies-diverse | 0 | — | — | 256 | 206040 | state-limit | bounded-unresolved |
| one-copy-diverse | 0 | — | — | 256 | 212840 | state-limit | bounded-unresolved |

- `five-copies-diverse`: bounded-unresolved; shared-copy bottlenecks: none identified.
- `one-copy-diverse`: bounded-unresolved; shared-copy bottlenecks: none identified.

The [independent audit](../TestResults/balance/tower-joint-party-allocation-20260915/independent-audit.json) checks the exact saved recipe pool, every recipe/party identity, canonical slot assignment, authored role coverage, family/copy/diversity constraint and counter cap. A shared-copy bottleneck, when present, proves a negative only over this supplied pool. Other native exhausted-negative results are explicitly marked as lacking independent closure. A search or result cap never establishes infeasibility. Individual outputs remain in [captured results](../TestResults/balance/tower-joint-party-allocation-20260915/captured.json).

## Tests, measurements and preservation

Exactly **10 new backend tests** passed through `build/run-tests.ps1`. The exhaustive fixture compares all assignments in **243 inventory/roster-size cases** over three synthetic recipes. Other cases test constrained slots, shared shortages, diversity/repetition, deterministic deduplication, all three caps, empty/invalid inputs, cancellation and immutability. The earlier constructor's 11 tests / 448 cases remain reused through sealed hashes; they were not rerun.

Native allocation took **0.208 seconds**, 0.203 CPU seconds, allocated **90,223,128 bytes** and peaked at **63,221,760 bytes** working set. These are standalone structural-allocation costs, not combat throughput or an end-to-end speedup.

| Phase before publication | Seconds | Accounting |
| --- | ---: | --- |
| audit | 0.172 | diagnostic |
| build | 2.359 | engineering build |
| captured | 0.360 | diagnostic |
| freeze | 4.031 | diagnostic |
| setup | 0.000 | diagnostic |
| test-build | 2.000 | engineering build |
| tests | 1.984 | diagnostic |

The chain carries **1768.944 diagnostic seconds** and **2,170,612,909 output bytes** before this scope. The [completion receipt](../TestResults/balance/tower-joint-party-allocation-20260915/control/completion.json) includes publication and one conservative closure second. New diagnostics are capped at 25 seconds / 192 MiB within the unchanged cumulative 1,800 seconds / 4 GiB. Both builds are separately measured engineering work with cached restore metadata and captured DLLs; no dirty gameplay rebuild or restore.

All **18,105 indexed files across 46 predecessor packages** were checked unchanged before/after. The 482,641 reserved values remain untouched. Existing harness source and unrelated dirty work were preserved. Failure receipts: **none**.

## Reproducible commands and remaining work

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-party-allocation-20260915'
& $python -B "$work/assemble.py"
& $python -X utf8=0 -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The tests phase runs `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter FullyQualifiedName~BalanceHarnessJointPartyTests`. Commands and logs are retained. Do not rerun sealed directories; reproduction requires a separately frozen output and budget. See the [protocol](Tower-Joint-Party-Allocation-Protocol.md), [input freeze](../TestResults/balance/tower-joint-party-allocation-20260915/freeze.json) and [file seal](../TestResults/balance/tower-joint-party-allocation-20260915/files.json).

Changed files: new standalone allocator and its ten tests, protocol/review, six active Markdown handoffs and separate diagnostic evidence. Existing constructor, policies and gameplay are unchanged. No configuration, migrations or deployment implications. The full dirty gameplay build/backend suite was not rerun. Both isolated builds and all required checks passed.

Remaining work is an explicit, bounded adapter from structural rosters into a new opt-in search policy, with proposal accounting and old-policy parity verified before use. These parties have no combat scores and must not be called strong builds. Any further diagnostic must carry forward the actual remaining time/output budget; no fresh seeds or fights are authorized. Do not silently restart the 30-minute allowance.

All 253 v19 recipes and unused 512 confirmation values remain intact. Reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved** and adoption **Hold** remain unchanged. No Kharad tuning, ability-order optimization, 129,536-fight confirmation or old-cap increase.
