# Coordinated complete-loadout search

13 September 2026. Authorized by the user's “Please proceed” following the proposed implementation and controlled benchmark. Scope: offline BalanceHarness only.

## Hypothesis and implementation

The completed depth/behavior comparison found improvements over the small v4 search but neither method met independent-search reliability or fixed-anchor competitiveness. This study tests whether coordinated changes to complete ordered character loadouts improve discovery. It does not assume a missing provider category or a causal explanation for the earlier failures.

Opt-in `independent-loadout-composition-v13` compares `coverage-deep-joint` with `loadout-composition-joint`. The comparator reproduces unchanged v4 at 384 candidates. Both arms begin with the same v4 random stream and initial construction. The new arm interleaves the ten v4 refinement operators with ten coordinated moves: distribute, compose, refine, place, distribute, compose, refine, place, distribute, compose. Initial construction count, one-in-four fresh proposals, parent selection, whole-party fitness, discovery sampling and final nomination remain unchanged. Defaults stay unchanged.

The library retains at most 128 distinct ordered character loadouts. Traverse measured parties by existing discovery rank, then their source slots in ascending order; retain the first source of each ordered loadout. This is a source-party ranking rule, not a claim of standalone character strength. Every library entry comes from a completed generated proposal in the current arm. Its proposal ID, source slot, ordered Essences, chosen destination slots and complete library hash are recorded with each coordinated proposal.

- **Distribute:** copy one library loadout to a random nonempty subset of party positions. Other positions retain their parent loadouts.
- **Compose:** choose between one and the party-size number of distinct library loadouts, assign a random positive count to each, and distribute them across shuffled positions. Every legal count partition is reachable; no saved reference counts are used.
- **Refine:** choose a parent character and change one Essence, two Essences or Essence order; apply that change to every exactly matching ordered loadout in the parent.
- **Place:** exchange two complete ordered loadouts, preserving the complete party inventory.

All complete proposals pass existing slot, family and owned-copy checks. Invalid and duplicate proposals consume proposal attempts but no fights; there are no silent repairs. Uniform v4 construction remains reachable. References, historical recipes/counts/ancestry, and held-out outcomes cannot enter generation. The library is reconstructed deterministically from recorded discovery evidence.

## Fixed comparison

Three paired restarts; 384 evaluated candidates per arm; eight common discovery seeds. Two finalists per arm are frozen by original discovery rank, plus all six external controls, with exact-recipe deduplication preserving every source association. The fixed anchor remains `team-1abe76ca1891d97a91d484f0a3662048`. Kharad stays at Health **3.04881408** / Power **3.85370128**. Ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences and exact existing gear; hypothetical ownership remains explicit.

Retain eight discovery samples to isolate proposal representation. The earlier discovery/validation drop motivates a separate sampling hypothesis; this experiment does not change sampling or attribute generalization changes to sample count.

Measure the complete selected family on 64 fresh screening seeds and 256 separate fresh validation seeds. Screening is descriptive; **always complete validation**, including zero-win finalists. No primary substitution, adaptive stopping, fresh-seed retry or validation-reserve reassignment.

Maximum combat allocation: `2 × 3 × 384 × 8 + 18 × 64 + 18 × 256 + 8 = 24,200`. The final eight are four predeclared historical parity checks and first-screen-seed replays of the three new primaries and fixed anchor. Exact deduplication may reduce actual fights. Maximum proposals: 8,192 per arm. Execution cap: **2,700 seconds**; retained package cap: **2 GiB**. Start/completion attempts are durably journaled, retries are zero, and interruption leaves an incomplete sealed run rather than permitting implicit continuation. Reconstruction runs zero fights and is timed separately.

Freeze a fresh output directory, producing executable/content/settings hashes, this plan and a complete seed ledger before combat. Exclude all **472,525** prior reservations, including unused and constructor arrays. Existing sealed reviews/packages are preserved.

## Decision

Joint alpha .05: .025 across the complete rate family and .025 across six paired comparisons (12 discordance intervals). Each new primary must have an adjusted rate lower bound ≥10%, paired improvement lower bound over its same-restart v4 primary >0, and paired difference lower bound against the fixed anchor ≥−10 percentage points. Require at least two of three qualifying restarts. Keep all three components visible when the combined rule fails.

The ordinary 10–50% evaluator and joint family assessment remain separate from search reliability. Every observed >50% remains a ceiling breach, even with an interval crossing 50%; earlier breaches remain unresolved. Neither technical completion nor one successful build establishes calibration, practical acquisition, near-optimality or floors 6–11 readiness.

The result will be saved as complete recipes, per-trial evidence, proposal ancestry, verified summary and a separate completion review. Promote neither defaults nor catalogs automatically, and do not add another policy or campaign after a failure without a new decision.
