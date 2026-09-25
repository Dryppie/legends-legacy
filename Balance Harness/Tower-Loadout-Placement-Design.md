# Whole-loadout placement proposer design

Current status: **Ready for a native, combat-free preview.** The bounded structural review found 238 legal nonreference recipes. The proposed policy is not implemented or registered in the harness. Its effectiveness remains unmeasured.

Develop one opt-in proposer that permutes complete five-Essence loadouts among the fixed actors of one five-owner subgroup. Keep the benchmark as its only parent and retain the current benchmark-validation selector. The next implementation should establish native parity, deterministic sampling and unchanged legacy behavior before any new scientific admission.

## Why this is the next hypothesis

The completed [90,112-fight diagnostic](Tower-Affinity-Neighborhood-Recognition-Execution.md) retired at its fixed budget: no eligible recipe, 29 below the practical threshold and 12 unresolved. Both distinct-recipe neighborhood means were below the benchmark; the difference between protected and unprotected neighborhood means remained unresolved. Those results do not establish that every recipe is poor, and they do not measure future search-root performance.

The earlier [edit diagnosis](Tower-Affinity-Preservation-Edit-Diagnosis.md) identified a structural cost: every accepted affinity proposal inserted Viper, while successive protection rules shifted which other Essence was removed. Preserving affinity endpoints displaced Pack Howler; subsequently preserving Howler increased Fairy removals in the [allied-action pilot](Tower-Affinity-Allied-Action-Stage-Review.md). These are observed associations, not causal estimates of individual Essence value. Adding another outcome-selected protection rule would continue narrowing that same insertion mechanism.

Placement asks a different question: can the existing successful composition work better when its complete loadouts use different fixed actors and equipment? Each subgroup has a maul, staff, two gauntlet users and a greatsword user. Keeping complete loadouts together preserves their internal Essence combinations and the subgroup's inventory of authored abilities. It does **not** preserve stat scaling, targeting, action order, survival, actual support uptime or combat strength. These interactions are the reason to test placement, rather than a reason to assume it helps.

This is a local improvement hypothesis around the confirmed benchmark, not a general search architecture or a solution to encounter balance. It cannot discover a missing Essence, alter the subgroup composition, change equipment, or introduce fundamentally different teams. Placement diversity must not be reported as composition diversity or independently established viable diversity.

## What is already implemented

The [existing placement operator](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) already swaps one Essence between two actors. It considers up to 32 random attempts, permits crossing subgroup boundaries, and sometimes chooses by a structural core-count heuristic. Simply adding an inventory-preserving single swap would duplicate that capability.

Other existing operations have different semantics. [Cross-character mutation](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs) replaces slots from the allowed pool; whole-character mutation constructs a new loadout. [Character-block mutation](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs) reconstructs two owners, and donor blocks copy the same destination owners from another parent. [Adaptive coordinated proposals](../LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs) remove and refill slots on several actors. They do not enumerate complete loadout permutations within one subgroup.

The new contribution is a bounded whole-loadout permutation neighborhood, with subgroup composition conserved and uniform sampling over distinct recipes. It is not the invention of placement search. Production grouping comes from [WorldTowerPartyRules](../LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs); [TowerBattleRunner](../LL/tools/BalanceHarness/TowerBattleRunner.cs) uses that mapping when materializing each actor.

| Alternative | Decision |
| --- | --- |
| Protect another removed Essence or broaden the affinity whitelist | Defer: still insertion-based, and the observed removals do not justify an outcome-fitted blacklist. |
| Reuse the single-Essence placement operator | Retain as existing capability; it can split loadouts and has a different sampling law. |
| Exchange two whole loadouts | Useful subset, but only 20 distinct recipes here; 12 are already reachable by one Essence swap. |
| Permute all loadouts in one subgroup | Chosen: bounded, preserves complete bundles, includes multi-owner cycles and supplies ample distinct proposals. |
| Permute both subgroups simultaneously or construct new teams | Defer: expands the change radius and search space before testing the smaller mechanism. |
| Fit owner, weapon or Essence scores to the saved outcomes | Reject for this version: confounded development observations do not supply a justified ranking model. |

## Exact proposed behavior

The [machine-readable design](Tower-Loadout-Placement-Design.json) is prospective. Draft policy/export/racing versions are v6/v6/v8; no native code recognizes them yet.

Start from the fixed `96b94357…` benchmark, ten owners and five canonical Essence IDs per owner. Enumerate all 5! source-to-destination bijections separately for owners 1–5 and 6–10. Modify exactly one subgroup. Copy only the Essence-ID list from each source loadout to its destination actor; retain every other scenario, actor, equipment, style and identity field. Preserve canonical Essence ordering as the existing composition-only representation requires. This does not assert that reordering abilities is universally gameplay-neutral.

Validate the parent and every resulting recipe against allowed IDs, case-insensitive family uniqueness, slot counts and owned-copy limits. A whole-loadout bijection conserves all subgroup Essence counts and complete-loadout multisets, so a legal parent stays within the same copy budget. It carries each loadout's internal combinations together; it does not pin them to their original physical owner. Grouping and physical context must remain bound to the captured scenario.

Remove the identity and all three explicit reference IDs. Deduplicate by canonical owner-build identity before sampling and retain every source-to-destination derivation. Identical loadouts can generate many equivalent permutations; derivation count must never increase a recipe's weight. Equal-looking actors remain separate destinations because their physical identities and execution ordering are retained.

Require at least 17 distinct nonreference recipes before any evaluation panel. Otherwise fail preflight; do not refill with another operator, widen to both subgroups, silently shorten a wave or borrow evaluation budget. On this fixed shape there are at most 240 assignments to examine and 238 nonidentity recipes, so exhaustive construction needs no rejection loop.

For each search root, sort distinct recipes by ordinal party ID, shuffle once with a separately namespaced stable root-derived stream in the pinned runtime, then consume the first nine and next eight across the two waves. Each distinct recipe has equal inclusion probability; the subset and order vary by root. Neither outcomes, historical rates, owner preferences nor derivation multiplicity enter generation. Preserve the same three references and existing batch racing and [16-sample nomination / 60-sample validation](../LL/tools/BalanceHarness/TowerBenchmarkValidation.cs). Benchmark fallback remains a provisional output safeguard, not confirmation or evidence of improvement.

The uniform shuffle and larger moves are both part of this proposed policy. A later policy comparison would test that complete package; it would not isolate a causal effect of bundle preservation alone.

## Combat-free census

The [review package](../TestResults/loadout-placement-design-20260924/files.json) binds the previous handoff, seed-free physical teams, captured request and inspected current source hashes. Its [complete catalogue](../TestResults/loadout-placement-design-20260924/catalogue.json) records all distinct owner builds, assignment derivations, physical scenario hashes and edit sizes. No saved combat outcome is an input to enumeration or weighting.

| Structural result | Count |
| --- | ---: |
| Assignments examined, including both identities | 240 |
| Distinct legal nonreference recipes | 238 |
| Recipes in each subgroup | 119 |
| Recipes changing 2 / 3 / 4 / 5 owners | 20 / 40 / 90 / 88 |
| Complete legal same-subgroup single-Essence swap set | 60 |
| Permutation recipes also in that single-swap set | 12 |
| Permutation recipes outside that single-swap set | 226 |
| Overlap with the retired 41-recipe insertion neighborhood | 0 |

The single-swap count describes the complete legal set under this review's subgroup restriction. It is not the output count or performance of the legacy 32-attempt sampler. Likewise, 238 legal recipes establish capacity, not quality or statistical power. Most permutations change four or five owners; keeping bundles intact does not make those changes behaviorally small. All 238 are retained without choosing favorable owner assignments.

## Implementation and subsequent experiment

The next step is a native preview only:

1. Add a small pure catalogue builder and opt-in versioned proposal/export path. Reuse existing legality, canonicalization, hashing and physical-scenario conversion. Add nullable provenance fields only for the new version so legacy serialized bytes remain unchanged.
2. Export the complete catalogue and both proposal waves using already exposed development roots. Check exact parity with all 238 Python recipe IDs and physical scenario hashes, invariants, derivations, deterministic replay and 9+8 uniqueness. Do not allocate roots or invoke combat preparation.
3. Test duplicate-loadout deduplication, tight owned-copy budgets, reference exclusion, cross-subgroup rejection, malformed source maps, underfill-before-panel behavior, cancellation and tampered provenance. Run backend tests through `build/run-tests.ps1` and preserve all existing policy/default behavior. Measure actual preview resource usage before admitting any study.

Only after those checks should a separate matched-budget pilot be frozen. The proposed control is the existing v5 allied-action proposer under the same benchmark-validation selector; returning the fixed benchmark without searching remains an essential absolute comparator. Keep 528 search fights per arm per independent root. Freeze root count, held-out panel size, decision rules, fresh-history checks and measured resource limits in that later design. No pilot sample size, power or runtime claim is admitted here.

Measure the independently evaluated **selected output**, its absolute gain over the benchmark, useful novel improvements and regressions. Use independent search roots as the replication unit for search reliability; many fights or recipes from one root are not substitutes. Any claim from a v5 comparison is relative to that specific control, not proof of superiority over every historical search policy. Repeated benchmark fallback alone does not demonstrate better discovery.

The retired neighborhood diagnostic stays retired. Its outcomes are development evidence. Its 2,048 used seeds and 14,333 unused fresh reservations remain permanently excluded from future fresh allocation. No recipe from it became eligible for confirmation. A favorable new pilot would still require the appropriate separate confirmation before a team recommendation or policy promotion.

## Verification, accounting and changes

All 15 synthetic tests passed, including an independently constructed Cartesian-bijection oracle, duplicate-loadout weighting, subgroup inventory and bundle conservation, owned-copy limits, case-insensitive families, immutable inputs and physical actor-field preservation. The bounded review authenticated all 173 inherited immutable pins. It carries forward the last verified history of 799,177 values across 264 files; it did not rescan that live registry or repeat battle audits.

The review declared 180 seconds / 67,108,864 bytes, fully charged at start including failure. Its measured time before sealing was 0.328 seconds. Cumulative recorded charges are **51,259.661 seconds / 36,344,545,598 bytes**; cumulative declared maxima are **140,160 seconds / 91,486,158,848 bytes**. The full allowance is charged once, without refunding prior work. Ordinary synthetic tests and publication checks are separate engineering work.

Added the [structural analyzer](analysis/loadout-placement-design.py), [its tests](analysis/test-loadout-placement-design.py), this report and the machine-readable design. Added LF rules for those new pinned files. Nine existing status documents change only line three; their historical bodies and all prior sealed evidence remain intact. New retained review and publication-verification packages support the handoff. No C#, gameplay, runtime policy defaults, migrations, application configuration or deployment changed.

Completed commands used the bundled Python interpreter:

```powershell
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-design.py"
python -B -X utf8 "Balance Harness/analysis/loadout-placement-design.py" --output "TestResults/loadout-placement-design-20260924"
```

The review output is single-use and refuses an existing directory. Native/backend tests were not run because this step changes no backend implementation; those are required for the next native preview. No required command remains blocked.
