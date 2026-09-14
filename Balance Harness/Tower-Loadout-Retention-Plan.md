# Fixed-budget loadout retention comparison

Frozen 13 September 2026 before allocation and combat. Target: the offline BalanceHarness optimizer. Game content, Kharad, player budget and the existing 2/3 reliability gate stay fixed. This opt-in experiment does not change the default policy.

## Saved-history evidence

The [preceding comparison](Tower-Generation-Feedback-Review.md) found no discovery, feedback, screening or new-finalist confirmation wins. The [construction audit](../TestResults/balance/tower-loadout-retention-work-20260913/diagnosis.json) reconstructs 682 recorded library hashes across six saved v13 arms, with zero new fights. The two older successful arms first observed wins at evaluated candidates 282 and 256. Their ancestry used loadout distribution and continued mutation of initially zero-win parties. Two independently supported recipes in the older third restart were born at candidates 241 and 258 and initially scored 0/8.

Crucial loadouts in those selected successful lineages were retained; this audit does **not** establish eviction as the cause of failure. Across the six arms, approximately 1,900 distinct ordered loadouts were observed, while only 182–260 entered the 128-place library during refinement. Failed restarts did not simply have fewer distinct beam parents. These are descriptive observations, not causal proof, necessary recipe ingredients or a tuning target. The bounded hypothesis is that retaining structural variety at the reusable character level can make additional constructions reachable while keeping ranked modules available.

## One change

Policy `independent-loadout-retention-v15` pairs unchanged `loadout-composition-joint` with `retained-loadout-composition-joint`. Both evaluate 384 candidates on eight discovery seeds and share the first 96 candidates, v13 random-stream initialization, operators, main/exploration beams, fresh frequency four, ownership rules and final selection.

Only the candidate arm's loadout library changes. Enumerate all distinct ordered character loadouts from completed same-arm independent parties, in complete-party fitness rank and ascending source slot. Resolve duplicates to the first source. Keep the first 64 modules. Fill up to 64 additional places by greedy farthest-first selection: maximize minimum structural distance to the already retained modules. Distance is `(slot count + 1) × number of left-side essences absent on the right + positional mismatches`; legal loadouts have equal size and distinct families, so membership distance is symmetric and takes precedence over ordering. Break ties by original complete-party rank and source slot. Recompute at each existing coordinated-loadout operation, with no additional randomness or combats. Retain fewer than 128 when insufficient distinct modules exist.

No standalone module fitness, known control recipe, confirmation outcome, new operator, special essence list, feedback allocation or parent-beam change is introduced. Existing provenance records retain library hashes and all source modules. This tests a reusable-module library, distinct from the earlier v12 behavior archive of complete parties. The 64/64 split is frozen as an equal ranked/diversity allocation, not tuned against these confirmation trials.

## Selection, controls and decision

Complete all three paired restarts, then independently screen the top 32 from each of six arms on 64 fresh shared trials. Preserve the original top two and select two finalists per arm by screening wins, using original rank for ties. Always screen zero-win arms. Do not pool discovery with screening or confirmation.

Import all **48** complete confirmed recipes from the preceding feedback campaign as external controls, preserving their prior provenance separately. Historical anchor: `team-1abe76ca1891d97a91d484f0a3662048`. Strongest preceding control: `team-a7e5de669c4a17287d84060e8ab6359b`. Neither enters generation.

Confirm every original nominee, screened nominee, control and every discovery/screening recipe observed above 50%, deduplicating exact ordered complete recipes with all origins. Capacity is **80**, increased solely to retain all 48 controls plus up to 24 distinct nominations with room for breaches. Preserve overflow and stop before confirmation; never truncate. Each admitted recipe receives 512 fresh shared confirmation trials.

Use the established joint alpha .025 for the complete rate family and .025 for nine paired comparisons (18 discordance intervals, multiplier 36). A candidate primary passes only when its confirmation rate lower bound is at least 10%, its paired improvement lower bound versus the same-restart screened v13 primary is above zero, and its paired difference lower bound versus the fixed anchor is at least −10 percentage points. Reliability requires **at least two of three** passing restarts. Report strongest-control differences separately. Preserve observed ceiling breaches, ordinary family assessment and joint family assessment separately from optimizer reliability. No automatic promotion.

These approximate Wilson intervals cover this frozen selected family. They do not provide lifetime repeated-study coverage, full generated-family acceptance, acquisition realism or global optimality.

## Fixed execution and preservation

| Phase | Fights |
|---|---:|
| 3 × 2 × 384 × 8 generation | 18,432 |
| 6 × 32 × 64 screening | 12,288 |
| At most 80 × 512 confirmation | 40,960 |
| Hard maximum | **71,680** |

Limits: 5,400 execution seconds, 4 GiB campaign bytes, zero combat retries, no resume, extensions or diagnostic combat replays. Record every start/completion durably. Preserve interrupted runs without restarting. Preparation and completed reconstruction run under a no-combat guard.

Master seed **2026091309**, namespace `tower-loadout-retention-v1`. Exclude every array in the latest ledger: 477,260 distinct prior values, including unused reservations. Allocate 3 generation, 8 discovery, 64 screening and 512 confirmation seeds: **587** new distinct values, **477,847** total. Feedback remains absent; the ledger retains an empty feedback array. Freeze the executable, content, sanitized settings, budget, mechanics, complete input provenance and this plan before starting.

Campaign: `TestResults/balance/tower-loadout-retention-20260913`. Work: `TestResults/balance/tower-loadout-retention-work-20260913`. Retain all evaluated recipes, full ancestry, screens, confirmation builds and independent statistics. Verify both earlier v13 and v14 generation histories without new fights, run relevant backend tests through the repository wrapper, reconstruct the completed campaign with its captured executable, and update active Markdown guides with the measured result. Prior completed evidence packages and historical reviews stay unchanged.
