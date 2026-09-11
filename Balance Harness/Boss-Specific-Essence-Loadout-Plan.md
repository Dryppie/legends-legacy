# Boss-specific Essence loadouts: analysis and implementation plan

**Progression audit and calibration — 11 September 2026:** [floors 2–5 review](Tower-Progression-Floors-2-to-5-Review.md) records independent four-Essence searches on floors 2–4, their separately confirmed linked Health/Power calibration against every known breach, and Kharad's fresh **20.9%** strongest-control check. Compatible controls and calibrated top builds persist for future searches. Floors 6–11 and practical acquisition coverage remain open.

**Fresh search and Kharad follow-up — 11 September 2026:** [new independent searches](Post-Calibration-Tower-Team-Search-Review.md) confirmed Garran's strongest saved team at **34.8%** and found a stronger Kharad team at **59%**, triggering a separate calibration. The [expanded-portfolio follow-up](Kharad-Expanded-Portfolio-Calibration-Review.md) applied another **8% to both Kharad Health and Power**; its strongest of 122 parties confirmed at **25.25%**, and the full family passes. At that stage, main-dashboard searches retained **4 floor-1 / 6 floor-5 controls**. Broader progression, practical Essence access and further independent ceiling searches remain open.

**First retained-build calibration — 11 September 2026:** compatible saved builds now enter new Tower Lab searches automatically as fresh benchmark controls; completed future studies retain their generated finalists. The [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) applied linked Health/Power factors of **1.06 to Garran** and **1.56 to Kharad** relative to their pre-campaign inputs. The strongest of 57/106 retained parties confirmed at **34% / 24.25%**, respectively, and both frozen families pass the 10–50% policy. This uses the declared full Essence pool including Rare Essences; this historical result is followed by the fresh searches and expanded calibration above.

**Original independent-team pilot results — 11 September 2026:** increments 1–5 of [Automatic Tower team discovery](Automatic-Tower-Team-Discovery-Plan.md) are complete. The [fixed pilots](Automatic-Tower-Team-Pilot-Review.md) found a floor-1 generated primary at **859/1,000 wins (85.9%)** and five floor-5 generated finalists at **972/972 each**. Independent search found viable builds; both declared cohorts fail the 50% balance ceiling. These pilots use the full 80-Essence pool with hypothetical ownership, including Rare Essences. The retained-build follow-up above now calibrates these declared budgets; practical Essence access and the wider progression curve remain separate coverage. Wider progression balance remains unestablished; no bosses were tuned by these pilots.

Status updated 11 September 2026: the boss-specific search machinery and Tower Lab workflow are implemented. The original analysis below remains the design rationale. The [implementation guide](Boss-Specific-Essence-Loadout-Implementation.md) records the delivered behavior; the [pilot](Boss-Specific-Essence-Loadout-Pilot-Review.md), [refinement](Boss-Specific-Essence-Loadout-Refinement-Review.md), [fixed Nhalia validation](Nhalia-Fresh-Validation-Review.md) and [Serevin/Serath coverage expansion](Boss-Coverage-Expansion-Review.md) record separate completed experiments. Those search experiments used the offline `LL/tools/BalanceHarness` and local Tower Lab without changing game content. The later user-authorized floor-1 tuning is recorded separately below.

The latest expansion completed 28,662 fights within its selected 28,682 cap. Serevin's four-slot discovery primary cleared 10/20 fresh trials versus its anchor's 1/20, with substantial floor-8 losses. Serath's nine-slot primary was an existing retained control and cleared 7/20, matching its anchor; a 12/20 finalist remains exploratory. [Four exact primary/anchor recipes](Boss-Expansion-Recipes-20260911/README.md) preserve their complete parties and limitations. The fixed Nhalia 100-seed observations now have a separate Tower Lab view and automatic exclusions on new schema-2 plans.

Further validation or broader coverage remains conditional work, not an extension of a completed campaign. Future fresh experiments must include the expansion's newly used seed ledgers as well as the portable historical exclusions. The 76 expansion finalists are available in saved reports and the campaign recipe library; they have not been folded into the 130-entry historical portable catalog. A compatible versioned report-format change is also needed to correct the legacy native Markdown heading for custom control cohorts without breaking old archive reconstruction.

## Progression requirements

The user's latest clarification sets these intended progression checkpoints:

| Checkpoint | Equipped Essences per character |
| --- | --- |
| Floor 1 | 4 |
| Around floor 5 | 5 |
| Around floor 10 | 6 |
| Floor 11, Serevin | At least 7 |

The floor-5 checkpoint is now explicit, and approximately six at floor 10 refines the preceding five-to-six wording. The precise intervening transition floors remain to be fixed; the older curve's six-slot budget starting at floor 8 was provisional interpolation. Seven is the main Serevin progression-evaluation budget. Lower-budget recipes remain valid diagnostic controls for detecting clears below these targets; do not enforce the targets through artificial loadout restrictions or suppress those outcomes.

The completed expansion instead selected a budget by search headroom: its best controls at five through ten slots each cleared 5/5, so it searched the four-slot zero-clear cell. That selection rule failed to carry the progression requirement into the campaign. Preserve its observed results as below-target evidence. A progression-focused follow-up must evaluate seven-slot parties alongside lower-budget controls, with explicit gear/level assumptions and the approved acceptance policy below. Merely searching seven-slot recipes cannot make Serevin require seven slots. The original sealed protocol and results remain unchanged.

**Approved Tower balance target, 11 September 2026:** at each floor's intended budget, no evaluated legal party may exceed **50%** wins and at least one must reach **10%**. Weak builds may remain below 10%; averaging them with a high-clear specialist must never hide an upper-bound breach. The [Tower acceptance policy](Tower-Balance-Acceptance-Policy.md) defines the scope, uncertainty and implemented standalone evaluator. A 10/10 result is an upper-bound concern, not acceptable balance. Independent staged studies now apply acceptance after fresh confirmation through the CLI and Tower Lab. Legacy/discovery-only rankings and archive verification retain their original semantics and do not automatically establish balance.

## Next priority: validate the progression curve

**Standalone user benchmark completed, 11 September 2026:** the [supplied five-character floor-1 party](Tower-Floor-1-User-Party-Review.md) won 1,000/1,000 battles at the existing four-slot entry equipment/level assumptions, exceeding the 50% ceiling. Its exact ordered recipe is an optional floor-1 reference. Do not repeat it across the Tower, pin its Essences in every search or treat it as a universal template. The user prefers a tool that generates effective complete parties automatically for each declared boss/budget. That tool and its acceptance evaluator are now implemented; the manual benchmark does not replace fresh evaluation of generated parties.

**Earlier user-party floor-1 tuning:** [linked Health/Power calibration](Tower-Floor-1-Linked-Tuning-Review.md) first restored original Health 1.27 / offense 1.24, then selected a 1.32 factor for both: Health **1.6764** / offense **1.6368**. The exact user party confirmed at **291/1,000 wins (29.1%; 95% interval 26.37–31.99%)**. All 1,000 local parity reports and 70 other-floor regression pairs matched. The later [retained-build calibration](Retained-Tower-Builds-Calibration-Review.md) supersedes those inputs with Health **1.776984** / offense **1.735008**. These historical measurements remain separate; neither establishes exhaustive floor or progression balance.

**First progression batch complete:** floors 1–5 now have scoped intended-budget evidence; this does not accept the full floor-1–11 progression curve. The [new audit and calibration](Tower-Progression-Floors-2-to-5-Review.md) covers all known earlier breach candidates on floors 2–4 and rechecks Kharad. Current strongest-party observations are:

| Floor | Slots per character | Strongest confirmation | Scope |
| --- | ---: | ---: | --- |
| 1: Garran | 4 | 348/1,000 (34.8%) | Earlier independent ceiling check; unchanged by the current calibration |
| 2: Velka | 4 | 31/150 (20.67%) | Full calibration family passes |
| 3: Morrowmaw | 4 | 147/400 (36.75%) | Full calibration family passes |
| 4: Vaelor | 4 | 81/400 (20.25%) | Full calibration family passes |
| 5: Kharad | 5 | 209/1000 (20.90%) | Fresh independent family passes; saved control supplies viability |

These cohorts use the full 80-Essence pool with hypothetical ownership. Four slots through floor 4 is an explicit working interpolation. Continue with floors 6–9 at five slots, floor 10 at six and floor 11 at seven, with separate lower-budget diagnostics. The [next-batch checklist](Automatic-Tower-Team-Discovery-Plan.md#next-batch-history-capacity-and-floors-611) first extends bounded history capacity: the latest previews already exclude 97,838 seeds. Preserve every exclusion and keep combat caps separate; the capacity change and remaining studies are pending.

The following table preserves earlier concerns under their historical content, rather than describing current acceptance of the already calibrated floors:

| Floor | Relevant recorded evidence | Concern in that historical study |
| --- | --- | --- |
| 1 | The [whole-party study](Tower-Whole-Party-Review.md) recorded 10/10 clears for each of eight new four-slot finalists. The earlier [curve confirmation](Tower-Curve-Review.md) recorded 57/100 for its balanced four-slot party. | Both observations exceed the newly approved 50% ceiling. Preserve the builds as high-clear controls; beatability does not satisfy balance. |
| 4 | The initial curve recorded 0/20 for each of its three four-slot compositions. Later [pilot controls](Boss-Specific-Essence-Loadout-Pilot-Review.md) reached a per-floor maximum of 4/20 at four slots. | Investigate an early difficulty spike before deciding whether the boss or tested builds need changing. |
| 5 | The curve's balanced five-slot party cleared 20/20. A four-slot Kodoku-study alternative also retained 20/20 on floor 5. | The intended-budget observation exceeds 50%, and the lower-budget result also questions the fifth-slot requirement. |
| 7 | The [Eydis refinement primary](Boss-Specific-Essence-Loadout-Refinement-Review.md) cleared 40/40 at five slots, after earlier reference parties struggled. | A specialist exceeds the ceiling despite weak reference builds. A reference-only or pooled assessment would hide this breach. |
| 8 | The pilot's four-slot Kodoku specialist cleared 20/20, with major losses on other floors. | A below-target specialist exceeds 50%; losses elsewhere do not cancel this floor's difficulty concern or demonstrate a four-slot climb. |
| 10 | The [six-slot progression confirmation](Tower-Party-Progression-Review.md) recorded 40/40 for every finalist, including the authored control, in each context. Its five-slot finalists recorded 0/20 on floor 10. | Six slots are feasible but the recorded 100% rate exceeds the target. Levels, gear and builds also changed between budgets, so slots alone do not explain the transition. |
| 11 | Two six-slot whole-party deployment variants each cleared 20/20. The later four-slot Serevin primary cleared 10/20. | Below-seven-slot clears conflict with the intended progression. The six-slot observations exceed 50%; four-slot 10/20 has uncertainty and does not pass either requirement. |

These are separate historical studies, with their own seeds, complete parties, gear and captured execution/content. Do not pool their samples or combine different per-floor winners into one party. Floors 2 and 3 are now covered by the current audit above. Floors 6 and 9 still need intended-budget coverage; absence from this historical table is not a balance pass. Each recorded fight starts fresh, and the studies do not establish acquisition timing or a continuous climb.

The standalone acceptance evaluator, independent generator, staged confirmation and Tower Lab integration are implemented. With scoped floors 1–5 calibrated, the **remaining floor-6–11 progression validation** still requires separate coverage:

1. Apply the implemented per-floor 50% ceiling and existence of at least one 10% build, with explicit uncertainty and incomplete-evidence handling. Specify the intervening floor budgets, realistic equipment/level/Essence progression and complete required parties. Keep the Tower policy separate from starter goals and over-budget transfer evidence; report lower-budget clears against the intended unlock progression.
2. Assemble an existing-evidence matrix first. Freeze a manageable set of exact authored, retained generalist and known specialist recipes, with source hashes and explicit adaptation rules. Compare intended-budget parties with lower-budget controls. Use a separate legal, fixed-level/equipment comparison when attributing differences specifically to Essence count; retain the real progression comparison as its own cohort.
3. Declare the actual-combat cap and fresh seed schedules before execution, excluding all completed campaign ledgers. Confirm the remaining floors 6–11 without choosing a different budget because its results provide more search headroom, and retain floors 1–5 for subsequent independent ceiling checks. Report uncertainty, draws, pacing, survival and below-budget clears alongside the primary results. Historical results nominate controls; they are not fresh validation of current content.
4. Identify concrete outliers against the declared criteria. If tuning follows, evaluate scoped changes in an isolated offline content snapshot and rerun the frozen comparisons, with all-15-floor regression coverage for any shared combat changes. Preserve original evidence and exact recipes. No automatic production change or slot gate follows from the audit.

## Approved boss tuning controls

User decision, 11 September 2026: use **one primary difficulty multiplier per boss**, normally scaling Health and Power together, with separate HP-only and Power-only adjustments when the observed encounter needs them. Garran's [earlier calibration](Tower-Floor-1-Linked-Tuning-Review.md) applied this policy in a scoped offline experiment. The [retained-build follow-up](Retained-Tower-Builds-Calibration-Review.md) implements an offline calibration script and applies independently confirmed linked settings to Garran and Kharad. Dashboard tuning controls and wider floor coverage remain separate work; independent team discovery does not automatically tune bosses.

Each boss retains its own authored Health/Power balance and mechanics. The default linked adjustment preserves that ratio. A multiplier of 1.10 increases both scaling inputs by 10%; it does not imply a 10% increase in combat difficulty or a predictable change in win rate. More health creates additional attack opportunities, while healing, mitigation, phase thresholds and control can change the response.

Implement the control in the offline harness and local Tower Lab using the existing floor-specific `guardianScaling.health` and `guardianScaling.offense` fields. The [shared scaling path](../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerGuardianScaling.cs) maps offense to the guardian's Power attribute. This control does not set `recommendedPowerRating` or multiply every combat attribute. Keep defense, resistance, penetration, regeneration and mechanics outside the default linked adjustment.

| Tuning mode | Health scaling | Offense / Power scaling |
| --- | --- | --- |
| Linked, default | Frozen baseline health × difficulty multiplier | Frozen baseline offense × difficulty multiplier |
| HP only | Frozen baseline health × difficulty multiplier | Frozen baseline offense |
| Power only | Frozen baseline health | Frozen baseline offense × difficulty multiplier |

Allow explicit mixed candidates, such as higher HP with lower Power, when correcting encounter pacing or damage pressure. Always derive candidates from the recorded baseline; do not compound slider changes or apply the multiplier again during export or live preparation. Save the baseline content hash, mode, multiplier or explicit adjustments, and resolved existing field values. No additional production scaling layer is needed initially.

### Choosing an adjustment

| Observed issue | Adjustment to investigate |
| --- | --- |
| Duration and damage pressure are appropriate, but clears are too frequent | Increase Health and Power together. |
| Boss dies before its mechanics matter | Increase HP. |
| Boss survives long enough, but the party comfortably heals through its damage | Increase Power. |
| Characters die too abruptly despite an acceptable clear rate | Lower Power and compare higher-HP alternatives. |
| Fight drags on with little danger | Lower HP and compare higher-Power alternatives. |
| One particular ability causes disproportionate deaths | Investigate that ability in a separately scoped change. |

These are hypotheses to test, not automatic rules. For example, extra HP may not challenge a party that can sustain itself indefinitely. Inspect victory and defeat durations, first-death timing, damage spikes, mechanic participation and timeouts alongside the clear rate. No numerical duration or early-death threshold has been approved; these remain diagnostics unless a future protocol explicitly declares a gameplay guardrail.

### Tuning workflow and Garran follow-up

1. Discover several strong complete teams at the intended fixed progression budget, retaining compatible known strong controls. Keep team search focused on strength, including results above 50%.
2. Freeze a baseline, bounded candidate grid, combat cap and paired discovery schedules. Start with linked scaling; compare separate HP/Power adjustments when diagnostics justify them. Because scaling can change which teams are strongest, include bounded team-search coverage of proposed settings before freezing the final confirmation cohort.
3. Select and freeze the boss setting and complete party cohort before confirmation. Use fresh seeds and the [Tower acceptance policy](Tower-Balance-Acceptance-Policy.md): no evaluated intended-budget build above 50%, at least one supported at 10% or higher, and explicit uncertainty. Do not average away a strong outlier or select a weaker team to make the boss pass.
4. Export the resolved local content change for review, verify preparation/replay and appropriate floor regressions, and preserve the original evidence. Discovery runs do not automatically change game content.

For Garran, the user's subsequent instruction was to restore **original Health 1.27 / offense 1.24** and tune from there. The [completed linked experiment](Tower-Floor-1-Linked-Tuning-Review.md) selected a **1.32** factor, initially **Health 1.6764 / offense 1.6368**, with **291/1,000** fixed-party wins. Two further HP-heavy/Power-light diagnostic settings produced 407/1,000 and 553/1,000 wins; the latter breaches the ceiling. The earlier [394/1,000 offense-only setting](Tower-Floor-1-Tuning-Review.md) remains historical rather than the current baseline. Subsequent independent discovery and retained-build calibration applied Health **1.776984** / offense **1.735008**; the latest fresh ceiling check found a strongest-party rate of **34.8%**, as recorded in the current evidence table above. Practical acquisition and exhaustive build coverage remain unestablished. The offense-only restriction was an earlier experiment design choice, not a requirement for future bosses or evidence that HP tuning is unnecessary.

The [automatic team-discovery pilots](Automatic-Tower-Team-Pilot-Review.md) held their captured boss content fixed and are complete. The user-requested fixed-party comparison used its own protocol, seeds and combat budget; it did not implement independent discovery or consume the pilots' allocation. Later tuning against generated and retained teams used separate frozen experiments, including the [completed floors 2–5 batch](Tower-Progression-Floors-2-to-5-Review.md). Future searches and calibrations retain that separation.

## Recommendation

Build a **library of strong strategies for each boss**, rather than choosing one all-floor winner or one highest-scoring Essence for each role. Use boss mechanics to propose candidates, search complete legal loadouts in their actual parties, and keep different successful approaches through fresh confirmation.

The recommended combination is:

1. Structured boss profiles and Essence interaction graphs to generate plausible counters.
2. Boss-specific joint refinement, starting from retained parties and equal-cost random exploration.
3. A small strategy archive that preserves distinct approaches: focused damage, add clearing, protection, sustain, and mechanic denial.
4. Fresh-seed confirmation of frozen candidates, with all-15-floor transfer reports and exact replay.

The first implementation should establish boss profiles and a separately versioned boss objective. Then compare search approaches at equal actual combat cost. A more elaborate optimizer cannot correct an objective that rewards success on the wrong encounter.

Here, “playstyle” means how a loadout and team handle combat. It does not automatically enable the separate **Combat Styles** system or change gear, training, ownership or composition budgets. A preferred role is a search/reporting intent, not a player class restriction or proof that the boss requires that role.

## What the current implementation can and cannot tell us

The existing search measures ordered legal loadouts through production Tower preparation and combat. Schema 3 searches five first-group roles, then compares first-group-only, repeated and alternating deployments. It retains controls and freezes finalists before confirmation. These are useful foundations.

Its main ranking prioritizes floor 1 or floor 10, then gains across the complete encounter matrix. A build that solves one difficult boss can lose this ranking to a generalist. Structural beam diversity—different Essence IDs/order—also does not necessarily preserve different combat strategies. Independently selected character winners can compete for the same triggers, overheal the same ally, or depend on support the assembled party does not provide.

The completed study found improvement in some six-/eight-slot deployments, weaker four-slot alternatives than the retained best, and saturated ten-slot results. This motivates better objectives and diversity; it does not demonstrate that a particular new algorithm will win. Historical rates apply to their saved executable/content/budgets, not automatically to subsequent working-tree changes.

## Boss mechanics and counter hypotheses

The following covers **all 15 released floors**. Observations come from the floor-to-ability-profile mapping and structured ability/summon definitions inspected for this analysis. Proposed counters are hypotheses, not verified loadouts. Conditions, target eligibility, cooldowns, dispellability and runtime interactions must be resolved before interpreting the observations as effective counters.

| Floor / boss | Observed authored mechanics | Approaches to compare |
| --- | --- | --- |
| 1 — Garran | Current-target strike, party-wide damage, attribute transfer and gate-seal modifiers | Focused damage with enough party protection; self-sustain tank versus distributed protection |
| 2 — Velka | Lowest-health targeting, party-wide Bleed, Bleed consumption and Haste conditions | Rescue healing/cleanse versus prevention and faster boss damage; assess exposed allies rather than tank HP alone |
| 3 — Morrowmaw | Broodling summons, party-wide damage, consuming owned summons and scaling per owned summon | AoE/add removal versus boss focus; compare fewer active broodlings and guardian damage, not total damage alone |
| 4 — Vaelor | Different health-based targets, physical/magical attack effects, Thorns and damage-type statuses | Damage mix, retaliation-safe damage and distributed sustain; confirm the actual mirror/status rules |
| 5 — Kharad | Highest-max-health targeting, two pillars, barrier, party damage and summon-group resonance | Pillar removal/order versus boss focus, tank resilience and barrier pressure; killing every add immediately is not assumed best |
| 6 — Orsenn | Repeated party damage, Cinder application/removal, Doom and Cremation | Group prevention/recovery, eligible status removal and damage before the dangerous cycle |
| 7 — Eydis | Periodic self-healing that scales with Abundance, defensive growth, Slow/Weaken | Sustained boss damage, eligible healing suppression or burst; survival without overcoming recovery is insufficient |
| 8 — Kodoku | Venomspawn, Poison, healing-received and regeneration suppression | Add clearing plus prevention/barriers versus stronger healing throughput; inspect which recovery routes are actually affected |
| 9 — Ni | Copies, health swap with an owned copy, repeated single-target damage and party damage | Spread damage/copy removal versus boss focus; measure health restored through swaps and the timing of copy removal |
| 10 — Mad King | Party damage, highest-health targeting, a damage-dealt/taken window and missing-health scaling | Focused damage, end-of-fight protection and burst during vulnerability; passive survival is not enough |
| 11 — Serevin | Dispel, Silence, Ink gained from dispelling and Ink reduction on stagger | Less dependence on dispellable buffs, stagger and resilient damage/support; test whether extra buffs feed the boss |
| 12 — Volgrin | Current/random/party targeting, Chill and health-triggered chain changes | Phase-aware protection and damage, control/stagger where effective, recovery across multiple targets |
| 13 — Nhalia | Party damage, condition-stack targeting, tide/Soaked mechanics and healing triggered by enemy healing | Prevention/barriers with necessary healing versus heavy sustain; track the boss healing caused by the party's recovery |
| 14 — Caldris | Single-target/party damage, plate stacks, shattered-plate state and cooldown reset | Pressure through plate cycles versus sustained damage/protection; resolve the stack transitions before proposing counters |
| 15 — Serath | Highest-/lowest-health targeting, Wound, multiple random targets and a party health-percentage-spread check | Health equalization, rescue healing and distributed protection versus damage-heavy parties; measure the spread at the actual check times |

Two examples show why generic role scores are insufficient:

- **Kodoku:** the inspected Miasma effects apply `ModifyHealingReceived` and `ModifyRegenerationRate` at -80 for 150 ticks. More nominal healing is not necessarily the best response. Barriers/prevention are candidates to test, not assumed immunity to every mechanic.
- **Nhalia:** Undertow listens to `OnEnemyHealed` and uses a 0.2 event-magnitude healing coefficient. The inspected ordinary health-restoration path publishes that event using restored health. A healer can help keep the team alive while also helping the boss recover. All relevant healing paths still need telemetry verification.

Tower starts with one guardian, but production abilities can create additional hostile combatants. AoE therefore cannot be judged from the initial enemy count. Conversely, a boss using AoE does **not** imply the party needs AoE damage: it may need group protection instead. Do not invent extra enemies for single-target floors to make an AoE candidate look useful.

## Define strategies by their useful contribution

Keep boss success as the primary outcome. Use the following measurements to explain behavior and preserve alternatives, rather than adding them into an arbitrary “power score.”

| Intent | Useful evidence | Misleading objective to avoid |
| --- | --- | --- |
| AoE / add clearing | Time with hostile summons alive, wave-clear timing, damage by target, boss progress during add windows | Maximum total damage, including unnecessary add damage or overkill |
| Single-target / burst | Guardian damage/progress, time to victory, damage during meaningful boss windows | Highest DPS on an irrelevant target or short isolated dummy fight |
| Tanking / protection | Team survival, damage redirected/prevented, targeted attacks, exposed ally deaths and actual targeting behavior | Maximum damage taken, threat generated or tank health in isolation |
| Healing / sustain | Effective recovery, health deficit, deaths prevented as a hypothesis, recovery timing and boss recovery triggered | Raw healing output; extra damage taken can inflate healing statistics |
| Barrier / prevention | Damage actually prevented/absorbed, expiry/waste and surviving burst windows | Barrier generated without knowing whether it was used |
| Control / stagger / denial | Denied actions, effective control time, stagger breaks, relevant debuff/cleanse timing | Counting applications that are resisted, redundant or ineffective against the boss |
| Hybrid / support | Party improvement with matched replacements and complementary teammate builds | Low personal damage interpreted as a weak build |

Tank claims require target-selector checks. A threat-oriented Guardian must not be assumed to protect allies from lowest-health, highest-health or random-target effects. Similarly, a `Stun` tag does not establish that a boss is stunned rather than handled through another production control/stagger rule.

Seed hypotheses can come from real definitions: Red Slime exposes damage to `AllEnemies`; Blue Slime exposes group healing/barrier effects; Goblin Shaman and Forest Spirit expose healing of a low-health ally; Brown/Transparent Slime expose self-Guard effects. These are starting ingredients, not recommended complete builds. Resolve triggers, scaling, timing and indirect references, then test combinations. Never hard-code their names as permanently best or require every proposed role to use them.

## Approaches worth comparing

| Approach | How it would work here | Advantage | Main limitation / decision |
| --- | --- | --- | --- |
| Mechanic templates | Seed recipes with hypotheses such as add clearing plus sustain, focused damage plus protection, or prevention plus limited healing | Cheap, inspectable starts; useful early | Can encode designer bias and miss unexpected hybrids. Use as seeds, never the sole legal pool |
| Interaction graph | Resolve trigger → condition/status → effect → recipient relationships; propose enabler/consumer pairs across characters | Finds combinations missed by isolated Essence scoring | Compatibility is not effectiveness. Handle caps, refresh/consumption, cycles and conflicting effects; keep unexplained Essences eligible |
| Boss-specific joint beam/local search | Change one/two Essences or coordinated teammate choices while scoring the full party on the selected boss | Reuses the current runner and mutation infrastructure; practical first optimizer | Single changes can miss combinations that only work together. Retain diverse starts, paired changes and random restarts |
| Strategy archive / quality diversity | Retain strong candidates in a small number of measured behavior categories | Produces multiple useful answers and preserves specialists | Bad descriptors create meaningless categories. Begin with a small explicit archive, not a huge grid of every metric |
| Cooperative coevolution | Maintain alternatives for characters/groups and evaluate them with several evolving collaborator parties | Can discover complementary teams instead of five isolated winners | Partner dependence and moving evaluations complicate caching and interpretation. Add after joint refinement, with stable reference collaborators and frozen generations |
| Pareto portfolio | Retain nondominated tradeoffs in boss clears, survival and duration, within a declared budget | Makes close alternatives visible without arbitrary weights | A large frontier can overwhelm users; role diagnostics must not allow a losing party to be advertised as the best boss build |
| Staged evaluation / racing | Give candidates equal early seed batches, then spend more discovery trials on promising/diverse survivors | Reduces simulation spent on clearly poor candidates | Early noise can discard a real winner. Use predeclared rounds and exploration reserves; fresh confirmation remains separate |
| Surrogate/Bayesian proposals | Learn which complete recipes are promising and prioritize production simulations | May help once diverse, trustworthy experiment data exists | Ordered categorical choices, partner interactions and balance changes make sparse models unreliable. Defer; predictions cannot certify builds |
| Exhaustive search / constraint solving | Enumerate small legal pools or use constraints to generate legal recipes | Excellent test oracle and legality support | Full combat fitness is nonlinear and enormous; a linear stat model cannot substitute for Tower execution |
| Reinforcement learning | Potentially learn a policy over sequential decisions | Relevant if controllable tactical decisions become the problem | Current task selects pre-fight loadouts under automatic combat. Adds complexity without addressing the objective/coverage gap; not recommended now |

The strategy-archive proposal is inspired by **MAP-Elites**, which retains high-performing solutions across selected behavior dimensions. Its published results motivate preserving variety; they do not prove an advantage in LegendsLegacy. Start with a modest archive and compare it at equal cost. [Mouret and Clune, 2015](https://arxiv.org/abs/1504.04909).

Evaluating characters with collaborators follows the idea of **cooperative coevolution**, where interacting subcomponents are assessed together. A character's value depends on its partners, so full-party trials and fixed reference partners remain essential here. This is an adaptation proposal, not an implemented method. [Potter and De Jong, 2000](https://pubmed.ncbi.nlm.nih.gov/10753229/).

Successive-halving research motivates staged resource allocation. Here the resource would be additional **discovery seeds at unchanged combat fidelity**, not a shorter fight or fewer enemies. Its original problem assumptions do not automatically provide valid elimination guarantees for noisy Tower simulations; validate the adaptation before using it. [Jamieson and Talwalkar, 2016](https://proceedings.mlr.press/v51/jamieson16.html).

## Boss-specific selection and evidence

Introduce a separate versioned objective; do not reinterpret schema-1/2/3 reports. Its contract should record target boss(es), budget, allowed pool, fixed/mutable characters, ally contexts, strategy intent, target-floor weights, search policy, stage seeds and actual-combat cap.

For a single boss, rank discovery primarily by target-boss victories or paired gains against each declared reference context. If multiple distinct contexts are required, predeclare whether worst-context gain or a weighted result is the objective. Follow with declared boss-progress/survival tie-breaks; duration must not reward fast defeats. Mechanics measurements define diagnostic/niche views, not a hidden substitute for victory. Preserve promising progress-based candidates for exploration when every candidate loses, but label them unsuccessful.

An all-floor generalist and a boss specialist are separate products. A specialist may knowingly regress elsewhere; report that explicitly. Every frozen finalist still receives a full **15-floor confirmation/transfer report** at its unchanged budget. A build evaluated against a known boss on fresh seeds demonstrates repeatability on that boss, not generalization to an unseen boss.

**Wilson 95% remains descriptive.** It does not choose Essences, identify synergy or establish optimality. Use paired gained/lost outcomes for candidate-control comparisons. Finalists, strategy categories and comparisons freeze before fresh confirmation; no repeated sampling until a desired result appears. Small intervals/point estimates are not multiplicity-adjusted method-superiority evidence. Multiple generation seeds evaluate search reliability; they are not extra independent combat observations on an already shared schedule.

Retain equal-cost uniform legal random search and the existing strategy as controls. Compare methods with the same starts, encounter fidelity, legal constraints, seed batches and total actual combat allowance; count reference evaluations and report cache hits separately. Full-party alternatives that overwrite both authored ally contexts identically do not gain independent evidence by repeating that context.

For explanation, compare matched legal replacements in the same party. Test an enabler alone, consumer alone and their combination on separately reserved diagnostic seeds when investigating synergy. Record both win-rate and mechanic effects. This supports a local interaction claim at that budget; it does not yield a universal Essence tier list or prove a particular heal prevented a death.

## Telemetry and source audit

The repository already exposes substantial telemetry: `EntityStats` includes damage/prevention, healing, regeneration, threat, target interactions, deaths and control durations; `AbilityStats` includes per-ability damage, healing, barriers, threat and stagger. `CompactCombatTelemetry` includes hostile summon counts/windows/active ticks and party health-deficit sampling. `BattleSummary` preserves these, with detailed event logs available through replay. Reuse these fields before proposing engine changes.

Their existence does not establish every desired metric's semantics. Audit effective versus attempted healing across direct, periodic, regeneration and lifesteal paths; barrier generation versus attributed absorption; summon damage ownership; boss-versus-add damage; death/expiry versus successful add clearing; and controls that become stagger. Verify whether target/event records are sufficient for phase windows and Serath's health-spread checks. Mark unsupported attribution unknown; do not synthesize it from raw totals. Any required observer must leave combat outcomes and random-number consumption unchanged, with independent parity tests.

The current `EssenceMechanicsInventory` retains direct specs and shared status/summon definitions but explicitly does **not** flatten indirect dependencies. Extend it with typed references, target scope, effective conditions, cooldown/trigger restrictions, scaling, caps, stacking and conflict information. Keep authored facts, runtime-confirmed behavior and untested counter hypotheses separate.

Primary local references:

- [Floor definitions](../LL/src/API/API.LL/Data/world-tower/tower-floors.json), [guardian ability mappings](../LL/src/API/API.LL/Data/combat/creature-abilities.json), [ability definitions](../LL/src/API/API.LL/Data/combat/abilities.json), [statuses](../LL/src/API/API.LL/Data/combat/statuses.json), [summons](../LL/src/API/API.LL/Data/combat/summons.json), [Essences](../LL/src/API/API.LL/Data/essences/essences.json).
- [Mechanics inventory](../LL/tools/BalanceHarness/EssenceMechanicsInventory.cs), [current mutations/beam](../LL/tools/BalanceHarness/LoadoutSearch.cs), [current party ranking](../LL/tools/BalanceHarness/TowerPartySelection.cs), [whole-party deployments](../LL/tools/BalanceHarness/TowerWholeParty.cs).
- [Production Tower preparation](../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerCombatRuntimeFactory.cs), [Tower runner](../LL/tools/BalanceHarness/TowerBattleRunner.cs), [engine health restoration](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs), [saved summary](../LL/tools/BalanceHarness/IdleBattleInput.cs).
- [Entity telemetry](../LL/src/Core/Domain/Models/Combat/EntityStats.cs), [ability telemetry](../LL/src/Core/Domain/Models/Combat/AbilityStats.cs), [compact telemetry](../LL/src/Core/Domain/Models/Combat/CompactCombatTelemetry.cs).

Inspected content SHA-256 values, recorded for provenance; rebuild the inventory from a frozen root before implementation measurements:

| Data file | SHA-256 |
| --- | --- |
| `world-tower/tower-floors.json` | `EF8397CDC7DF0F76EBCB015905DD92B9D38D7B40BC45B57D443BCA0F8E4B1EA1` |
| `combat/creature-abilities.json` | `789B124B065C4A10776494DDA3D8D26DF87D4B65CED69DA57968A1D461A73E94` |
| `combat/abilities.json` | `17F3209D42E700658BDE0F7758FCE03D1703DC07F49944B321E8689E4B32BB6B` |
| `combat/statuses.json` | `AEF34ADAA88A748165BA9F0B2529EEAFBF803C5A41DD4D1BF8177EC1B78418F1` |
| `combat/summons.json` | `85A17FF60925A836AB670A15AB364C1E6F2E0BE51D68BDC086B4F3185D041E5A` |
| `essences/essences.json` | `222E1B9AB4730F58FBEA60CE1A62816F8A236ECA74748D692F0AB80246DF617B` |

## Implementation sequence and exit checks

| Step | Deliverable | Required evidence before moving on |
| --- | --- | --- |
| 1. Boss and mechanic inventory | All 15 boss profiles; resolved Essence dependency graph; facts/hypotheses/unknowns; source hashes | No missing profile/reference silently ignored; targetability, summon and trigger cases checked against production; all 4–10-slot budgets remain legal |
| 2. Objective and strategy contract | Boss objective alongside unchanged generalist policy; role intent and mutable-party definition; small strategy archive | Known counterexample where an all-floor winner loses to a specialist; deterministic ranking; identical contexts identified; invalid claims/constraints rejected |
| 3. Controlled approach comparison | Equal-cost random, boss-specific joint refinement, and graph-seeded refinement with strategy retention | Common starts/costs, legal paired changes, multiple generation seeds, predeclared discovery samples and no lost controls |
| 4. Fresh confirmation and diagnosis | Frozen controls/finalists; all-floor transfer matrix; separate mechanic diagnostics | Independent normal-Tower parity, archive reconstruction, detailed victory/defeat/draw replay, exported recipes reproduce results |
| 5. Tower Lab workflow | Choose boss, slot/gear budget and optional intent; preview cost; inspect several alternatives and exact party dependencies | Browser/API run, cancel, report, export and replay; unsupported recommendations labeled; no silent budget changes |
| 6. Expand only after comparison | More boss/budget cells, evolving collaborators or staged evaluation if useful | Measured improvement/coverage benefit at equal cost; preserve negative findings and earlier evidence |

First audit every floor. For the deeper comparison, start with contrasting mechanisms: Morrowmaw for adds, Eydis for recovery pressure, Kodoku for suppressed sustain, and Nhalia for healing feedback. Include Garran/Mad King as damage/protection references and Serevin/Serath when validating buff dependence and health distribution. Use baseline probes at the existing separate budgets to choose informative cells; do not force a four-slot party against a ceiling it cannot approach or claim improvement from an already saturated ten-slot cell. All floors remain profiled and covered by final transfer reports; later comparison expansion should include each floor and retain 4–10-slot support.

The pending four-/five-slot generalist refinement remains useful as a control track. Its floor-1 objective must remain distinct from the new boss-specialist objective. This analysis broadens the planned work; it does not implement either experiment or replace their existing evidence.

Before measurement, freeze candidate count, number of methods/restarts, seed rounds, complete control/finalist allocation, distinct contexts and fight cap. More candidates and more trials are competing uses of the same budget. A simple fixed-sampling estimate is:

`discovery fights = methods × restarts × candidates per arm × target floors × discovery seeds × evaluated contexts`

`confirmation fights = all frozen parties including controls × 15 floors × fresh seeds × evaluated contexts`

For scale, one finalist at 40 fresh seeds on all floors costs 600 fights per context; twelve finalists in two contexts cost 14,400 confirmation fights before discovery. This is a cost illustration, not an approved experiment size. Include reference, diagnostic and noncached repeated work when declaring the actual cap. Calibrate runtime/storage first and preserve cancellation output. Do not shorten fights, remove adds or alter RequiredSlots as a cheaper stand-in for production fidelity.

Suggested outputs are boss-profile JSON/Markdown, candidate provenance and strategy classification, per-boss results, a candidate-by-floor transfer matrix, exact character/group recipes, discovery/confirmation ledgers, diagnostic comparisons and verified replay. Keep unconfirmed exploratory candidates separate from confirmed alternatives. Show “best found for this boss under this budget,” with observed weaknesses and uncertainty.

## Verification and boundaries

This planning pass inspected repository definitions/code and the cited research; it ran no new combat experiment and establishes no winning loadout. Backend tests are not needed for this documentation-only change. Implementation must use `build/run-tests.ps1`, including small-space search oracles, coordinated-mutation/interaction cases, all-floor legality, target/summon/control semantics, deterministic seeds, cap/cancellation behavior, no confirmation leakage, archive tampering and independent normal-Tower parity. If shared combat telemetry changes, run the affected production tests and full backend suite.

Keep the accepted starter evidence and sealed whole-party package untouched. Historical reports remain historical; do not relabel their generalist winners as newly confirmed boss specialists. No starter 50–90% band applies automatically. No production tuning, account changes, migrations, configuration changes or deployment is part of this plan. Phase 2 integration remains deferred.
