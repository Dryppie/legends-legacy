# Finding strong Essence loadouts — implementation plan

Status: foundation increment A, bounded character search from B, its two-ally reliability follow-up, five-role whole-party deployment search from C, and 4–10-slot controls with confirmation/export/replay from D–E are implemented on 10 September 2026. Further B–E expansion remains open, including independent later-character optimization and iterative discovery contexts. Originally written 9 September 2026. Target: the offline `LL/tools/BalanceHarness` and its local Tower Lab dashboard. Tower is the first evaluation context; later idle, dungeon and PvP adapters must establish their own production parity and rankings.

The [5–10-slot progression increment](Tower-Party-Progression-Review.md) adds explicit schema-2 budgets and **Find loadouts** in Tower Lab. It runs six slots first with floor 10 as the selection priority, followed by separate five-/seven-/eight-/nine-/ten-slot studies, always covering all 15 floors. The batch predeclares a 41,040-combat cap, historical starts, fixed reference parties, three-stage seed schedules and exclusions. Each budget still searches only the first-cell Guardian, Restorer and two Strikers; the Controller and later cells remain authored. Level and gear change between these cohorts, so this is not an isolated slot-count experiment. Wilson intervals remain descriptive; full ordered loadouts are selected by minimum priority-floor gains against each context's control, then all-floor gains and declared tie-breaks.

The new `tower-party-search` evaluates each candidate with both ally contexts during selection, using minimum gains over each context's own control. It searches first-cell Guardian, Restorer and both Strikers independently at the Standard rank-1 four-Essence budget, retains generalists/specialists, screens bounded joint combinations and freezes parties before unused-seed confirmation on all 15 floors. Both methods receive identical historical Restorer A/B/C starting candidates and actual combat budgets. This is a separate versioned policy; earlier evidence is unchanged. The study completed **29,760 combats**, with four alternatives per character, 24 joint candidates and eight confirmed parties. Seven searched finalists won 40/40 on floor 1 versus the control's 26/40. The strongest observed all-floor party came from random seed 9677 (195/600 original allies, 199/600 alternative later cells, versus 46/600 for each control). See the [joint-party review](Tower-Party-Search-Review.md) and [harness commands](../LL/tools/BalanceHarness/README.md#character-robustness-and-joint-party-search--10-september-2026).

The earlier [reliability study](Tower-Loadout-Reliability-Review.md) extended the pilot with four search seeds, six discovery samples per floor, forty fresh confirmation samples, two fixed ally contexts and a shared finalist union tested on every floor in both contexts. It concentrated on the Standard rank-1 four-Essence entry budget rather than multiplying every earlier gear cohort. Search/ranking algorithms stayed unchanged in that experiment to investigate replication and precision. Schema 2 added declared historical-seed exclusions, full confirmation reconstruction and lossless compressed battle storage; schema-1 evidence stays readable. Wider role/slot/gear coverage remains open.

The follow-up completed **35,280 battles**, with **116 passing Tower tests** and **13 matching detailed replays**. All fifteen finalists won 40/40 on floor 1 with strong allies, but some won only 18–19/40 with original allies versus that context's 25/40 control. Strong teammates can mask weak target builds. Historical A/B and a new random-search candidate won 40/40 in both contexts. Guided search still has no dependable advantage over the equal-cost random baseline. Future policy changes should preserve contrasting-context evidence and use fresh confirmation seeds; no policy is retrospectively applied to these results. Complete compressed archive size is approximately 655 MB, with 87.74% reduction in report payloads.
The [four-Essence pilot review](Tower-Loadout-Pilot-Review.md) records the new `tower-loadout-search` command. It searches one character with fixed original allies, all 15 floors, four separate entry gear cohorts and two search seeds. Guided search and uniform legal random search receive equal actual combat budgets. The entry pilot ranks floor-1 victories first, then all-floor victories, guardian health and survival; this explicitly differs from the earlier progression-curve generalist objective. Search seeds share discovery combat schedules and are not independent extra samples. Finalists freeze before unused-seed confirmation. The existing ordered-loadout policy remains in effect.

The [foundation review](Tower-Loadout-Foundation-Review.md) records 165 prepared contexts, 652 accepted proposals, eight family-rule rejections, an 80-definition mechanics inventory, 99 passing Tower tests and 2,514 passing backend tests. Eighteen paired order probes (36 combat executions) found 12 gameplay differences and one changed outcome. **Search must preserve and explore Essence slot order.** An opt-in reference identity vector now isolates Essence changes while preserving legacy defaults. This is preparation/order evidence, not quality ranking or completion of the four-slot search pilot. The current contract supports declared pools, pins, fixed level-1/unascended budgets, proposal caps and ordered probes; advanced cost/training/search controls below remain future work.

The progression batch completed **41,040 combats**, with **144 passing Tower tests**, full archive reconstruction, **44 matching detailed replays** and **140 identical normal-Tower exported fights**. A separate 5,040-fight rendered-browser study passed run/cancel/results/download/replay checks. All six-slot finalists, including the control, cleared floor 10 in 40/40 confirmation trials per context under the provisional budget. Ten-slot finalists reached 298/300 original-context wins and 300/300 with alternative allies. All-floor and per-floor results remain descriptive; different budgets are separate cohorts, and confirmation never reselects finalists. The [progression review](Tower-Party-Progression-Review.md) records results and the next bounded increment: Controller/later-cell search at fixed budgets, followed by stronger method replication with fresh seeds.

The next scoped C–E increment is implemented as schema-3 [whole-party search](Tower-Whole-Party-Review.md): Controller discovery, first-cell/repeated/alternating deployments, every preceding finalist retained as a control, a matched deployment trio frozen before fresh confirmation, and explicit policy-to-party evidence. Tower Lab exposes this for every 4–10-slot budget while retaining the earlier scope. The predeclared seven-cohort batch runs six slots first, increases generation-seed replication and caps actual work at 72,480 fights. All-floor coverage, existing gear/levels, production parity and descriptive Tower statistics remain required. Independent optimization of every later character, iterative discovery contexts, composition search and acquisition/training remain future work.

Whole-party measurement completed **72,480 fights**, 176 joint candidates and 88 frozen finalists, including all 30 retained controls. **161 Tower tests passed**; all seven archives reconstructed, **58 detailed replays matched** and **80 normal-Tower exports reproduced exactly**. Six-/eight-slot deployment comparisons improved, but four-slot new finalists did not beat the retained best; five-slot results were mixed, and seven-/ten-slot best totals tied retained results. These findings support deeper joint refinement starting from strong complete four-/five-slot parties, with more discovery samples and fresh confirmation. They do not establish method superiority, optimality or an automatic Tower target. The review records all cohorts, browser verification, scope auditing for concurrent edits to an unused catalog, and the reproducible evidence package; Phase 2 remains deferred.

## Next implementation: joint refinement of retained parties

**Planned, not yet implemented.** Whole-party schema 3 is complete for its recorded scope. Four-slot new finalists did not exceed the retained best, and five-slot results were mixed. The next experiment should test whether refining strong complete parties improves on assembling independently selected character winners.

1. Start with separate four- and five-slot cohorts, retaining their existing level, Uncommon gear, quality/tier/rank, Essence progression and ownership assumptions. Cover all 15 floors and legal RequiredSlots. Freeze a content root and executable before the batch; preserve earlier evidence and all existing 4–10-slot support.
2. Seed joint refinement from strong complete retained parties. Evaluate legal ordered loadout changes in the full team's context, including Controller and both Strikers. Declare which characters and deployment policies may change before discovery; independent optimization of every later character is a separate extension.
3. Predeclare the candidate/actual-combat cap, generation seeds, discovery samples, retained controls and finalist allocation. Use more than the previous single discovery sample per floor/context and compare with an equal-cost random baseline. Reserve disjoint discovery and confirmation schedules, excluding all recorded historical seeds.
4. Keep the declared floor-1 priority and all-floor gains/tie-breaks for these budgets unless a separately versioned objective is introduced before combat. Wilson 95% intervals describe uncertainty; they do not select Essences. Preserve Essence order and do not pool duplicated full-party contexts as independent evidence.
5. Freeze alternatives and controls before fresh confirmation on every floor. Report paired gains and regressions against the retained parties, even when the new search loses. Preserve exact builds, provenance, Markdown/JSON reports, normal-Tower export parity and verified replay. Confirmation must not reselect the winners.

Completion requires relevant backend checks through `build/run-tests.ps1`, deterministic archive reconstruction, independent normal-Tower parity and a complete all-floor comparison under the predeclared cap. If the dashboard changes, verify its preview/run/cancel/results/export/replay flow. The result remains **best found within the recorded budget**; no automatic promotion, Tower difficulty target or boss tuning follows. Phase 2 integration stays deferred. Sample counts and total compute budget still need to be fixed when implementing this experiment.

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

## Selection principle clarified — 10 September 2026

**Select Essences by the measured combat performance of complete legal loadouts in their intended parties.** There is no standalone score that identifies the correct Essence regardless of gear, progression, allies or enemies. Personal damage/healing totals, mechanic tags and descriptions help explain or generate candidates; they are not sufficient selection criteria.

The planned decision process is:

1. Generate legal whole-loadout candidates from known interactions, existing recipes and random exploration. A suggested synergy is a hypothesis to test.
2. Simulate candidates through production combat at identical declared budgets and with matching combat seeds within each comparison. Keep encounter weights and ally contexts explicit.
3. Retain promising, diverse candidates primarily by party victories across the intended encounters. Use remaining boss health and survival as declared discovery tie-breaks or exploration signals when wins are tied; a losing build is still a losing build.
4. Test individual substitutions, paired substitutions and changes across allies to find combinations whose value is not visible in isolated Essence rankings. Preserve generalists and specialists separately.
5. Freeze finalists and confirm them on unused seeds. Report gains, regressions and uncertainty; keep close or inconclusive alternatives instead of declaring an unsupported universal winner.

**Wilson 95% confidence intervals describe uncertainty in a measured clear rate. They do not search the catalog, measure synergy, or certify that a build is correct or optimal.** Paired improvement intervals answer a different question: how a candidate's outcomes differ from a control on matching seeds. Neither the Wilson lower bound nor a confidence interval is the current discovery ranking score, and this clarification does not introduce such a ranking policy. Any future use of confidence bounds for candidate elimination must be explicitly specified and validated as part of the search method.

The earlier dashboard search enumerates all 16 combinations of **four predefined role substitutions**, ranks discovery by total wins, then lower mean remaining guardian health, higher party survival and stable ID, and confirms a frozen shortlist. The whole-loadout pilot adds generation and single/double/order mutations for a character with fixed allies. The joint-party extension searches four first-cell characters in contrasting contexts, and **Find loadouts** now exposes that workflow for 4–10 slots. Wider role/context optimization remains planned. Existing evidence establishes stronger builds within the tested pool; it does not establish the best choices from the full catalog. “Best found” always carries the recorded budget, ownership, party, encounters and search limits.

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

Completed audit decision: preserve order. The foundation's identity-pinned probes disprove general permutation equivalence, and reversed lists match independent normal-Tower execution in parity tests. Add bounded order mutations alongside single/double substitutions in increment B. Re-evaluate the original control and earlier candidate 05 with pinned identities; the earlier four-substitution search remains valid historical party evidence but did not hold runtime identities constant across candidates.

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

Increment **A** is complete for the scoped contract and mechanics inventory recorded above. The first **B** pilot is implemented: three-member diverse beam, direct-mechanic start, uniform legal restarts, single/double substitutions and order mutations, exact scoped trial caching, paired random comparison, frozen confirmation, exports and replay. The reliability extension adds two fixed ally contexts and transfers the same frozen finalists between them with stronger sampling. Tests include a fully enumerated small space with a two-Essence fitness valley, determinism, cancellation, budget/pool exhaustion, identity isolation, historical-seed exclusion, compressed replay, confirmation reconstruction and independent normal Tower parity. Scoped **C** adds two-context character selection, specialist retention, separately searched Strikers and joint party screening/confirmation. The progression increment extends this to explicit 4–10-slot budgets and scoped **D–E** dashboard preview/run/cancel/report/export/replay. Whole-party schema 3 additionally implements Controller discovery and first-cell/repeated/alternating deployment comparisons with retained controls. This does not complete B–E: deeper joint refinement, independent later-character optimization, wider ally robustness, iterative context updates, equal-cost comparison against the earlier authored search, owned/trained budgets, multiworker scheduling and resume remain open.

Run backend verification through `build/run-tests.ps1`, initially the `BalanceHarnessTower` filter plus new search classes. Add affected production-validation tests as needed; run the full backend suite if shared combat/preparation code changes. Check deterministic results across supported worker counts, seed/order identity, stale-cache rejection, budget invariants, partial evidence and saved-recipe replay. Use exhaustive small legal pools as an algorithm test oracle without promising the noisy simulation winner is the mathematical optimum.

Keep runtime observations separate from controlled performance claims. Calibrate pilot time, memory and artifact size before allowing long jobs, and expose estimated cost and hard caps. Reuse the current production Tower runner rather than the direct-runtime setup in `AbilityBalanceSimulator`; useful scheduling/search ideas there do not establish Tower gameplay parity.

## Open decisions and practical defaults

Proceed initially with fixed equipment, level-1/unascended Essences, explicit hypothetical ownership, current curve/controlled cohorts and descriptive scores. These defaults avoid blocking foundation work. Owned-player recommendations require a supplied inventory; trained-build optimization requires an explicit training budget. A smaller practically meaningful win-rate difference requires a larger predeclared confirmation budget. Later floor gear, unseen-encounter claims and any numerical balance policy remain separate design decisions.

The linked reviews distinguish implemented increments and measured evidence from the remaining planned scope. None of these increments tunes bosses, changes game configuration, introduces a migration, deploys a service or resumes deferred Phase 2 integration.
