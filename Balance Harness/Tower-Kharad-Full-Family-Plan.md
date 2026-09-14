# Kharad +16% complete-family confirmation

Prepared 13 September 2026. This plan becomes frozen when the new package writes `protocol.json`, before its first battle. This is an offline floor-5 confirmation of the already selected candidate, with no new search or tuning grid.

## Frozen scope

Use all **17,821** distinct seed-free recipes in the [saved inventory](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/next-family/summary.json), preserving every source and breach. Verify the compressed family hash, all 35 consumed source hashes, typed normalized identities, equipment budget and encounter, and materialize all parties before combat. The earlier 32-recipe Pass supplies anchor nominations only; none of its outcomes enter the new assessment.

Copy the selected candidate content exactly: Kharad Health **3.5366243328**, Power **4.4702934848**. Preserve live Health **3.04881408**, Power **3.85370128** during confirmation. Budget remains ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences, all 80 eligible Essences under hypothetical ownership, exact fixed gear, no styles or contributions. No acquisition or later-floor acceptance is implied.

## Allocation and decision

Opt into schema 2 / `tower-staged-bonferroni-wilson-95-v2`, whose only statistical-contract change is capacity from 10,000 to 20,000 recipes. Schema 1 keeps its existing limit. Both retain the 500,000-fight ceiling and the same two fixed looks: alpha .025 per stage, two-sided Bonferroni-adjusted Wilson intervals over the **entire** stage family. File batches do not divide the comparison family.

Force **327 anchors** directly into stage two: the union of all 32 current confirmation recipes, all 62 inventory recipes with a recorded discovery result above 4/8, and all 243 recipes with historical anchor hints. Overlaps count once. Historical outcomes allocate anchors only.

| Allocation | Fixed amount |
| --- | ---: |
| Non-anchor first-stage recipes | 17,494 |
| Fresh first-stage seeds per recipe | 24 |
| First-stage fights | 419,856 |
| Maximum second-stage family, including anchors | 416 |
| Fresh second-stage seeds per selected recipe | 192 |
| Maximum second-stage fights | 79,872 |
| Maximum total actual attempts | **499,728** |
| Combat retries, diagnostic fights and optional extensions | **0** |
| Total execution time cap | **14,400 seconds** |
| Package storage cap | **12 GiB** |

Stage one resolves zero-win recipes: at family size 17,494, 0/24 has upper bound **49.19684%**; 1/24 has upper bound **53.26485%** and advances. All anchors and every unresolved first-stage recipe receive the separate 192-seed sample if their combined count is at most 416. The complete selection and first-evidence hash are saved before stage two. More than 416 yields **Inconclusive**, without dropping teams or adding capacity. A first-stage observed rate above 50% yields **Fail**. Any second-stage observed rate above 50% also yields Fail; uncertainty cannot hide a breach. Pass requires every final upper bound ≤50% and at least one final lower bound ≥10% in the fixed cohort. No pooling between stages or old studies.

The second-stage adjustment uses the complete selected family, conditional on stage one and independent new seeds. At the maximum 416 teams, an illustrative 58/192 has interval **18.87669%–44.60234%**; this arithmetic example is not a measured candidate result. Wilson coverage remains approximate and is not a lifetime guarantee over repeated studies.

Import every integer in every array of the [474,838-reservation ledger](../TestResults/balance/tower-kharad-v13-calibration-20260913/seed-ledger.json), including unused constructor/selection reservations. Freeze 24 + 192 distinct fresh seeds, full exclusions, exact recipe family, anchors, content/settings/execution hashes, source snapshots, captured executable and this plan before combat. The resulting ledger has **475,054** distinct reservations. An interrupted campaign remains incomplete; no automatic retry, budget reset or protocol amendment is authorized by this plan.

## Resource basis and verification

Previous candidate package timestamps measured approximately 267 seconds for 16,384 confirmation fights and 1,265 seconds for 18,432 discovery fights, including differing preparation and artifact work. These are planning observations, not controlled throughput guarantees. The full-family confirmation is expected to take hours; its fixed four-hour cap and 12-GiB storage cap bound that uncertainty. The previous discovery package used about 660 MB for 2,304 evaluations; the full inventory and per-recipe materialization increase overhead. Check free disk before launch and charge every attempted battle in a durable global journal.

Before launch, run repository tests through `build/run-tests.ps1`, including legacy/large-family boundaries, complete-family confidence calculations, capacity stops and both policy versions' cancellation/resume/tamper reconstruction. Build and retain the exact producing binaries and source. Afterward reconstruct all batches with the captured executable under a guard that forbids new combat; independently verify intervals, seeds, family completeness, selection, attempt accounting, preserved inputs and final manifests.

Write a separate result review and application decision. Keep the candidate's previous independent-search reliability **Fail (0/3)** separate from complete-family confirmation; the earlier live-setting **Pass (2/3)** is not candidate evidence. Acceptance here concerns the registered fixed-budget family only. Acquisition, search reliability at the candidate, unsearched recipes and floors 6–11 remain separate work.
