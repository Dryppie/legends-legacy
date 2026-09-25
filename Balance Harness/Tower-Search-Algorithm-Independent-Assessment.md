# Independent assessment of World Tower team search

Current status (2026-09-25): Original affinity creation with benchmark validation is the supported baseline. The one nomination experiment completed 15,744 actual fights and all audits; both arms retained the benchmark on all 12 roots. This tuning cycle is closed. See [completed result and next work](<Tower-Affinity-Nomination-Pilot-01-Execution.md>).

**Later implementation and evidence update — 23 September 2026:** The prospective design below was implemented. After one preserved technical failure, a separately declared [second pilot](Tower-Adaptive-Racing-Pilot-02.md) completed and reached `AbandonThisConfiguration`. The subsequent [27,648-fight recognition diagnostic](Tower-Frozen-Pool-Recognition-Execution.md) passed both audits: 68 of 72 measured challengers were below the fixed benchmark, two tied and two had small uncertain positive gains. Proposal quality is now the next development priority, beginning with a versioned proposal-policy contract and deterministic batch export for a controlled generation comparison. The original assessment remains historical rationale; its proposed settings are not demonstrated improvements, and 132 unsampled candidates remain unmeasured.

23 September 2026. Target: the offline `LL/tools/BalanceHarness`, specifically supplied-reference search for the captured floor-5 scenario. This is an assessment and prospective design, not implementation or authorization to run an experiment. No combat, native preparation, seed allocation, gameplay change, deployment or historical evidence modification was performed.

## Recommendation

Develop a **small, reference-seeded beam search with fresh batch racing and coordinated legal mutations**. Separate proposal generation from panel allocation so each can be tested independently. Keep exact references available, retain a few competitive but different challenger parents, and spend additional trials on survivors before letting them determine the next generation. Use an independent confirmation gate for recommendations.

The strongest diagnosis is a combination of **poor broad proposals and unreliable recognition**, not inadequate confirmation machinery. The baseline spends substantial discovery work on distant fresh teams with no observed wins, then concentrates parent selection and nomination on one repeatedly reused eight-seed panel. Only two generated teams survive nomination, and 32 fresh selection trials often cannot distinguish their win probabilities. More confirmation precisely measures the outputs that survived; it cannot repair the discarded search trajectory.

There is an especially useful benchmark missing from the method-to-method headlines: **return the strongest previously confirmed team without searching**. Recounting saved confirmation rows shows that the baseline's selected outputs averaged **3.16, 5.46 and 3.53 percentage points below `96b94357…`**, respectively, in the original exploration, offset exploration and fresh-screening comparisons. These are separate retrospective observations, not a pooled estimate or a new adoption decision. They make avoiding regression a first-class objective.

This recommendation is a hypothesis. Neither a new algorithm's improvement nor the proportion of missed strong candidates is established. The existing racing, anchored and fresh-screening results are relevant negative evidence against their particular allocations and operators, not evidence that all racing, local search or screening fails.

**First implementation step:** introduce a versioned, panel-aware evaluation record and deterministic batch-racing kernel, exercised with literal saved outcomes and synthetic evaluators. It should accept a frozen candidate batch, request explicit fresh paired panels, record every survivor decision, and enforce the fight budget. Do this before another selector variation, surrogate model or general-purpose optimizer.

**Next scientific experiment:** after implementation and admission, compare the unchanged practical baseline with the proposed policy over 12 new paired search roots at **528 search fights per policy per root**, followed by 256 fresh evaluation trials for the union of their two outputs and all three references. The maximum is **28,032 fights**. This is a development go/no-go pilot, not a powered demonstration of reliable five-point improvement. The pending 52,000-fight confirmation remains valuable for deciding the strength of five exact historical teams; it is not the highest-information next experiment for algorithm design.

## Scope and evidence discipline

I read the root and `LL/AGENTS.md` instructions. The checkout has substantial modified and untracked search, analysis, test and gameplay files. The assessment uses current working-tree source to describe current behavior and retained producing evidence to describe historical runs. Those are different identities; current builds must not silently replace captured gameplay assemblies.

Verification here was bounded and read-only:

- Checked externally recorded scientific manifest hashes for the three recent generator/pipeline comparisons and the 24-root three-reference tie comparison; checked consumed result, completion, native-receipt and template bytes against those manifests.
- Checked the fixed-family manifest against its execution receipt, authenticated its result, audits and saved study, and independently recounted **44,000 terminal outcome entries and all twelve paired contrasts**. Native and independent result JSON agreed.
- Recounted every confirmation rate used below from the three saved comparison studies: **41,000, 39,000 and 43,000 outcome entries**. Checked ordered paired seeds within each family and agreement with result rows. These are reads of historical observations, not new simulator trials.
- Authenticated and inspected all **24 `search-NN.json` checkpoints** in the latest tie comparison, reconstructing evaluated operator totals, output identities and the five one-win challenger leads.
- Computed the legal composition count and single-edit neighborhood sizes from the captured allowed pool and supplied recipes, without constructing new teams.

This is targeted authentication and recounting, **not a fresh full archive/native replay audit**. I did not rehash every compressed battle report or enumerate the entire live reservation registry. Existing full audits remain supporting historical evidence. The most recent recorded history count is 633,313 values across 240 files; a future admission must refresh it. Reading an old count does not establish the live union.

Some long-lived narrative sections are stale. For example, the state review's closing answer still calls three-reference integration the next gap although that integration has run. Its dated follow-ups, current code and primary receipts take precedence. The resource amendment is still prospective: the native confirmation profile names v1 and the Python owner still enforces 7,800 seconds. The proposed v2 allowance is not implemented merely because an amendment JSON exists.

## 1. The actual optimization problem

### Objective and distinct products

For a frozen scenario/context `c`, let `Y(x,c,s)` be one for a legal team's victory on random seed `s`, and zero for defeat or draw. The useful fitness is `p(x,c) = E_s[Y(x,c,s)]`. Faulted or incomplete simulator executions are missing/invalid evidence, not defeats.

In the current practical mode there is one context, so the optimization target is simply victory probability in that captured scenario. The generic discovery fitness uses the **minimum win rate across contexts**, followed lexicographically by remaining guardian health, survival, winning duration and stable identity. Its secondary measures are search heuristics, not additional validated objectives.

Four tasks should remain distinct:

| Task | Appropriate endpoint | What the current evidence supplies |
| --- | --- | --- |
| Find one strong exact team | Independent victory-rate and paired-reference estimates for a frozen recipe | Supported for `96b94357…` in the captured scenario |
| Reliably improve search | Distribution of output quality and useful improvement over independent search roots at a fixed cost | Not established by the confirmed recipe or conditional comparison bounds |
| Find diverse effective teams | Several independently viable teams with meaningful composition/behavior separation | Recipe distance alone is insufficient; current practical mode selects one output |
| Assess encounter balance or robustness | Prespecified floors, gear/progression contexts and encounter versions; coverage and uncertainty for that population | One floor-5 search does not establish it; a strong legal team can falsify a ceiling without proving search completeness |

**Primary development objective:** improve the expected independently measured output win probability at a fixed search-fight budget, relative to both the unchanged practical policy and the strongest confirmed reference `R* = 96b94357…`. Also measure the chance of finding a novel useful improvement and the chance/magnitude of selecting a worse team. A method that merely avoids bad outputs is useful, but should not be described as discovering stronger teams.

For operational recommendation, retain the current reference until an exact challenger passes an independent, prospectively declared gate. The existing practical gate means an observed gain of at least five points against every reference, positive adjusted paired lower bounds and supported 10% viability. **It does not establish that the true gain is at least five points**: that would require the lower bound itself to exceed five points. Search output, measured challenger and recommended replacement need separate fields.

`R*` is the strongest reference supported by the inspected fixed-family evidence, not a proof of a global optimum. Preserve the other two references as controls and possible search parents. Do not choose the benchmark reference retrospectively from each test panel's maximum.

### Representation and legality

The captured template contains **ten level-40 characters, five Essences each, tier 1, rank 2, Standard quality**, one fixed-equipment context, fixed attribute rolls, fixed actor identities/time and an uncleared encounter without contributions. The allowed pool contains **85 Essence IDs from 82 case-insensitive source-monster families**. `OwnedCopies` is null in this cohort.

`PartyChoice.Builds` maps numbered character slots to Essence ID lists. Its ID hashes that mapping. The scenario adds equipment, character identity and progression; a party ID alone is therefore not a sufficient combat-cache identity.

| Decision | Present in this search? | Consequence |
| --- | --- | --- |
| Essence membership | Yes, all 50 assignments are mutable | The primary combinatorial search variable |
| Which character owns an Essence/loadout | Yes | Owners have fixed gear/identities; moving a loadout changes the team |
| Essence ability order | Fixed to ordinal ID order | Removes permutations deliberately; does not prove order is mechanically irrelevant |
| Party placement/subgroup | Indirectly through ownership | Slots 1–5 and 6–10 are different subgroups; slot mapping is not freely relabeled |
| Equipment, progression, attributes, combat styles | Fixed | Optimizing these would be a different experiment and acquisition budget |
| Copies | Repeated Essence IDs across different characters are legal | Optional ownership limits apply globally when supplied; null does not mean a one-copy account |
| Repeated complete loadouts | Legal across owners | There is no practical-policy rule requiring ten different builds |
| Essence families | At most one member of a source family per character | Different IDs can still conflict; family legality is case-insensitive |

Subgroup membership is `floor((slot-1)/5)+1`, not an independent stored optimization gene. A transplant across that boundary changes recipient relationships while retaining the destination character's fixed gear and identity. Never canonicalize by sorting whole characters or treating subgroup swaps as equivalent without production-level equivalence evidence.

The current source prevents order mutation for composition-only policies. Sorting is an outcome-independent policy restriction, not permission to reuse measurements of historically ordered recipes as if they were identical. Canonical recipes and exact scenario bindings must both match.

With null copy limits, the number of legal unordered five-Essence loadouts is the coefficient of `z^5` in `product_f(1 + m_f*z)`, where `m_f` is the number of allowed IDs in family `f`. For this actual pool the coefficient is **32,526,117**. Ten labeled owners give `32,526,117^10`, approximately **1.33 × 10^75** legal assignments. This count describes the declared combinatorial domain, not equally powerful teams or uniform generation. Each of the three references has **3,952 distinct legal single-replacement neighbors**. Forty-four sampled neighbors inspect about 1.11% of one such neighborhood.

The practical fresh sampler shuffles Essence IDs and greedily accepts legal families; it is not a proven uniform sampler over complete legal teams. Families with different numbers of IDs and, when enabled, sequential depletion of owned copies can bias construction. Earlier owners can consume scarce copies before later owners are filled. That ownership effect is a plausible issue in other budgets, not an explanation for this null-copy-limit cohort. More consequential here is the proposal distribution's distance from known effective compositions, rather than a missing ability to represent those compositions.

### Current baseline, end to end

The relevant baseline is `retained-composition-three-references-v1`, with `tower-staged-incumbent-tie-v1`, three exact supplied starts, one root/context, 46 measured recipes, a 256-proposal cap, five nominees and one selected output. The captured selection designation remains `399bc776…`, even after the stronger team was added. The general CLI planner and older two-reference modes are not interchangeable with this baseline.

1. **Initialize.** Canonicalize explicit references and evaluate them on the current discovery panel. Historical fitness is not imported. Attempt the three supplied teams, then six fresh constructions. Those are proposal opportunities; rejections can reduce accepted fresh counts.
2. **Generate.** After initialization, every fourth proposal opportunity is fresh construction. Other opportunities cycle through single, double, cross-character, whole-character and recombination edits. Proposal scheduling is attempt-based, not successful-evaluation-based.
3. **Choose parents.** In the baseline path, use the top four discovery-ranked measurements, or with probability one quarter draw from supplied anchors. The distance-augmented `Retain` helper belongs to the separate supplied-block path; it is not the active three-reference baseline population rule.
4. **Mutate.** Single edits traverse a parent-specific randomized modular scan of owner/position/pool tuples. Some are no-ops or family-illegal. Double/cross-character mutation sometimes uses the existing mechanics interaction catalogue; otherwise it substitutes pool IDs. Whole-character replacement uses a constructive recipe. Recombination independently copies each owner's entire loadout from one of two parents.
5. **Validate/deduplicate.** Check full slot coverage, exact Essence count, allowed pool, family and optional copy constraints. Reject already measured party IDs within the arm. Rejections cost proposal opportunities and construction time but no fights. Fresh construction has up to 32 bounded construction checks.
6. **Evaluate and retain.** Each accepted recipe receives the same eight discovery seeds. Rank by win count, guardian health, survival, winning duration, then ID. Previously measured parents are not refreshed in this baseline. Newly generated descendants can depend repeatedly on those same outcomes.
7. **Nominate.** Protect all three references and add the two highest-ranked distinct challengers. Order the five by discovery rank. Forty-one of the 43 generated recipes receive no selection trials in that search.
8. **Select.** All five get 32 disjoint, fresh, paired selection seeds. Highest wins wins. Only zero-win ties use guardian health; positive ties fall back to frozen nominee order except that the designated primary is preferred when tied at the positive maximum. The optional broader reference-tie version extends that preference; it is not the baseline.
9. **Freeze and confirm.** Freeze the selected output and all reference controls before confirmation. Merge exact equivalent physical recipes while retaining their roles. The practical three-reference decision uses four or fewer physical teams and a family of ten interval quantities: four rates plus three gain/loss pairs. Minimum practical confirmation is 256 trials; the inspected practical execution used 1,000. Minimum size is not a power guarantee.

The normal baseline search costs **46×8 + 5×32 = 528 fights**. Confirmation is separate and often much more expensive. The two-reference version costs 496 search fights. In the eight reported runs the fight total does reconcile to **244,616**: 63,616 search fights and 181,000 confirmation fights, or approximately **74.0% confirmation**. This is an allocation description, not proof that those confirmations were wasted; several answered different scientific questions.

Caching is already explicit. `TowerLoadoutArchive.Key` includes scope, arm and prepared input. Cross-arm work is deliberately charged separately. Confirmation can deduplicate physical recipes. A new cross-arm or cross-stage cache requires a versioned cost and evidence contract; removing `arm` from a key alone is not a safe optimization.

## 2. What the evidence establishes

### Confirmed team and policy comparisons

The fixed-family study independently supports one exact improvement. `96b94357…` won **4,188/5,500**, versus **3,885/5,500** for `399bc776…` and **3,529/5,500** for `8287f779…`. Its gains/losses were 1,211/908 and 1,472/813, yielding +5.509 and +11.982 points. The adjusted paired lower bounds were +2.150 and +8.560 points. It alone qualified among six challengers. Four of the other five challengers finished below the designated reference; one finished above it but below the improvement gate.

The winning recipe changes two owners relative to `399bc776…`: Bark Golem to Viper on character 8 and Bark Golem to Flame Imp on character 9. That shows the current representation can express useful interacting edits. It does not show which edit caused the gain or whether each helps alone.

The subsequent practical run selected that now-supplied team, winning 762/1,000 versus 700 and 624 for the controls. This corroborates the exact recipe in another panel. Selecting a supplied winner is not an independent rediscovery.

| Comparison | Search units | Observed method difference | What it supports |
| --- | ---: | ---: | --- |
| Incumbent positive-tie preference | 24 common roots; 4 differing outputs | +2.575 pp; conditional lower +1.930 pp | Improvement for those frozen selector outputs; absolute wins missing for 20 identical-output roots |
| Reference exploration | 12 paired roots | −0.142 pp | No promotion; 11 output pairs identical |
| Owner-offset exploration | 12 paired roots | +0.175 pp | No promotion; 11 output pairs identical |
| Fresh screening | 12 paired roots | +0.800 pp; conditional lower −0.642 pp | No promotion; changed both discovery feedback and screening |
| Broader three-reference ties | 24 common roots | Exactly zero | Both selectors chose the same recipe at every root; absolute confirmation was not measured |
| Earlier racing | 3 paired roots | −0.733 pp | No promotion of the tested 16-candidate tournament; actual discovery cost 288 versus 368 |
| Earlier single-anchor neighborhood | 3 paired roots | +1.700 pp | No promotion of 44 one-edit neighbors; gains partly came from avoiding a weaker output |

The later comparison bounds condition on the frozen outputs. Thousands of confirmation trials reduce combat uncertainty for those outputs, but do not create thousands of independent search runs. Repeated recipes, shared panels and paired policies must not be counted as independent replications.

### Additional recount: compare outputs with the strongest reference

For each existing 12-root experiment, I summed each arm's selected-output wins and `96b94357…` wins on the **same root-specific confirmation panel**, then divided by 12,000. References already existed in each physical confirmation union. No missing outcomes were imputed.

| Closed experiment | Policy | Output wins /12,000 | R* wins /12,000 | Output minus R* | Roots above /equal /below R* |
| --- | --- | ---: | ---: | ---: | ---: |
| Exploration | Baseline | 8,760 | 9,139 | −3.158 pp | 3 /3 /6 |
| Exploration | Candidate | 8,743 | 9,139 | −3.300 pp | 3 /3 /6 |
| Offset | Baseline | 8,418 | 9,073 | −5.458 pp | 0 /3 /9 |
| Offset | Candidate | 8,439 | 9,073 | −5.283 pp | 0 /3 /9 |
| Fresh screening | Baseline | 8,715 | 9,138 | −3.525 pp | 1 /6 /5 |
| Fresh screening | Candidate | 8,811 | 9,138 | −2.725 pp | 1 /5 /6 |

These are **retrospective descriptive benchmarks**, newly calculated for this assessment. Do not pool them into a precision claim or reinterpret their original promotion criteria. Selection can return an older reference as well as a challenger; the deficits are not exclusively challenger errors. Nonetheless, the baseline had the stronger reference available throughout. A policy comparison can look neutral while both policies discard substantial known value.

Avoiding those recorded regressions could recover several points of output quality without discovering a team stronger than R*. That is a plausible practical improvement target, and the constant-reference arithmetic is directly demonstrated on these panels. It is **not a forecast** that the proposed beam will recover those points. There is no defensible numerical forecast for its improvement above R*'s independently measured approximately 76% win rate.

The corresponding resource receipts separate actual execution from admission allowances:

| Closed experiment | Search /confirmation fights | Recorded execution seconds, including its audits/publication | Retained scientific archive, MiB |
| --- | ---: | ---: | ---: |
| Exploration | 12,672 /41,000 | 3,540.141 | 1,150.74 |
| Offset exploration | 12,672 /39,000 | 3,409.204 | 1,114.14 |
| Fresh screening | 12,672 /43,000 | 3,506.516 | 1,209.15 |
| Three-reference tie | 12,672 /0 | 683.765 | 357.35 |

These are `completion.json.seconds` and `observedBytes` under each scientific archive in the evidence map. They exclude the separately precharged 600-second/512-MiB admission allowance and are not complete engineering totals or peak-disk measurements. They show why combat budget, elapsed time and retained storage must be reported independently. Different confirmation unions and verification work prevent interpreting them as isolated per-policy throughput measurements.

### Demonstrated weaknesses and unresolved hypotheses

| Issue | Demonstrated observation | What remains unknown |
| --- | --- | --- |
| Fresh proposal quality | Latest 24-root checkpoint recount: **381 fresh recipes, 0 wins in 3,048 discovery fights**, out of 8,832 discovery fights total (34.5%) | Their independent win probabilities; rare useful distant compositions; whether a different constructive prior helps |
| Parent concentration | Single three-reference run used six primary parents; one population persisted for 25/33 mutation decisions | Whether another parent set would have produced stronger descendants |
| Tiny feedback panels | Eight discovery seeds determine parent reuse and two challenger nominations | Exact false-elimination rate; almost all discarded candidates lack fresh measurements |
| Winner's curse | Original exploration nominees fell from 52/56 discovery wins to 148/224 selection wins | How much is adaptive overfitting, ordinary selection bias, or genuinely mediocre teams |
| Noisy final selection | Latest five challenger outputs each led the best reference by 1/32; four had different half-panel winners | Whether any is truly stronger; half-panel and leave-one-out checks are not calibrated error probabilities |
| Recombination redundancy | One root had six attempts, five duplicates; two parent pairs differed on only one owner | Impact on useful throughput when attempt limits actually bind |
| Owner coverage bias | Original exploration never directly edited slots 8–10 of R* across 12 roots | Whether those missed edits would help; offset correction did not improve the tested endpoint |
| Interaction search | Useful confirmed two-owner change; existing guided pair and block operators | Which dependencies matter most; mechanism tags alone do not measure joint benefit |
| Overhead | Verification and publication take measurable time and space | A new policy's cost profile; old per-run totals are not calibrated runtime predictions |

The 381 fresh recipes are not 381 independent experiments on eight new seeds each: each root reuses a common panel, and the trajectory is adaptive. Their zero counts are compelling engineering evidence of low realized yield, not a binomial significance test over 3,048 independent victories. In the separately inspected single practical root, 17 fresh recipes consumed 136/368 discovery fights and none became a parent or nominee. They were 43–49 replacements from the nearest reference, versus 1–6 for measured mutations. Raw diversity was mostly in a low-performing region.

Recognition has a more direct demonstrated failure than the missing-data counts alone suggest: saved fresh confirmation includes cases where selection chose substantially worse outputs despite retaining R*. In the screening comparison's pair 6, a challenger won selection 26/32 versus R* at 24/32, then confirmation was 681 versus 762. It survived every leave-one-out selection perturbation. Stability to deleting one observation is not evidence of correctness.

Conversely, the confirmed `96b94357…` initially had only a one-win selection lead. Declaring all one-win leaders noise would have discarded a real improvement. A fixed near-tie rule tuned from these cases is not a substitute for evaluation allocation.

At a true win probability of 0.7615, the probability of 8/8 is approximately 0.113 under independent Bernoulli trials. Perfect discovery scores are therefore unsurprising among many comparable teams. One discovery win is 12.5 points; one selection win is 3.125 points. In the observed 6-gain/5-loss selection contrast, the plug-in paired standard error is about **10.35 points** at n=32, compared with a 3.125-point lead. This is an illustration of resolution, not a confidence interval or an independent test after selection.

The main conclusion is therefore specific: **generation wastes a material fraction of its realized budget, recognition sometimes selects known inferior alternatives, and the archive cannot determine how many superior candidates recognition discarded**. Confirmation is capable of answering exact-team questions. Its coverage and cost dominate the campaign, but improving confirmation alone will not make the search reliably productive.

## 3. Choosing an architecture

| Approach | Interaction handling and sample use | Complexity / failure mode | Assessment |
| --- | --- | --- | --- |
| Adaptive evaluation of the existing 46 recipes | Allocates more observations to contenders, but cannot improve a poor candidate pool | Modest; can over-prune on tiny samples or repeatedly pay for anchors | Essential component, insufficient alone |
| Reference-seeded beam / iterated local search with batch racing | Keeps complete interacting teams; supports coordinated edits and fresh feedback between generations | Moderate and inspectable; local basins and breadth-versus-precision tradeoff remain | **Recommended** |
| Larger evolutionary population or learned sampling distribution | Can recombine modules and learn recurring assignments | Sparse feedback can collapse marginals; independent per-slot probabilities break family/copy constraints and interactions | Defer until stronger candidate-level data exists |
| Full quality-diversity archive / diverse restarts | Can maintain effective alternatives in different behavioral niches | Descriptor choice and noisy niche elites add another selection problem; many cells need many fights | Use a small diversity reserve now, not a full archive |
| Surrogate-assisted optimization | Could share information across similar recipes and predict interactions | Biased adaptive data, runtime shifts, sparse fresh labels, high-dimensional categorical structure | Not justified as the first change |

The current baseline is already a small evolutionary/local search. Renaming it a genetic algorithm would change nothing. The proposed change is the **feedback and proposal contract**: keep challenger capacity separate from references, refresh competitive cohorts on new panels, preserve some distance diversity, and reduce destructive full-team restarts in favor of legal neighborhoods of measured strong teams.

Fixed-budget best-arm work such as [Karnin, Koren and Somekh (2013)](https://proceedings.mlr.press/v28/karnin13.html) motivates successive allocation to survivors. Its guarantees do not automatically apply to adaptively created team populations, correlated paired outcomes, hard diversity slots or this recipe domain. Use the allocation idea and validate the complete policy here.

The existing `TowerEvaluationAllocationSearch` is a useful starting interface, not an untested blank slate. Its earlier experiment admitted 16 teams in two rounds, including six initial fresh teams, screened on eight values, promoted top parents plus anchors to 16 new values, and used only the last promotion panel to nominate. It actually spent 288 discovery fights while the comparator spent 368. It returned the old confirmed team in all three roots. That constrains claims that simply adding fresh parent tournaments will help. The new proposal must earn its value through **better admitted teams, explicit challenger slots and full use of the matched budget**, not by invoking the word “racing.”

Likewise, fresh screening's 46×4 +23×8 design bought breadth by halving the feedback available while generating teams. Its populations differed from the baseline, and screen membership still depended on four trials. It did not isolate the benefit of fresh screening on a common frozen pool. The 17/24 changed challenger nominations demonstrate that screening can change recognition; they do not demonstrate better output quality.

[MAP-Elites](https://arxiv.org/abs/1504.04909) motivates retaining strong solutions across descriptors. Here there is no validated map of useful behavioral niches, and the existing log-based behavior bins can be noisy. A four-parent beam with one composition-diversity slot gives an inexpensive exploration mechanism without maintaining hundreds of noisy elites.

Surrogates should wait for a table of exact, context-bound recipes with explicit sample counts, shared-seed identities, provenance and independent validation across roots. Many fights on a few selected teams do not supply the same information as many representative labeled teams. If later justified, start with a regularized tabular model using owner/subgroup Essence features and a few declared interactions; compare its out-of-root ranking and fresh simulator regret against simple baselines. Split by root/lineage and deduplicate recipes before evaluating generalization. Do not random-split correlated trial rows.

Evidence that would change the recommendation includes: fresh evaluation finding that discarded candidates are almost uniformly worse than the chosen ones; a generation-only intervention dominating the full policy; fewer candidates causing systematic loss of strong lineages; or a blinded surrogate ranking yielding reproducible gains per combat. In those cases shift the budget toward generation, broaden restarts, or introduce a surrogate only where the new evidence supports it.

## 4. Proposed algorithm in concrete terms

All values in this section are **proposed design defaults, not tuned or proven settings**. The first version targets this one captured context. Multi-context robustness requires a separate objective and cost contract.

### State and legal proposals

Keep the existing canonical owner-to-Essence representation, validators, fixed equipment and full scenario identity. Retain:

- The three immutable references, with R* designated as the development benchmark.
- A beam of four distinct **challenger** teams; references do not consume those four positions.
- An archive of exact candidate IDs, parentage, operator, changed owners, legality/rejection reasons and measured panels.
- Separate random streams for proposal type, owner scheduling, legal replacement choices and combat panels.

The reference preference used for final ties remains the current declared `399bc776…` rule in the first comparison. This avoids bundling another tie hypothesis into the algorithm test. R* is nevertheless always retained, measured and used as the principal external benchmark. Changing a future default designation to R* can be assessed separately; it is not assumed to solve strict-leader errors.

Propose a first wave of nine unique challengers and a second wave of eight. Suggested operator opportunity counts are:

| Proposal class | Wave 1 | Wave 2 | Construction |
| --- | ---: | ---: | --- |
| Legal single replacement | 4 | 3 | Choose owner/removal, then sample an actually family/copy-legal replacement |
| Coordinated two- or three-assignment edit | 2 | 2 | Free all affected copies first; combine guided interaction pairs with unguided legal pairs; verify realized distance |
| Partial character rebuild | 1 | 1 | Retain 2–3 of the five Essences and reconstruct the remainder legally |
| Loadout transfer / complementary recombination | 1 | 1 | Respect subgroup/gear destinations; require meaningful differences and reject parent clones |
| Fresh legal team | 1 | 1 | Preserve a broad escape route; no hard role/core requirement |

Advance operator and owner counters on every attempted proposal, including rejections. Cycle a root-shuffled permutation of owners independently for each operator/parent; do not always begin at character 1. Use at most 128 proposal attempts per wave, at most 32 construction checks per attempt. Fill the declared number of unique candidates or mark the search incomplete; do not quietly substitute a favorable smaller experiment.

For nonfresh operators, start with 50% of parent choices from R*, 25% from the other two references, and 25% from the challenger beam uniformly. In the first wave, when no beam exists, redirect that last share uniformly across the references. These probabilities are an explicit supplied-knowledge prior; they do not import historical trial outcomes as new measurements. Fair comparison gives both policies the same three reference recipes.

For recombination, require at least two differing owner loadouts before mixing whole owners, and sample a nonempty proper subset of the differing owners. Reject known physical compositions before evaluation. If only one owner differs, copying whole owners can produce only a parent; route that proposal to a separately recorded legal partial-loadout operator. Copy/family limits remain authoritative. No subgroup transplant is assumed beneficial merely because its mechanics look coherent.

This policy reduces full fresh teams from roughly a third of measured discovery candidates to two of seventeen new candidates. That is a deliberately substantial intervention and the main coverage risk. It retains occasional full restarts, other-reference neighborhoods and multi-owner edits so that an entirely monotone hill climb is not required.

### Fresh batch allocation with an exact 528-fight cap

The small pilot should use simple fixed rungs with adaptive survivor membership. This is easier to inspect than an elaborate posterior allocator and makes the budget exact.

| Stage | Distinct teams | New trials each | Fights |
| --- | ---: | ---: | ---: |
| Wave 1 screen: 3 references +9 new challengers | 12 | 8 | 96 |
| Wave 1 continuation: references +4 challenger survivors | 7 | 8 | 56 |
| Wave 2 screen: references +4 carried challengers +8 new | 15 | 8 | 120 |
| Wave 2 continuation: references +4 challenger survivors | 7 | 8 | 56 |
| Final selection: references +2 challengers | 5 | 40 | 200 |
| **Total search** | | | **528** |

All five blocks use mutually disjoint seeds. The wave-2 cohort is frozen before its fresh screen, including the carried parents. An old parent competes against new children using that wave's common evidence, not its previously selected high score. Within a wave, surviving candidates can use the cumulative 16 outcomes from its two rungs as a search score. Prior-wave outcomes are retained for diagnosis, not silently pooled with unequal histories for ranking.

At the first rung of each wave, keep the top three challengers by wins, then one diversity challenger from those within one win of the third-ranked challenger's count. Among eligible nonelite candidates, maximize the minimum owner-preserving replacement distance to the three elites; resolve ties by existing fitness then ID. If no additional eligible candidate exists, retain the next ranked challenger. References are protected separately. This rule defines a small competitive diversity reserve; it does not select arbitrarily weak teams merely for being distant.

After the continuation, all four challengers form the next beam in cumulative wave-score order. After wave 2, nominate the top two challengers by that wave's cumulative 16-trial fitness. Sort the resulting five-member set, including references, by the same common wave-2 fitness before freezing its order. Do not place references first merely because they are protected members: that would silently introduce a broader positive-tie preference. Use the existing final win/tie semantics on the new 40-trial panel and freeze one output.

The choice of 40 instead of 32 selection trials only reduces sampling standard error by a factor of `sqrt(32/40) ≈ 0.894`, about 10.6%. **That is not a solution to fine five-point discrimination by itself.** The architectural hypothesis is that improved proposals and fresh feedback produce a more useful shortlist at the same total cost. Independent confirmation still bears the strength claim.

The reduction from 43 generated challengers to 17 is intentional and unproven. If the experiment shows that breadth matters more than refreshed feedback, abandon this allocation. Do not promote it because it has more stages or better-looking uncertainty plots.

### Pseudocode

```text
search(scope, references[3], benchmark_Rstar, root, budget=528):
    validate exact scenario, canonical recipes, families, copies and versions
    bind distinct construction streams and five fresh search panels
    reserve confirmation separately; keep its outcomes inaccessible
    beam = []
    seen = IDs(references)
    archive = empty

    for wave in [1, 2]:
        new_count = 9 if wave == 1 else 8
        children = propose_unique_legal_teams(
            references, beam, operator_schedule[wave], seen,
            attempts_limit=128, checks_limit=32)
        if count(children) != new_count: return IncompleteEvidence
        seen += IDs(children)

        cohort = distinct(references + beam + children)
        freeze(cohort, next_fresh_panel(8), before_any_outcome=True)
        screen = evaluate_complete_paired_panel(cohort)
        validate every scheduled outcome and exact identity

        challengers = cohort excluding references
        survivors = top_three_plus_competitive_diverse_one(screen, challengers)
        freeze(references + survivors, next_fresh_panel(8))
        continuation = evaluate_complete_paired_panel(references + survivors)
        beam = rank(survivors, this_wave_screen + continuation)
        archive decisions, paired gains/losses, dropped IDs, all costs

    nominees = rank(references + first_two(beam), wave_2_common_16_trials)
    freeze(nominees, next_fresh_panel(40))
    selection = evaluate_complete_paired_panel(nominees)
    output = existing_declared_win_and_tie_selector(nominees, selection)
    assert combat_queries == 528
    freeze(output, all_reference_controls, complete_scope)
    return SearchOutput(output), RecommendedReference(benchmark_Rstar), archive

confirm(frozen_output, frozen_controls, preregistered_panel_and_gate):
    evaluate each distinct exact physical recipe on the fresh common panel
    validate completeness, pairing, scope and both audits
    evaluate the full declared multiplicity family once
    recommend a replacement only if its independent gate passes
    otherwise retain existing confirmed recommendations
```

The search result is provisional even when its selection count is high. In an operational run a selected older reference does not revoke R*'s prior confirmation. In an algorithm experiment the raw selected output is still measured and scored; reporting only the safe deployed fallback would conceal a bad search.

### Elimination, uncertainty and stopping

The pilot's rung elimination is **budget pruning**, not a statistical assertion that discarded teams are inferior. Eight trials cannot support simultaneous confident elimination among many close teams. Report the cutoff, ties, distance reserve and missing later measurements explicitly.

If a later larger budget enables confidence-based elimination, use paired differences `D_s = Y(candidate,s) − Y(reference,s)` and a valid multiple-look construction. A simple finite-look Hoeffding bound for K prespecified contrasts and L prespecified looks has radius `sqrt(2*log(2*K*L/alpha)/n)` for differences in [-1,1], under the relevant independent-sampling assumptions. At these small n it is extremely wide. Do not repeatedly apply ordinary 95% Wilson intervals and call that sequentially valid. A properly implemented [confidence sequence](https://arxiv.org/abs/1810.08240) is an alternative for arbitrary stopping, with candidate-birth/multiplicity and fresh-sampling rules made explicit.

In particular, a candidate invented using a seed panel cannot retrospectively treat that panel as fresh validation. The next cohort is fixed before new outcomes arrive. Search heuristics may use adaptive training scores; strength statements may not.

Stop at the declared fight, proposal, time or storage limit, or after final selection. No outcome-based root replacement, hidden refill, extra “tie-breaking” battles or silent fallback from incomplete measurements. An invalid or partial block cannot become a completed fitness row. A failed run remains in the method reliability/accounting report; it cannot simply be omitted from successful-run averages.

### Pairing, cache and diversity safeguards

Use identical ordered combat seeds and compatible scenario/identity bindings for every candidate in a comparison block. Equal seed integers are a coupling device, not a guarantee that different teams consume identical random events. Their random call paths can diverge. Measure discordant outcomes and actual covariance; common random numbers need not reduce variance in every contrast.

The minimum evaluation key includes content and effective settings, gameplay/runtime identity, the exact prepared scenario including actor identities/gear/ordering, seed, and execution semantics. Keep stage access permissions separate from physical result identity. A cached outcome cannot be counted twice as a new observation or used to bypass a held-out stage. An old recipe may be reused as a proposal; its old confirmation trials remain development evidence.

Within a run, deduplicate before fighting and reuse observations only for the same authorized recipe/panel. Across experimental arms, maintain equal **logical query budgets** and disclose physical deduplication separately; savings must not purchase extra candidates for one arm. The simplest first implementation keeps the existing arm charging and shares only frozen confirmation unions. Cross-root training-result caching is not needed for the pilot.

Retaining an immutable R* avoids repeatedly needing to rediscover it. Give references no historical success pseudocounts, protect their membership, and spend proposal opportunities on new legal compositions. Keep repeated recipe IDs and near-duplicate distances in telemetry so “many evaluations” cannot be mistaken for broad coverage.

Composition diversity is only a search device. If the desired product becomes a portfolio of diverse effective teams, define a separate portfolio endpoint: for example, independently viable alternatives under the same ownership budget, distinct owner/subgroup compositions, and measured outcome complementarity. Choosing a team after seeing a seed's outcome is an unattainable oracle and must not be counted as deployable portfolio strength.

### Independent confirmation after adaptation

Before any confirmatory outcome is read, freeze exact output membership, scenario scope, contrasts, sample count, error family and decision rule. Prefer freezing every root's outputs before the first confirmation result is exposed to the experiment owner. Separate search and confirmation RNG domains and enforce the repository's historical exclusion rules.

One output plus three references gives four rates and three gain/loss contrasts, hence family ten under the current approximate Wilson construction. Multiple candidate outputs create a larger family if any is eligible for adoption. Selecting the best confirmation estimate and then reusing a single-candidate interval is invalid. Either confirm one prospectively selected team or adjust across the complete frozen set and report every result.

The method pilot below is **not a team-adoption study**. Its 256-trial panels estimate algorithm outputs; they should not be relabeled as adequately powered five-point adoption tests. A subsequent exact-team confirmation can use the existing fixed-family machinery with a fresh panel and its own precision calculation. Do not reuse pilot holdout outcomes after they have informed candidate or algorithm selection.

## 5. A bounded experiment that can change the decision

### First experiment: complete policy, unchanged baseline, constant-reference benchmark

Freeze two policies before allocation:

- **B:** the current three-reference practical baseline, 46×8 discovery plus 5×32 selection, original incumbent-tie selector and original primary designation.
- **N:** the proposed two-wave beam/racing policy, exactly 528 search fights, same scenario, pool, legal constraints, three references and final tie semantics.
- **R* benchmark:** always return `96b94357…`; zero search fights. Measure it in every confirmation union, including roots where B and N agree.

Use **12 new independent paired roots**, each with independent construction randomness and disjoint search/evaluation panels from every other root and historical experiment. Within a root, pair the policies' random environments where valid. A shared initial discovery prefix is permissible if declared, but neither policy can read the other's outcomes. Label search and selection panels by role: no policy's selection or final evaluation panel may be another policy's adaptive training panel. Different algorithms consuming the same construction seed are not guaranteed to create comparable proposal sequences.

After all 24 outputs freeze, evaluate each root's distinct union of `{B output, N output, three references}` on **256 fresh common seeds**. Maximum five physical teams per root. Identical outputs share observations and contribute exact zero to the method contrast, but their absolute strength relative to R* is still measured. This directly avoids the missing absolute outcomes in selector-only studies.

| Pilot component | Maximum fights |
| --- | ---: |
| Two searches ×12 roots ×528 | 12,672 |
| Up to five frozen recipes ×12 roots ×256 | 15,360 |
| **Total** | **28,032** |

No unused allowance transfers between roots or policies. No new campaign is launched by this assessment. Admission must establish a separate elapsed-time/storage allowance; no runtime claim is inferred from the fight count alone.

### Endpoints and uncertainty

For each root r, calculate paired held-out differences `d_r = p_hat(N_r) − p_hat(B_r)` and `g_r = p_hat(N_r) − p_hat(R*)`. Report every root, selected identities, all reference rates, gains/losses, and the following:

| Measure | Purpose |
| --- | --- |
| Equally weighted mean `d_r` | Primary method-comparison endpoint |
| Mean/median/worst `g_r`, plus all twelve values | Whether searching improves on available knowledge or introduces regressions |
| Novel outputs and their gains over R* | Separates new discovery from returning a reference |
| Counts above +3/+5 points and below −3/−5 points | Descriptive effect distribution; noisy point classifications are not true success probabilities |
| Independent qualification rate, if later measured | Operational success probability of a confirmed improvement; distinct from raw point gains |
| Operator yield, unique recipes, parent diversity, cutoff ties, survival by rung | Diagnoses the failure mechanism, not an alternative promotion endpoint |
| Logical queries, actual fights, cache hits, invalid proposals | Budget comparability and duplicate work |
| End-to-end seconds, combat seconds, verification seconds, retained/peak bytes | Practical throughput and resource costs, reported separately |

Compute paired seed-level uncertainty for frozen outputs, accounting for dependence through shared references. Also report root-level variation. A root-level paired t interval or root bootstrap can be a clearly labeled small-sample approximation; neither should be sold as distribution-free reliability evidence. Do not bootstrap individual fight rows as though roots do not exist. Do not double-count shared outcomes for repeated references.

An intentionally conservative conditional Hoeffding calculation illustrates the pilot's limits. With 3,072 paired evaluation positions and differences in [-1,1], the one-sided 95% independent-sampling margin is `sqrt(2*log(20)/3072) ≈ 4.42 points`. The declared finite-population sampling protocol also needs its depletion correction. This is **not** a claim of 80% power, and it does not bound future-root variability. For twelve independent bounded root differences, a distribution-free root-level bound would be much wider. Small average improvements can remain inconclusive.

A decisive small experiment here means one that can expose a large regression, identify obviously unproductive allocation, or justify a carefully sized next study. It does not mean guaranteeing a conclusive answer about a two- or five-point population improvement with twelve roots.

### Proposed decision rules

Freeze these as development criteria before running; they are proposed engineering thresholds, not historical statistical requirements:

1. **Technical acceptance:** every planned root completes with valid pairing, complete panels, correct scope, exact search cost and both independent/native evidence checks. Report any operational failure; do not replace failed roots. A technical failure blocks an efficacy claim.
2. **Go to a larger fresh evaluation:** N's mean observed advantage over B is at least two points, its mean relative to R* is nonnegative, and at least three roots produce novel outputs at least three observed points above R*. This is permission to investigate, not evidence of a reliable discovery probability. Report uncertainty even when these point gates pass.
3. **Abandon this configuration:** after the complete pilot, N is at least two observed points worse than B, or materially below R* without a compensating yield of promising novel outputs. Archive the negative result; do not reinterpret better diversity or lower verification cost as strength promotion.
4. **Inconclusive:** small or mixed effects, or failure of the exploratory yield criterion. Retain the baseline and R*. Use diagnostics to decide whether one separately specified follow-up is worthwhile; do not extend the same panel until it passes.
5. **Actual policy promotion:** require a new, prespecified experiment showing positive root-aware uncertainty bounds, a useful mean effect, acceptable regression/failure rates and consistent benefit relative to R*. Choose its root count and within-root sample count from pilot variance and a declared precision target, then freeze it before fresh allocation. Do not invent the required power or guarantee an arbitrary fixed root count here.

The go threshold is deliberately stronger than “outperforms an occasionally poor comparator”: it also requires novel promising outputs. An always-R* policy may be the best practical recommendation even if the search pilot fails.

### Ablations that distinguish generation from recognition

Do not run a large parameter sweep on the same holdout. If the first pilot warrants further work, use a fresh **2×2 comparison**:

| Policy | Proposal system | Evaluation allocation |
| --- | --- | --- |
| B | Current | Current 46×8 +5×32 |
| G | New legal neighborhood mix and competitive diversity parent rule | Current candidate/stage budget |
| A | Current operator/fresh schedule and parent choices, adapted only as needed to freeze wave batches | New two-wave allocation |
| N | New | New |

Each receives 528 logical search fights. For G, repeat the declared opportunity cycle until 43 unique challengers have been evaluated; it is not a 17-candidate policy. For A, proposal parent decisions within each batch are frozen from the preceding wave's measurements; that timing change is an unavoidable part of the allocation intervention, not an isolated trial-count effect. Document this precisely. Any diversity change is part of G/N, not silently added to A.

Twelve roots with four searches and at most seven confirmation recipes per root at 256 trials would cost at most **46,848 fights**: `12×(4×528 +7×256)`. This is a **separate possible follow-up**, not additional work hidden inside the first pilot's allowance. Predeclare N−B as primary; G−B, A−B and interaction effects are secondary with appropriate multiplicity or explicitly descriptive status. If G works and N does not, keep the generator and discard racing. If A works without G, stop developing elaborate operators.

Only after that should a no-diversity ablation or a different fresh-restart fraction be considered. Avoid simultaneous tuning of beam size, confidence cutoffs, mutation radius, primary-reference designation and search seeds.

### Historical reuse and missing-data experiment

All inspected historical results are now development data. Reuse exact recipes as explicitly supplied inputs, source/runtime captures as compatibility references, saved outcome rows as arithmetic fixtures, and empirical discordance/timing as planning information. Never treat a retrospective rule evaluated on these same outcomes as fresh method confirmation.

A lower-code alternative, if implementing the pilot is premature, is a **frozen-pool recognition diagnostic**. Before collecting new outcomes, choose a fixed set of historical roots and retain each root's two nominees, two discovery-cutoff near misses, two randomly selected lower-ranked mutations, and the three references. Measure all nine on a new fixed panel, without changing recipes or ranking by unseen scores. At eight roots and 256 trials this costs at most **18,432 fights**. It estimates whether strong missed candidates exist in those sampled pools; it does not estimate the counterfactual full adaptive trajectory or general search reliability. Record the sampling probabilities and keep strata separate. Do not measure only already attractive near misses and generalize to all discarded candidates.

This diagnostic directly addresses an unidentifiable quantity in the existing archive. It is a better fallback algorithm-design study than spending 52,000 fights only on already selected outputs. It is not required in addition to the primary pilot, and both need separately authorized prospective protocols.

## 6. Implementation sequence and the pending confirmation

### Essential algorithm work

| Priority | Component and concrete files | Deliverable |
| --- | --- | --- |
| 1 | `TowerBossDiscoveryRun.Measure`, `TowerEvaluationAllocationSearch.PanelEvaluator`, measurement records in `TowerBossGeneration.cs` | Versioned candidate/panel observations with exact ordered seeds, paired outcomes, sample count and stage provenance; frozen-batch racer usable without combat in tests |
| 2 | New separately versioned kernel beside `TowerEvaluationAllocationSearch.cs`; dispatch in `TowerBossImprovement.cs` | Explicit two-wave state machine, protected references, four challenger slots, complete blocks, exact 528-fight budget |
| 3 | `TowerSuppliedCompositionSearch.cs`, `TowerBossPartyGenerator.cs`, `TowerReferenceExploration.cs` or a small new operator module | Legal conditional sampling, root-shuffled owner schedules, bounded coordinated edits, clone-free recombination; old versions unchanged |
| 4 | `TowerBossDiscoveryContract.cs`, `TowerPracticalAllocation.cs`, `TowerBossStudy.cs`, `TowerBossStudyPolicy.cs` | New cost/schedule validation, five frozen nominees, 40-trial selection, independent final freeze; reject accidental use through legacy fixed-panel contracts |
| 5 | `TowerReferenceExplorationComparison.cs` and its archive/runner pattern, `TowerPracticalSearch.cs` | One prospective method pilot reporting absolute gains relative to R*, paired policy effects and operational failures |

Do not extend a giant policy predicate with behavior that secretly changes existing replay. A small explicit version and stage record are preferable to a new generic optimization framework. Preserve Core/Infrastructure dependencies: all search policy belongs in the offline tool, not gameplay code.

The very first kernel can accept **pre-existing frozen candidate IDs and literal outcome matrices**. It need not generate a party or invoke native preparation to prove its state machine, survivor accounting and stage isolation. A bounded implementation of this evaluator creates reusable evidence for both the beam policy and the frozen-pool diagnostic.

### Essential correctness and statistical safeguards

- Exact legality and canonical identity, separate full combat identity, owner/subgroup preservation and optional global copy constraints.
- Freeze membership before each fresh block; no unequal-panel ranking disguised as common fitness; retain all missing measurements as missing.
- Fixed logical/physical cost accounting, no duplicate observation inflation, no hidden refill after rejection or cache savings.
- Deterministic reproduction of proposal/survivor decisions and archive reconstruction under the producing runtime.
- Independent final evaluation, complete multiplicity family, original confirmation samples inaccessible to adaptive search.
- Separate raw search output, independently evaluated output and recommendation; R* remains available when improvement is unconfirmed.

Relevant verification should use literal deterministic evaluators to test delayed superior candidates, close ties, all-zero blocks, catastrophic losers, duplicate recipes, family/copy failures, reference protection, incomplete panels, changed seed order, cancellation and exact budget boundaries. Test that a late superior team can enter the beam and that a dropped team is not falsely assigned a later score. Maintain legacy replay fixtures. Run backend checks through `build/run-tests.ps1`; a passing synthetic test validates implementation behavior, not scientific superiority.

### Optional work and infrastructure that can wait

Optional: cumulative confidence sequences at larger budgets, broader beam/restart schedules, explicit within/across-subgroup modules, alternative portfolios, a regularized surrogate after representative data exists, or an independently justified ability-order/equipment search. None is necessary for the first pilot.

Defer: a generalized optimization service, a full MAP-Elites grid, distributed combat execution, a new database/dashboard, global cross-campaign caching, broad archive migration, and rebuilding all historical engineering accounting. Reuse existing captured-runtime, owned-process, reservation and archive machinery. Optimize overhead only after measuring it in the actual proposed workload.

The recorded verifier comparison, 55.94 versus 18.41 seconds, is useful engineering progress on one warm/cached ordering. It does not establish an end-to-end admission speedup or require a new accounting project before a read-only design can be completed. Future combat still needs valid current admission, time/storage ownership and historical exclusions.

### Is the 52,000-fight confirmation the best next step?

**For algorithm design, no. For resolving those five exact teams' strength, it is a reasonable, already specified study.**

It asks a clean question: five selected historical challengers versus all three controls on 6,500 paired values, with family 38 and all-qualifier reporting. Its documented conditional power lower bound assumes a specified candidate truly gains eight points against each reference. It is not the probability that any of these five candidates is useful, nor power to establish general search reliability. The actual one-win selection margins do not supply that planning alternative.

Even a fully completed result would leave most discarded recipes unmeasured and would not compare a replacement generation/allocation policy. A success would expand the reference set; failure would reject those five exact improvements without identifying where a better search should spend its budget. The proposed 28,032-fight pilot asks the algorithm question directly; the 18,432-fight frozen-pool diagnostic asks the missing recognition question with less implementation work.

The admission failure was real and pre-combat. The saved receipt reports `AdmissionFailedNoReservation`, 499.765 measured seconds, a full 600-second/512-MiB charge, zero fights and zero new values. The prospective amendment proposes cumulative 8,400 seconds/4.5 GiB and requires versioned enforcement; current code still has v1's 7,800-second total. Do not launch against an amended Markdown allowance. If exact-team confirmation is chosen later, implement and verify that accounting change first and preserve the failed attempt's charge. This assessment neither resumes nor cancels the historical plan.

## 7. Evidence map

Line references below describe the inspected working tree on 23 September. Historical artifacts bind their producing versions independently. JSON links use line 1 as the document entry point; field names identify the evidence within large files.

| Evidence | Location and relevant lines/fields |
| --- | --- |
| Canonical composition and order restriction | [TowerCompositionSearch.cs](../LL/tools/BalanceHarness/TowerCompositionSearch.cs#L9), lines 9–18; [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs#L332), 332–381 |
| Supplied initialization, parent concentration and attempt schedule | [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs#L147), 147–259; `Retain` at 75–87 is a different path |
| Protected references and two challenger nominations | [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs#L271), 271–299 |
| Single scan, fresh sampling and legality | [TowerSuppliedCompositionSearch.cs](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs#L323), 323–359 and 419–444 |
| Complete-matrix fitness and ordering | [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs#L184), 184–201 |
| Panel measurement and actual battle interface | [TowerBossDiscoveryRun.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryRun.cs#L16), 16–38 |
| Scenario construction, constraints and costs | [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs#L127), 127–137, 227–298, 318–355, 475–503 |
| Production subgroup rule | [WorldTowerPartyRules.cs](../LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs#L5), 5–20 |
| Production preparation, seed and executor | [TowerBattleRunner.cs](../LL/tools/BalanceHarness/TowerBattleRunner.cs#L37), 37–81 and 96–128 |
| Positive/zero tie semantics and confirmation freeze | [TowerBossStudyPolicy.cs](../LL/tools/BalanceHarness/TowerBossStudyPolicy.cs#L85), 85–112 and 128–168 |
| Exact trial cache and arm namespace | [TowerLoadoutArchive.cs](../LL/tools/BalanceHarness/TowerLoadoutArchive.cs#L12), 12–46 |
| Practical gate, approximate intervals and exports | [TowerPracticalSearch.cs](../LL/tools/BalanceHarness/TowerPracticalSearch.cs#L57), 57–104 and 111–128 |
| Explicit designated-primary preset | [TowerPracticalPreset.cs](../LL/tools/BalanceHarness/TowerPracticalPreset.cs#L13), 13–37 |
| Three-reference reuse and preserved primary designation | [TowerPracticalThreeReference.cs](../LL/tools/BalanceHarness/TowerPracticalThreeReference.cs#L21), 21–65 |
| Original/offset exploration and owner traversal | [TowerReferenceExploration.cs](../LL/tools/BalanceHarness/TowerReferenceExploration.cs#L23), 23–84 |
| Fresh screening width, samples and nomination | [TowerPracticalScreening.cs](../LL/tools/BalanceHarness/TowerPracticalScreening.cs#L13), 13–31 and 48–84 |
| Existing racing protocol | [TowerEvaluationAllocationSearch.cs](../LL/tools/BalanceHarness/TowerEvaluationAllocationSearch.cs#L16), constants/cost at 16–34; `RunAsync` and `promoted`/`parents` decisions |
| Baseline scope, pool and ownership count used here | [Exploration template](../TestResults/balance/tower-reference-exploration-comparison-20260922/template.json#L1): `budget`, `requiredPartySize`, `allowedEssences`, `ownedCopies`, `starts`, `stages` |
| Confirmed team, eight rates and twelve contrasts | [Fixed-family result](../TestResults/balance/tower-fixed-family-confirmation-20260922/result.json#L1); [saved outcome rows](../TestResults/balance/tower-fixed-family-confirmation-20260922/study/study.json#L1); [external execution receipt](Tower-Practical-Fixed-Family-Confirmation-Execution.json#L1) |
| Practical retained reference | [Three-reference result](../TestResults/balance/tower-practical-three-reference-20260922/result.json#L1); [execution completion](../TestResults/three-reference-practical-execution-20260922/completion.json#L1) |
| Incumbent-tie effect and its conditional denominator | [Incumbent-tie result](../TestResults/balance/tower-incumbent-tie-comparison-20260922/result.json#L1): `pairs`, `activeRestarts`, `meanDifference`, `lowerBound`, `fights` |
| Exploration effects and absolute output/reference recount | [Original result](../TestResults/balance/tower-reference-exploration-comparison-20260922/result.json#L1), [saved rows](../TestResults/balance/tower-reference-exploration-comparison-20260922/study/study.json#L1); [offset result](../TestResults/balance/tower-reference-exploration-offset-comparison-20260922/result.json#L1), [offset rows](../TestResults/balance/tower-reference-exploration-offset-comparison-20260922/study/study.json#L1) |
| Fresh-screening effects and absolute recount | [Result](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/result.json#L1), `descriptiveViews.pipeline` distinguishes arms; [saved rows](../TestResults/balance/tower-practical-fresh-screening-comparison-20260922/study/study.json#L1) |
| Latest 24-root zero difference and raw search census | [Tie result](../TestResults/balance/tower-three-reference-tie-comparison-20260923/result.json#L1); [root 8 checkpoint](../TestResults/balance/tower-three-reference-tie-comparison-20260923/study/search-08.json#L1) and sibling `search-01` through `search-24` files |
| Single-root geometry and parent/recombination census | [Coverage summary](../TestResults/three-reference-search-coverage-20260922/summary.json#L1), `groups`, `populationRuns`, `recombinations`; [review](Tower-Practical-Three-Reference-Coverage-Review.md) |
| Nomination missingness, fresh-score decline and sensitivity | [Search-stage review](Tower-Practical-Search-Stage-Review.md#L7), 7–75; [saved review](../TestResults/practical-search-stage-review-20260922/review.json#L1); [exploration diagnosis](Tower-Practical-Reference-Exploration-Diagnosis.md) |
| Confirmation can find a true one-win selection leader | [Selection-margin review](Tower-Practical-Selection-Margin-Review.md); [fixed-family execution](Tower-Practical-Fixed-Family-Confirmation-Execution.md); [confirmed reuse](Tower-Confirmed-Team-Reuse.md) |
| Limits of selection perturbation and screening attribution | [Fresh-screening stage review](Tower-Practical-Fresh-Screening-Stage-Review.md); [three-reference tie stage review](Tower-Practical-Three-Reference-Tie-Stage-Review.md#L22), 22–46 |
| Earlier negative allocation/local-search evidence | [Racing comparison](Tower-Practical-Search-Allocation-Comparison.md), [native result](../TestResults/balance/tower-allocation-comparison-20260917/result.json#L1); [anchored comparison](Tower-Practical-Anchored-Neighborhood-Comparison.md), [native result](../TestResults/balance/tower-anchored-comparison-20260917/result.json#L1) |
| Pending exact-family protocol and qualified power assumptions | [Confirmation plan](Tower-Practical-Three-Reference-Confirmation-Plan.md#L9), 9–69; [implementation](Tower-Practical-Three-Reference-Confirmation-Implementation.md) |
| Actual failure and still-prospective resource amendment | [Failure receipt](../TestResults/three-reference-confirmation-admission-20260923/failure.json#L1); [amendment](Tower-Practical-Three-Reference-Confirmation-Resource-Amendment.md#L28); [native version profile](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationProtocol.cs#L24); [Python ceiling](../build/run-three-reference-confirmation.py#L22) |
| Broad historical context and user-facing command distinctions | [State review](Tower-Balancing-Tool-State-and-Gaps-Review.md), [practical guide](../LL/tools/BalanceHarness/PRACTICAL-SEARCH.md#L42), [README](../LL/tools/BalanceHarness/README.md) |

Scientific manifest SHA-256 pins checked for the principal recounts:

| Archive | Manifest hash |
| --- | --- |
| Fixed family | `997b72992fb5a66821b165e582c891b187ecc50b15e11315c5ba7e5ce214c4fe` |
| Exploration | `482707e32c6e4e398793e45e61627b452a765db237c382a9c75a34c8f6163a7e` |
| Offset exploration | `273ecad90d0f4532e889e77feae153dfe6f559a1ce69010a27af4d1778821431` |
| Fresh screening | `031c7dfdc14f71ff911fdec94472bd161ef72d99b90882ec23ec93dd3fe38042` |
| Three-reference tie | `68f7468ba270f1d51a075772a6fed3d800ad627a1376a00c4829c10ef9d806d1` |

The offset and screening pins are retained in their respective execution directories' `scientific-pin.json` files and were also checked. Manifest authentication here covers the stated consumed files; it is not a claim that every archive member was re-audited during this assessment.

## Verification and delivery

The only repository file added by this assessment is this document. Existing uncommitted source, tests, reports and gameplay changes were preserved. Verification used bounded Python reads with the bundled interpreter (`-B -X utf8`), SHA-256 comparisons, saved-outcome recounts, exact budget/space arithmetic, 56 local Markdown target/line checks and direct new-file whitespace checks. Both scoped `git diff --check` and `git diff --no-index --check -- NUL <document>` were run; the latter includes the untracked document and reported only the repository's LF-to-CRLF normalization warning (difference exit status 1).

One exploratory checkpoint glob also matched a non-checkpoint JSON; that read-only analysis stopped with `KeyError` and was corrected to the exact `search-\d{2}.json` pattern. Two initial text searches used nonexistent guessed filenames; the actual allocation report and battle-input definition location were then resolved from repository files. No simulator command was involved in either correction. All final evidence calculations and document checks completed.

Backend tests were not run because no executable behavior changed; no required verification command remained blocked. A future implementation must use `build/run-tests.ps1`. No migrations, application configuration changes, database actions or deployment implications arise from this document. The proposed algorithm and experiments remain unimplemented and unrun.
