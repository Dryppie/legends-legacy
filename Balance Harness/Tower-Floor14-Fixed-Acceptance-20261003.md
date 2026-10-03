# Floor 14: fixed Restoration acceptance — 3 October 2026

**Status: acceptance implementation verified; ready for frozen screening and conditional confirmation.** The [complete fixed refinement](Tower-Floor14-Restoration-Refinement-20261003.md) selected Power factor **0.60** and penetration **40**. Four eligible compositions reached the diagnostic minimum and the strongest recipe won **11/32**. The 0.625 setting also qualified; the frozen selection rule chose the lower factor. Diagnostic outcomes cannot establish acceptance or contribute acceptance samples.

Target the primary LL World Tower and offline Balance Harness. Hold Caldris's guardian offense **10.933125 × 0.60 = 6.559875** and penetration **1 → 40** fixed in isolated catalogs. Preserve Health **12.68625**, every other guardian stat, all abilities, other floors and player progression. Live floor 14 remains unchanged until independent acceptance and application parity pass. No deployment or database operation.

Retain all **198 exact recipes / five actual compositions / 135 eligible recipes**: all 183 prior controls and all 15 exact Restoration subsets. Equipment eligibility remains at most eight specialized items on at most two characters. Keep level 60, tier 2, seven Essences, stronger owned Legendary/Masterpiece/Rank-5 gear, identities and raw actor, Essence and item order.

Use the qualified unchanged-guardian preparation for every batch: `TestResults/tower-balance-pass-floor14-qualified-limited-restoration-preparation-study-20260929`, manifest `b7a7d2fceefe7b4b23eacda0ad68acd589eef0450c8d031efbffb1e0cda86a62`, cells `bd225c811b4391334b8c73bbd2f46b9c6ebd1556abe984d1bc49284fb22131a9`. Never scale an already scaled candidate.

Frozen [acceptance proposal](../TestResults/tower-floor14-restoration-refinement-publication-20261003/acceptance-proposal.json), SHA-256 `a3bb2171d1ef26f7d3378b0e183de47860f5c3ac44cdaedd556f3ef8cb2eb671`. Preserve its pre-implementation status. Starting exclusions: **931,644**. The completed original **18,688 input comparisons / 73 replays** and all **198 native preparations** remain reusable evidence; do not repeat them without a material source/runtime change.

## Execution and decision

1. Implement and verify the strict candidate, aggregate and native application contracts. Reject altered floors, factors, recipes, equipment limits, phase sizes, incomplete batches, reordered loadouts and reused seeds. Run Python guards and backend verification through `build/run-tests.ps1`; compare all 198 source/candidate pairs with the nominated native participant snapshots on the fresh and preserved qualified combat runtimes.
2. Run **16 batches × 32 fresh seeds per phase**: **512 observations per recipe / 101,376 fights per phase**. Finish screening before assessment; independent confirmation runs only if the full screen passes. Maximum **202,752 fresh fights / 1,024 reservations**. No historical or diagnostic samples count toward acceptance.
3. Independently recount both complete phases, raw participants and resource/seed accounting. Use approximate simultaneous 95% Bonferroni-Wilson intervals across all 198 recipes. At least two distinct eligible compositions need a recipe with lower bound at least **10%**; every recipe needs upper bound at most **50%**. Integer limits at 512 samples: **minimum 77 wins / maximum 214 wins**, separately in both phases. These limits are recomputed for this family size.
4. If both phases pass, freeze separate application verification: **101,376 confirmation input comparisons / 3,168 complete historical replays**, one saved seed per recipe per confirmation batch, with no new seeds. Only passing parity permits local application. Then verify exact candidate bytes, native preparation and relevant backend regressions.

No interim statistical decisions, tuning, second candidate, omitted controls, retries, replacement seeds, extensions or pooled phases. Preserve all failed attempts and reservations.

## Resources and ownership

The nominated diagnostic's first-batch doubled estimate is **385.38 seconds / 329,350,810 bytes**, within the native **840-second**, native-owner **900-second** and **2 GiB output** budgets. Reserve projected output for all remaining batches plus **2 GiB** before allocation. Recheck measured resources, the full completed prefix and source/runtime/catalog/ledger pins before each batch. Each batch supervisor retains a 1,800-second limit. Observe supervisor stdout only while any native owner is active.

No acceptance driver is admitted yet. The next work is implementing and testing that contract, then running this fixed sequence. Floor 13 remains locally applied; floor 15 and the final current-version 1–15 sweep follow. No migration, environment configuration change or deployment.

Readiness: **350 Python tests and 438 backend cases / zero skips pass**. All **198 source/candidate pairs (396 native preparations per runtime)** match the nominated snapshots on the fresh and preserved qualified combat runtimes. Both changed shared guard sources have explicit preimage bindings. No combat engine source or assembly changed. [Readiness receipt](../TestResults/tower-floor14-acceptance-ready-20261003/completion.json).
