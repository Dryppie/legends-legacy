# Group diversity implemented and verified without combat

15 September 2026. Added opt-in **`independent-group-diversity-v1` / `group-diversity-joint`**. It spends early fresh requests on different content-derived groups before returning to count/filler variants. **79/79 backend tests passed**, all 38 old-policy captured construction requests matched exactly, and the independent audit reconstructed all 1,956 schedule positions and the complete greedy order. No fights, combat preparations, fresh values or replays.

The [initial implementation run](Tower-Group-Diversity-Implementation-Review.md) remains sealed with its 78-pass/1-fail result and full diagnostic charge. This corrected verification adds the missing explicit policy/method to the loadout provenance allowlist. The 79 fixtures and diversity schedule are unchanged; reference work was reused without rerunning it.

## What changed

The new order greedily prioritizes unseen authored effect-evidence keys, then unseen source-core IDs, then unseen Essences. Seeded ties and ordinal IDs make it reproducible. It covers the entire bounded catalogue before repeating a count/filler sweep. With ten owners, sweep counts are 5, 1, 10, then 5 with another filler draw; later complete cycles rotate count anchors. One/two-owner anchors are deduplicated. Every eighth fresh request remains uniform.

The generator caches one bounded order for the active generation label. Its selection uses no combat results, control recipes, nominations, names or hardcoded Essence identities. Atomic group reservation, count-drift rejection, per-arm fresh charging, candidate/attempt limits, cancellation, mutations, fitness and nomination rules are reused. Ability order remains ordinal. Existing policies/defaults are unchanged.

This policy combines broader early group coverage, novelty ordering and middle-count-first scheduling. These are explicit tradeoffs, not a controlled claim about a single causal mechanism. Authored-effect coverage is structural diversity; it is not a measure of damage or synergy strength.

## Captured-input construction measurements

Exactly 19 construction requests per policy on the retained captured inputs and label 17; 17 guided requests and two uniform positions per policy. No captured-gameplay fitness callback was invoked.

| Policy | Requests | Accepted | Distinct accepted recipes | Requested groups | Evidence keys | Source cores | Essences in requested groups |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| group-count | 19 | 19 | 19 | 17 | 27 | 24 | 21 |
| group-variation | 19 | 19 | 19 | 5 | 17 | 10 | 13 |
| group-diversity | 19 | 19 | 19 | 17 | 34 | 34 | 28 |

The requested-group columns describe the selected structural hypotheses, not incidental groups formed by fillers. Rejected requests remain in the denominator and in [coverage.json](../TestResults/balance/tower-group-diversity-verification-20260915/coverage.json). All successful guided constructions had exactly their requested/placed/final owner counts. Every accepted recipe passed slot, distinct-Essence, family and canonical-order checks. No outcome was substituted or retried.

Full native construction/schedule work took **204.256 ms**. Detailed ordering and construction timings are retained in [content.log](../TestResults/balance/tower-group-diversity-verification-20260915/content.log). Native timing includes the complete catalogue/order/schedule and 57 construction requests; nested trace timings must not be summed. Different cache positions prevent treating this as a controlled throughput benchmark.

The frozen reference produced a complete 128-evaluation old-variation synthetic trajectory with hash **`3b52fb57669f7758cdd8475b55351c91a7d88e640c5c2198d2059f04a21fe13a`**. The candidate's test reproduced it exactly. The existing composition, joined and group/count golden-output cases also passed, and all 19 group/count plus all 19 group-variation construction outputs matched the unchanged reference executable byte-equivalent canonical JSON. Gameplay DLLs remained the same captured binaries; no dirty gameplay build.

## Verification and limits

The 79 cases ran once through `build/run-tests.ps1`: 63 retained policy cases plus 16 diversity cases. They cover greedy novelty/permutation, breadth and eventual count coverage for 1/2/10 owners, uniform/empty fallback, invalid/large indices, placement/filler streams, owned-copy rollback, accidental count drift, deterministic bounded search, cancellation/checkpoints, duplicate/attempt charging, per-arm reset, metadata/policy isolation and old-variation parity. Engine-entry guards were active.

The independent Python audit separately reconstructed the greedy selection vector and SHA-256 tie labels, all 1,712 guided positions and 244 intervening uniform positions, requested recipes/counts, recipe identities and structural coverage. All **9,138 indexed files across 23 earlier sealed packages** verified unchanged before/after. Existing storage accounting, durable fight charging and archive verification code were not edited.

Diagnostic work before publication: **7.593 seconds**, carried forward **550.237 seconds**; compilation separately **4.109 seconds**. The final [completion receipt](../TestResults/balance/tower-group-diversity-verification-20260915/completion.json) includes publication and a conservative one-second closure allowance. Caps remained 300 seconds new diagnostics / 1,800 cumulative, 512 MiB new package / 4 GiB cumulative, zero retries. The preceding failed fixture and corrected accounting remain charged.

## Commands, files and remaining work

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-group-diversity-verification-20260915'
& $python -B "$work/workflow.py" preserve
& $python -B "$work/workflow.py" candidate-build
& $python -B "$work/workflow.py" tests-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" content
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

The `tests` step dispatches `build/run-tests.ps1 -NoBuild` with the frozen five-class filter and isolated artifacts, retaining TRX. Builds use `--no-restore`. Every command in this corrected scope ran once; none failed or was blocked. The earlier failing command remains preserved and charged. Do not rerun sealed output paths; reproduction needs a separate frozen package and budget.

Changed source: new `TowerGroupDiversitySearch.cs`, policy integration in `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerBossDiscoveryContract.cs`, `TowerCompositionSearch.cs`, `TowerLoadoutComposition.cs` and `TowerPartyCoverage.cs`, plus `BalanceHarnessGroupDiversityTests.cs`. The [implementation patch](../TestResults/balance/tower-group-diversity-verification-20260915/implementation.patch), snapshots, isolated binaries/tests and diagnostic receipts are retained. This protocol/review and six active Markdown handoffs were updated. No gameplay/content, configuration, migrations, deployment or default policy change; unrelated checkout work was preserved.

Construction coverage is verified. Combat strength is **unmeasured** for this policy, and the measured breadth increase does not guarantee stronger builds or inclusion of a particular control combination. Next prepare a small equal-input/equal-budget comparison against the preceding variation policy with its own frozen allocation and selection rules; do not execute a new fight study or allocate values under this implementation scope. All **482,506 reservations**, including v19's unused 512, remain preserved. Its 253 recipes and 129,536-fight confirmation remain untouched. Fixed ability order; no Kharad tuning. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption **Hold** remain unchanged.
