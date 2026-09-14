# Kharad single-team precision resolution: prospective design

**Design only — no new seeds or fights allocated.** The [complete-family review](Tower-Kharad-Full-Family-Review.md) measured all 17,821 recipes and returned Inconclusive because one adjusted upper bound exceeds 50%. Preserve that completed study and its result.

The next concrete work is a separately frozen **1,000-fight** measurement of `team-db3598435c3754b58e2b6d9c10ff7be2` at the unchanged isolated +16% candidate and fixed budget. No new discovery, tuning grid or full-family resampling is needed by this proposed statistical design.

| Component of the new complete-family decision | Alpha | Family |
| --- | ---: | ---: |
| Existing first-stage bounds, with original selection verified | .025 | 17,494 |
| Existing second-stage bounds, all recomputed more strictly | .0125 | 361 |
| Entire fresh family selected after that tightening | .0125 | 1 |

Independent normal-quantile arithmetic confirms that tightening the entire existing second-stage family leaves **exactly one** unresolved team. The largest retained second-stage upper becomes **49.31504%**; 17 retained teams still have lower bounds ≥10%. Existing resolved first-stage uppers remain **49.19684%**. The total alpha is .05, with approximate Wilson coverage and no lifetime repeated-study guarantee. Historical outcomes are not pooled into the new team’s sample. Its final bound would use only the new 1,000 fights.

For the fresh one-team sample, at most **460 wins out of 1,000** gives a supported ≤50% upper bound under alpha .0125. An illustrative **391/1,000** yields **35.32472%–43.01044%**; this is arithmetic, not a measured result. Preserve every observed >50% from every source stage as a rejection. If the fixed fresh result remains uncertain, report Inconclusive and stop.

Before any fresh combat:

1. Implement and test the separate composite decision contract. It must reconstruct the complete original family and both source stages, verify producing hashes, tighten every second-stage bound before selection, and reject missing/extra/replaced evidence or optional sample extensions. The current two-stage command does not implement this third-stage composite decision.
2. Verify the sealed source protocol and manifest, exact unresolved recipe, candidate content, settings and producing execution. Retain the original Inconclusive report.
3. Import every array in the **475,054-reservation ledger**, then freeze exactly 1,000 disjoint fresh seeds, the complete required family, all source-bound allocations, the sole fresh recipe and independent fight/time/storage/retry limits in a new package. No budgets or start markers may be reset in the completed package.
4. Measure exactly the frozen sample, reconstruct it without combat, evaluate the complete composite family, and record the new application decision. Keep search reliability **Fail 0/3**, acquisition and later-floor readiness separate.

Read the complete [zero-combat arithmetic](../TestResults/balance/tower-kharad-full-family-work-20260913/precision-design.json), [source assessment](../TestResults/balance/tower-kharad-full-family-20260913/campaign/assessment.json), [exact saved recipe](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-db3598435c3754b58e2b6d9c10ff7be2.json), and [authoritative seed ledger](../TestResults/balance/tower-kharad-full-family-20260913/seed-ledger.json). Time/storage limits, producing implementation and the new schedule are intentionally not frozen by this design.
