# Kharad finalist rescreen: implementation and comparison proposal

Prepared 13 September 2026 from the [completed saved-data diagnosis](Tower-Kharad-Search-Diagnosis-Review.md). Target: the offline `LL/tools/BalanceHarness` selection workflow. **Selected for implementation; not implemented or executed.** This document allocates no fights or seeds. Freeze a separate execution protocol after implementation and verification.

## Problem and one change

Current v13 discovery evaluates 384 candidates per arm on eight shared seeds, then immediately nominates the two highest-ranked candidates. In the applied-setting archive, only 17 of 2,304 evaluations recorded a win, each exactly 1/8. Guardian-health tie-breaking chose primaries that later underperformed unselected candidates on the same 192-trial schedule. Neither simply promoting a saved secondary nor declaring the construction problem solved is supported.

Add one **independent finalist rescreen** for the v13 loadout-composition arm:

1. Complete the unchanged 384-candidate generation and eight-seed discovery. Save the original primary and secondary and the complete original ranking.
2. Freeze the exact top **32** distinct recipes from that arm, in the existing ranking order, before any rescreen outcome. Use only that restart's independently generated candidates.
3. Measure every shortlisted recipe on exactly **64 fresh shared seeds**, disjoint from discovery, generation, confirmation and every historical reservation. Complete the entire matrix, including zero-win candidates.
4. Rank by rescreen victories descending, then original discovery rank ascending. Use rescreen outcomes alone for the victory count; do not pool the eight earlier trials. The first two become the rescreened primary and secondary.
5. Freeze these nominations and the full confirmation family before opening any fresh confirmation outcome. A zero-win rescreen falls back deterministically to original discovery order; it does not skip confirmation or claim success.

The unchanged deeper-v4 arm retains its original discovery-selected primary and secondary. Generation, random streams, parent selection, loadout library, operators, default policy, player budget and Kharad content remain fixed. This isolates the effect of final selection, including its additional compute cost.

The width of 32 is a bounded engineering choice informed by this post-hoc diagnosis, **not an independently validated optimum**. It includes the observed rank-three-to-five misses, all 17 one-win candidates and some zero-win alternatives in these archives. It misses some later low-rate positives. No sweep over widths, seed counts or tie-breakers is part of the proposed comparison.

## Implementation boundary

Follow the existing compact campaign and saved-recipe patterns. Add an explicit opt-in workflow around final selection; do not change `TowerBossGeneration.Rank`, the v13 generator, `TowerSearchBenchmark.Select`, or existing commands' behavior.

- A small `TowerFinalistRescreen` selection component should accept a complete discovery result and its declared definition, freeze the top-32 recipe/ancestry/original-rank records, and select two only from a complete, matching 64-trial evidence matrix. Keep imported controls, held-out outcomes and previous winning recipes outside this API's generation/shortlist inputs.
- An accompanying runner should use existing compact execution and verification for the three rescreens. Persist the frozen shortlist, original nominations, source/content/settings/execution identities, evidence, selected nominations, and original-to-selected mapping. Treat partial or mismatched evidence as an explicit stop.
- The comparison driver should compose unchanged discovery, the rescreen, and fresh confirmation. Reuse existing deterministic recipe normalization and exact-duplicate provenance preservation. The existing benchmark's 24-recipe selection limit and fixed 256-trial quality helper do **not** implement this proposed larger 512-trial comparison; add a separately named evaluator/driver with explicit limits, following the completed calibration's arithmetic.
- Retain every evaluated recipe and both original and new nominations, including failures. Historical recipes and IDs in the diagnosis explain findings; none becomes a constructor template, feature rule, parent or default catalog entry.

Required verification covers unchanged archived v13 generation/ranking, exact top-32 membership and order, complete seed/recipe identity checks, missing/duplicate/wrong-stage evidence rejection, tied and zero-win rescreens, preservation of original nominations, confirmation-family overflow, paired statistics and interruption accounting. Run meaningful backend tests through `build/run-tests.ps1`; fixture combats are separate from campaign accounting. Keep executable reconstruction under a no-combat guard.

## Proposed frozen comparison

Use three new paired generation restarts, both unchanged methods, 384 candidates per arm and eight discovery seeds. Rescreen only the three v13 arms. All methods retain the fixed floor-5 budget: ten level-40, tier-1, rank-2 Standard characters; five level-1 unascended/unevolved Essences; exact gear; no styles/contributions; all 80 eligible Essences under hypothetical ownership. Local Kharad stays at **Health 3.5366243328 / Power 4.4702934848**.

The confirmation family is the exact-recipe union of:

- Original primary and secondary from all six discovery arms: at most 12.
- Rescreened primary and secondary from all three v13 arms: at most 6 additional.
- The same 20 saved controls from the calibration, freshly measured and excluded from independent search.
- Every newly observed discovery or rescreen recipe above 50% wins, even if unselected.

Without additional breach nominations this is at most **38** recipes. Reserve capacity for **64**, retaining all source associations on exact duplicates. If the union exceeds 64, preserve the complete list and stop with unresolved coverage; never truncate it. Every included recipe receives the same **512 untouched confirmation seeds**. Do not promote a secondary or other recipe after inspecting those outcomes.

| Phase | Maximum fights |
| --- | ---: |
| Discovery: 2 methods × 3 restarts × 384 candidates × 8 seeds | 18,432 |
| Rescreen: 3 v13 restarts × 32 candidates × 64 seeds | 6,144 |
| Confirmation: at most 64 recipes × 512 seeds | 32,768 |
| **Total** | **57,344** |

At 38 distinct confirmation recipes with no extra breach nominations, the total is **44,032**. These totals include no setting screen or diagnostic replay. Proposed caps: **5,400 execution seconds**, **4 GiB**, **zero combat retries, resume or adaptive extensions**. Count all starts durably, including attempts interrupted before completion. Freeze inputs, producing identities, package inventory and resource accounting before the first fight. A technical interruption preserves evidence and requires a separately specified recovery decision.

Exclude every array in the current **476,054-reservation precision ledger**. The proposed schedules require three generation integers, eight discovery seeds, 64 rescreen seeds and 512 confirmation seeds: **587 disjoint new reservations**, none allocated by this document. The rescreen seeds are shared across shortlisted recipes and restarts; confirmation seeds are independently shared across the complete frozen family. Do not reuse the calibration's previously unused selection reservations.

## Decisions fixed before execution

Keep the original search-reliability requirements. A rescreened v13 primary must have adjusted viability lower bound ≥10%, supported paired improvement over the same-restart original deeper-v4 primary, and paired lower difference against the unchanged fixed anchor ≥−10 percentage points. At least **two of three restarts** must pass all three.

Measure the effect of the extra rescreen separately: compare each rescreened v13 primary against its **same-restart original v13 primary** on the common confirmation seeds. Define a separate rescreen-benefit gate requiring a supported positive paired difference in at least **two of three** restarts. If the same recipe remains primary, its paired difference is zero. Adopt the selection change only if both the reliability and benefit gates pass; otherwise preserve the result without promotion or a follow-on allocation.

For the joint analysis, allocate alpha **.025 across all distinct confirmation rates** and **.025 across nine paired comparisons**: three against deeper-v4, three against the fixed anchor, three against original v13. Each paired difference uses two discordance intervals, so the existing `.05` Wilson interface needs a rate-family multiplier of `2 × family size` and a discordance multiplier of **36**. Keep approximate-interval and repeated-study limitations explicit. Report ordinary nominated-family balance separately using alpha .05 across the entire nominated rate family. No samples are pooled between phases.

The fixed anchor currently has zero observed wins at the applied setting, so anchor recovery alone is uninformative about competitive strength. Preserve its identity and report the viability and comparator components separately. Do not replace it with a stronger reference after outcomes.

This experiment can establish a scoped selection improvement or fail cleanly. It cannot establish near-optimal construction, certify all new unshortlisted recipes, justify another boss adjustment, or resolve practical ownership and floors 6–11. Late-emerging candidates and the third restart's absence of observed wins leave search depth/construction as a distinct hypothesis if final selection proves insufficient.
