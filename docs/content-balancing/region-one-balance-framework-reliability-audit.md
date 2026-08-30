# Region 1 Balance Framework Reliability Audit

Date: 2026-08-30  
Historical audit baseline: balance schema 24  
Latest validated follow-up: balance schema 46 plus population-replication policy v3  
Primary deterministic seed: 1337  
Additional end-to-end seeds: 2029 and 8471

## Current status after schema 46

The framework physically recovers five controlled faults with valid prerequisites: Health, Offense, Regeneration, AddPressure, and DistributedAttrition. The schema-36 seed-1337 confirmation used three frozen rosters, five retained builds per representative profile, and fifteen common combat seeds per family/dose; it ran 3,465 reliability combats and passed all five experiments. Add pressure is judged by a physical control-and-survival envelope rather than a non-monotonic relative clear-rate delta; distributed attrition is judged by non-primary damage, direct injected-effect attribution and target breadth, observed PartyAttrition, and Defensive-family response rather than total hostile DPS alone. Aggregate damage concentration remains diagnostic.

Schema 37 tests whether those conclusions survive a broader frozen population while also running the requested progression-cohort fidelity matrix. With ten retained builds per representative profile, the six-floor E4/E5/E6 matrix retained three valid rosters for every profile, added 2,745 combats, and found material E5 conclusion changes on Floors 3, 4, and 8. The complete run used 6,255 reliability combats. Health, Offense, and AddPressure remained `Pass`; Regeneration and DistributedAttrition retained correct physical injection and diagnostic recovery but became `Inconclusive` on their relative family-response assertions. The overall broader-population fault result is therefore 3/5 `Inconclusive`, not a second 5/5 confirmation.

Schema 38 adds four common-seed physical doses for Regeneration and DistributedAttrition at `1.10×`, `1.20×`, `1.30×`, and `1.40×`, reusing the unchanged full-strength execution and leaving both verdict gates intact. The ten-build run used 7,335 combats. Regeneration self-sustain rose coherently at every dose and SingleTargetSpecialist remained the strongest clear family, but the existing full-strength relative-family gate stayed `Inconclusive`. DistributedAttrition increased non-primary damage at every dose; Defensive retained the highest party sustain and longest survival in the ten-build panel, but its full-strength relative clear advantage still collapsed at the zero-clear floor. The matching five-build run used 5,130 combats and exposed a population-material blocker: Floor 7 retained only 2/3 required unique IntendedBalanced rosters, so Regeneration was correctly `Unavailable` before injection. Its available attrition panel gave Defensive higher sustain but shorter average duration than IntendedBalanced. There is therefore no completed cross-population Regeneration envelope and no single attrition survival envelope justified by both panels. Schema 38 retains the reviewed gates and records the graded evidence as diagnostic instead of converting either result to a pass.

Schema 39 replaces raw average duration in the diagnostic attrition panel with explicit first-death event rate, observed first-death timing, and Kaplan–Meier restricted mean first-death-free ticks to the common combat limit. Victory without a friendly death remains death-free through that limit; a non-victory resolution without a death is censored at its observed duration. Three complete ten-build master-seed runs preserve the verdict gates and expose population sensitivity rather than hiding it. Seed 1337 used 7,335 combats and returned Health, Offense, and AddPressure `Pass`, with Regeneration and DistributedAttrition `Inconclusive`. Seed 2029 used 7,245 combats: Regeneration passed, while DistributedAttrition was `Unavailable` because concentration fell only 0.42 percentage points against the required 0.50 despite a 1.1491× non-primary-DPS increase. Seed 8471 used 7,290 combats: Regeneration passed, AddPressure was `Inconclusive`, and DistributedAttrition was `Unavailable` because non-primary DPS rose 1.1548× while concentration moved 0.22 points in the opposite direction.

The proposed family envelopes also fail replication. Regeneration's SingleTargetSpecialist is strongest at every dose only on seed 1337; seed 2029 favors IntendedBalanced at every dose and seed 8471 favors Defensive. Defensive attrition restricted-mean survival is mostly favorable on seed 1337 but below IntendedBalanced at every dose on seeds 2029 and 8471. The current distributed physical-reach contract is therefore population-sensitive even before its family-response gate: aggregate non-primary damage reliably increases, while emergent damage concentration does not reliably identify the authored all-party injection. Do not replace or relax the gate from these results. The next justified telemetry is direct ability-specific `Slam the Gates` damage attribution, kept balance-only until it proves a stable causal signature.

Schema 40 implements that causal telemetry without adding a production mechanic or universal content dimension. Only detached distributed-fault executions enable the existing event log. The report attributes damage to the exact injected `effect.creature.garran.slam_the_gates.damage.balance_distributed_attrition` effect ID and records its distinct target breadth per activation wave. Physical reach now requires the existing 1.10× non-primary-DPS increase plus positive directly attributed damage reaching at least two targets in one wave; aggregate concentration remains visible but diagnostic. `ExecuteRaidPlaybackAsync` now honors its supplied `CaptureEventLog` ruleset flag, while existing production callers continue to pass or construct `false`.

The same three ten-build, three-roster, fifteen-seed confirmations show a stable causal response. On seed 1337, injected IntendedBalanced DPS rises 2.3518, 4.5382, 6.6302, and 8.6882 across the four doses; on seed 2029 it rises 1.9927, 3.9122, 5.9047, and 7.5913; on seed 8471 it rises 2.3369, 4.5884, 6.8882, and 8.9564. Every family/dose panel reaches five distinct targets in one wave. DistributedAttrition is therefore physically reached on all three populations. Its final verdict is `Inconclusive` on seed 1337 because the unchanged Defensive-family response fails, and `Pass` on seeds 2029 and 8471. Overall controlled-fault results are 3/5 `Inconclusive` on seed 1337, 5/5 `Pass` on seed 2029, and 4/5 `Inconclusive` on seed 8471 because AddPressure's family envelope fails there. The framework is now causally reliable for distributed injection reach, but family-response conclusions remain population-sensitive and still block expansion or release certification.

Population-replication policy v1 formalizes that decision without rerunning combat or changing a per-seed verdict. It requires at least three distinct enabled master populations and evaluates every supplied population so a favorable subset cannot be selected after the fact. A supported fault is `Confirmed` only when every population passes, `PopulationSensitive` when complete populations mix pass and inconclusive outcomes, `Rejected` when a complete panel contains a failure or no population passes, and `InsufficientEvidence` when populations, fault rows, or physical prerequisites are missing. Unsupported faults are tracked separately and block expansion eligibility even when every supported fault is confirmed. A two-of-three majority is therefore not a pass.

The policy evaluator is deliberately separate from the single-seed production runner. It accepts completed in-memory reliability snapshots, verifies distinct seeds, identical analyzer/options protocols, and unmodified production content, then returns a simulation-free aggregate. No aggregate artifact or CLI comparison mode is emitted yet; the matrix below records the reviewed application to the three immutable schema-40 runs rather than presenting a new per-run schema field.

Applied to the schema-40 evidence, Health and Offense are `Confirmed` at 3/3. Regeneration, AddPressure, and DistributedAttrition are each `PopulationSensitive` at 2/3 pass plus 1/3 inconclusive. CleanseDemand is `InsufficientEvidence` in all three populations. The aggregate policy verdict is `PopulationSensitive` and expansion eligibility is false. This is stricter than averaging clear rates or counting majority passes and preserves the evidence that family behavior changes with the generated population.

Schema 41 implements the approved authored-contract disposition and separates two outcomes on every supported fault: `diagnosticVerdict` answers whether the injected mechanic was physically reached and correctly recovered, while `familyContractVerdict` answers whether an author-approved affected-family premise replicated. A missing family contract cannot turn a recovered diagnostic into a full pass: it is serialized as `InsufficientEvidence` and the composite fault remains `Inconclusive`. Health and Offense have no family contract and remain `Pass` when their diagnostic passes.

AddPressure now uses the approved physical MultiTarget contract. At full fault strength, MultiTargetSpecialist must have the strongest add-window reset rate, retain the existing material reset margin over IntendedBalanced, and increase normalized summon uptime; reset rate and normalized uptime must also remain coherent across the graded payload panel. Absolute/relative clear ordering and raw active ticks remain visible diagnostics but no longer gate the contract because terminal clear floors and shorter victories distort them. Regeneration and DistributedAttrition retain their proven physical contracts but deliberately have no invented family replacement. Regeneration remains family `InsufficientEvidence` until an absolute sustained-damage/self-sustain contract is authored. DistributedAttrition remains family `InsufficientEvidence` because both the original Defensive premise and the schema-43 preregistered burden-mitigation cohort fail replication; another attempt requires an author-defined premise or materially different production-observable.

The unchanged ten-build, three-roster, fifteen-seed runs on seeds 1337, 2029, and 8471 each execute 7,335, 7,245, and 7,290 reliability combats. Every supported physical diagnostic passes on every seed. AddPressure's approved family contract also passes 3/3; on seed 8471, for example, MultiTarget reset rate is 56% versus the next-best family's 47%, its full-dose reset advantage over IntendedBalanced is +31 percentage points, and normalized summon uptime rises 60%→70%. Each run therefore has three composite passes—Health, Offense, and AddPressure—and two honest inconclusives caused only by unauthored family contracts. Population policy v2 aggregates diagnostic and family evidence separately: all five supported diagnostics are `Confirmed`, AddPressure's family contract is `Confirmed`, Regeneration and DistributedAttrition family evidence is `InsufficientEvidence`, and CleanseDemand remains unsupported. Expansion eligibility remains false.

Schema 42 performs the next authored-contract evidence check without changing those verdicts. Each World Tower trial now records physical Guardian damage taken per second, and every graded Regeneration family row reports both that value and realized damage-minus-Guardian-self-sustain per second. This is direct encounter evidence rather than generic benchmark DPS or a population-relative percentile; no threshold was selected from the confirmation seeds.

The candidate-premise review rejects both obvious family replacements. Across the 36 full-fault Regeneration roster panels, the correlation between summed physical SingleTargetSustained capability and clear rate is +0.55, +0.08, and -0.46 on seeds 1337, 2029, and 8471; correlation with Guardian remaining health is -0.74, -0.38, and +0.29. The generic capability direction therefore reverses. Direct full-dose net damage is more explanatory but still not a family identity: its leader is SingleTargetSpecialist on seeds 1337 and 2029, while seed 8471 is led by MultiTargetSpecialist even though Defensive has the highest clear rate. The audited full-dose net/clear leaders are SingleTargetSpecialist 10.69/18%, SingleTargetSpecialist 11.17 versus IntendedBalanced 7%, and MultiTargetSpecialist 15.67 versus Defensive 60%, respectively.

The attrition candidate is more clearly blocked. Raw AttritionResilience is exactly 180 seconds for all 36 audited full-fault rosters, so the current probe is saturated and cannot construct a physical cohort. Its retained submetrics do not rescue a stable ordering: roster prevented-damage ratio versus observed first-death timing is +0.64, -0.90, and +0.64 by seed; probe self-sustain is -0.87, -0.82, and +0.80. Pooled correlations would hide both reversals. Regeneration and DistributedAttrition therefore remain family `InsufficientEvidence`; schema 42 adds the evidence needed to reject premature contracts rather than converting either to a pass.

A post-schema-42 bounded pressure sweep tests whether the existing AttritionResilience dimension can be uncensored by pressure alone. It reuses the 30 deterministic seed-1337 random builds—ten each from E4, E5, and E6—so optimizer selection cannot explain the result. At the authored `1.8×` pressure all builds are capped; temporary isolated `2.2×` and `2.6×` probes also leave 30/30 at 180 seconds. Even the preregistered `4.0×` upper bound leaves 26/30 capped: E4, E5, and E6 retain 9/10, 8/10, and 9/10 caps, and the four uncapped deaths occur only at 162.0–174.0 seconds. This fails the usefulness criterion before cross-population replication. The checked-in pressure remains `1.8×`; no threshold, family cohort, or production content was changed. Pressure magnitude alone is therefore not the next solution—the existing dimension needs an uncensored continuous observation within the same scenario before another family-contract attempt.

Schema 43 implements the bounded observation as average initial-friendly health-deficit ratio, sampled once per completed combat tick by compact telemetry. It is serialized by PvE benchmark v2 and exposed by capability profiler v3 only as AttritionResilience supporting evidence. It does not alter benchmark scoring, the dimension's survival-seconds raw value or ranking, party selection, reliability gates, or production content. The unchanged `1.8×` pressure now yields 23, 26, and 26 distinct deficit values across the 30 deterministic random builds on seeds 1337, 2029, and 8471, even though 30, 29, and 30 builds remain survival-capped. The corresponding ranges are `0.0018–0.0151`, `0.0027–0.0150`, and `0.0040–0.0149`.

The internal physical direction replicates: deficit versus mitigation is `−0.57`, `−0.59`, and `−0.66`, while deficit versus final health is `−0.41`, `−0.17`, and `−0.37`. Deficit versus realized sustain is positive at `+0.58`, `+0.65`, and `+0.65`, which identifies reactive healing opportunity rather than a stable sustain advantage. The preregistered seed-4243 holdout runs 6,480 reliability combats. Its full DistributedAttrition panel has no clears, and lower synthetic deficit only weakly predicts later first death: Spearman `−0.12` across 180 trials and `−0.34` across the 12 roster clusters. The measurement passes the uncensoring and internal-direction audit but fails to establish a sufficiently predictive independent cohort. DistributedAttrition therefore remains diagnostic `Pass`, family `InsufficientEvidence`, composite `Inconclusive`; no family threshold is promoted.

The subsequent preregistered Regeneration damage-survival candidate also stops before replication. Seed 12041 is protocol-unavailable because its Floor-7 rosters select `E6_P75` rather than the historically expected E5 source pool; it is excluded without evaluating candidate axes. The corrected profile-relative seed-14281 discovery runs 7,425 reliability combats and has a complete 12-roster Regeneration panel, but its 17-build E6 source pool produces zero strict qualifiers: eight builds are above the `29.25` sustained-damage median, eight are below the `0.0075` health-deficit median, and the sets do not overlap. Every roster therefore has zero exposure and the required three exposure values do not exist. The candidate fails its preregistered usefulness gate before net-damage or outcome correlations can be interpreted; no replication or family threshold is permitted.

The preregistered three-stage progression review also fails its unanimous evidence gate without changing production progression. Seeds 12041, 14281, and 16633 use one protocol and respectively add 2,520, 2,655, and 3,240 progression-fidelity combats. Only seed 14281 supplies a complete six-floor matrix with monotonic E4/E5/E6 P75 mean power. Seed 12041 has non-monotonic power (`67.58`, `70.46`, `68.33`) and no 40–80% neutral Floor-4 reference; seed 16633 is power-monotonic (`61.49`, `68.60`, `71.31`) but has no neutral Floor-3 reference. E5 materially changes five, three, and two available floor conclusions, so its relevance replicates, but the prerequisites for approving fixed Floors 1–4 / 5–7 / 8–10 boundaries do not. The model remains an author-review candidate, not an implementable progression policy.

Schema 44 diagnoses both failed prerequisites without changing any verdict or combat. Every progression floor now retains the complete neutral-reference candidate panel in JSON and Markdown, including factors that never enter the 40–80% window. The seed-12041 Floor-4 panel is a lower-bound exhaustion: factors `1.00` through `0.30` clear 0%, and the minimum `0.25` factor reaches `35.56%`, just below the frozen window. Seed 16633 Floor 3 is materially farther away: the same range remains 0% until `0.25`, which reaches only `4.44%`. Neither missing matrix is a refinement failure or absent combat evidence. The non-monotonic seed-12041 P75 power is separately traced to population composition: E4, E5, and E6 use different slot-derived random streams and only 17 evaluated candidates each, so profile means combine progression-package effects with different Essence genomes. Schema 44 reports these confounders; it does not lower the `0.25` factor bound or change cohort construction.

Schema 45 confirms the population-composition diagnosis with the preregistered matched-genome probe. Each of ten six-Essence random genomes contributes all 15 E4 subsets, all 6 E5 subsets, and its full E6 build, for 220 variants and 1,100 common-seed PvE combats per population. Seeds 12041, 14281, and 16633 all pass the frozen primary gate: mean power is respectively `60.0612/64.3733/67.1130`, `60.0746/64.5978/68.3000`, and `58.1771/62.4612/64.9200`; median E5−E4 and E6−E5 steps are `+4.2743/+2.3258`, `+4.4683/+4.0434`, and `+4.3780/+2.3884`. All 30/30 individual genome ladders are also strictly monotonic, reported diagnostically without an adoption threshold. This establishes that the earlier E5>E6 P75 mean inversion came from comparing compositionally different generated populations. It does not supply the missing Floor-4 or Floor-3 neutral references, authorize fixed three-stage boundaries, or change production content.

Schema 46 closes a protocol-provenance defect exposed during the author review. Population policy v2 compared only the reliability analyzer version and reliability options; it could not distinguish runs that used different upstream optimizer, representative-build, capability, party-family, or World Tower protocols. Every reliability artifact now records the semantic upstream protocol: schema and scoring versions, initial build count, optimizer version/options, representative version/options, capability version/content fingerprint/probe budget, party-family version/options, and World Tower version/options. Policy v3 treats either missing provenance or any mismatch as `InsufficientEvidence`; cache paths are excluded because they do not change evidence semantics.

This prevents a misleading reinterpretation of the progression result. The schema-45 matched-genome artifacts remain valid within-run diagnostics because their exhaustive subset comparisons do not depend on representative selection. They cannot, however, be combined with older progression artifacts as a policy-v3 replication panel because those artifacts do not carry the new upstream descriptor and their emitted representative means show that the cohort protocol differed. Current-default reruns that find neutral references therefore do not overturn the preregistered three-stage rejection. No factor bound, cohort, verdict, or production content changes in schema 46.

### Preregistered matched-genome progression-power probe

Before evaluating another population, the package-versus-composition diagnostic is frozen. Each six-Essence random source genome contributes every legal four-Essence subset (`15`), every five-Essence subset (`6`), and the full six-Essence build (`1`). Each variant is rematerialized through the normal E4/E5/E6 level, unlocked-slot, and reference-gear packages and evaluated by the unchanged five-scenario PvE benchmark with common scenario seeds. Subset enumeration prevents a favorable removed or added Essence from being chosen after observing scores. The probe is enabled only with the optional reliability/progression study and cannot feed representative selection, capability normalization, party construction, power anchors, progression targets, encounter calibration, or certification.

The primary acceptance rule is fixed across three protocol-compatible master populations: matched mean aggregate benchmark power must be strictly increasing E4→E5→E6 in every population, and the median per-source-genome E5-minus-E4 and E6-minus-E5 deltas must both be positive in every population. The share of individual source-genome ladders that are strictly monotonic is reported without an acceptance threshold. A unanimous primary pass identifies the prior P75 inversion as a generated-population-composition problem and permits an author review of cohort construction; it does not authorize a three-stage floor mapping. Any population-level mean or median reversal rejects that explanation without subset or threshold revision.

The result passes unanimously. Every population keeps `productionContentModified: false`, uses ten source genomes, and reports strict E4<E5<E6 means plus positive median steps. The composition confound is resolved; the three-stage mapping remains rejected because neutral-reference completeness still passes only 1/3 populations.

CleanseDemand remains deliberately `Unavailable`, not failed or inferred. Schema 35 proves why from live inputs: 232 loaded production abilities contain zero `Cleanse` effects, 0/26 profiled builds execute a cleanse, and Floor 8 retains 0/3 required cleanse-specialist rosters. A controlled cleanse experiment would be fictional until production player content supplies that capability.

This validates controlled physical diagnostic recovery for five dimensions and one approved affected-family contract. It does **not** certify authored Region 1 balance. Two family contracts are still unauthored, authored encounters remain too hard for the audited populations, the progression study rejects the fixed three-stage candidate under its unanimous gate, elite evidence is developer/search-only, CleanseDemand lacks content prerequisites, and the study is explicitly non-release. No production content was modified.

| Controlled fault | Current expected diagnosis | Current result | Status |
| --- | --- | --- | --- |
| Increased Guardian health | Duration/health pressure recovered from paired physical telemetry | Health recovered | PASS |
| Increased Guardian offense | Increased hostile DPS and attrition/collapse | Offense recovered | PASS |
| Increased Guardian ability healing | Increased self-sustain per second and boss-sustain pressure | Diagnostic confirmed 3/3; no approved family contract | DIAGNOSTIC CONFIRMED / FAMILY INSUFFICIENT |
| Increased brood payload | AddPressure plus MultiTarget reset-rate advantage and normalized uptime | Diagnostic and approved family contract confirmed 3/3; calibration `Review` | CONFIRMED |
| Increased distributed party damage | PartyAttrition, direct multi-target injected damage, broader non-primary damage | Diagnostic confirmed 3/3; no approved family contract | DIAGNOSTIC CONFIRMED / FAMILY INSUFFICIENT |
| Increased harmful-status pressure | CleanseDemand plus cleanse-specialist response | No player cleanse effect, observed cleanser, or valid roster | UNAVAILABLE |

### Cross-population acceptance matrix

| Supported fault | Seed 1337 | Seed 2029 | Seed 8471 | Policy v2 |
| --- | --- | --- | --- | --- |
| Health | Pass | Pass | Pass | Confirmed |
| Offense | Pass | Pass | Pass | Confirmed |
| Regeneration | Diagnostic Pass / Family Insufficient | Diagnostic Pass / Family Insufficient | Diagnostic Pass / Family Insufficient | Diagnostic Confirmed / Family InsufficientEvidence |
| AddPressure | Diagnostic Pass / Family Pass | Diagnostic Pass / Family Pass | Diagnostic Pass / Family Pass | Diagnostic Confirmed / Family Confirmed |
| DistributedAttrition | Diagnostic Pass / Family Insufficient | Diagnostic Pass / Family Insufficient | Diagnostic Pass / Family Insufficient | Diagnostic Confirmed / Family InsufficientEvidence |
| CleanseDemand | Unavailable | Unavailable | Unavailable | InsufficientEvidence; independently blocks expansion |

### Authored family-contract review

The following evidence-backed dispositions were approved for schema 41. Only AddPressure had enough information to encode a replacement family contract; the other two mechanics intentionally remain without one.

| Mechanic | Replicated physical evidence | Population contradiction | Recommended disposition |
| --- | --- | --- | --- |
| Regeneration | Full-fault Guardian self-sustain reaches 1.44×, 1.52×, and 1.50× reference, with coherent healing-dose telemetry | The strongest full-dose clear family is SingleTargetSpecialist on seed 1337, IntendedBalanced on 2029, and Defensive on 8471 | **Implemented:** removed the named-family ordering promise, retained physical healing recovery, and reports family `InsufficientEvidence` until an absolute sustained-damage-versus-self-sustain contract is authored and replicated. |
| AddPressure | At full dose, MultiTargetSpecialist's add-window reset advantage over IntendedBalanced is +46, +66, and +31 percentage points; its summon uptime rises by +9, +11, and +10 points | On seed 8471 every fault family reaches a 0% clear floor and MultiTarget raw active ticks fall 494.4→489.9 as combat duration shortens, so the “strongest absolute clear family” and raw-active-tick clauses fail despite superior add control | **Implemented:** retained the MultiTarget identity with strongest reset rate, the material reset margin, normalized uptime increase, and graded reset/uptime coherence. Relative/absolute clear and raw ticks are diagnostic only. |
| DistributedAttrition | Non-primary friendly DPS rises 1.17×, 1.15×, and 1.15×; the exact injected effect deals positive damage with five-target breadth on every dose/population; `PartyAttrition` recovery replicates | Defensive minus IntendedBalanced first-death restricted mean is +40.0, -30.2, and -69.6 ticks; full-dose party sustain difference is +0.81, -3.11, and -0.29 per second. `Defensive` is not a stable sustain proxy, and schema-43 burden-mitigation discovery (`+0.54`) reverses on replication (`−0.36`) | **Implemented:** removed the Defensive relative-clear promise, retained causal diagnostic recovery, and reports family `InsufficientEvidence`. No cohort is approved from the current probe; another contract requires an author-defined premise or materially different observable. |

These recommendations deliberately separate two questions that the current single fault verdict combines: “did the framework recover the injected mechanic?” and “did the authored family premise replicate?” Health and Offense have no configured family premise and their diagnostic recovery is confirmed; Regeneration and DistributedAttrition recover the mechanic but lack a replicated family premise; AddPressure retains a credible family premise whose current clear/tick clauses are saturation-sensitive. A later implementation should preserve those two outcomes separately rather than turning missing family evidence into a diagnostic pass.

## Historical schema-24 executive conclusion

At the schema-24 baseline, the framework was a strong production-path simulation and telemetry foundation, but it was **not yet reliable enough to certify Region 1 encounter balance or diagnostic correctness**.

The historical audit found five blocking issues:

1. Authored Region 1 encounters are at a 0% clear-rate floor for every selected P75 population in all three audited master seeds. Controlled difficulty faults therefore cannot be distinguished from an already terminal baseline.
2. Party-family sampling retains and evaluates rosters that fail the defining family constraints. In the 30-roster request, 56 of 101 evaluated family/progression groups contained invalid rosters; several groups had zero valid rosters.
3. Family confidence intervals treat every party-by-seed combat as an independent Bernoulli observation. This ignores clustering by roster and can substantially overstate evidence when many seeds are run on only three parties.
4. Several advertised diagnostic outcomes are typed but unreachable. `PriorityObjectiveUnmet`, `ControlWindowUnmet`, and `CleanseDemandUnmet` are never assigned by the failure analyzer. Health, defense, and resistance assisted-calibration parameter groups are also never selected.
5. Region 1's ten smooth floor targets map to only two actual P75 populations: E4 on Floors 1-5 and E6 on Floors 6-10. E5 is never selected.

At that baseline, the requested central claim was therefore **not proven**:

> The current framework has not yet demonstrated that it reliably recovers a known injected World Tower fault and the affected party archetype.

No production content was modified. No Region 2+ work was performed. No new universal capability was introduced. The schema-25–42 follow-ups below record which blockers were resolved and which remain.

### Implementation follow-up

Schema 25 implements the audit's first sampling-correctness slice: only unique constraint-passing family rosters are retained, exhausted populations are typed `InsufficientFamilyMaterial`, roster-level uncertainty is authoritative while pooled-trial Wilson remains diagnostic, and each frozen run emits a nested roster/seed stability grid.

Schema 26 adds the optional neutral-reference fault-injection slice. In the bounded seed-1337 production-path study, deterministic refinement found 60% references at shared factors 0.31 on Floor 1 and 0.46 on Floor 7. The Offense fault was recovered correctly; the Health fault was observably misclassified as Offense through `PartyAttrition`; the Regeneration fault produced no clear-rate drop and remained inconclusive. The resulting framework verdict was `Fail`, which is the intended honest gate behavior. Add and cleanse faults remain explicitly unavailable. The measurements and conclusions below remain the historical schema-24 audit baseline; diagnostic reachability and progression-population blockers are not resolved by these slices.

Schema 27 adds paired physical-telemetry recovery and verifies that each injected knob reaches combat before scoring the diagnostic. Repeating the same bounded seed-1337 production-path study retained both 60% neutral references. Health now recovers correctly: hostile DPS remained at 0.9764× reference while combat duration rose to 1.0805× and Guardian end-health increased by 0.3361. Offense also recovers correctly with hostile DPS at 1.2781× reference. The Floor 7 regeneration experiment is now explicitly `Unavailable`: both reference and fault recorded zero passive regeneration and identical combat results. The authored `0.1` Guardian regeneration multiplier scales a `0.25` base attribute below the engine's one-health rounding threshold, while Eydis's actual sustain comes from a MaxHealth-scaled ability heal that the regeneration override does not modify. The framework verdict is therefore `Unavailable` with two of three supported parameter groups recovered, rather than the schema-26 false `Fail`. Add and cleanse faults remain explicitly unavailable.

Schema 28 adds a detached Guardian ability-healing control and total self-sustain telemetry without changing authored content or live World Tower preparation. The same bounded seed-1337 study recovered all three tested dimensions. The 1.40× Eydis ability-healing fault increased effective ability healing to 1.8558× reference and total self-sustain per second to 1.6484×, extended duration to 1.1410×, and reduced IntendedBalanced clear rate from 60% to 7%. Paired evidence recovered the existing Regeneration tuning dimension even though terminal losses remained `PartyAttrition`. The family response stayed within the reviewed ten-point tolerance: SingleTargetSpecialist relative advantage changed by -6.7 percentage points and Defensive relative advantage also changed by -6.7 points. Health and Offense retained their schema-27 recoveries. The bounded diagnostic study therefore passes three of three supported controls; AddPressure and CleanseDemand remain explicitly unavailable and the historical broader audit blockers below are not thereby cleared.

Schema 29 adds one narrow Floor 3 add-pressure experiment by duplicating only Morrowmaw's existing brood-summon effect on the detached balance runtime. The exact schema-28 seed/population budget retained 53.33% IntendedBalanced reference clear at shared factor 0.30. One extra summon copy reduced clear rate to 6.67%, increased average peak additional hostiles from 6.7778 to 14.7111 (2.1705×), and increased final additional hostiles from 3.8889 to 11.5111. `AddPressure` was primary or contributing in 100% of failed trials even though lethal outcomes remained primarily `PartyAttrition` in 93.18%, proving that contributing-condition evidence is necessary. The diagnostic therefore correctly identified the injected mechanic and the assisted calibrator correctly returned `Review` because it has no add-count parameter group. The experiment remains `Inconclusive`, not `Pass`, because Floor 3 retained zero valid MultiTargetSpecialist rosters and therefore cannot prove the authored specialist response shape. Health, Offense, and Regeneration retained their previous passes, producing three passes plus one honest specialist-population Review. CleanseDemand is the only remaining unsupported controlled fault.

Schema 30 resolves the MultiTarget roster-availability blocker without lowering its `mean MultiTarget >= 60` or `MultiTarget - mean SingleTarget >= 5` requirements. The representative population already contained two measured MultiTarget-100 builds; the generic selector was forcing specialist parties to include the global Focus and PartySustain anchors, both of which had MultiTarget 0. Only MultiTargetSpecialist now omits those generalist coverage anchors. The exact production rerun retained three valid parties with mean MultiTarget 95, 95, and 100 and specialization margins of 32.5, 35, and 42.5 points.

The newly measurable response shape does **not** pass. At the Floor 3 factor-0.30 reference, MultiTargetSpecialist cleared 100% versus IntendedBalanced 53.33%. With the duplicated brood summon it cleared 33.33% versus IntendedBalanced 6.67%. The specialist remains the strongest absolute family, but its relative advantage contracts from 46.67 to 26.66 percentage points, a 20.01-point adverse change. Its average deaths rise from 1.07 to 3.87 and final adds from 0 to 3.53. The framework therefore reports AddPressure as physically reached and diagnostically matched while keeping the family-response result `Inconclusive` and calibration at `Review`. Health, Offense, and Regeneration remain `Pass`, for an overall three-of-four `Inconclusive` result across 915 trials. This is evidence that either the doubled mechanic overwhelms the specialist's survivability trade-off, the current MultiTarget capability measure does not predict brood control strongly enough, or the authored “advantage should strengthen” assumption is too broad. It is not evidence for weakening the gate or automatically changing production content.

Schema 31 adds exact first-add-window lifecycle telemetry to separate those explanations. Compact combat now records the first tick with additional hostiles and the first later tick with zero hostile summons; the reliability report presents both the resolution rate and average resolved duration by family so censored failures cannot disappear into a successful-clear average. In the same 915-trial seed-1337 production workload, MultiTargetSpecialist resolved the first add window in 100% of reference and doubled-brood trials. Its average resolution time moved only from 67.27 to 72.33 ticks, and every resolution preceded the first friendly death. IntendedBalanced resolved 100% at 135.93 ticks in reference and 86.67% at 149.92 ticks under fault; Defensive fell from 20% to 0%, and SingleTargetSpecialist from 6.67% to 0%.

That physical evidence rules out the narrow hypothesis that MultiTarget capability fails to transfer to brood removal: the specialist is roughly twice as fast as IntendedBalanced and uniquely retains a 100% first-window clear rate under doubled brood. Its `Inconclusive` response shape is instead explained by survivability under repeated pressure. The fault panel still leaves an average 2.87 hostile summons alive at combat end for MultiTargetSpecialist, average friendly deaths rise to 3.87, and clear rate falls to 33.33% even though the first window was removed before any first death. The current gate remains unchanged because it tests whole-encounter relative clear-rate response, while the new lifecycle evidence is diagnostic. The next design decision should therefore examine repeated-window burden and the reviewed family-response contract, not weaken MultiTarget measurement or silently retune production content.

Schema 32 measures that repeated burden across the full combat. It distinguishes total summons from distinct spawn ticks, treats all summons created on one tick as one wave, counts continuous living-summon windows and resolved transitions, and accumulates summon-active ticks once per simulation tick. The same 915-trial seed-1337 run shows that the fault does **not** accelerate Morrowmaw's cadence: every family retains an average 180-tick wave interval. It instead nearly doubles per-wave payload. MultiTargetSpecialist moves from 15.00 total summons across 3.13 waves (4.79 summons/wave) to 32.53 across 3.73 waves (8.71 summons/wave), while average peak summons doubles from 5 to 10.

MultiTargetSpecialist remains uniquely effective, but no longer resets every repeated window. Its reference panel resolves 3.13 of 3.13 observed windows, uses 248.07 active-summon ticks, and spends 37% of combat with summons alive. Under doubled brood it resolves 3.00 of 3.67 windows, uses 375.47 active-summon ticks, and spends 47% of combat with summons alive. The ten fault defeats average 3.90 observed windows, 2.90 cleared windows, 429.70 active ticks, 52% summon uptime, and five friendly deaths. The five victories resolve all 3.20 observed windows, average 267 active ticks, 37% uptime, and 1.60 deaths.

This confirms a payload-driven saturation threshold rather than faster waves or a failed MultiTarget selector. The specialist clears early waves and remains materially better than every comparison family, but one unresolved late window appears once casualties reduce its removal capacity. The family-response gate remains `Inconclusive`: schema 32 adds explanation, not grounds to waive the reviewed whole-encounter contract. The next bounded validation should be a graded detached brood-payload panel to locate the response threshold before deciding whether “relative clear-rate advantage must increase” is a sound universal contract or should be replaced by a physical control-and-survival response envelope.

Schema 33 runs that graded panel without changing the existing full-strength verdict gate. Every authored brood wave receives one detached duplicate, but only the duplicate's health and power are scaled to `0.25`, `0.50`, `0.75`, or `1.00`; authored broodlings and spawn cadence remain unchanged. The `1.00` panel reuses the exact schema-32 fault evidence rather than rerunning it. The bounded workload therefore rises by only 180 combats, from 915 to 1,095. MultiTargetSpecialist clear rate moves from 100% at the frozen reference to 80%, 66.67%, 66.67%, and 33.33% across the four doses. Its average cleared/observed-window ratio moves from 100% to 93.88%, 90.20%, 90.74%, and 81.82%, while summon-active ticks increase monotonically from 248.07 to 288.53, 317.33, 344.40, and 375.47. Friendly deaths increase from 1.07 to 1.93, 2.53, 2.87, and 3.87. This is a coherent physical dose response: the specialist progressively spends longer controlling brood, then loses whole encounters as casualties accumulate.

The current relative-clear-rate contract is not a coherent dose-response measure. MultiTargetSpecialist advantage over IntendedBalanced changes by 0, -13.33, +6.67, and -20.01 percentage points, so the sign reverses at `0.75` even though every physical burden measure worsens monotonically or nearly monotonically. All four doses remain response-shape failures because SingleTargetSpecialist becomes 20 to 46.66 points less disadvantaged relative to a rapidly collapsing IntendedBalanced baseline, despite not becoming the correct or strongest family. With only 15 trials per family, clear rates also move in 6.67-point steps. Schema 33 therefore provides evidence to retire “the intended specialist's relative clear-rate advantage must increase” as a universal add-pressure contract. The smallest next change should preserve the existing diagnostic and `Review` behavior but replace this one response assertion with a reviewed physical control-and-survival envelope: the MultiTarget family must remain the strongest absolute family, retain a material add-window reset advantage over IntendedBalanced, and show a coherent burden response as injected payload increases. That policy change should be tested against a higher common-seed confirmation panel before it can turn the current `Inconclusive` result into a pass.

The higher-seed confirmation used the same three frozen rosters per family but increased common combat seeds from five to fifteen, producing 45 combats per family/dose and 3,285 total reliability combats. The response strengthened rather than disappearing: MultiTargetSpecialist clear rate was 100%, 77.78%, 68.89%, 60%, and 31.11%; reset rate was 100%, 93.46%, 90.91%, 88.82%, and 81.66%; summon-active ticks were 242.67, 300.47, 318.18, 341.89, and 380.58; and friendly deaths were 0.93, 2.20, 2.56, 3.16, and 3.93. Yet the legacy relative-clear advantage at full dose changed by only -2.22 points while SingleTargetSpecialist's relative position moved +66.67 points solely because IntendedBalanced collapsed from 71.11% to 4.44% and SingleTarget remained at zero. This confirms the old assertion was measuring movement around a floor, not specialist control.

Schema 34 replaces only the AddPressure response assertion. The full-strength fault must leave MultiTargetSpecialist as the strongest absolute family, preserve at least a ten-point window-reset advantage over IntendedBalanced, increase its summon-active burden versus reference, and remain coherent across graded doses within a five-point reversal tolerance. Legacy clear-rate deltas remain visible but diagnostic. On the 3,285-combat confirmation, MultiTargetSpecialist was strongest at 31.11% versus the next family's 4.44%, retained a +21.85-point reset advantage, and increased active burden from 242.67 to 380.58 ticks; every graded dose passed the physical envelope. AddPressure therefore passes with physically reached telemetry, `ObservedFailureMode` recovery, an observable clear-rate drop, and the correct calibration response of `Review`. Together with the existing Health, Offense, and Regeneration recoveries, the bounded controlled reliability study now passes four of four supported faults. This validates these four controlled dimensions; it does not resolve CleanseDemand, authored-content difficulty, progression fidelity, or release certification.

Schema 35 converts the remaining CleanseDemand assumption into an explicit prerequisite audit without fabricating a cleanse kit or adding combat work. The seed-1337 production artifact loaded 232 abilities and found zero `Cleanse` effects; the single `Dispel` effect belongs to hostile creature content and does not satisfy player cleanse coverage. None of 26 capability-profiled builds produced a cleanse, the maximum observed cleanses and cleanses per 15 seconds were both zero, and Floor 8 retained 0/3 required MechanicSpecialist rosters with `InsufficientFamilyMaterial`. The report now persists those catalog, physical-capability, and roster counts even when the optional reliability combat study is disabled. CleanseDemand therefore remains correctly `Unavailable`, but its reason is measured and automatically changes when content prerequisites change rather than being a hard-coded assertion. A controlled harmful-status injection must not be implemented until the production catalog exposes a real player cleanse effect, the mechanic-pressure probe observes it, and enough valid Floor 8 cleanse-specialist rosters exist.

Schema 36 adds the requested excessive-attrition experiment without introducing a new universal capability or production tuning knob. On the detached Floor 1 Guardian, it adds only the configured excess damage coefficient—40% in the validated run—to Garran's authored all-party `Slam the Gates` effect. His basic attacks, cadence, target selection, and production JSON remain unchanged. New per-trial telemetry measures damage taken outside the attention-defined primary target and the highest single-character share of total friendly damage, preventing a generic offense increase or tank-only collapse from satisfying the physical gate. In the 3,465-combat confirmation, IntendedBalanced clear fell from 60% to 0%, `PartyAttrition` appeared in 100% of failed trials, non-primary friendly damage per second rose 1.1815×, and damage concentration fell by 0.94 percentage points. Defensive's relative advantage over IntendedBalanced increased 6.67 points, clearing the reviewed five-point gate. The assisted calibrator correctly returns `Review` because an ability-specific distributed-damage group would not be interchangeable with global Guardian offense.

Schema 37 adds a diagnostic progression-fidelity matrix without changing progression anchors or authored content. On each selected Floor 3–8 it finds a 40–80% neutral reference with the currently selected population, then evaluates deterministic IntendedBalanced E4, E5, and E6 P75 rosters at the same factor and common seeds. The ten-build seed-1337 population mapped Floors 1–4 to E4, Floors 5–7 to E5, and Floors 8–10 to E6. All matrix cells retained 3/3 valid rosters. E5 changed Floor 3 from 76% clear at E4 to 100% and cut median duration from 915 to 541 ticks; Floor 4 moved from 71% to 100% and 450 to 341 ticks; Floor 8 moved from the current E6's 78% clear and 841 ticks to E5's 24%, 1,411 ticks, and `PartyAttrition`. These exceed the predeclared fifteen-point clear-rate and ten-percent duration thresholds, so at that stage the smallest supported next step was an explicit three-stage progression review, not ten floor optimizers. The later three-population review recorded above confirms E5 relevance but rejects automatic model adoption because its complete and monotonic prerequisites do not replicate.

The broader panel also serves as a cross-population fault audit. Regeneration still raised Guardian self-sustain per second 1.4408× and was recovered correctly, but Defensive's relative position improved 48.89 points because IntendedBalanced collapsed toward the clear-rate floor; the family assertion became `Inconclusive`. DistributedAttrition still raised non-primary damage 1.1703×, reduced concentration 1.78 points, and recovered `PartyAttrition`, but both IntendedBalanced and Defensive reached zero clear and Defensive's relative advantage fell 31.11 points. This is the same floor-arithmetic risk previously removed from AddPressure. Do not weaken either gate from this single result; the next bounded validation should determine whether a graded physical response envelope can replace relative clear-rate movement for regeneration and distributed attrition.

Schema 38 performs that bounded validation without changing either gate. Each mechanic runs at 25%, 50%, 75%, and 100% of the configured excess multiplier; the 100% row is the existing full fault and is not rerun. In the ten-build panel, Regeneration self-sustain for IntendedBalanced rose from 2.94 to 3.25, 3.59, and 3.95 per second, while SingleTargetSpecialist clear remained strongest at 66.67%, 46.67%, 22.22%, and 17.78%. DistributedAttrition non-primary DPS for IntendedBalanced rose from 35.50 to 37.34, 38.86, and 39.63; Defensive party sustain stayed highest and its average duration exceeded IntendedBalanced at every dose. Those are credible candidate signatures, but the five-build replication does not support adopting them universally: the Regeneration panel cannot run because Floor 7 has only 2/3 required IntendedBalanced rosters, and Defensive's attrition duration is below IntendedBalanced at all four doses despite higher party sustain. The safe conclusion is to retain the current family-response contracts, report saturation explicitly, and require complete additional populations plus a non-confounded survival policy before any replacement gate is promoted.

Schema 39 supplies that non-confounded survival panel and repeats it on master seeds 1337, 2029, and 8471 with ten builds per profile, three frozen rosters, and fifteen common seeds. Defensive restricted-mean first-death-free ticks at full attrition are 585.82 versus IntendedBalanced 545.78 on seed 1337, 433.82 versus 464.00 on seed 2029, and 446.40 versus 516.00 on seed 8471. The Defensive survival hypothesis therefore does not replicate. Regeneration's SingleTarget clear ordering likewise reverses across populations even though Guardian self-sustain rises coherently on every complete panel. More importantly, the current distributed concentration reach check passes only seed 1337; the all-party fault still increases non-primary DPS by at least 1.1491× on the other seeds, but aggregate concentration is shaped by roster mitigation, attention, deaths, and encounter duration. The framework correctly refuses a pass, and the evidence points to direct ability-specific damage attribution rather than another aggregate threshold adjustment.

Schema 40 follows that evidence. The exact injected effect is captured only during the balance-only distributed fault, and each trial records its final damage, DPS, hit count, distinct activation waves, and peak distinct targets per wave. This replaces concentration only in the physical-reach predicate; concentration and the unchanged Defensive-family gate remain reported. All three master seeds show monotonically increasing injected DPS and five-target breadth across every dose. The two schema-39 false `Unavailable` results become correctly separated outcomes: seeds 2029 and 8471 pass physical reach, while seed 1337 also reaches physically but stays `Inconclusive` on its family response. The remaining reliability blocker is therefore authored family-response stability, not uncertainty that the injected all-party control executed.

## Method and evidence boundary

The audit used:

- implementation review of the schema-24 balance runner, party construction/evaluation, failure analysis, calibration, capability profiling, progression mapping, elite certification, and CR analysis;
- the full backend test suite;
- one bounded production-path smoke baseline with assisted calibration;
- four same-seed party-budget comparisons: 3x50, 10x15, 15x10, and 30x5;
- two additional 10x15 end-to-end master seeds;
- one expanded representative/capability population comparison, increasing each P50/P75/P90 profile from 10 to 50 retained builds; and
- direct inspection of the emitted immutable JSON evidence.

The runs intentionally used a minimal developer elite-search configuration. They are suitable for framework diagnosis, not release certification or elite/meta conclusions.

The raw local evidence is under:

```text
.tmp/balance-reliability-baseline
.tmp/reliability-3x50
.tmp/reliability-10x15
.tmp/reliability-15x10
.tmp/reliability-30x5
.tmp/reliability-expanded-capability
.tmp/reliability-seed-2029
.tmp/reliability-seed-8471
```

## 1. Current reliability assessment

| Area | Rating | Basis |
| --- | --- | --- |
| Generic build balance | Adequate | Legal builds and production-engine benchmarks are strong. Default benchmarks use one build-specific random stream, and end-to-end master-seed results move materially, so build/search uncertainty is not controlled by the default run. |
| Capability profiling | Weak | Damage measurements are useful and physical, but survival scenarios often saturate at 60/180 seconds, PartySustain is usually zero, and no audited build produced a cleanse. Relative scores can therefore look differentiated while the durable raw measurement is tied or zero. |
| Party-family composition | Weak | Invalid constraint-failing rosters are retained and evaluated. Several families exhaust unique signatures far below the request. The current result may not describe the named archetype. |
| Authored World Tower difficulty | Weak | The production path is authoritative, but all ten authored floors were 0% clear for the selected P75 populations in all audited seeds. This establishes “far too hard for these populations,” but does not resolve the intended shape or tuning dimension. |
| Failure diagnosis | Weak | Physical telemetry is useful, but only `PrimaryTargetCollapse`, `PartyAttrition`, `BossSustainDominance`, `AddPressure`, and `Other` are reachable. Priority ordering can hide add or regeneration pressure behind a party defeat. |
| Calibration recommendations | Weak | The shared factor is useful as a sensitivity probe. Assisted calibration can only select offense or regeneration; health/defense/resistance are unreachable and add/cleanse have no supported knob. |
| Specialist detection | Insufficient evidence | Every audited party family had 0% clear on authored content. Floor 8 has no valid cleanse specialist population. Relative response shapes cannot be certified from these results. |
| Progression scaling | Weak | Ten targets use two actual populations. E5 is skipped, and the maximum target-to-population gap is 3.59%. The curve is more granular than its evidence. |
| Elite/meta stress testing | Adequate | Multi-restart/local/holdout machinery is substantial, but this audit used search-only developer budgets and repeatedly found local improvements. No release claim follows. |
| Release certification | Insufficient evidence | Current release certification requires curated evidence that may not exist pre-release, party-family evidence is statistically clustered, and fault-injection validation is absent. |

## 2. Party-sampling stability

### 2.1 Equal-combat-budget study

All configurations used seed 1337, identical generation/search options, common combat-seed prefixes, and the production World Tower path.

| Request | Actual party-family combats | Simulated ticks | Approx. full-run wall time | Observation |
| --- | ---: | ---: | ---: | --- |
| 3 parties x 50 seeds | 15,150 | 6,242,736 | 45 s | High RNG replication, minimal roster coverage |
| 10 parties x 15 seeds | 14,595 | 6,065,961 | 46 s | Broadly the best balanced diagnostic budget tested |
| 15 parties x 10 seeds | 13,880 | 5,827,421 | 41 s | Similar budget; more families exhaust valid/unique material |
| 30 parties x 5 seeds | 12,120 | 5,154,970 | 40 s | Many requested families could not retain 30 unique parties |

These wall times include the entire bounded balance pipeline, not only party evaluation. Allocations are not measured by the party-family stage. Capability caches were not shared across these separate output roots, so these timings do not claim cache reuse.

### 2.2 Representative stability results

Clear rate was 0% throughout and is not a useful stability discriminator. A pooled Wilson interval narrowed as low as approximately 0.00-0.03 with 150 losses, but that precision only describes the already-saturated baseline and incorrectly assumes roster-cluster independence.

More informative measures moved with roster coverage:

| Floor/family | 3x50 | 10x15 | 15x10 | 30x5 | Interpretation |
| --- | ---: | ---: | ---: | ---: | --- |
| F3 IntendedBalanced median ticks | 344 | 344 | 344 | 343.5 | Duration center is stable |
| F3 IntendedBalanced PrimaryTargetCollapse | 59.3% | 57.3% | 55.5% | 44.3% | Failure-mode conclusion is roster-sensitive |
| F3 MultiTargetSpecialist median ticks | 331 | 326.5 | 326.25 | 337 | Small but non-monotonic roster effect |
| F7 IntendedBalanced median ticks | 439 | 454.5 | 426.5 | 438.5 | Approximately 28-tick range |
| F7 SingleTargetSpecialist median ticks | 386 | 402 | 425 | 408 | Roster choice changes the apparent response materially |
| F8 MechanicSpecialist median ticks | 937.5 | 942.5 | 949.5 | 950.5 | Center is fairly stable, but every roster is an invalid “specialist” |

Within-party combat RNG remains material. In the 3x50 run, mean per-roster P10-P90 duration ranges were about 54-103 ticks on Floors 3 and 7. Between-roster standard deviation was typically smaller on Floor 3, but reached roughly 33-37 ticks for some Floor 7 balanced/defensive groups. Thus:

- combat RNG dominates duration dispersion for a fixed roster;
- roster diversity materially affects family-level duration centers and failure-mode mixtures; and
- more combat seeds cannot repair invalid or narrow roster construction.

### 2.3 Constraint and retention failure

The 30x5 party builder requested 30 rosters per family. It often retained fewer unique signatures and frequently retained constraint failures. Examples:

| Floor/family | Retained | Constraint-passing |
| --- | ---: | ---: |
| F3 SingleTargetSpecialist | 7 | 0 |
| F5 DamageHeavy | 30 | 0 |
| F7 DamageHeavy | 21 | 0 |
| F8 MechanicSpecialist | 30 | 0 |
| F8 DamageHeavy | 30 | 0 |
| F10 SingleTargetSpecialist | 30 | 9 |
| F1-F10 LowerPowerP50 balanced cohort | 30 each | 0 each |

These are not merely certification blockers. Because invalid parties are still included in family aggregates, they invalidate the diagnostic meaning of the aggregate itself.

### 2.4 Statistical defect

The family Wilson interval is computed from total clears and total combats:

```text
successes = sum clears across rosters
trials    = parties x seeds
```

Repeated trials from one roster are correlated. The primary sampling unit for family generalization is the roster, not the combat. The current interval is therefore vulnerable to pseudoreplication.

The report should separately expose:

- within-roster combat-seed variance;
- between-roster variance;
- a roster-cluster bootstrap or beta-binomial family interval; and
- the number of valid unique rosters, not merely requested or retained rosters.

### 2.5 Provisional evidence budgets

These budgets apply **only after invalid rosters are excluded and clustered uncertainty is implemented**.

| Use | Valid distinct rosters/family | Common seeds/roster | Combat budget/family | Rationale |
| --- | ---: | ---: | ---: | --- |
| Developer diagnosis | 8 | 5 | 40 | Fast, catches large roster effects, and avoids spending most budget on one composition |
| Release candidate | 15 | 10 | 150 | The tested point where duration centers were generally useful without 50-seed oversampling |
| Deep investigation | 30 | 20 | 600 | Use only for controversial floors; add at least three independent population/search panels |

The current 3x25 release minimum should not be increased blindly; it should be replaced after a valid-roster, clustered-interval study. When a family has fewer than the required valid unique rosters, the result should be `Unavailable/InsufficientFamilyMaterial`, not a pooled conclusion.

## 3. Progression cohort fidelity

### 3.1 Current Floor 1-10 mapping

| Floor | Target power | Recommended CR | Authored CR | Selected profile | Actual profile power | Absolute gap | Relative gap | Level / slots / gear |
| ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | --- |
| 1 | 67.40 | 187.00 | 161 | E4_P75 | 67.40 | 0.00 | 0.00% | 30 / 4 / T1 Rare Exceptional Balanced |
| 2 | 67.61 | 187.89 | 163 | E4_P75 | 67.40 | 0.21 | 0.31% | 30 / 4 / T1 Rare Exceptional Balanced |
| 3 | 68.16 | 190.28 | 165 | E4_P75 | 67.40 | 0.76 | 1.12% | 30 / 4 / T1 Rare Exceptional Balanced |
| 4 | 68.96 | 193.74 | 171 | E4_P75 | 67.40 | 1.56 | 2.26% | 30 / 4 / T1 Rare Exceptional Balanced |
| 5 | 69.91 | 197.84 | 173 | E4_P75 | 67.40 | 2.51 | 3.59% | 30 / 4 / T1 Rare Exceptional Balanced |
| 6 | 70.92 | 202.16 | 175 | E6_P75 | 73.43 | 2.51 | 3.54% | 50 / 6 / T1 Epic Exceptional Balanced |
| 7 | 71.87 | 206.26 | 185 | E6_P75 | 73.43 | 1.56 | 2.17% | 50 / 6 / T1 Epic Exceptional Balanced |
| 8 | 72.67 | 209.72 | 188 | E6_P75 | 73.43 | 0.76 | 1.05% | 50 / 6 / T1 Epic Exceptional Balanced |
| 9 | 73.22 | 212.11 | 189 | E6_P75 | 73.43 | 0.21 | 0.29% | 50 / 6 / T1 Epic Exceptional Balanced |
| 10 | 73.43 | 213.00 | 196 | E6_P75 | 73.43 | 0.00 | 0.00% | 50 / 6 / T1 Epic Exceptional Balanced |

E5_P75 is 73.50 in the seed-1337 population, slightly above E6_P75 at 73.43. Nearest-power selection consequently skips E5 entirely. Floors 1-5 use one character population; Floors 6-10 use another, with a discontinuous jump in level, Essence count, and gear.

### 3.2 Capability distributions of the two selected populations

Physical P10/P50/P90 values from the 10-build P75 profiles:

| Dimension | E4_P75 | E6_P75 | Unit |
| --- | --- | --- | --- |
| SingleTargetBurst | 20.97 / 22.77 / 24.91 | 31.98 / 33.13 / 38.65 | damage/s |
| SingleTargetSustained | 20.99 / 22.22 / 23.80 | 30.13 / 32.33 / 34.62 | damage/s |
| MultiTarget | 21.92 / 24.51 / 28.83 | 36.22 / 42.00 / 44.28 | damage/s |
| FocusSurvivability | 60 / 60 / 60 | 60 / 60 / 60 | seconds |
| AttritionResilience | 180 / 180 / 180 | 171.72 / 180 / 180 | seconds |
| PartySustain | 0 / 0 / 0 | 0 / 0 / 0.53 | ally sustain/s |

The physical survival values saturate and PartySustain is nearly all zero. Relative normalized percentiles still span wide ranges because supporting metrics participate in scoring, but those percentiles are not durable physical encounter requirements.

Neither selected profile contained a build with an observed cleanse or dispel.

### 3.3 Granularity experiment result

Increasing every representative profile from 10 to 50 builds did not recover cleanse evidence. It also changed the anchor definitions:

| Anchor | 10-build P75 | 50-build P75 | Cohort sigma change |
| --- | ---: | ---: | ---: |
| Region 1 start | 67.40 | 66.72 | 0.68 to 2.83 |
| Region 1 end | 73.43 | 72.90 | 0.79 to 2.40 |

This is not a safe specialist-coverage strategy: changing retained cohort size changes the measured percentile anchor and broadens it substantially.

Because authored encounters are at 0% clear, the audit cannot honestly claim whether an E5 or five-stage progression model changes clear-rate conclusions. The floor effect masks that experiment.

### 3.4 Recommendation

Do not build ten optimizers. First add a diagnostic-only progression matrix that evaluates E4_P75, E5_P75, and E6_P75 (plus intended gear/level variants) against selected Floors 3-8 on a neutral balance-only reference. The current E5 profile must not be blindly assigned to mid floors because its measured power is already slightly above E6.

If that matrix shows meaningful changes, the smallest likely production model is three actual progression stages (early E4, mid E5 with explicit mid gear, late E6), not ten. If conclusions remain unchanged in a non-saturated reference, retain the simpler model but label floor precision honestly.

## 4. Controlled fault-injection results

### 4.1 Critical experiment status

The requested fault-injection experiment cannot currently be run validly through the public balance pipeline:

- authored baselines are already 0% clear;
- party-family evaluation has no temporary adjustment request;
- calibration evaluation returns only aggregate clear/duration/death/health metrics, not trial diagnostics or family behavior;
- add cadence/count and harmful-status pressure are not exposed as balance-only clone parameters; and
- the player content/profiled population contains no cleanse effect.

Running “+40%” on an already 0% baseline would create a false PASS/FAIL matrix. The correct audit result is that fault-recovery evidence is absent.

### 4.2 Diagnostic reachability matrix

This matrix combines measured baseline behavior with code-path reachability. It is **not** presented as completed injected-fault evidence.

| Known fault | Expected signature | Current reachable signature/response | Result |
| --- | --- | --- | --- |
| +40% health | Longer duration, timeout/DPS pressure, health tuning | A no-add/no-regen timeout becomes `Other`; assisted calibration has no mapping to Health | FAIL (unreachable) |
| +40% offense | Collapse/attrition, deaths, lower health, offense tuning | `PrimaryTargetCollapse` or `PartyAttrition` maps to Offense; the audited hard baselines produced `NoImprovement` within the assisted 0.70/0.85 grid | PARTIAL |
| +50% regeneration | BossSustainDominance, duration, regeneration tuning | Reachable only for timeout with regeneration >=20% of friendly damage and no final adds; maps to Regeneration | PARTIAL, unproven end-to-end |
| Increased add pressure | AddPressure and multi-target advantage; Review if not tunable | `AddPressure` is primary only on timeout with surviving adds. If the party dies, collapse/attrition is primary and add pressure is merely contributing; assisted calibration may then choose Offense | FAIL for lethal add pressure |
| Increased distributed attrition | PartyAttrition, sustain/attrition prediction | `PartyAttrition` is reachable but maps to generic Offense; no separate evidence shows PartySustain/AttritionResilience became predictive | PARTIAL |
| Increased harmful status pressure | CleanseDemandUnmet and cleanse-family advantage | `CleanseDemandUnmet` is never assigned; no profiled or production player build had Cleanse; all Floor 8 MechanicSpecialist rosters failed their constraint | FAIL |

### 4.3 False-positive and priority risks

- Party defeat always resolves to collapse or attrition before add/regeneration checks. Add and sustain pressure become contributing conditions and cannot drive assisted calibration.
- Timeout checks add pressure before boss regeneration. An encounter ending with adds and high regeneration is always primarily AddPressure.
- `friendly_cleanse_count`, dispel count, and denied-action ticks are recorded as evidence but never participate in mode selection.
- Authoritative mechanic cause is always null in the current production executor/analyzer path.

## 5. Calibration comparison

### 5.1 Measured baseline

In the assisted seed-1337 smoke baseline:

- every authored floor was 0% clear;
- the shared factor selected x0.25 on Floors 1-6 and 9-10, producing 100% clear on those eight floors;
- Floor 7 selected x0.4688 and produced 60% clear;
- Floor 8 selected x0.4688 and produced 80% clear; and
- assisted calibration returned `Review/NoImprovement` on all ten floors.

The shared factor therefore improved numeric target error but usually overshot from 0% to 100%, while simultaneously changing health and offense. This is a distance/sensitivity result, not identity-preserving calibration.

Assisted calibration was directionally honest in refusing a proposal, but its current reachability is too narrow:

```text
PrimaryTargetCollapse -> Offense
PartyAttrition        -> Offense
BossSustainDominance  -> Regeneration
everything else       -> Review
```

Health, Defense, and Resistance exist in the parameter enum and probe code but no diagnostic mode selects them.

### 5.2 Long-term responsibility

The shared calibrator should be retained and renamed/reported conceptually as:

> Generic difficulty sensitivity / distance-from-target probe

It should not be the headline “calibrated encounter” result and should not feed an identity-sensitive specialist verdict without a separate one-parameter validation.

Evidence-driven assisted calibration should remain the only route to a parameter-specific proposal. Until fault injection proves recovery precision, its output should be `ProposalForReview`, never “corrected encounter.”

## 6. Capability population coverage

### 6.1 Current coverage boundary

`ProductionBalanceRunner.CreateCapabilityInputs` profiles:

- generated baseline builds; and
- source builds selected into P50/P75/P90 representative profiles.

The complete optimizer population remains transient. Encounter-specific specialists and elite finalists are not added to the capability input set by this method.

The audit therefore cannot enumerate “specialists lost before profiling” from immutable artifacts: the necessary full evaluated-candidate capability evidence is not persisted.

### 6.2 Measured specialist loss symptom

- Default run: 111 unique capability profiles, zero observed cleansers.
- Expanded 50-build representative run: 202 unique capability profiles, zero observed cleansers.
- Floor 8 MechanicSpecialist: maximum cleanse percentile was 50 for every party because all raw values tied at zero; the constraint requires at least 60.
- Expanded coverage left IntendedBalanced and MechanicSpecialist Floor 8 results identical at 0% clear and median 820 ticks in that run.

This may mean either no player Cleanse exists (confirmed for the current content catalog), the probe cannot trigger a real cleanse kit, or both. It is not evidence that cleanse specialists are balanced.

### 6.3 Smallest useful strategy

Do not profile every optimizer candidate. Add an audit-only selector over the already evaluated candidate population:

```text
representative source builds
+ top/bottom K by each physical benchmark scenario
+ Pareto frontier candidates
+ encounter-specific retained specialists
+ elite finalists
+ candidates containing a typed mechanic operation known from content
```

Profile only newly selected IDs through the existing cache. Persist counts for total evaluated, selected for capability, already cached, and omitted. Keep percentile anchor construction independent from this coverage set so specialist coverage cannot move progression anchors.

## 7. Certification policy recommendation

The current `CertifiedElite` policy requires curated builds and parties. Missing curated evidence produces `InsufficientPlayerEvidence`. That is honest, but it conflates shippability with later empirical model validation.

Define two distinct claims:

### Pre-release balance certification

Suggested state: `PreReleaseBalanceCertified`.

Requires deterministic generated cohorts, valid specialist/family cohorts, independent population/search panels, elite and complete-party search, holdouts, progression ordering, authored response-shape validation, and passing framework fault injection. Manually designed developer/tester builds may be included but must be labeled as such.

### Post-live model validation

Suggested state: `PostLiveModelValidated`.

Adds actual player build/composition distributions, live clear/wipe telemetry, observed meta tails, and predicted-versus-observed calibration.

Generated P50/P75/P90 must remain labeled generated-population percentiles.

Recommendation: document this policy now, but defer enum/schema implementation until the fault-injection and sampling blockers are fixed. A new certification label before trustworthy evidence would add ceremony rather than confidence.

## 8. Combat Rating health recommendation

The current CR health classification is a combat-performance regression label:

- `Excellent` requires Spearman >=0.90 and R2 >=0.80;
- `Good` requires Spearman >=0.75 and R2 >=0.60; and
- the model also gates on generic-score error and within-band performance spread.

That does not match the intended permanent-investment contract.

Measured classifications changed solely with the generated master seed:

| Seed | Classification | Spearman | R2 |
| ---: | --- | ---: | ---: |
| 1337 | Concerning | 0.6462 | 0.3842 |
| 2029 | Poor | 0.4575 | 0.1138 |
| 8471 | Poor | 0.3962 | 0.1298 |

Within each E4/E5/E6 sampled profile, CR is constant, while legal Essence specialization drives benchmark differences. Low universal-performance R2 is therefore expected and not proof of an unhealthy progression metric.

Recommendation:

- rename the current label to `CombatPerformanceCorrelation` and keep it diagnostic;
- do not emit `Poor CR health` from that regression alone;
- add a separate progression-contract health result based on monotonic level, slot, tier, rarity, quality, tempering, and equipment-investment perturbations; and
- explicitly test large CR changes without permanent investment and permanent investments that fail to move CR.

No CR formula redesign is recommended by this audit.

## 9. Response-shape and physical-requirement audit

### 9.1 Current response shapes

The authored response catalog is clear and reviewable, but current evidence does not validate it:

- Floor 3 MultiTargetSpecialist: 0% clear, as did IntendedBalanced; no clear-rate advantage established.
- Floor 7 SingleTargetSpecialist: 0% clear. Median defeat duration ranged from 282 to 425 ticks across audited population/budget runs; it was not consistently better than balanced.
- Floor 8 MechanicSpecialist: 0% clear and no valid cleanse roster.

No supported balance-only injection can currently increase add, healing-ramp, or poison pressure and rerun the family evaluator. The requested monotonic response-shape test is therefore absent.

### 9.2 Physical versus relative measurements

The code follows the distinction partially:

- combat telemetry and capability raw values are physical;
- party construction appropriately uses normalized percentiles;
- mechanic-family constraints use a relative mechanic percentile rather than a physical cleanse rate;
- authored response profiles specify clear-rate envelopes, not physical encounter requirements;
- assisted calibration optimizes clear-rate error rather than a physical mechanic threshold; and
- progression “power” is a normalized equal-weight benchmark score, not a physical combat requirement.

The framework does not currently allow population composition to silently mutate a stored physical encounter requirement because no such requirement is stored. The stronger conclusion is that durable physical encounter requirements have not yet been authored.

Do not add new universal dimensions. Add physical requirements only where the current telemetry already supports them, for example sustained guardian DPS, first-death time, regeneration-to-damage ratio, add-clear time, or actual cleanses per interval once a cleanse kit exists.

## 10. Uncertainty decomposition

| Boundary | Current handling | Audit result |
| --- | --- | --- |
| Combat RNG | Common seeds per roster; configurable replication | Material for duration. Five to fifteen seeds appear more useful than fifty once roster coverage is valid. |
| Party sampling | Deterministic roster generator | Dominates some failure-mode mixtures and family centers; currently confounded by invalid rosters and signature exhaustion. |
| Build population | One generated population per run | Material. Start anchors across seeds were 66.93-68.70; end anchors were 73.43-75.87, wider than each retained cohort sigma. |
| Search/optimizer | Deterministic for one master seed; optional elite restarts | Material and confounded with build population in normal runs. Elite search-only results still found local improvements. |

The additional master-seed runs demonstrate end-to-end sensitivity but do not isolate build generation from optimizer randomness or benchmark RNG. A reliability harness should vary one boundary at a time while freezing the others.

## 11. Framework changes required

### Must fix before trusting Region 1 balance — implemented

All seven framework-correctness items below are implemented. Their completion establishes that the tooling reports its evidence honestly; it does not supply the missing authored family contracts, cleanse content, elite player evidence, or progression policy required for expansion.

1. Exclude constraint-failing rosters from family aggregates; retry bounded generation and return insufficient material if the valid target cannot be met.
2. Replace pooled-only Wilson evidence with explicit within-roster and between-roster uncertainty plus a cluster-aware family interval.
3. Add a balance-only neutral-reference fault-injection path that preserves production content and returns full trial diagnostics and party-family results.
4. Make health/offense/regeneration fault recovery testable; ensure lethal add pressure cannot be mislabeled as generic offense merely because the party died.
5. Remove or mark Floor 8 cleanse specialization `Unavailable` until a real player cleanse capability exists and is physically measurable.
6. Stop presenting ten-floor progression precision from two populations; run the E4/E5/E6 diagnostic matrix before choosing the smallest stage model.
7. Demote shared-factor output from primary calibration to generic sensitivity/distance evidence.

### Should improve before release

1. Add incremental specialist/Pareto/elite capability coverage without changing percentile anchor definitions.
2. Separate CR progression health from combat-performance correlation.
3. Add pre-release versus post-live certification policy language.
4. Persist experiment cost: combat count, simulated ticks, stage wall time, allocations, and cache hits/misses.
5. Persist enough per-roster seed summaries to reproduce stability grids without rerunning the whole upstream pipeline.

### Useful later

1. Live player-distribution and predicted-versus-observed model validation.
2. Region 2+ rollout only after the complete Region 1 expansion gate passes or its remaining author/content exceptions are explicitly approved.
3. Additional physical mechanic requirements when production content introduces those mechanics.

### Do not implement

1. New universal capability dimensions for this validation.
2. A universal Utility score combining cleanse, dispel, control, and stagger.
3. Ten independent floor optimizers.
4. Automatic production tuning or production-content mutation.
5. Brute-force increases to every simulation count.

## 12. Recommended next implementation slice

The original optional reliability slice is implemented through schema 46. Population-replication policy v3 aggregates diagnostic recovery and family-contract replication independently, requires explicit matching upstream population provenance, rejects incompatible protocols, and prevents either missing family evidence or a favorable population majority from becoming a false pass.

No additional empirical implementation slice is justified from the existing measurements. The remaining actions are author/content prerequisites, not reasons to search more thresholds or multiply combat budgets:

The [Region 1 affected-family contract decision](region-one-family-contract-decision.md) packages the settled evidence, recommended physical contract shapes, required author choices, and replication policy for the two missing family premises.

1. Freeze AttritionResilience as diagnostic evidence only. The preregistered burden-plus-mitigation rule passes seed 9013 but reverses on seed 11027; do not test further combinations from the same probe without an author-defined premise or a materially different production-observable.
2. Freeze the tested Regeneration damage-survival conjunction as rejected diagnostic evidence. Its corrected discovery population has zero qualifying source builds and cannot reach the preregistered exposure-spread prerequisite; do not revise the medians or search further empirical combinations without an author-defined premise or a materially different production-observable.
3. Treat the matched-genome power diagnosis as complete. Do not revise subset construction or add power thresholds; any further three-stage review requires an author-owned neutral-reference policy, cohort-construction decision, and a fresh preregistered panel whose schema-46 population protocols match exactly.
4. Keep the confirmed AddPressure contract frozen; do not retune its reset or uptime thresholds from the confirmation seeds.
5. Keep CleanseDemand out of scope until production player content provides measurable cleanse capability and enough valid specialist rosters.

### Preregistered burden-mitigation follow-up

Before inspecting another master population, the next AttritionResilience candidate is frozen as a conjunctive E4 source-build rule. A build qualifies only when its `average_health_deficit_ratio` is strictly below the frozen E4 candidate-pool median and its prevented-damage ratio is strictly above that pool's median. Ties do not qualify. A trial's exposure is the share of its actual sampled members whose representative build maps back to a qualifying source build; roster exposure is the mean of those trial shares.

The untouched-population primary endpoint is roster-cluster first-death-free timing at the full DistributedAttrition fault. Missing first deaths are censored at the common combat limit. The candidate is useful only if all three conditions hold without fitting: at least three distinct roster exposure values, Spearman correlation of at least `+0.50` between exposure and mean first-death-free ticks, and at least `10%` later mean first death in the highest exposure tertile than the lowest. One passing population permits one replication; it does not authorize a family contract. Any failed condition ends this candidate without threshold revision.

The first untouched seed, 6311, is unavailable rather than failed: its generated population retains only 2/3 required unique Floor-1 SingleTargetSpecialist rosters, so no reliability fault has a complete frozen family population. The unchanged rule is therefore evaluated on seed 9013. Its 19-build E4 source pool has median deficit `0.0102`, median prevention `0.4154`, and five strict qualifiers. Twelve roster clusters produce four exposure values, Spearman `+0.54`, and a `+32.0%` top-versus-bottom-tertile first-death advantage (`606.4` versus `459.4` ticks). This passes the preregistered discovery gate and permits exactly one replication.

Seed 11027 fails that replication without a prerequisite problem. Its 18-build E4 pool has median deficit `0.01105`, median prevention `0.4156`, and seven strict qualifiers; twelve rosters again provide four exposure levels. The outcome direction reverses to Spearman `−0.36`, and the top tertile reaches first death `0.9%` earlier than the bottom (`469.3` versus `473.6` ticks), failing both outcome gates. The burden-mitigation conjunction is therefore population-sensitive and rejected as a cohort basis. Its medians, strict tie rule, and acceptance thresholds are not revised. Further empirical combinations from the same saturated probe would be post-hoc metric fishing; DistributedAttrition requires an author-defined affected-family premise or a materially different production-observable before another contract attempt.

### Preregistered Regeneration damage-survival follow-up

Before inspecting another master population, the Regeneration candidate is frozen as a conjunctive E5 source-build rule. A build qualifies only when its physical `SingleTargetSustained` raw value is strictly above the frozen E5 candidate-pool median and its AttritionResilience `average_health_deficit_ratio` is strictly below that pool's median. Ties do not qualify. A trial's exposure is the share of its actual sampled members whose representative build maps back to a qualifying source build; roster exposure is the mean of those trial shares. This is a cross-family diagnostic cohort, not a new named family or an authored threshold.

The untouched-population primary endpoint is roster clear rate at the full Regeneration fault. The necessary physical bridge is average Guardian damage taken minus realized Guardian self-sustain per second; Guardian remaining-health ratio is a secondary direction check. The candidate is useful only if all four conditions hold without fitting: at least three distinct roster exposure values; Spearman correlation of at least `+0.50` between exposure and roster net Guardian damage per second; Spearman correlation of at least `+0.50` between exposure and roster clear rate; and the highest exposure tertile clears at least `10` percentage points more often than the lowest while leaving Guardian remaining-health ratio at least `0.10` lower. One passing population permits exactly one replication and does not authorize a family contract. An incomplete family population is unavailable; any failed outcome condition ends this candidate without median, tie-rule, endpoint, or threshold revision.

Seed 12041 is a protocol-unavailable dry run rather than a candidate result. Its complete Floor-7 reliability panel draws from `E6_P75`, not the historically expected `E5_P75`, so an E5-only rule gives every roster zero exposure and cannot test the preregistered minimum spread. No candidate-axis values were evaluated. Before opening another population, the replacement protocol freezes the same strict median conjunction, exposure calculation, endpoints, and four acceptance gates against the source-build pool of the P75 profile actually selected for Floor 7. Seed 12041 is excluded from discovery and replication; this profile-relative correction may not be revised after another run.

The corrected seed-14281 discovery is complete and rejects the candidate before an outcome test. Floor 7 selects `E6_P75`; the matching source pool contains 17 builds with a sustained-damage median of `29.25` damage per second and an average-health-deficit median of `0.0075`. Eight builds lie strictly above the damage median and eight lie strictly below the deficit median, but their intersection is empty. The 12 full-fault roster clusters consequently have one exposure value (`0`), not the required three. Net-damage, clear-rate, tertile, and Guardian-health gates are non-interpretable rather than grounds for selecting a different conjunction. No replication is run, the rule is not revised, and Regeneration remains diagnostic `Pass`, family `InsufficientEvidence`, composite `Inconclusive`.

### Preregistered three-stage progression-model review

With both unauthored family candidates frozen, the next in-scope review returns to the existing progression-fidelity blocker rather than searching more metric combinations. The candidate author model is fixed as Floors 1–4 using E4, Floors 5–7 using E5, and Floors 8–10 using E6. This is evaluated only as a recommendation for author review; the diagnostic does not change progression anchors, target power, gear packages, representative selection, or production content.

Before inspecting the progression matrices already emitted for seeds 12041 and 14281, the acceptance rule is frozen against those two protocol-compatible artifacts plus one untouched run using the same options. All three populations must leave `productionContentModified` false, retain complete 3/3 E4/E5/E6 IntendedBalanced rosters on every tested Floor 3–8, and report monotonic P75 mean benchmark power. In addition, E5 must materially change at least one tested floor conclusion in every population under the analyzer's existing thresholds: at least `15` clear-rate points, at least `10%` median-duration change, or a different dominant observed failure mode relative to the currently selected profile. A unanimous pass supports review of the fixed three-stage boundaries but not automatic implementation. Any incomplete or non-monotonic population, or any population with no material E5 change, rejects the model recommendation without threshold revision.

The three-population result rejects automatic progression-model adoption. Seed 12041 runs 2,520 matrix combats, leaves production content unchanged, and finds material E5 changes on Floors 3, 5, 6, 7, and 8; however, Floor 4 has no 40–80% neutral reference and P75 mean power is non-monotonic because E5 (`70.46`) exceeds E6 (`68.33`). Seed 14281 runs 2,655 combats and is the only complete, power-monotonic pass of the prerequisites, with material changes on Floors 3, 7, and 8. The untouched seed 16633 runs 3,240 combats and is power-monotonic, with material changes on Floors 4 and 8, but Floor 3 has no neutral reference. E5 relevance therefore replicates 3/3, while complete protocol prerequisites replicate only 1/3. The frozen Floors 1–4 / 5–7 / 8–10 mapping is not implemented or promoted; author-owned progression packages and a complete neutral-reference policy must be resolved before another adoption attempt.

Expansion remains blocked. Additional combat on the existing three populations would increase precision without supplying either missing authored premise.

## 13. Validation decision

The Region 1 “test the tester” milestone is complete. The framework reliably detects the five supported injected mechanics, preserves unavailable prerequisites, separates physical diagnostic recovery from affected-family claims, exposes sampling and population sensitivity, and now refuses cross-population aggregation without complete upstream protocol provenance.

The rollout decision is **NO-GO for Region 2+ expansion**. Retain the current Region 1 smooth-step progression and frozen neutral-reference bounds. Do not implement the candidate fixed E4/E5/E6 floor mapping, invent Regeneration or DistributedAttrition family contracts, fabricate CleanseDemand evidence, or compensate by increasing every simulation budget.

Reopen expansion review only after all applicable prerequisites are supplied: author-approved affected-family contracts for Regeneration and DistributedAttrition; real player cleanse content with physically observed capability and valid specialist rosters; release-grade elite/player evidence or an explicitly approved pre-release certification exception; and, if a three-stage progression model is still desired, an author-owned neutral-reference/cohort policy followed by a fresh preregistered schema-46 panel with identical population protocols.

## Verification

Repository-required backend verification:

```text
build/run-tests.ps1
Passed: 1756
Failed: 0
Skipped: 0
Duration: 3m 6s
Build warnings: 2 pre-existing xUnit2031 analyzer warnings in unrelated tests
Build errors: 0
```

All diagnostic balance runs completed successfully. No migrations, configuration changes, production content changes, database operations, deployments, or player-facing changes were made.
