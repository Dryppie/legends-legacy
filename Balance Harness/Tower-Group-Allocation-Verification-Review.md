# Category-allocation policy verified without combat

15 September 2026. **Both isolated builds, all 110 backend tests and the independent captured-data audit passed.** The opt-in `independent-group-allocation-v1` / `group-allocation-joint` policy now has engineering verification. It offers different ways to fill scarce remaining Essence slots while keeping ability execution order fixed. Combat strength and adoption remain unresolved.

This completes the work stopped by the [previous setup error](Tower-Group-Allocation-Implementation-Review.md). That failed package is unchanged and remains charged. The corrected preflight counts **85 Fact methods plus 25 InlineData cases**, matching all 110 cases reported by the test runner. Before freezing this verification, one unrun test assertion was also corrected: it now checks whether the `allocation` JSON property exists, rather than matching that word inside the unrelated route value. Production source is unchanged from the implementation package.

## What the policy now does

For each reserved group, the policy finds missing authored categories with a compatible provider. If there are more missing categories than free slots, each category gets a bounded first-slot alternative. Otherwise there is one allocation. Siblings share group ownership, placement and filler seeds; each consumes an ordinary proposal, under the existing attempt and evaluation caps. Every eighth fresh request remains uniform. The schedule retains the metadata-derived group order and advances the existing count/filler sweep after each complete allocation sweep.

The selected category runs first, followed by the original remaining completion categories. The existing reach-based provider selection, seeded ties, slot/family/copy limits, reservation preservation and group-count-drift rejection remain in force. Optional allocation metadata records variant index/count, eligible categories and priority; completion records exact before/after insertions. Old policies omit the new property. No Essence names, control recipes or combat results guide the policy. Changing which Essence occupies a slot does not introduce ability-order search.

The public input remains explicit: set `generation.policyVersion` to `independent-group-allocation-v1` and `generation.methods` to `["group-allocation-joint"]` in a separately frozen discovery definition. This review does not authorize executing a new balance study or change any default.

## Captured construction results

Using the same captured 80-Essence inputs and existing diagnostic label 17, the command made exactly **95 zero-combat construction requests**: 19 each for count, variation, diversity, completion and allocation. All **76 old-policy choices matched exactly**. The new policy produced **19 legal requests, 15 distinct recipes**, including two uniform requests. It recorded 145 completion insertions through 29 provider-choice steps on the 17 guided requests.

The audit independently reconstructed the full metadata schedule: **564 guided allocation positions across all 214 groups**. Of those groups, **181 receive alternatives**; the remainder receive a single allocation. These are schedule positions, not extra constructed teams or fights. The first actual 19 requests visited **8 groups**, compared with **17** for the old completion policy. Allocation spends more of the same short prefix on category alternatives and less on group breadth.

A concrete captured example uses Cave Bat / Pack Howler / Venomous Spiderling / Wind Harpy, reserving four of five slots on five characters. Three sibling requests fill the same last slot differently:

| Saved fresh index, zero-based | First category | Fifth Essence added to all five owners |
| --- | --- | --- |
| 9 | Protection | Treant Guardian |
| 10 | Recurring control | Giant Worm |
| 11 | Recovery | Treant Sapling |

This demonstrates the intended slot-allocation behavior on authored content, independently of the earlier Fairy/control example. It does not establish which fifth Essence is better. Another recorded group—Giant Bat / Pack Howler / Royal Venom—receives Fairy when recurring control is prioritized, through the generic provider rule. No Fairy-specific preference was added.

All four repeated recipes remain visible in the 19 requests. When two category orders select the same providers and fill the same slots, they can produce the same canonical team. The ordinary search continues to charge proposals and handle duplicates under its existing rules; this verification neither removes duplicate charges nor increases caps.

## Determinism, constraints and limitations

The full saved completion search reconstructed exactly with **44 archived measurement callbacks**, including its proposals, evaluations and shortlist. Generation hash: `9f3215072ef69c44cd6f73984895e265176aae7bb0f61a094951594f27870aa5`. No combat, preparation or new score was computed. Existing policy goldens also passed in the backend suite.

The fifteen allocation cases exercise category alternatives, shared seeds, full slots, metadata order, already-covered categories, case-insensitive family exclusions, shared owned copies, uniform routing, failed reservations, opt-in enforcement, trace serialization, synthetic search budgets/determinism/provenance, cancellation and sweep progression. Cancellation preserves the attempted proposal and allocation trace. All constructed recipes passed independent slot/family/copy, canonical-order and reservation checks. Durable combat charging and archive-verification code were not changed or exercised with new fights.

Remaining limits: this policy explores first-category alternatives, not every possible category subset or provider choice. Multi-category providers can make alternatives equivalent. The schedule's reduced early group breadth is real, and no combat evidence establishes whether that tradeoff is beneficial. The 19 requests are a construction diagnostic, not a reliability sample. Historical reliability, v19 status and adoption are unchanged.

The next step is to prepare a bounded equal-budget comparison of verified completion versus allocation, fixing the same gameplay inputs, ordinary fight/attempt caps, seed ownership and nomination rules before execution. Preparation should use zero combat; any fresh values require a new explicit exception because previous approvals are exhausted. Do not expand into a larger search or confirmation run from these construction results.

## Changed files and timing context

This verification changes only the JSON-property assertion in `LL/tests/EssenceSystem.Tests/BalanceHarnessGroupAllocationTests.cs`, the isolated verification workflow/evidence, this protocol/review and six active Markdown handoffs. The production implementation verified here is `TowerGroupAllocationSearch.cs`, completion/count integration, and explicit policy registration in `TowerBossDiscoveryContract.cs`, `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerCompositionSearch.cs`, `TowerLoadoutComposition.cs` and `TowerPartyCoverage.cs`. The failed package retains the original source patch and test. Unrelated checkout work is preserved.

The candidate build had zero warnings. The test build retained one existing xUnit2031 analyzer warning in the old composition test at line 188; it is unrelated to allocation and did not prevent the 110 cases from passing.

The captured trace measured **55.600 ms** for the old completion policy's 19 requests and **48.116 ms** for allocation's 19 requests. These are single, fixed-order construction measurements over different recipes and warm-up states; they do not establish a performance speedup. The archived search reconstruction took **94.165 ms** within the driver. The complete timed construction/reconstruction/schedule block took **448.323 ms**, with no fight throughput measured. Full trace and receipts are retained below.


## Measured verification and reproduction

**110/110 backend tests passed through `build/run-tests.ps1`**: 95 existing cases and 15 new allocation cases. Isolated builds took **5.828 seconds** combined. Captured construction and archived generation reconstruction took **448.323 ms** natively. The [saved performance trace](../TestResults/balance/tower-group-allocation-verification-20260915/content.log) retains per-policy construction and old-search reconstruction measurements; these short construction measurements do not predict fight throughput.

The independent audit exactly matched all **76 old construction requests** and the saved **44-evaluation completion search**. It reconstructed all **564 allocation positions across 214 groups**, plus the 19 actual new construction requests and their completion steps. All **14,507 indexed files across 36 predecessor packages** verified unchanged before/after. Source, gameplay DLLs, fixtures, output assemblies and unrelated dirty-file hashes remained pinned.

Before publication, this package charged **9.873 diagnostic seconds**. Carried diagnostics: **1199.726 seconds**; carried output: **1,600,627,342 bytes**. The [completion receipt](../TestResults/balance/tower-group-allocation-verification-20260915/completion.json) records measured publication plus a conservative one-second seal allowance and final cumulative charges. Limits remain 180 seconds / 256 MiB new, 1,800 seconds / 4 GiB cumulative, zero retries. No command failed or was blocked. All required checks completed once; no fights, fresh values, preparations or combat replays occurred.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-group-allocation-verification-20260915'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" candidate-build
& $python -B "$work/workflow.py" tests-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" content
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The test phase invokes `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests"` with the seven composition/joined/count/variation/diversity/completion/allocation test classes as its filter. Exact arguments, logs, TRX and frozen sources are retained in the package. Reproduction requires a separate frozen package and carried accounting; never rerun or modify this sealed evidence.

No gameplay content, service configuration, migrations or deployment changes. The policy is explicit opt-in and is not the default. Preserve **482,596 reservations**, v19's unused 512 and all 253 recipes. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved, adoption Hold. No Kharad tuning, fresh-value authorization or 129,536-fight confirmation.
