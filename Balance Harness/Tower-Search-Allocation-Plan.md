# Fixed-budget v13 depth versus isolated searches

Frozen 13 September 2026 before seed allocation or combat. Target: the offline BalanceHarness optimizer. Kharad stays at Health **3.5366243328** / Power **4.4702934848**, with the same level-40, tier-1, rank-2, ten-character/five-Essence player budget, fixed gear and hypothetical full ownership. No default or game-content promotion.

## Question and allocation

The [lineage comparison](Tower-Party-Lineages-Review.md) failed reliability 0/3. Alternative founders remained available and supplied modules, but no new nominee established supported viability. Unchanged v13's first discovery win appeared at candidate 307; earlier successful searches also needed late refinement. These observations motivate testing how to allocate search effort, not a claim that depth or isolated starts will succeed.

Policy `independent-search-allocation-v17`, namespace `tower-search-allocation-v1`, compares six allocation units across three paired restarts:

| Comparison unit | Component searches | Evaluations | Discovery fights |
| --- | --- | ---: | ---: |
| `loadout-composition-joint` (comparator) | One deep v13 run | 768 | 6,144 |
| `isolated-loadout-composition-joint` (candidate) | `isolated-a-loadout-composition-joint` and `isolated-b-loadout-composition-joint` | 384 + 384 | 6,144 |

Execute nine component searches in unit-seed order, deep then isolated A then isolated B. Each starts with empty parents, measurements, exploration and module library. No component receives another component's recipes, outcomes, library or random state during generation. Both approaches use the existing v13 operators, ranking, four-place main beam, four-place exploration, ranked 128-module library and fresh frequency four. No v14 feedback, v15 module reserve, v16 founder preservation or external control-derived construction.

Preserve v13's initial-population formula: one quarter of the component candidate budget. The deep run therefore starts with 192 fresh candidates; the two isolated runs start with 96 each. Both allocate 192 fresh initial candidates in total. This is a budget comparison using v13's existing formula, not an extension that preserves an earlier 384-candidate trajectory. Maximum proposal attempts are 8,192 for the deep component and 4,096 for each isolated component, keeping the per-unit attempt cap equal. Reject exhausted/incomplete components before grouped selection. Rejected proposals do not consume combat; all accepted component evaluations remain charged, including duplicate recipes across components.

Each paired restart has one fresh root generation seed. Initialize `Random` through the existing `StableRandom.Seed("independent-teams-v1", streamId)` rule. Deep retains `coverage-joint-<seed>`; isolated A and B use `tower-search-allocation-v1-isolated-a-<seed>` and `tower-search-allocation-v1-isolated-b-<seed>`. These separate named streams use no shared mutable state. Full provenance retains component method and root seed. Freeze the three root seeds before execution; do not choose streams from outcomes.

## Grouped selection and complete confirmation

Only after all nine components complete, aggregate each isolated pair into one comparison unit. Rank complete recipes with v13's existing lexicographic fitness and ordinal party-ID tie break. Deduplicate exact normalized ordered recipes within a unit for selection; preserve both charged evaluations, component ranks, party IDs and proposal IDs. Repeated recipes on the shared eight-seed discovery schedule must have identical complete scoring evidence; conflicting duplicates invalidate the study. Freeze the complete grouped ranking and all origins, the original top two, and the top 32 distinct recipes. No per-component screening quotas or extra finalists.

Each of six units receives 32 × 64 fresh shared screening trials, including zero-win units. Select the two most successful screened recipes, tied by frozen aggregate discovery rank. Screening never pools discovery outcomes. Keep both original and screened nominees.

Register all **74** complete recipes from the preceding lineage comparison as external controls, preserving their original provenance separately. Historical anchor: `team-1abe76ca1891d97a91d484f0a3662048`. Fixed strongest preceding control: `team-a7e5de669c4a17287d84060e8ab6359b` (145/512 in that comparison). Controls never enter independent generation, random-stream choice, parents or module rank.

The full confirmation family includes all controls, every unit's original/screened nominees and every discovery/screening recipe observed above 50%, including candidates outside the screened shortlist. Exact-recipe deduplication preserves every group and component origin. Capacity **112** covers 74 controls plus up to 24 distinct nominees and 14 additional breach recipes. Preserve overflow and stop before confirmation; never truncate, extend or increase capacity after outcomes. Every admitted recipe receives all **512 fresh shared confirmation trials**.

## Decision rule

The candidate is the isolated pair; the comparator is the single deep run. Each of the three paired allocation units contributes one candidate primary. A restart passes only when all three conditions hold: its adjusted rate lower bound is ≥10%; its paired difference lower bound against the screened deep primary is >0; and its paired difference lower bound against the fixed historical anchor is ≥−10 percentage points. Reliability requires **at least two of three** passing restarts. Treating the six isolated components as six reliability restarts is prohibited. Deep viability, secondaries and strong-control comparisons remain separately reported and cannot replace the primary or gate.

Keep joint alpha .025 across the entire confirmation rate family and .025 across nine paired differences (eighteen discordance intervals, multiplier 36). Report ordinary/joint family acceptance and observed ceiling breaches separately from optimizer reliability. Approximate Wilson intervals do not establish lifetime repeated-study coverage, acceptance of the complete discovered family, practical acquisition or global optimality. A Pass makes the candidate eligible for review; it does not promote a default automatically.

## Frozen resources and preservation

| Phase | Fights |
| --- | ---: |
| Three paired 768 × 8 allocation units | 36,864 |
| Six 32 × 64 screens | 12,288 |
| At most 112 × 512 confirmation trials | 57,344 |
| Hard maximum | **106,496** |

The new version alone may exceed the older 100,000-fight discovery-definition ceiling; historical versions retain their existing limit. Set **5,400 execution seconds**, **4 GiB campaign storage**, zero retries, no resume, no extensions and no diagnostic combat replays. Prior v16 phase measurements were 1,328.74 seconds for half this generation allocation, 251.64 seconds for the same screening allocation, and 803.83 seconds for 37,888 confirmation fights. Linear planning estimates place this larger maximum around 4,126 seconds; this is a resource estimate, not a guaranteed runtime. Enforce actual limits and preserve an interruption without restarting.

Master seed **2026091320**. Exclude every array in the latest **478,434-reservation** ledger, including unused values. Allocate 3 root generation, 8 discovery, 64 screening and 512 confirmation values: **587 new reservations**, **479,021 total**. No feedback seeds. Internal deterministic random-stream derivation is separate from root/combat seed reservations. Freeze all schedules, settings, content, player budget, mechanics, executable, source provenance and this plan before the first campaign fight.

Campaign: `TestResults/balance/tower-search-allocation-20260913`. Work: `TestResults/balance/tower-search-allocation-work-20260913`. Run tests through `build/run-tests.ps1`, reconstruct earlier v13/v14/v15/v16 campaigns without combat, verify preparation, execute once, reconstruct the complete captured campaign, independently recompute grouped ranks, duplicate origins, statistical decisions and accounting, export every complete confirmed recipe, and update active Markdown. Preserve prior source/content state, frozen plans, reviews and completed packages. No migrations, repository configuration changes, deployment or external environment changes.
