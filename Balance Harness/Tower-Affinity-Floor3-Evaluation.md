# Floor-3 supported search evaluation

Completed on 2026-09-25. The unchanged supported search retained its benchmark. All three references and both generated finalists won **0/128** held-out fights on the current combat build. This run demonstrates no improvement in clearing the encounter.

A separate matched runtime check explains why the earlier reference screen no longer identifies a useful case: the exact preselected benchmark, recipe, identities and 128 seeds won **88/128 on the earlier runtime and 0/128 on the current runtime**. Content and effective settings were identical. This isolates a runtime/code difference from input ordering or a different seed sample. The current build includes the recent armor formula change; this comparison is between complete builds, not a controlled test of that one source edit alone.

Keep `affinity-creation-with-benchmark-validation-v1` unchanged. Establish a viable reference at an explicit progression budget on the current combat build before drawing further search-quality conclusions. Do not tune the search or increase this failed case's budget after seeing its outcomes and describe that as success of this evaluation.

## What actually ran

One 528-fight search used the existing affinity-creation policy, 17 proposals, racing schedule and 60-pair benchmark validation gate. Before held-out combat, the two generated finalists and all three existing references were frozen. Each then received the same 128 separate held-out seeds: **640 additional fights**, with no selection changes based on those results.

| Frozen team | Held-out wins | Mean boss health remaining |
| --- | --- | --- |
| Preselected benchmark `68a156b5380e…` | 0/128 | 48.47% |
| Existing reference 2 `5168053b9469…` | 0/128 | 47.69% |
| Existing reference 3 `d17448333a43…` | 0/128 | 48.80% |
| Generated finalist 1 `33e530332e2d…` | 0/128 | 44.94% |
| Generated finalist 2 `bf90eaabae2c…` | 0/128 | 46.78% |

Lower remaining health is favorable, but neither generated team cleared the floor. Finalist 1, the frozen challenger, left the boss with 3.52 percentage points less health than the benchmark on average. Its validation result was 0/60 against the benchmark's 0/60, with zero gained or lost wins; the unchanged gate correctly retained the benchmark.

Both finalists made real composition changes: finalist 1 replaced two Essences on character 1; finalist 2 replaced two on character 4. They were not order-only variations. This is one exploratory search root and does not estimate general search reliability or independently confirm a team.

The primary run completed **1,168 fights in 124.37 seconds** inside the fixture. The outer owned process took 127 seconds, exited successfully and drained all eight owned processes. Retained study size was 123,997,201 bytes. Its fixed limits were 840 fixture seconds, 900 outer process seconds and 1 GiB, with no retries or extensions.

The matched earlier-runtime diagnostic added **128 fights using the already reserved held-out seeds**, for **1,296 actual fights in this work**. It ran no search and allocated no additional values. The completed native scorecard exceeded a convenience reader's 32 MiB cap; read-only verification used an explicit 128 MiB cap. No battle was repeated.

## Inputs and design decisions

- Floor 3, five characters, four Essences each, level 30, tier-1/rank-1 Uncommon Standard equipment, baseline rolls, level-1 unascended/unevolved Essences, no styles or contributions, hypothetical ownership. The budget was not adjusted after observing results.
- The seed-free handoff retained `f3-50cbb8cef4d4dad78fcf`, `f3-846afdb79866f9ca123c` and `f3-a835efa1ee203add93c9`. These are three distinct per-character Essence compositions; the original first reference remained the benchmark.
- The existing supported composition search requires fixed ordinal Essence-ID ordering. The evaluation records that conversion, retains the original recipes, and makes fresh measurements of all converted references. The generator does not search over Essence permutations. Historical win rates are not transferred to these inputs.
- An isolated build captured current combat assemblies, including the armor change. Content was copied from the current repository; effective threat and checkpoint settings were held fixed to the earlier screen. Captured files are retained with hashes.
- The existing complete-reservation allocator reserved **237 new values**: one construction root, 108 search seeds and 128 held-out seeds. These were disjoint from **831,939 historical exclusions**, with a durable journal, registry lease and published reservation. The resulting union is 832,176 values; failure or non-improvement does not make them reusable for fresh confirmation.
- Three references passed native preparation before allocation. The held-out panel was excluded from search, and all five measured teams were frozen before any held-out result. The final recommendation did not change after held-out measurement.

## Implementation and checks

- `LL/tests/EssenceSystem.Tests/BalanceHarnessAffinityFloorEvaluationTests.cs` adds an opt-in bounded evaluation fixture. Ordinary test runs skip allocation and combat. It reuses the existing reservation, registry, native search and archive components, and tests panel partitioning and permutation-equivalent input encoding.
- `Balance Harness/analysis/run-affinity-floor-evaluation.py` pins the source handoff and original plan, checks that retained and test assemblies match, records the fixed request and starts `build/run-tests.ps1 -NoBuild` inside the existing Windows process owner. Each invocation requires new request and study directories; there is no retry/resume path.
- `LL/tools/BalanceHarness/AFFINITY-SEARCH.md` links this result and documents the opt-in command.

The initial sandboxed build could not read the user NuGet configuration. The same repository test command succeeded with the needed filesystem access. **46 tests passed**, covering the new deterministic checks, supported affinity profile and current armor/attribute combat rules; the scientific fixture was initially skipped. The opted-in run then passed **all three fixture tests**, including actual execution and reconstruction.

Native verification reconstructed the complete 528-fight search. The fixture authenticated and reconstructed all 640 held-out inputs and checked saved reports against measured results. An additional Python readback independently recomputed the 237-value allocation/rejection journal and all 640 held-out win counts, health means and paired gained/lost counts from saved compressed reports. It passed with zero new fights. No requested checks remain blocked.

Commands, from the repository root:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessAffinitySearchTests|FullyQualifiedName~ArmorBalanceTests|FullyQualifiedName~AttributeCombatSystemTests' -ArtifactsPath '.artifacts/affinity-floor3-evaluation-20260925'
python -B -X utf8 'Balance Harness/analysis/run-affinity-floor-evaluation.py' --package 'TestResults/affinity-floor3-evaluation-owner-20260925' --output 'TestResults/balance/tower-affinity-floor3-baseline-20260925' --artifacts '.artifacts/affinity-floor3-evaluation-20260925'
```

The second command launches fresh allocation/combat and is not a report-view command. Those completed paths cannot be reused. The driver requires the pinned local historical evidence and a previously tested isolated build. The available bundled Python executable was used.

There are no gameplay changes from this task, migrations, service configuration changes or deployments. Concurrent armor and analytics work was left intact. The floor-5 algorithm tuning cycle remains closed.

## Retained evidence

Primary study: `TestResults/balance/tower-affinity-floor3-baseline-20260925/`.

Owner, declaration and separate Python readback: `TestResults/affinity-floor3-evaluation-owner-20260925/`.

Matched runtime diagnostic: `TestResults/affinity-floor3-runtime-comparison-20260925/`.

| Artifact | SHA-256 |
| --- | --- |
| Primary study `files.json` | `9843e70f212f0de2c6463372c93fabb1285ba68909c25a0e903157474084da9f` |
| Primary study `result.json` | `ee873098012673992e54be113acf41cc4863e953f0ec153027a1e7b6bf16558e` |
| Matched runtime `result.json` | `8e4c2e4333181561534409a9935dbb137421d5a8cdc3eed80071afb680a9f8c4` |
