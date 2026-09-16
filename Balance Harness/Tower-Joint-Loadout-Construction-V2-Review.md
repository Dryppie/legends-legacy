# Joint loadout construction: structural feasibility

15 September 2026. **VerifiedStructuralConstructor**. Zero fights, fresh seed values, battle preparation or replay. The joint constructor found structurally complete recipes for **48/48 authored cores**, retaining **768 recipes (740 distinct)**. **0 core searches exhausted their search space; 48 stopped at a state/result limit.**

The previous [encoding failure](Tower-Joint-Loadout-Construction-Review.md) remains sealed and charged. This separate scope changes only preparation encoding and preservation/accounting; the constructor, tests, native adapter and audit retain their original hashes.

## Implementation and meaning

[`TowerJointLoadoutConstructor`](../LL/tools/BalanceHarness/TowerJointLoadoutConstructor.cs) fills missing authored roles jointly around a supplied mechanic core. It branches on the missing category with the fewest compatible providers, while considering multi-role providers and preserving slots, family exclusivity and available copies. Every result carries exact Essence IDs and authored evidence witnesses for each requested category. Ordering is canonical and deterministic; no random seed, reference recipe or combat outcome enters the constructor.

The synthetic greedy-trap fixture shows why this is useful: a single-role provider can occupy the family/slot needed by a multi-role provider. Joint construction finds the compatible combination. This is a structural capability test, not evidence that a particular loadout wins a fight.

The API is standalone and is **not registered as a search policy or selected by existing campaigns**. “Complete” means it covers the caller's required authored categories within the slot limit. It may leave spare slots. It neither fills those slots nor assigns loadouts across a party. Available copies are the remaining inventory for this one loadout; a future party allocator must account for copies already used by other characters. Evidence categories do not prove targeting compatibility, useful uptime, damage or survival.

The search adds only providers needed by a currently missing role and stops a branch when coverage is complete; it does not enumerate every padded or redundant loadout. Structural feasibility is complete when the search exhausts, despite this pruning. State/result caps return `SearchExhausted=false`; a capped empty result is unresolved, never an infeasibility proof. Cancellation throws and no partial result is presented as complete.

## Captured metadata results

All 48 distinct legal two/three-member base cores came from the same sealed 80-Essence catalogue. Required roles were fixed before execution: recurring control, enemy pressure, attack enabling, protection and recovery. Each invocation allowed five slots, at most 256 visited states and 16 retained results. No controls selected the cores or required roles.

| Authored core | Structurally feasible | Retained recipes | Visited states | Termination |
| --- | --- | ---: | ---: | --- |
| Pack Howler Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | yes | 16 | 42 | recipe-limit |
| Pack Howler Essence / Venomous Spiderling Essence | yes | 16 | 20 | recipe-limit |
| Flame Imp Essence / Smolder Rat Essence | yes | 16 | 20 | recipe-limit |
| Pack Howler Essence / Spider Queen Essence — Webbed Domain / Venomous Spiderling Essence | yes | 16 | 20 | recipe-limit |
| Venomous Snake Essence / Viper Essence | yes | 16 | 41 | recipe-limit |
| Grave Hound Essence / Pack Howler Essence / Venomous Spiderling Essence | yes | 16 | 42 | recipe-limit |
| Blood Harpy Essence / Cinder Beetle Essence | yes | 16 | 41 | recipe-limit |
| Flame Harpy Essence / Smolder Rat Essence | yes | 16 | 20 | recipe-limit |
| Pack Howler Essence / Venomous Spiderling Essence / Viper Essence | yes | 16 | 42 | recipe-limit |
| Pack Howler Essence / Venomous Snake Essence / Viper Essence | yes | 16 | 42 | recipe-limit |
| Bloodfang Wolf Essence / Goblin Essence | yes | 16 | 41 | recipe-limit |
| Poisonous Rat Essence / Viper Essence | yes | 16 | 20 | recipe-limit |
| Venomous Spiderling Essence / Web Weaver Spider Essence | yes | 16 | 20 | recipe-limit |
| Blood Zombie Essence / Bloodfang Wolf Essence | yes | 16 | 20 | recipe-limit |
| Venomous Spiderling Essence / Viper Essence | yes | 16 | 41 | recipe-limit |
| Cave Bat Essence / Web Weaver Spider Essence | yes | 16 | 20 | recipe-limit |
| Bog Mite Essence / Venomous Snake Essence | yes | 16 | 21 | recipe-limit |
| Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | yes | 16 | 20 | recipe-limit |
| Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | yes | 16 | 20 | recipe-limit |
| Bog Mite Essence / Venomous Spiderling Essence | yes | 16 | 21 | recipe-limit |
| Cinder Beetle Essence / Smolder Rat Essence | yes | 16 | 20 | recipe-limit |
| Bog Mite Essence / Rotroot Shambler Essence | yes | 16 | 21 | recipe-limit |
| Spider Queen Essence — Webbed Domain / Venomous Spiderling Essence | yes | 16 | 20 | recipe-limit |
| Grave Wisp Essence / Venomous Spiderling Essence | yes | 16 | 41 | recipe-limit |
| Cave Bat Essence / Venomous Spiderling Essence | yes | 16 | 41 | recipe-limit |
| Green Slime Essence / Viper Essence | yes | 16 | 41 | recipe-limit |
| Pack Howler Essence / Wind Harpy Essence | yes | 16 | 20 | recipe-limit |
| Rotroot Shambler Essence / Viper Essence | yes | 16 | 41 | recipe-limit |
| Ice Harpy Essence / Wandering Ghost Essence | yes | 16 | 41 | recipe-limit |
| Bog Mite Essence / Green Slime Essence | yes | 16 | 21 | recipe-limit |
| Giant Bat Essence / Pack Howler Essence | yes | 16 | 20 | recipe-limit |
| Bloodfang Wolf Essence / Cinder Beetle Essence | yes | 16 | 41 | recipe-limit |
| Bog Mite Essence / Pack Howler Essence / Venomous Spiderling Essence | yes | 16 | 20 | recipe-limit |
| Bloodfang Wolf Essence / Dire Wolf Essence | yes | 16 | 41 | recipe-limit |
| Goblin Warrior Essence / Pack Howler Essence | yes | 16 | 20 | recipe-limit |
| Blackjaw Spider Essence / Pack Howler Essence | yes | 16 | 20 | recipe-limit |
| Blood Harpy Essence / Goblin Essence | yes | 16 | 41 | recipe-limit |
| Grave Wisp Essence / Web Weaver Spider Essence | yes | 16 | 20 | recipe-limit |
| Cave Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | yes | 16 | 42 | recipe-limit |
| Bog Mite Essence / Poisonous Rat Essence | yes | 16 | 22 | recipe-limit |
| Blood Harpy Essence / Blood Zombie Essence | yes | 16 | 20 | recipe-limit |
| Grave Wisp Essence / Pack Howler Essence / Venomous Spiderling Essence | yes | 16 | 42 | recipe-limit |
| Pack Howler Essence / Venomous Snake Essence | yes | 16 | 20 | recipe-limit |
| Grave Hound Essence / Venomous Spiderling Essence | yes | 16 | 41 | recipe-limit |
| Grave Hound Essence / Web Weaver Spider Essence | yes | 16 | 20 | recipe-limit |
| Blood Harpy Essence / Dire Wolf Essence | yes | 16 | 41 | recipe-limit |
| Pack Howler Essence / Spider Queen Essence — Royal Venom | yes | 16 | 21 | recipe-limit |
| Frost Imp Essence / Pack Howler Essence | yes | 16 | 20 | recipe-limit |

The [captured records](../TestResults/balance/tower-joint-loadout-construction-v2-20260915/captured.json) retain every core, bounded result and witness. The [independent audit](../TestResults/balance/tower-joint-loadout-construction-v2-20260915/independent-audit.json) verified every emitted recipe and witness, then independently checked feasibility for exhausted cores against 0 captured-catalogue subsets. No reference-similarity metric or combat ranking was applied.

## Verification and costs

Exactly **11 new backend tests** passed through `build/run-tests.ps1`. The exhaustive fixture checked **448 role/copy/slot cases** against all 16 subsets of a four-provider catalogue. Other tests cover joint versus blocked greedy construction, input order, family/copy constraints, state/result caps, complete/missing/incompatible anchors, cancellation, malformed metadata and evidence/input preservation. Existing policies and source remain byte-identical, so their sealed backend/parity evidence was reused without generation or combat replays.

Native metadata construction took **0.085 seconds**, 0.094 CPU seconds, allocated **9,767,592 bytes** and peaked at **44,335,104 bytes** working set. These are standalone construction costs, not combat throughput or an end-to-end speedup.

| Phase before publication | Seconds | Accounting |
| --- | ---: | --- |
| audit | 0.188 | diagnostic |
| build | 2.422 | engineering build |
| captured | 0.234 | diagnostic |
| freeze | 3.719 | diagnostic |
| setup | 0.015 | diagnostic |
| test-build | 1.937 | engineering build |
| tests | 1.875 | diagnostic |

The prior chain carries **1758.194 diagnostic seconds** and **2,121,334,876 output bytes**. The [completion receipt](../TestResults/balance/tower-joint-loadout-construction-v2-20260915/control/completion.json) includes publication and one conservative closure second. New diagnostics are capped at 40 seconds, package output at 192 MiB, within the unchanged cumulative 1,800 seconds / 4 GiB. The two isolated builds are separately measured engineering work. No gameplay rebuild or package restore occurred.

All **17,803 indexed files across 45 sealed predecessor packages** were verified before/after, including all **482,641 reservations**. Existing harness source and unrelated dirty files were preserved. Failure receipts: **none**.

## Commands and disposition

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-loadout-construction-v2-20260915'
& $python -B "$work/assemble.py"
& $python -X utf8=0 -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The tests phase invokes `build/run-tests.ps1 -NoBuild -ArtifactsPath "$work/tests" -Filter FullyQualifiedName~BalanceHarnessJointLoadoutTests`; all subprocess commands and exit receipts are retained. Do not rerun sealed output directories. Reproduction requires a separately frozen output/budget with identical sources, binaries and metadata. See the [protocol](Tower-Joint-Loadout-Construction-V2-Protocol.md), [freeze](../TestResults/balance/tower-joint-loadout-construction-v2-20260915/freeze.json) and [evidence seal](../TestResults/balance/tower-joint-loadout-construction-v2-20260915/files.json).

Changed files: the standalone constructor, its focused backend tests, this protocol/review, six active Markdown handoffs and the separate diagnostic package. No existing policy, gameplay, configuration or migration changed; no deployment implications. The full dirty gameplay build/backend suite was not rerun. Both isolated builds and all required checks passed.

The next question is how to select and place these structural recipes across characters while preserving party copy limits and retaining diversity within an explicit proposal budget. That integration needs its own zero-combat verification; these results do not authorize another balance study, demonstrate stronger builds or justify default promotion. Before another diagnostic, carry forward the actual remaining time/output budget; do not silently restart the 30-minute allowance.

V19 retains all 253 recipes and unused 512 confirmation values. Reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved** and adoption **Hold** remain unchanged. No fresh seeds, fights, Kharad tuning, ability-order optimization, 129,536-fight confirmation or old-cap increase.
