# Automatic Tower team discovery

**Competitive search update — 12 September 2026:** [stronger Kharad searches](Tower-Competitive-Build-Search-Review.md) confirmed new teams at **948/1,000** and **1,000/1,000** on the previously passing content. Its earlier scoped Pass is superseded for the expanded build portfolio. Retained-build improvement and the history-capacity extension are implemented; the fresh quality audit and separate linked calibration are documented in the new review. Near-optimality remains unestablished.

**Progression audit and calibration — 11 September 2026:** [floors 2–5 review](Tower-Progression-Floors-2-to-5-Review.md) records independent four-Essence searches on floors 2–4, their separately confirmed linked Health/Power calibration against every known breach, and Kharad's fresh **20.9%** strongest-control check. Compatible controls and calibrated top builds persist for future searches. Floors 6–11 and practical acquisition coverage remain open.

**Fresh search and Kharad follow-up — 11 September 2026:** [new independent searches](Post-Calibration-Tower-Team-Search-Review.md) confirmed Garran's strongest saved team at **34.8%** and found a stronger Kharad team at **59%**, triggering a separate calibration. The [expanded-portfolio follow-up](Kharad-Expanded-Portfolio-Calibration-Review.md) applied another **8% to both Kharad Health and Power**; its strongest of 122 parties confirmed at **25.25%**, and the full family passes. At that stage, main-dashboard searches retained **4 floor-1 / 6 floor-5 controls**. Broader progression, practical Essence access and further independent ceiling searches remain open.

**First retained-build calibration — 11 September 2026:** compatible saved builds now enter new Tower Lab searches automatically as fresh benchmark controls; completed future studies retain their generated finalists. The [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) applied linked Health/Power factors of **1.06 to Garran** and **1.56 to Kharad** relative to their pre-campaign inputs. The strongest of 57/106 retained parties confirmed at **34% / 24.25%**, respectively, and both frozen families pass the 10–50% policy. This uses the declared full Essence pool including Rare Essences; this historical result is followed by the fresh searches and expanded calibration above.

**Original independent-team pilot results — 11 September 2026:** increments 1–5 of [Automatic Tower team discovery](Automatic-Tower-Team-Discovery-Plan.md) are complete. The [fixed pilots](Automatic-Tower-Team-Pilot-Review.md) found a floor-1 generated primary at **859/1,000 wins (85.9%)** and five floor-5 generated finalists at **972/972 each**. Independent search found viable builds; both declared cohorts fail the 50% balance ceiling. These pilots use the full 80-Essence pool with hypothetical ownership, including Rare Essences. The retained-build follow-up above now calibrates these declared budgets; practical Essence access and the wider progression curve remain separate coverage. Wider progression balance remains unestablished; no bosses were tuned by these pilots.

The tool should generate effective complete parties for a selected boss and progression budget, without needing a hand-authored answer. The user's [five-character example](Tower-Floor-1-User-Party-Review.md) demonstrates the kind of complementary team we want to discover. Its earlier [29.1% confirmation against linked-scaled floor 1](Tower-Floor-1-Linked-Tuning-Review.md) is a reference observation, not a recipe template, required Essence list or universal party. The earlier 39.4% observation used the previous offense-only setting and remains historical.

## 1. Product behavior

The user chooses a floor, reviews its level/equipment/Essence budget and allowed pool, then selects **Find teams**. The tool constructs and searches complete legal parties, confirms a small frozen set on unused seeds, and reports:

- The strongest generated team found within the recorded search budget, with exact character builds and ordered Essences.
- Up to four additional useful approaches when the search actually finds them, with observed tradeoffs and supporting combat results.
- Separately identified reference parties, including the user's example only when explicitly registered for the target floor and budget.
- A balance assessment against the approved 10–50% policy, distinct from search success and archive verification.
- Exact exports, provenance, sample counts, uncertainty and replay.

Finding the same team independently is allowed. Artificially banning Illusion Fox, enforcing unique Essences across the entire party or weakening a generated winner would defeat the purpose. The requirement is that candidate generation does not depend on receiving the example's recipe. Repeated Essences across characters remain legal wherever production ownership/family rules allow them.

The tool must also be honest when it finds only one useful team, no viable generated team or an above-ceiling team. It must not fabricate alternatives or claim the search found the global optimum.

**Approved tuning direction, 11 September 2026:** the [boss tuning controls](Boss-Specific-Essence-Loadout-Plan.md#approved-boss-tuning-controls) use one per-boss difficulty multiplier to scale Health and Power together by default, with HP-only, Power-only and explicit mixed adjustments available when combat diagnostics justify them. The earlier [user-party experiment](Tower-Floor-1-Linked-Tuning-Review.md) selected Garran at Health 1.6764 / offense 1.6368. The later [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) applied Health 1.776984 / offense 1.735008 and also tuned Kharad. Independent discovery keeps the resulting boss content fixed. Further tuning needs its own frozen budget and fresh seeds; the implemented offline calibration script resolves linked adjustments to existing content fields without adding another production scaling layer.

## 2. Reuse the existing machinery

The [current implementation](Boss-Specific-Essence-Loadout-Implementation.md) already provides most execution infrastructure. The key extension is separating independent team generation from retained-recipe refinement.

| Existing component | Reuse | Required change |
| --- | --- | --- |
| `TowerBossInventory` | Boss mechanics, Essence families and interaction candidates | Supply generic capability/interaction data to constructive generation; keep unresolved interactions marked as hypotheses. |
| `TowerBossOptimization` | Legal random complete parties, ordered mutations, joint/graph proposals and combat ranking | Add a policy whose parents originate only from generated teams. Support whole-character replacement and recombination alongside local edits. |
| `TowerBossSearchContract` / `TowerBossSearch` | Frozen recipes, budgets, scheduling, execution and verification | Introduce a new schema/policy with separate benchmark references, search starts and stage budgets. Existing schemas retain their semantics. |
| `TowerBattleRunner`, reference-build factory and `TowerLoadoutArchive` | Production preparation/combat, identity control, compressed records, export and replay | Preserve combat fidelity and the exact budget; never change gear or actor identities to improve a candidate's score. |
| Tower Lab boss-search panel | Cost preview, run/cancel, results, export and replay | Expose independent discovery as the new mode and label generated results separately from imported references. |
| Approved Tower policy | Per-floor 50% ceiling and existence of a 10% build | Standalone evaluator implemented in increment 1, staged confirmation in increment 3, and Tower Lab integration in increment 4. |

Today, schema-2 refinement repeatedly starts from a historical anchor and feeds controls into every arm. Its strategy archive only selects behavior representatives among equal primary scores. It also couples small confirmation limits to mandatory all-floor transfer. Those are useful historical experiment contracts, but they do not establish discovery without reference recipes. Version the changes instead of silently changing their meaning.

## 3. Independent discovery and reference improvement

Provide two explicit modes:

| Mode | Candidate-generation inputs | Reference handling |
| --- | --- | --- |
| **Discover from scratch — default** | Current mechanics, legal Essence pool, fixed character budgets, generic capability goals and generation seeds | References are held outside generation, parent selection, scoring and finalist selection. Compare them after generated finalists freeze. |
| **Improve a supplied team — optional** | A declared starting party plus the same legal pool and mechanics | Reference-derived descendants are permitted and visibly labeled. Retain fresh complete-party exploration. Never describe this mode as independent discovery. |

For independent mode, the proposal interface must not receive reference Essence vectors, reference-derived mechanism replacements or reference fitness. It must not load reference files indirectly through the default historical context builder. Neutral equipment/identity templates and the allowed pool remain available. Keep any identity-pinning data behind preparation, not in the candidate-generation inputs. Import historical seed exclusions independently of which reference recipes are selected, then freeze explicit generation/discovery schedules; reference removal must not change those schedules or the neutral identities.

Every proposal records its generation seed, method, parent IDs, operator and exact ordered recipe. Lineage propagates through mutation and recombination. An imported reference and all its descendants remain reference-derived. If an independently generated recipe happens to equal a reference, record both identities/provenances without changing the generated sequence or hiding the convergence.

Benchmark references are registered by explicit floor, full legal party, gear/level/Essence budget and source evidence. Preserve all declared compatible strong controls, deduplicated by exact prepared recipe. Prior cross-floor observations can qualify a reference only through their actual complete target-floor recipe; do not silently repeat the user's floor-1 party across the Tower. Historical outcomes remain historical when content or execution changes.

## 4. Generate complementary complete parties

### A. Freeze the encounter and budget

Use each floor's actual `RequiredSlots`. Preserve the current progression checkpoints: four Essences at floor 1, five around floor 5, six around floor 10 and at least seven at floor 11. The plan must record the exact intermediate transition floors and gear assumptions before evaluating them. Never lower a floor's intended budget merely because it leaves more room for search improvement.

For the first implementation, keep each character's level, equipment, quality/tier/rank, Essence training and identity fixed. Any legal Essence can occupy any eligible character position. Existing labels such as Guardian or Restorer describe equipment templates; they must not force a tank/healer/DPS role count. Equipment optimization, acquisition timing, Combat Styles and changing the roster's power budget are separate extensions.

Support a hypothetical legal pool and an explicitly supplied owned pool, labeling which is used. Enforce source-family restrictions within each character and any supplied inventory-copy limits. Do not invent a party-wide no-duplicates rule.

### B. Create several starting approaches from data

Use two independent methods initially:

1. **Uniform legal random teams:** sample complete ordered parties from the eligible pool. This is the comparison baseline and keeps unexpected combinations discoverable.
2. **Constructive generation plus joint refinement:** assemble fresh teams around generic capabilities suggested by the boss, then search them as complete parties.

Constructive generation should explore mixtures such as distributed sustain, protection with concentrated damage, damage-over-time pressure, stagger/control and add handling where the encounter actually has adds. These describe functions, not named Essence packages. Select concrete Essences from the current inventory/interaction data with randomized choices and varied allocations across characters.

Allow hybrid characters and repeated support effects. A party with several characters contributing healing or protection must be reachable without a special case for the user's example. Count target eligibility, trigger ownership, stacking rules, upkeep and competing resource costs when proposing interactions. A tag match only proposes a candidate; actual whole-party combat determines whether it helps.

### C. Improve teams in their full combat context

Refine generated teams using a deterministic, recorded mix of:

- One- and two-Essence substitutions within a character, plus Essence-order changes.
- Coordinated substitutions across two characters, including enabler/consumer proposals where the mechanics permit them.
- Replacement of a whole character's loadout to escape a poor local solution.
- Recombination of compatible generated teams, followed by full legality checks.
- Fresh complete-party generation at least every fourth refinement attempt.

Evaluate every proposal with the complete required party in real Tower combat. A weak change in isolation can still belong to a useful coordinated change. Conversely, combining individually strong characters must not be assumed to produce a strong team.

Maintain a small beam of promising generated teams, plus a bounded exploration reserve for materially different ordered recipes and capability patterns. Preserve the strongest measured candidate regardless of novelty. Diversity affects exploration and additional outputs; it must not replace the primary win-rate objective.

### D. Rank strength, then assess balance separately

The new independent objective ranks target-floor party victories, followed by declared boss-progress, survival and victory-duration tie-breaks. With multiple declared ally contexts, use the worst-context target success under equal paired sampling; comparison with imported references belongs in reporting. Give this objective its own version rather than reusing the legacy paired-gain label.

Do **not** optimize toward 30%, stop improving at 50% or discard above-ceiling candidates. The 30% aim was a boss-tuning selection rule. This tool should expose stronger teams. An independently discovered 80% team is useful search output and evidence that the boss exceeds the desired ceiling for that budget.

## 5. Sampling, alternatives and balance assessment

Separate three stages:

1. **Discovery:** generate and refine candidates on a fixed paired schedule. Keep full proposal lineage and actual combat counts.
2. **Selection validation:** evaluate a bounded discovery-selected shortlist on a different fixed schedule. Use this stage to freeze the primary generated team and up to four alternatives before final confirmation.
3. **Confirmation:** evaluate every frozen generated finalist and every declared compatible benchmark reference on fresh paired seeds. No new candidates, reselection or extra samples in response to these outcomes.

Reserve shortlist positions for each method/restart and for different observed approaches. Keep alternative selection reproducible: retain the primary, then select distinct capability/behavior representatives within ten percentage points of the primary's selection-stage win rate, breaking ties by win rate and stable recipe ID. This margin is an initial search-policy proposal, not a gameplay target. Do not fill unused slots with duplicates or relabel different Essence orderings as different combat strategies without behavioral evidence.

Confirmation presents every reference and generated build independently. Draws count as non-wins. Under the [approved policy](Tower-Balance-Acceptance-Policy.md), any observed rate above 50% blocks acceptance, and at least one build must establish viability at 10% or higher. Other weak builds can remain below 10%. Missing trials or boundary uncertainty cannot produce a pass, and averages must not hide a strong outlier.

Show ordinary pointwise 95% Wilson intervals for readability. For the scoped aggregate assessment, predeclare family-adjusted Wilson intervals using a Bonferroni allocation of 0.05 across all registered confirmation recipe/context cells; label these as approximate intervals. Every included cell must support the upper ceiling and at least one must support the lower threshold. Freeze the family before confirmation and test the interval implementation against independent numerical fixtures. Do not claim exhaustive protection against unsearched builds.

Keep three separate conclusions: **did independent search find viable teams**, **how did those teams compare with references**, and **does the tested floor/budget cohort meet the balance policy**. A viable reference alone does not establish that independent generation succeeded. Conversely, a generated team above 50% must remain visible even when balance fails.

Explanations should use observed recovery, damage distribution, survival, add presence and denied actions with their existing attribution limits. Optional matched substitutions use separately reserved diagnostic seeds. Do not turn raw healing/DPS totals into standalone Essence scores or causal synergy claims.

## 6. First implementation experiment

**Completed:** the [pilot review](Automatic-Tower-Team-Pilot-Review.md) records execution of this design, the complete reference portfolio and the predeclared floor-5 sample adjustment required by the combat cap. The allocation and sequence below retain the experiment design; they are not outstanding runs.

Start on **tuned floor 1, Health 1.6764 / offense 1.6368**, at the exact level/equipment budget used by the supplied party. This is the separately confirmed 1.32 linked adjustment from original Health 1.27 / offense 1.24. Freeze current content and use that party only as a benchmark reference. The latest 29.1% observation does not substitute for fresh reference measurement in the discovery experiment. Exclude the linked-tuning seed ledger as well as all earlier campaign, user-benchmark and offense-only tuning seeds. The discovery pilot's proposed allocation below is unchanged; the completed calibration was a separate experiment.

Proposed initial allocation for one effective full-party context:

| Stage | Proposed allocation | Maximum combats |
| --- | --- | ---: |
| Independent discovery | 2 methods × 3 generation seeds × 128 evaluated complete parties × 8 paired combat seeds | 6,144 |
| Selection validation | Up to 16 generated candidates × 64 new paired seeds | 1,024 |
| Final confirmation | Up to 5 generated finalists plus `C` declared references × 1,000 new paired seeds | `5,000 + 1,000C` |
| Optional explanation diagnostics | 2 preselected team hypotheses × 4 matched replacements × 32 reserved seeds | 256 |
| Export/replay verification | Explicit maximum including successful, failed and drawn examples where available | 200 |
| **Total** | Before any additional context or transfer coverage | **`12,624 + 1,000C`** |

`C` is the actual deduplicated reference count resolved before combat, not an assumed two. For illustration only, two references would cost at most 14,624 fights. The importer must also retain compatible strong historical controls; it must not silently discard them to achieve that illustrative cost. Charge every scheduled reference, context, candidate evaluation and replay. Reject plans exceeding the recorded hard cap before execution; retain the existing 100,000-fight safeguard for this first scope. Cache hits, exhausted legal pools and duplicate proposals must be accounted for explicitly, not replaced with unbounded retries.

The 128 constructive-method evaluations begin with 32 independently assembled complete teams, followed by 96 proposals under the recorded mutation/restart schedule. The random method evaluates 128 independent legal teams. Methods share combat seeds within a stage; different generation seeds do not multiply the independent combat sample count. A bounded unsuccessful run remains a legitimate negative result.

Import verified seed exclusions from all preceding campaigns, including the user-party benchmark, both floor-1 tuning ledgers and the completed or cancelled implementation/browser studies. Audit the union independently of which references are selected; Tower Lab's bounded history scan needs explicit additions for outside or deeper archives. Preserve producing binaries, content and all frozen stage artifacts. Do not reuse a known result's seeds as fresh confirmation.

The first useful evidence is a generated team with no reference-derived ancestry that confirms viability at this budget. The tool may independently rediscover the example; exact convergence is reported honestly. Finding several materially different viable approaches is a stronger result, not something to assume in advance.

After the floor-1 workflow is verified, run a separately frozen floor-5 study at its five-slot budget using the same generation policy and boss-derived inputs, without injecting the user's team. Later studies independently build every required character on larger-party floors. Keep target-only confirmation as the new mode's default; optional transfer studies declare the additional allies, budgets and costs explicitly. Legacy all-15-floor reports retain their original behavior, and the wider progression audit still needs separate coverage of every floor.

## 7. Implementation sequence

| Increment | Deliverable | Completion evidence |
| --- | --- | --- |
| 1. Contracts and acceptance — implemented | Independent boss-search schema 3, reference-free generator inputs, separate references/starts, stage budgets, provenance and target-only cost; standalone Tower evaluator and CLI | Contract/acceptance tests, full-party production preparation, equivalent-recipe deduplication, verified Tower archive adapter and legacy reconstruction. See the [implementation guide](Automatic-Tower-Team-Discovery-Implementation.md). |
| 2. Independent generation — implemented | Fresh constructive starters, uniform baseline, all full-party operators, lineage, discovery-only CLI and saved-trial reconstruction | Exhaustive sampling/search and cross-character interaction oracles; reference-invariant proposals, scores and shortlist; three matching 32-combat production runs with replay/export, cancellation and tamper checks. See the [generation review](Automatic-Tower-Team-Generation-Review.md). |
| 3. Staged confirmation and reporting — implemented | Frozen shortlist/finalists, exact exports, qualified alternatives, paired reference comparisons, acceptance and producing executable bundle | Deterministic reconstruction, fresh-seed checks, interrupted-run handling, 10/10 rejection, exact convergence, real-combat reference invariance, replay/export parity and execution from retained binaries. See the [confirmation review](Automatic-Tower-Team-Confirmation-Review.md). |
| 4. Tower Lab integration — implemented | Mode selector, floor/budget/pool preview, cost, progress/cancel and separate generated/reference/balance views | API and real-browser checks for exact preview/run/cancel/results, reference isolation, partial status, export/replay, desktop/mobile rendering and history import. See the [Tower Lab review](Automatic-Tower-Team-Lab-Review.md). |
| 5. Fixed pilots — complete | Floor 1 at four slots and floor 5 at five slots, with all compatible references from the declared portfolio | Generated viability passes; both cohorts fail the ceiling. Full reports, exact recipes, source/seed audits and reconstruction are in the [pilot review](Automatic-Tower-Team-Pilot-Review.md). |
| 6. Retention and first progression batch — complete | Persistent compatible controls, linked calibration and fresh ceiling checks through floor 5 | [Progression review](Tower-Progression-Floors-2-to-5-Review.md): full-family confirmation, every known breach covered, exact application parity and archived failed/inconclusive attempts. |
| 7. Remaining progression coverage — open | Historical seed capacity is extended; floors 6–9 at five slots, floor 10 at six and floor 11 at seven, with separate lower-budget diagnostics | New frozen studies and confirmations are still required; practical ownership and gear access remain explicit limitations. |

The new contract lives in `TowerBossDiscoveryContract.cs`, with `TowerBalanceEvaluator.cs` and `TowerBalanceRuns.cs` for standalone assessment. `TowerBossPartyGenerator.cs`, `TowerBossGeneration.cs` and `TowerBossDiscoveryRun.cs` implement independent discovery. `TowerBossStudyPolicy.cs` and the `TowerBossStudy` execution/archive/Markdown files add the staged workflow through `tower-boss-study` and `tower-boss-study-verify`. The earlier preparation, discovery-only and standalone acceptance commands remain available. Legacy boss-search behavior is preserved. Generic policy defaults contain no preferred Essence IDs or reference recipes; the [Tower Lab workflow](Automatic-Tower-Team-Lab-Review.md) now exposes the independent study.

New schema limits must explicitly cover the proposed candidate counts, three evaluation stages and 1,000-sample confirmation without inheriting the current schema-2 100-candidate/100-confirmation limits or mandatory all-floor cost multiplication. Preserve schema-1/2 readers, formulas, formatting and serialized hashes for archived runs. Correct obsolete report wording through the versioned formatter, including the historical claim that Tower has no approved target.

The fixed pilots, automatic retention, Garran/Kharad calibration and [first progression batch](Tower-Progression-Floors-2-to-5-Review.md) are complete under the declared full-pool assumptions. Floors 2–4 now have full-family linked calibrations covering every known earlier breach; the first inconclusive Velka candidate remains archived separately from its passing follow-up. The history-capacity extension is complete. Remaining work includes competitive search validation, floors 6–11 at their declared progression budgets, lower-budget diagnostics and practical acquisition coverage. Future tuning still requires a new frozen protocol and fresh confirmation.

### Server-wide progression: strongest-build evidence

The user clarified that Tower progression is server-wide and should require the best players. The benchmark must therefore represent some of the strongest achievable complete teams within the intended floor budget. A passing win-rate assessment of the currently tested portfolio alone does not satisfy this requirement. Current floors 1–5 results remain scoped measurements; near-optimal build quality has not been established.

The evidence exposes a search gap. The [post-calibration Kharad search](Post-Calibration-Tower-Team-Search-Review.md) found a new team at **59%** while the strongest saved control scored **27.7%** on the same fresh schedule. After separate tuning, the [latest Kharad search](Tower-Progression-Floors-2-to-5-Review.md) found a strongest new finalist at **7.8%**, while the saved best scored **20.9%**. Independent generation can discover major improvements but does not reliably recover the strength already known. Each recent search proposed 768 complete parties, initially evaluated on eight paired seeds; its many later confirmation/calibration battles improve measurement precision without proportionally expanding the search space. Floors 2–4 still need independent challenger searches against their newly applied settings.

The [competitive search increment](Tower-Competitive-Build-Search-Review.md) implements the history prerequisite, explicit retained-build improvement and the first controlled search-quality comparison. Use the following requirements before accepting further floors for server-wide progression:

1. **Search the current boss settings.** Retuning can change which synergies win. Run fresh challenger searches after each applied calibration, retaining strong previous candidates and their exact recipes.
2. **Use both independent discovery and improvement of saved winners.** Keep independent generation free of reference ancestry. Use the implemented, explicitly labeled retained-build improvement workflow with single-Essence substitutions, order changes, complete-character replacements and coordinated changes across characters. Using saved winners in this separate workflow must preserve their ancestry and must never become a hardcoded universal party.
3. **Measure search reliability and improvement with budget.** Compare several independent restarts and complementary search methods, then predeclare a larger-budget follow-up. Track best independently validated performance against actual search cost and the saved best. Record whether methods repeatedly reach comparable strength and whether added search still produces material gains. Failure to beat a control in one bounded run is not evidence of saturation.
4. **Keep search and balance objectives separate.** Search maximizes performance even above 50%. Freeze candidate selection before fresh confirmation. A newly confirmed stronger team updates the retained portfolio and can trigger a separate calibration; do not remove it or stop improving to preserve a passing balance result.
5. **Declare the competitive budget and decision rule.** Fix legal Essence access, copies, training, character levels, equipment and supported combat systems for top players at that floor. Current fixed gear and hypothetical ownership do not establish this practical budget. Before execution, specify the search allocation, meaningful-improvement threshold, uncertainty treatment and stopping rule. The first Kharad protocol now predeclares a five-percentage-point gap, paired simultaneous intervals and separate recovery, restart-reliability and budget-plateau checks. This experimental rule does not establish the practical top-player budget or global optimality.

The aim is converging evidence that stronger searches make little further progress, supported by fresh comparisons. It is not a numerical probability that a build is globally optimal. Win-rate confidence intervals describe the tested recipes' combat outcomes and do not estimate the probability that an unseen superior team exists. Keep the evaluator's existing scoped `Pass` and archived evidence intact; report competitive search quality separately until the new requirement is supported.

### Next batch: history capacity and floors 6–11

The prior batch left **97,838 excluded historical seeds**. The [competitive search increment](Tower-Competitive-Build-Search-Review.md) extends the cumulative limit to **1,000,000**, verifies larger imports and preserves all earlier exclusions. The capacity prerequisite below is complete. Stronger search validation remains required before final competitive acceptance; use the resulting workflow when continuing floors 6–11.

1. **Complete:** update history validation consistently across discovery definitions, standalone balance definitions and Tower Lab's history union. Check import/request-size limits for the larger definitions. Preserve all existing exclusions and reconstruction of older archives; do not truncate history or change sealed packages.
2. **Complete:** keep the **100,000-combat cap per study or confirmation family** separate from history capacity. The retained index's per-study seed limit describes that study's executed schedule, not the cumulative historical union. Do not increase combat, reference or candidate limits as a side effect of extending history.
3. **Complete:** verify a history larger than 100,000 entries can pass through preparation, dashboard preview/import and archive reconstruction while old seeds remain excluded. Retain rejection of overlapping stage schedules and over-budget execution. Run the relevant backend checks through `build/run-tests.ps1`.
4. Freeze new independent studies for **floors 6–9 at five Essences per character**, then **floor 10 at six** and **floor 11 at seven**. Generate every required party member, keep compatible saved builds as confirmation controls, and declare gear, levels, pool, ownership, stage budgets and fresh seeds before combat. Lower-budget Serevin comparisons remain separate diagnostics.
5. If a floor fails or remains inconclusive, preserve that result and freeze a separate linked Health/Power calibration. Include earlier above-ceiling candidates, shortlisted parties and compatible controls; confirm the selected setting on new seeds under the unchanged 10–50% policy. Retain exact builds and verify local application before recording a pass.

The current results use the full 80-Essence pool with hypothetical ownership. Practical acquisition coverage and reliable independent rediscovery of Kharad's strongest saved build remain open. The [completed batch review](Tower-Progression-Floors-2-to-5-Review.md) preserves its historical scaling and evidence. Use the [competitive search review](Tower-Competitive-Build-Search-Review.md) for the subsequent Kharad findings and active search-quality status.

## 8. Verification and boundaries

Run backend checks through `build/run-tests.ps1`. Cover cross-character interactions, Essence order, legal repeated Essences across characters, owned-copy limits, role-free Essence placement at fixed gear, deterministic restarts, independent reference removal, mutation/recombination ancestry, no viable generated result, a converged reference recipe, saturated high-clear results, uncertainty boundaries and missing evidence. Verify every character on larger-party floors can be generated independently rather than automatically repeating one five-character group.

The independence test must inspect the generator's inputs and parent lineage as well as the final recipe. A cosmetically altered imported team does not pass. Changing an explicit reference in improve-team mode must preserve its reference-derived label.

No new dependency, migration, production API, account mutation, deployment or automatic boss-content change is part of independent discovery. The separately executed [tuning workflow](Boss-Specific-Essence-Loadout-Plan.md#approved-boss-tuning-controls) preserves each discovery experiment's frozen content and budget. Gear/level assumptions, explicit ownership limits and content versions stay visible. Implementation tests and completed discovery/calibration campaigns have separate evidence; the remaining floor-6–11 campaigns have not run.
