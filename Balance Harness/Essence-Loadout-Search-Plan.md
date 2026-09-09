# Finding strong Essence loadouts — implementation plan

Status: proposed implementation; no optimizer or new measurements implemented by this document. Written 9 September 2026. Target: the offline `LL/tools/BalanceHarness` and its local Tower Lab dashboard. Tower is the first evaluation context; later idle, dungeon and PvP adapters must establish their own production parity and rankings.

## Intended outcome

Given a character's level, equipment, available Essences and intended party/encounters, find several demonstrably strong legal loadouts, explain their tradeoffs, and make them reproducible. Return **best found within the recorded search budget**, with uncertainty and alternatives. Reserve an optimality claim for a completely enumerated, bounded search space; even exhaustive enumeration has simulation uncertainty.

The output is a loadout recommendation for a stated context, not a universal Essence tier list. A healer's best loadout depends on the allies it keeps alive; a damage dealer may benefit from another character supplying debuffs. High personal damage or healing alone cannot establish a good character build. Roles are search hints and reporting labels, not restrictions on player classes.

The end-to-end flow is:

1. Set character, ownership and progression constraints.
2. Generate varied legal whole-loadout candidates from current content.
3. Search character alternatives in real party contexts.
4. Search how those alternatives combine into parties.
5. Freeze finalists and confirm them on unused combat seeds.
6. Explain results, export recipes and retain verified replay.

## Starting evidence and constraints

The [first bounded search](Tower-Essence-Search-Review.md) tested combinations of only four authored substitutions. Its leading generalist won 330/450 confirmation fights versus 218/450 for the control at unchanged budgets. This establishes the value of investigating Essence choices, not the quality ceiling of the full catalog. Preserve that control, candidate 05 and all original evidence as versioned comparison inputs; do not overwrite their recipes or retrospectively change their results.

The inspected catalog contains **80 definitions from 77 source monster IDs**. There are 1,581,580 unordered four-definition selections before family restrictions, and over a trillion ten-definition selections. A full party multiplies the search space further. Therefore use bounded simulation-guided search; brute force is useful only for small validation pools.

Preserve the user's progression requirements:

- Support **4, 5, 6, 7, 8, 9 and 10 equipped Essences per character** and all **15 released Tower floors**.
- Floor 1: four Essences, Uncommon equipment, Standard/Fine quality, rank 1–2. Compare the four quality/rank combinations separately; level 30 and tier 1 remain current modeling assumptions.
- Floor 10: around six Essences. Its equipment budget remains provisional.
- Keep the current all-floor progression curve as one explicit cohort. Also retain the existing level-90 controlled slot-count cohort for isolating loadout changes. Never merge their scores into one progression claim.
- Production RequiredSlots and legal slot unlocks apply. Different gear, level, ownership or Essence training budgets produce separate comparisons.
- No starter 50–90% target, boss tuning, baseline promotion or acquisition assumption is introduced automatically. Phase 2 integration remains deferred.

## 1. Define the search contract and candidate identity

Add a versioned search definition with these inputs:

| Input | Required meaning |
| --- | --- |
| Character budget | Level, full equipment recipe, attributes and fixed equipment/Combat Style state where supported |
| Essence budget | Equipped count, allowed definitions, per-Essence level/ascension/evolution, optional pinned selections |
| Ownership | Explicit owned/allowed pool; shared party inventory constraints if supplied |
| Context | Party template, character position(s), scouting state, floors and fixed encounter weights |
| Search controls | Candidate, generation, battle, time, memory and worker limits; algorithm version |
| Randomness | Separate candidate-generation, discovery-combat and final-confirmation seeds |
| Evaluation | Primary objective, tie-breaks, shortlist policy, confirmation samples and reporting rules |

Default to the existing level-1/unascended Essence budget and fixed equipment. The first implementation can search a declared hypothetical ownership pool; label it explicitly. An owned-only search must respect each character's supplied inventory. Acquisition-time or dust-cost optimization comes later after real cost models exist. Do not silently upgrade Essences or add styles unsupported by the Tower adapter.

Use the production reference factory and preparation path to validate unknown IDs, unlocked slots, distinct source families, item rules and progression. Audit parity with normal saved-loadout validation, including source variants. Save rejected candidates with actionable reasons.

**Audit ordering before deduplication.** Normal loadouts have slot indices; the reference factory gives Essence instances index-based identities. Establish with independent normal-path tests whether slot/ability order changes execution, tie-breaking or passive effects. If order matters, preserve and search an ordered vector. If equivalent, canonicalize only after proving equivalence. Keep character identity fixed across alternative builds where production semantics permit it, so recipe labels do not accidentally become another experimental variable.

Completion: legal candidates can be reproduced from a saved contract; changing a budget/content/order changes the appropriate identity; invalid recipes fail before combat. Every supplied slot count and floor can be materialized legally or has an explicit rejection, never silent omission.

## 2. Build an inspectable catalog of mechanics

Read current Essence definitions and their resolved active/passive abilities, attribute bonuses and progression effects. Describe damage scaling/type, targeting, cooldowns, healing/barriers, status application/consumption, mitigation, threat, summons, triggers, stack limits and ally/enemy conditions. Include relevant equipment interactions.

The inspected Essence entries have no populated tags, so do not assume existing tags provide a usable synergy catalog. Derive metadata from structured production effects where possible. Keep small, versioned overrides for behavior implemented outside those structures, with source references and an explicit unknown classification. Human-readable descriptions and name matching are insufficient as the sole source of mechanics.

Use this metadata to propose combinations such as status enabler + consumer, Power scaling + healing, protection + fragile support, or coordinated attacks + on-hit effects. It is a candidate-generation aid, **not a hand-written combat simulator or an Essence power score**. Unknown or unusual mechanics remain eligible for unbiased exploration rather than being filtered out as weak.

Completion: each available definition resolves or is explicitly rejected; metadata can be audited against source effects; catalog changes invalidate derived metadata. Keep the implementation data-driven and small enough to maintain without adding logic for every individual Essence.

## 3. Search whole character loadouts in party context

Implement a deterministic search that keeps several promising and different candidates at each step, with multiple starting points. Start with the following candidate sources:

- Existing authored role recipes, the unchanged control and the previous strong candidate.
- Mechanically coherent alternatives proposed from step 2.
- Random legal whole loadouts sampled across the declared pool, with the sampling distribution recorded.

For each context, evaluate complete parties through `TowerBattleRunner`/`TowerBenchmark`; replace the character under investigation while keeping the other participants and budgets fixed. Use at least two declared ally contexts before calling a character loadout general-purpose: for example, the original balanced allies and a previously fixed alternative roster. Keep their identities frozen for that experiment. A specialist tied to one party context is still a valid result.

Generate one-Essence substitutions for local improvement. Also generate bounded two-Essence substitutions and fresh whole-loadout restarts: some useful combinations need both an enabler and a payoff and will never be found by accepting only individually improving swaps. Preserve structurally different candidates so the search does not collapse immediately onto one family of near-duplicates. For larger slot counts, seed from lower-count results **and** independent fresh builds; a good ten-slot loadout need not contain the best four-slot loadout.

Use equal paired seed batches for candidates competing in the same selection round. A cheap initial screen may use few trials, followed by more discovery trials for survivors. Do not rank candidates with unequal precision as though their raw win rates were equally reliable. Record every evaluated candidate, parent/mutation, seed batch, rejection, elimination and simulation cost. Cache only exact matches of content, executable, materialized party, rules and seed; preserve deterministic scheduling with isolated combat state per worker.

Primary ranking is the party's clear performance on the predeclared encounters. Retain per-floor scores and uncertainty. Survival, guardian health and duration are diagnostics or declared discovery tie-breaks. When every candidate loses, those signals can guide exploration but cannot turn losses into successful builds. Keep floor specialists alongside generalists; do not invent a character score by summing damage, healing and mitigation using arbitrary weights.

Completion: the search escapes a test case where a two-slot combination is needed, retains diversity, stops at its budget, and reproduces its selection from the search log. Compare it against uniform legal random sampling and the existing authored search at equal actual simulation budgets across multiple search seeds. If it does not find better results or reach comparable results more cheaply, revise the method before adding complexity.

## 4. Search party combinations and robustness

Keep a small diverse shortlist per character/role and combine those loadouts in full legal parties. First hold the role counts and equipment fixed to isolate Essence choices. Search joint changes across two characters as well as individual changes, and periodically revisit character loadouts in improved ally contexts. Version each context update; never compare results from different allies as the same character experiment.

Allow the two Strikers to choose different loadouts. Test redundant buffs/statuses, conflicting consumption, coverage gaps, protection and sustain versus damage. Start with the existing repeated five-character cells for 10-/15-character floors, then permit bounded variations between cells. Clearly report whether a result assumes identical repeated cells or individually optimized members. Later, vary composition in a separate experiment at the same total budget.

Produce separate leaderboards for:

- General-purpose loadouts under a fixed, declared mix of encounters and allies.
- Floor/boss specialists and the encounters where they lose value.
- The same character's best alternatives for a different supplied gear or ownership budget.

All 15 floors receive an initial screen and final evaluation. Additional discovery effort can prioritize unresolved floors 4/7 and sparse-clear floor 13, but must not silently remove easier floors or redefine the generalist objective. Retain all seven slot-count cohorts; run them in separate budgeted jobs rather than creating one unbounded cross-product.

Completion: party search can discover complementary asymmetric loadouts, detects and reports regressions on the declared evaluation matrix, and reports tradeoffs rather than selecting a different winner per floor and presenting that as one universal party.

## 5. Confirm finalists without reusing selection evidence

Discovery is adaptive and exploratory. Freeze a small finalist list, unchanged controls, exact budgets, all-floor evaluation and sample counts **before** confirmation. Confirmation uses a separate, never-consumed seed set and the same frozen content/settings/executable. Check actual generated seed overlap, not merely different master seeds.

Proposal for the initial usable product: cap discovery at **10,000 new combat trials per experiment** and confirmation at **six parties including controls × 15 floors × 100 trials = 9,000 trials**. These are starting engineering budgets, not approved statistical precision or balance thresholds. Expose the exact cost before running. Multiple gear/slot cohorts require separate experiments and an explicit aggregate cap. Pick confirmation size in advance based on the smallest difference worth resolving; do not rerun or extend it only because the outcome is disappointing.

Report paired changes, per-floor win/draw counts, nominal intervals, survival and pacing. Treat overlapping/noisy results as tied or inconclusive. If future claims require statistical superiority across many candidates/floors, predeclare the comparison family and an appropriate multiplicity correction; otherwise keep results descriptive. New combat seeds measure randomness robustness on known bosses. Any claim of robustness to unseen encounters additionally needs held-out encounter contexts or later production-parity adapters, not just new seeds.

Finalists and controls stay in the report even when confirmation disappoints. Once results have informed a new search, those seeds become historical discovery evidence and are no longer an untouched test set. Maintain an experiment/seed-use ledger. Stop at the declared budget or discovery plateau; neither constitutes proof of global optimality.

Completion: reports show selection provenance, seed separation and all failures; independent reruns/replays match. Search-seed restarts, random-search comparisons and held-out confirmation are distinct forms of evidence, not interchangeable extra samples.

## 6. Explain, export and rerun after balance changes

For each recommended loadout, show exact Essences/order, progression, gear, intended allies, covered floors, gains/regressions, assumptions and uncertainty. Give several meaningfully different alternatives when evidence does not separate a single winner. Report observed per-role/per-ability contributions only where production telemetry supports them; first audit effective healing, overheal, barrier absorption, status uptime and failed triggers before promising those metrics.

Add matched legal replacement experiments to explain which selections matter. Removing an Essence changes slot usage and is a separately labeled ablation, not an equal-budget comparison. Test selected paired substitutions to investigate synergy; compare their joint and individual effects in the same context. Freeze these diagnostic comparisons before new diagnostic seeds, label them exploratory, and avoid treating contributions or selected correlations as universal causal Essence rankings.

Extend Tower Lab with **Find loadouts**: choose a character/party budget, allowed pool, 4–10 slots, desired context and search budget; inspect cost; run/cancel; review candidates; export a recipe; verify a fight. Generated export catalogs must be usable by existing benchmarks without manual character edits. No account inventory mutation or production write is required.

After balance changes, provide two distinct operations:

1. **Retest saved builds:** regenerate the same recipes against current content and compare matched schedules, showing what changed for existing builds.
2. **Search again:** find replacements under the same declared budgets, using a fresh experiment and unused final seeds. Report this new quality ceiling separately so optimization cannot hide a regression in existing builds.

Persist the contract, resolved catalog/metadata, candidate provenance, budgets/cost, cached-trial references, selection, both stage bundles, Markdown/JSON reports and exported recipes. Preserve executable/content identities and verified replay. Cancellation checkpoints the search log and completed trials. Resume is a later explicit feature requiring exact content/config/algorithm compatibility; do not claim it exists in the first release.

## Delivery order and verification

| Increment | Deliverable | Exit check |
| --- | --- | --- |
| A — foundations | Search contract, legal pool, budget/ownership filtering, ordered identity audit and metadata inventory | All 4–10-slot fixtures and 15 floors covered; independent production preparation/order parity; no silent exclusions |
| B — character search | Deterministic whole-loadout search, one-/two-slot mutations, diversity/restarts and exact caching | Small-space enumeration reference, synergy-valley case, reproducibility, budget/cancellation tests; comparison with random sampling |
| C — party search | Complementary loadout combinations and distinct generalist/specialist results | RequiredSlots, asymmetric Strikers, repeated-cell limitations and cross-context tradeoffs verified |
| D — evidence | Frozen finalists, unused seeds, experiment ledger, reports and export | Tamper/overlap rejection, independent normal-Tower parity, draw/defeat/victory replay, preserved historical evidence |
| E — dashboard and measurement | Costed one-button workflow and full recorded experiment | Browser/API run, cancel, report, export/reimport and replay; all-floor final report and equal-budget search-method comparison |

The first implementation task is **A**, followed by a small B experiment at the four-slot entry budget. Use all floors for coverage, with role-specific discovery priorities recorded upfront. Expand measurement to every slot-count/gear cohort after search correctness and cost are understood. Completing the four-slot experiment alone does not complete the broader 4–10-slot plan.

Run backend verification through `build/run-tests.ps1`, initially the `BalanceHarnessTower` filter plus new search classes. Add affected production-validation tests as needed; run the full backend suite if shared combat/preparation code changes. Check deterministic results across supported worker counts, seed/order identity, stale-cache rejection, budget invariants, partial evidence and saved-recipe replay. Use exhaustive small legal pools as an algorithm test oracle without promising the noisy simulation winner is the mathematical optimum.

Keep runtime observations separate from controlled performance claims. Calibrate pilot time, memory and artifact size before allowing long jobs, and expose estimated cost and hard caps. Reuse the current production Tower runner rather than the direct-runtime setup in `AbilityBalanceSimulator`; useful scheduling/search ideas there do not establish Tower gameplay parity.

## Open decisions and practical defaults

Proceed initially with fixed equipment, level-1/unascended Essences, explicit hypothetical ownership, current curve/controlled cohorts and descriptive scores. These defaults avoid blocking foundation work. Owned-player recommendations require a supplied inventory; trained-build optimization requires an explicit training budget. A smaller practically meaningful win-rate difference requires a larger predeclared confirmation budget. Later floor gear, unseen-encounter claims and any numerical balance policy remain separate design decisions.

This plan changes documentation only. It does not tune bosses, change game configuration, introduce a migration, deploy a service or resume deferred Phase 2 integration.
