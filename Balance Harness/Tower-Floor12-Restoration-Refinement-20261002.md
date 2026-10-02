# Floor 12: fixed Restoration pressure refinement — 2 October 2026

## Completed result: one ceiling failure

All **8,576 fresh fights / 32 reservations** completed and passed the independent recount and native comparisons. The eight-item healer routes won **14, 13, 10, 7, 7, 5, 5, 3 and 2 / 32** across the nine actual compositions. Five met the exploratory 6/32 floor, but the 14/32 result exceeds the unchanged ceiling of 13. **No nomination or acceptance** followed; the threshold was not relaxed.

All **241 old native recipes** match their closed 0.575 references and all **27 new variants** passed the native baseline/donor checks. The native fixture and twelve penetration guards passed with zero native skips. Native combat took **237.70 seconds**. Final exclusions: **928,956**. [Completion receipt](../TestResults/tower-floor12-restoration-refinement-diagnostic-driver-20261002/completion.json), SHA-256 `af7955b5851427b21bd4f86ebb3cfec1068633d82aaea14f181e786db6b44021`; [all nine equipment comparisons](../TestResults/tower-floor12-restoration-refinement-next-20261002/equipment-comparisons.json).

The next [separate fixed 0.60 diagnostic](Tower-Floor12-Restoration-Upper-20261002.md) preserves the complete family and limits, with a new 8,576-fight / 32-seed maximum panel. It does not extend or pool this closed result. Live floor 12 remains unchanged.

## Closed preceding result

The complete 0.55 Power / 40 penetration diagnostic closed with **8,576 fights / 32 fresh seeds**, all 268 recipes recounted and native comparisons passing. Eight-item Restoration setups won **6–17/32 across all nine actual compositions**. Three exceeded the 13/32 exploratory ceiling, at **14, 15 and 17 wins**. The strongest twelve-item healer control won 11/32. No setting was nominated or applied. This reveals that the exact eight-item mix can outperform the full twelve-item gear profile; item count is not a monotonic power ordering.

Closed receipt: `TestResults/tower-floor12-restoration-diagnostic-driver-20261002/completion.json`, SHA-256 `e02be3c423d104179cddcc6a314274bd209488bd1422d6b63cbe6c7b35d88d8a`. Complete equipment comparisons are saved in `TestResults/tower-floor12-restoration-next-20261002/equipment-comparisons.json`.

## One separate fixed diagnostic

Test exactly **one offense factor 0.575**, yielding **11.9 × 0.575 = 6.8425**, with penetration factor **40**. Start from the unchanged qualified 268-recipe preparation. Preserve Health **9.414125**, all other guardian attributes and abilities, every other floor and every raw player build. No old outcome is pooled with this panel.

Retain **268 recipes / nine actual compositions / 155 eligible recipes**. Use exactly **32 fresh seeds / 8,576 fights**, excluded from the full starting union of **928,924**. No search, omitted control, retry, replacement, extension, second setting, confirmation or application. Freeze the declaration and all prerequisites before allocation.

Reuse the completed diagnostic's strict native comparisons: all 241 old recipes must exactly match their earlier closed **0.575** native participants. All 27 new variants must retain unchanged actors and exact donor/baseline equipment, with the original identity/Essence/ability order. Independently recount every raw outcome and per-recipe prepared-participant invariance. The twelve penetration guards and dedicated comparison/nomination checks must pass first.

Use the same slower 0.50 resource reference scaled to 268 recipes and doubled: **492.48 seconds**. Recheck time, bytes and disk admission; retain **840 seconds native / 900 seconds native owner / 2 GiB output / 2 GiB free-disk reserve / 1,800 seconds enclosing execution owner**. Backend execution uses `build/run-tests.ps1`. Observe supervisor stdout only during execution.

After complete verification, nominate this fixed setting only if **two distinct eligible compositions reach at least 6/32**, and **every recipe stays at or below 13/32**. Otherwise nominate none and close without extension. This is a diagnostic heuristic, not statistical acceptance. Any nomination requires a separate strict full-family acceptance contract and fresh independent phases, followed by application parity.

Runner: `TestResults/tower-floor12-restoration-refinement-diagnostic-20261002.py`. Control: `TestResults/tower-floor12-restoration-refinement-diagnostic-driver-20261002/`. No live gameplay edit, migration, configuration change or deployment is included.
