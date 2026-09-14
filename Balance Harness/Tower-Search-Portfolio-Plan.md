# Equal-budget search portfolio experiment

Declared **14 September 2026**, before seed reservation or combat. User authorization covers implementation, verification and one bounded offline BalanceHarness experiment. Readiness is recorded separately before preparation. Policy: `independent-search-portfolio-v19` / `tower-search-portfolio-v1`. The master seed is **2026091402**. No historical study is resumed.

## Question

Can a fixed combination of deep search and the v18 isolated pair improve independent discovery reliability over an equally funded deeper search? In the [completed v18 experiment](Tower-Late-Allocation-Review.md), the methods produced viable builds on different roots. That motivates this prospective comparison; choosing their best confirmation outcomes retrospectively does not establish a combined-policy result.

## Equal effort and independent state

| Unit per root | Component evaluations | Initial fresh evaluations | Proposal limit |
| --- | --- | ---: | ---: |
| Comparator | One deep search: 1,536 | 384 | 16,384 |
| Portfolio | Deep: 768 | 192 | 8,192 |
| Portfolio | Isolated A/B: 256 each, then another 256 to the better component | 96 each | 4,096 each |
| **Portfolio total** | **1,536** | **384** | **16,384** |

Use three fresh paired roots. Every accepted evaluation receives the same eight fresh discovery trials. Preserve v13 operators, top-four elite and four exploration parents, the ranked 128-module library and every-fourth fresh proposal. Each of the four components has its own random state, proposal counter, parents and library. No saved recipes or controls enter generation. Duplicate recipes across components remain separately charged, with all origins retained.

The comparator uses `coverage-joint-<root>`. The portfolio's 768-candidate deep component uses the distinct `tower-search-portfolio-v1-deep-<root>` stream. Isolated A/B retain `tower-search-allocation-v1-isolated-a-<root>` and `tower-search-allocation-v1-isolated-b-<root>`. All stream identifiers are hashed under `independent-teams-v1`. The comparator's 384-initial horizon and the portfolio deep component's 192-initial horizon are deliberate; neither is a continuation of an archived search.

Both isolated prefixes must complete 256 evaluations. Rank their best measurements by discovery win rate descending, guardian health ascending, survival descending, victory duration ascending, then ordinal party ID; a complete tie prefers A. Record both full prefix rankings and attempt counts. Continue the chosen component's existing state to 512; the loser stays at 256. Initial fresh count remains 96 regardless of final allocation. No reset, transferred proposal allowance, cross-component parent/module sharing or allocation based on screening outcomes.

## Selection, controls and confirmation

After complete generation, merge the portfolio's three components by complete ordered recipe, retaining every origin and evaluation charge. Rank by the existing discovery ordering. Identical recipes must have identical discovery outcomes on the shared schedule. Each unit produces one top-32 shortlist per root: **six lists total**. Screen every listed recipe on **64 fresh shared trials**, including zero-win lists. Select the top two by screening wins, with ties resolved by original discovery rank. Preserve the original top two as well. All nominations freeze before confirmation.

Import **all 112 controls** from the [complete v18 family](../TestResults/balance/tower-late-allocation-work-20260914/family.json). Keep anchor `team-1abe76ca1891d97a91d484f0a3662048` and stronger prior control `team-a7e5de669c4a17287d84060e8ab6359b` fixed. Confirmation includes every control, original nominee, screened nominee and required discovery/screening rate above 50%. Deduplicate complete recipes, preserving origins.

Capacity is **144**: the ordinary envelope is at most 112 + 24 = 136 recipes, leaving eight cells before additional breaches. If the complete required family exceeds capacity, preserve it and stop as capacity-exceeded without partial confirmation. Otherwise every family member receives **512 fresh shared trials**. No pooling historical outcomes, replacing nominees after confirmation or dropping unsuccessful cells.

The reliability gate is unchanged: portfolio primary rate lower bound >= .10, paired-difference lower bound > 0 against the matching 1,536-candidate deep primary, and anchor-difference lower bound >= -.10, on **at least two of three** roots. Joint alpha splits .025 across all confirmation rates and .025 across nine paired differences (18 discordance intervals). Report stronger-control comparisons and ordinary/joint family acceptance separately. The trial family is scoped to this experiment; no repeated-study, near-optimality, acquisition or full generated-family claim follows.

## Frozen resource envelope

| Phase | Maximum fights |
| --- | ---: |
| Discovery: (1,536 + 1,536) × 3 × 8 | 73,728 |
| Screening: 6 × 32 × 64 | 12,288 |
| Confirmation: 144 × 512 | 73,728 |
| **Total** | **159,744** |

Binding limits: **21,600 execution seconds (six hours), 8 GiB, zero combat retries, no resume and no cap extensions**. Preserve every started/completed attempt and all reserved seeds on failure or interruption. Read-only verification after execution is separately timed. Keep the existing 32-report chunks, dense confirmation batches, complete storage checks and writer/integrity guards.

V18 took 4,414.55 seconds for 36,864 discovery fights and 6,229.40 seconds overall, retaining 2,495,502,622 bytes. Doubling discovery can cost more than twice as much because growing archives are repeatedly scanned and generation checkpoints are serialized. A rough two-to-four-times discovery estimate is 2.45–4.91 hours, before roughly half an hour of later phases and uncertain overhead. Plan for roughly **four to six hours**; the six-hour cap is binding even if this estimate proves low. Eight GiB allows substantial storage headroom over the prior 2.32 GiB campaign without changing historical limits. No performance rewrite is included in this policy comparison.

Use every array in the [480,120-reservation ledger](../TestResults/balance/tower-late-allocation-20260914/seed-ledger.json), and any intervening ledger, as exclusions. Preparation reserves exactly 3 roots + 8 discovery + 64 screening + 512 confirmation = **587 fresh values**, for **480,707** total if no intervening reservations occur. Freeze their actual values, the full plan, definition, controls, content, settings and producing executable before execution. No separate diagnostic fights are budgeted.

## Readiness and completion

Register larger candidate, proposal, fight, storage and time limits only for v19. Before preparation, tests through `build/run-tests.ps1` must cover equal effort, separate state, unchanged isolated trajectories, both allocation choices, interrupted components, complete grouped rankings and duplicate charges, all controls, nomination and evidence integrity, overflow, the numerical gate and historical limits. Replay all six saved v13–v18 studies using saved measurements with zero combat and require exact complete-generation and shortlist equality. Verify unchanged gameplay assemblies, content and settings. Run zero-combat checks of the prepared package and rejection of modified inputs/caps, omitted controls, extra files and repeated execution.

Execute once after readiness and protocol freeze. Complete saved-report reconstruction, independent statistics, full recipe exports, source preservation and active Markdown updates for any outcome. Passing permits a scoped reliability finding; it does not automatically promote the default optimizer or catalog. Failure keeps the policy experimental. An incomplete run remains unresolved.

Kharad remains Health **3.5366243328** / Power **4.4702934848**. No gameplay content, configuration, migration or deployment changes. Practical acquisition, strongest-control coverage and floors 6–11 remain separate milestones.
