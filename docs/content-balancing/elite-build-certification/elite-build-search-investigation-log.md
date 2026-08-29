# Elite Build Search Investigation Log

**Status:** Active investigation; no search configuration is approved for certification.

**Evidence date:** 2026-08-28

**Current certification analyzer:** Algorithm v20

This document consolidates the experiments used to investigate elite-build search convergence, especially the persistent E5 failure on seed `8471`. It is an evidence log, not a replacement for the normative [certification policy](elite-build-certification-policy-v1.md) or [certification gate](elite-build-certification-gate.md).

## Frozen constraints

All conclusions in this log preserve these requirements:

- restart best-score spread must remain at or below `0.50`;
- independent restarts may not receive another restart's winning genome;
- bridge-audit builds may not enter certification evidence or become restart inputs;
- only authoritative production-boundary PvE benchmarks may determine scores;
- a lower, falsely converged ceiling is not acceptable evidence;
- missing curated player evidence blocks `CertifiedElite` even when generic search passes;
- search-only mode is diagnostic and cannot certify;
- an experimental setting is not promoted from one root seed;
- seed `1337` is run only after the candidate configuration passes seed `8471`;
- developer and release defaults remain unchanged unless a proposal passes the complete gate;
- no experiment may relax the frozen `0.50` restart tolerance.

The main diagnostic configuration was three independent restarts, population `96`, minimum `24` generations, maximum `40` generations, and `12` elites. Unless stated otherwise, diagnostics used seed `8471` in search-only mode.

## Initial evidence

The higher-budget baseline was tested against two root seeds:

- seed `1337` produced E4/E5/E6 spreads of `0.00`, `0.00`, and `0.45`;
- seed `8471` produced spreads of `0.34`, `0.94`, and `0.12` in the retained algorithm-v6 artifact;
- E5 restart scores on seed `8471` were `86.21`, `85.27`, and `86.21`;
- the weaker E5 result was four substitutions away from the stronger genome;
- E4 and E6 were already within the frozen `0.50` tolerance;
- the proposed `96`/`24`-`40`/`12` budget was not approved because it was not robust across both seeds and E5 failed on `8471`.

Increasing the restart-refinement beam from four to eight seeds did not change E5. It increased the retained certification population from `59,006` to `82,506` candidates, changed E4 from `0.34` to `0.46`, and left E5 and E6 at `0.94` and `0.12`. This ruled out insufficient local-refinement seed count as the primary E5 cause.

## Known E5 endpoint genomes

### Weaker isolated restart

- Build ID: `E5_CERT_c1a9f8d75c537390`
- Score: `85.27`
- Genome:
  - `essence.bark_golem`
  - `essence.elder_treant_thornstorm`
  - `essence.illusion_fox`
  - `essence.venomous_spiderling`
  - `essence.wind_harpy`

### Stronger restart

- Build ID: `E5_CERT_9d3f5362169df0b9`
- Score: `86.21`
- Genome:
  - `essence.bark_golem`
  - `essence.giant_bat`
  - `essence.plague_ghoul`
  - `essence.poisonous_rat`
  - `essence.spider_queen_royal_venom`

The genomes share only `essence.bark_golem`; their minimum substitution distance is four.

## Experiment summary

| Analyzer | Experiment | Unique certification candidates | Runtime | E4 spread | E5 spread | E6 spread | Outcome |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 6 | Higher-budget baseline | 59,006 | Not retained | 0.34 | 0.94 | 0.12 | E5 failed |
| 6 | Eight refinement seeds | 82,506 | Not retained | 0.46 | 0.94 | 0.12 | More local seeds did not help E5 |
| 7 | Initial 35% crossover | 59,936 | Not retained | 0.46 | 0.61 | 0.00 | E5 failed and lost the 86.21 ceiling |
| 8 | Corrected 35% crossover | 58,224 | Not retained | 0.41 | 1.71 | 0.41 | E5 worsened materially |
| 9 | Exhaustive width-16/depth-3 valley beam | 80,681 | 1,538.39 s | 0.29 | 0.94 | 0.07 | E5 unchanged; runtime failed |
| 10 | Prefiltered valley beam, 256/depth and 768/restart/profile | 61,107 | 1,249.75 s | 0.00 | 0.94 | 0.07 | E5 unchanged; stronger E4 ceiling pruned |
| 11 | Audit-only shortest-path bridge | 59,006 plus 146 audit nodes | 1,247.34 s | 0.34 | 0.94 | 0.12 | Proved a genuine shortest-path valley; no certification change |
| 12 | 20% direct three/four-gene basin jumps | 62,296 | 1,407.29 s | 0.46 | 1.09 | 0.37 | E5 worsened; runtime failed |
| 13 | 20% jumps plus 12-candidate explorer archive | 59,793 | 1,386.22 s | 0.17 | 0.15 | 0.99 | False E5 convergence caused by ceiling loss |
| 14 | 256 isolated stratified candidates/restart/profile | 109,724 | Approximately 517.35 s | 0.29 | 1.09 | 0.34 | Ceilings preserved; E5 still failed |
| 15 | 256-candidate scenario-niche quality island/restart/profile | 62,865 | Approximately 256 s | 0.63 | 1.30 | 0.34 | No island beat its refined baseline; E4/E5 failed |
| 16 | Audit-only E5 descriptor separability | 60,903 plus 1,112 audit candidates | Approximately 304 s | 0.63 | 1.30 | 0.34 | Mechanic family separated neighborhoods; source/effect signatures fragmented and scenario shape failed |
| 17 | Audit-only coarse mechanic-map validation | 60,903 plus 1,112 audit candidates | Approximately 289 s | 0.63 | 1.30 | 0.34 | Eight-axis mechanic archetype passed with 55 observed niches |
| 18 | 256-candidate mechanic-archetype island/restart/profile | 63,023 | Approximately 303 s | 0.63 | 1.30 | 0.34 | Island reached the high coarse niche but no island beat its refined baseline |
| 19 | Audit-only coarse-niche collision study | 60,903 plus 1,112 audit candidates | Approximately 272 s | 0.63 | 1.30 | 0.34 | Four-axis residual collided with weak candidates and had 0% high accuracy |

Runtime was not recorded in the retained summaries for the baseline, expanded-refinement, or crossover runs. Those values must not be reconstructed or guessed.

## Detailed findings

### 1. Baseline and expanded local refinement

The baseline demonstrated that the larger ordinary genetic search can succeed on seed `1337` but is not robust to seed `8471`. On `8471`, two E5 restarts reached `86.21` and one remained at `85.27`.

The eight-seed restart-refinement experiment evaluated substantially more candidates without changing either E5 endpoint. This indicates that the problem is not simply that the correct local-search seed was omitted. Once a restart enters the weaker basin, one- and two-swap refinement does not escape it reliably.

### 2. Elite-parent crossover

The crossover hypothesis was that recombining independently strong parents inside each restart would assemble the stronger E5 genome more frequently.

The initial algorithm-v7 trial reported E5 scores of `85.27`, `84.66`, and `84.91`, for `0.61` spread. Although close to the tolerance, it lost the known `86.21` ceiling and was therefore invalid.

The corrected algorithm-v8 trial reported E5 scores of `86.21`, `85.27`, and `84.50`, widening the spread to `1.71`. E4 and E6 passed at `0.41`, but E5 became less stable. Crossover remains opt-in and disabled in both approved profiles.

Conclusion: parent recombination does not reliably preserve or assemble the specific four-substitution E5 basin.

### 3. Exhaustive bounded valley search

The width-`16`, depth-`3`, budget-`5,000` experiment allowed restart-local search to retain temporarily weaker layers instead of requiring immediate improvement.

It took `1,538.39` seconds, evaluated `80,681` unique certification candidates, and exhausted the `5,000`-candidate allowance for every restart/profile. E4 and E6 passed at `0.29` and `0.07`, but E5 remained `86.21`, `85.27`, `86.21`, with `0.94` spread.

Conclusion: the configured three-layer valley search was expensive and did not move the isolated E5 restart into the stronger basin.

### 4. Metadata-prefiltered valley search

The prefilter attempted to reduce authoritative benchmarks by ranking valley candidates with observed Essence performance, P99 usage, admin-adjusted deltas, and pair synergy.

It generated `79,022` distinct restart-local candidates, rejected `72,110`, authoritatively benchmarked `6,912`, and retained `61,107` unique certification candidates. Runtime was still `1,249.75` seconds. E5 remained unchanged at `0.94` spread.

E4 appeared to converge at `0.00`, but all three restarts reached only `78.32`; the stronger `78.61` ceiling found by the exhaustive run was pruned. This is the clearest example of why spread alone is insufficient.

Conclusion: the surrogate was not reliable enough to decide which candidates deserved authoritative evaluation. Prefiltering reduced benchmarks but hid a stronger basin and did not fix E5.

### 5. Audit-only shortest-path bridge

Algorithm v11 separated topology analysis from certification. It enumerated every legal genome composed from the differing endpoint genes at the minimum substitution distance, benchmarked the materialized builds authoritatively, and computed a deterministic maximin path. Audit candidates never entered the shared certification candidate dictionary and did not change restart scores, percentiles, finalists, challenges, verdicts, or `TotalUniqueCandidatesEvaluated`.

The complete run retained `59,006` certification candidates and separately evaluated `146` bridge nodes: 6 for E4, 70 for E5, and 70 for E6.

The E5 maximin path was:

| Step | Build ID | Score | Genome |
| ---: | --- | ---: | --- |
| 0 | `E5_CERT_c1a9f8d75c537390` | 85.27 | `bark_golem`, `elder_treant_thornstorm`, `illusion_fox`, `venomous_spiderling`, `wind_harpy` |
| 1 | `E5_CERT_019b0a79c4c51e01` | 83.97 | `bark_golem`, `elder_treant_thornstorm`, `illusion_fox`, `plague_ghoul`, `wind_harpy` |
| 2 | `E5_CERT_3ead7b4a8e044528` | 83.69 | `bark_golem`, `elder_treant_thornstorm`, `plague_ghoul`, `spider_queen_royal_venom`, `wind_harpy` |
| 3 | `E5_CERT_bb12b8362b1506f6` | 83.87 | `bark_golem`, `elder_treant_thornstorm`, `plague_ghoul`, `poisonous_rat`, `spider_queen_royal_venom` |
| 4 | `E5_CERT_9d3f5362169df0b9` | 86.21 | `bark_golem`, `giant_bat`, `plague_ghoul`, `poisonous_rat`, `spider_queen_royal_venom` |

E5 bridge measurements:

- minimum score: `83.69`;
- largest single-step regression: `1.30`;
- total temporary regression below the `85.27` source: `4.28`;
- non-regressing bridge: no;
- bridge bounded to a `0.50` maximum step regression: no.

E4 required a `1.50` regression across its minimum bridge. E6 required a `1.80` regression. The audit proves that E5 has a genuine fitness valley within the complete minimum-substitution state space. It does not prove that every longer path through genes outside the endpoints has the same valley.

### 6. Direct coordinated mutation

Algorithm v12 replaced `20%` of ordinary births with exact legal three- or four-gene mutations inside each restart. It did not add births or share candidates across restarts.

The run produced `3,366` coordinated mutations, took `1,407.29` seconds, and retained `62,296` candidates. Results were:

- E4: `78.15`, `78.61`, `78.32`; spread `0.46`;
- E5: `86.21`, `85.50`, `85.12`; spread `1.09`;
- E6: `87.34`, `87.09`, `87.46`; spread `0.37`.

Conclusion: single-generation large jumps introduce diversity, but the stronger E5 basin is still reached by only one restart. Unretained jumps do not provide a reliable route across the valley.

### 7. Persistent explorer archive

Algorithm v13 tested whether temporarily weak descendants needed to persist across generations. A 12-candidate restart-local archive retained explorer descendants as alternate parents while leaving the official elite unchanged.

The run produced `1,664` new coordinated seeds and `1,513` archive continuations, took `1,386.22` seconds, and retained `59,793` candidates. Results were:

- E4: `78.32`, `78.32`, `78.15`; spread `0.17`, but the `78.61` ceiling was lost;
- E5: `85.27`, `85.12`, `85.27`; spread `0.15`, but the `86.21` ceiling was lost;
- E6: `86.56`, `86.47`, `87.46`; spread `0.99`.

Conclusion: persistent undirected exploration can make the spread look better by displacing successful trajectories. The E5 result was false convergence and cannot be promoted.

### 8. Isolated stratified portfolio

Algorithm v14 tested deterministic coverage without changing baseline RNG or removing baseline candidates. Every restart completes its baseline optimizer and four-seed refinement beam first. It then benchmarks `256` separately seeded legal portfolio genomes per profile and refines a separate four-seed portfolio beam. The final result must dominate the fully refined baseline result.

The first v14 execution captured the baseline before refinement. That measurement was too weak because a portfolio seed could displace a productive baseline refinement seed. Its artifact is retained for auditability but is superseded and must not be used for conclusions.

The corrected execution:

- directly benchmarked `2,304` portfolio genomes;
- evaluated `80,560` baseline-plus-portfolio local candidates;
- retained `109,724` unique certification candidates;
- completed in approximately `517.35` seconds;
- preserved the E4/E5/E6 ceilings of `78.61`, `86.21`, and `87.46`.

Corrected results:

| Profile | Fully refined baseline scores | Final scores | Final spread | Interpretation |
| --- | --- | --- | ---: | --- |
| E4 | 77.98, 78.61, 78.32 | 78.61, 78.61, 78.32 | 0.29 | Portfolio moved restart 1 into the stronger basin |
| E5 | 86.21, 84.91, 85.27 | 86.21, 85.12, 85.27 | 1.09 | Only a 0.21 gain in restart 2; stronger basin still isolated |
| E6 | 87.46, 87.46, 87.12 | 87.46, 87.46, 87.12 | 0.34 | Baseline already passed; portfolio made no change |

Conclusion: ceiling preservation works, and simple arithmetic stratification can improve coverage, as E4 demonstrates. It is still not structured enough to cover the stronger E5 basin reliably. Increasing the same portfolio size is not justified by this single failed configuration.

### 9. Isolated scenario-niche quality-diversity island

Algorithm v15 implemented the bounded follow-up behind `--elite-quality-island`. Each restart completes the unchanged optimizer and full baseline refinement first. A separate deterministic island then seeds a behavioral map from that restart's own authoritative baseline population, evaluates 32 fresh legal candidates, and spends the remaining budget on one-substitution descendants of current niche champions. Niches are the candidate's strongest/weakest production PvE benchmark scenario pair, with at most 25 stable gameplay niches. Another restart's candidates, bridge nodes, and experimental descendants are never eligible parents. The final restart result must dominate its serialized baseline.

The first execution combined scenario pairs with complete ability-role sets. That produced 96–124 niches per island, spread 224 descendants too thinly, and left every island well below baseline. The artifact is retained but superseded. The corrected run used the same seed, baseline, and `256`-candidate budget while limiting the map to scenario pairs and pre-filling it from the complete restart-local baseline population.

The corrected seed-`8471` run benchmarked `2,304` island candidates, retained `62,865` unique certification candidates, and completed in approximately `256` seconds. Islands occupied 7–11 niches and recorded 1–13 champion replacements per restart, so the archive mechanism was materially exercised. No island beat any fully refined baseline:

| Profile | Baseline/final restart scores | Best island scores | Spread | Interpretation |
| --- | --- | --- | ---: | --- |
| E4 | 77.98, 78.61, 78.32 | 75.82, 77.36, 77.80 | 0.63 | Ceiling preserved, but the island did not reproduce the v14 portfolio gain |
| E5 | 86.21, 84.91, 85.27 | 82.64, 84.04, 84.55 | 1.30 | Strong basin remained isolated; no restart improved |
| E6 | 87.46, 87.46, 87.12 | 87.08, 86.15, 86.07 | 0.34 | Baseline already passed; no island gain |

The run met the developer runtime criterion but remained `SearchUnstable`. Seed `1337` was not run. The option remains disabled and the budget is not promoted. The result rejects strongest/weakest scenario pairs as sufficient behavioral niches at this budget; occupancy and replacement activity alone do not demonstrate useful basin coverage.

### 10. Audit-only E5 descriptor separability

Algorithm v16 implemented the recommended study behind `--elite-descriptor-audit`. The audit materializes each known E5 anchor and every unique legal one-substitution neighbor, then evaluates those builds through the production PvE benchmark. It separately compares the 17 unique optimizer-retained E5 candidates from the three baseline restarts. Audit builds cannot enter a restart population, seed refinement, alter a ceiling, enter percentile/finalist cohorts, or affect a verdict or `TotalUniqueCandidatesEvaluated`.

The seed-`8471` run retained the unchanged E4/E5/E6 restart results and separately evaluated `1,112` E5 genomes: `372` in the strong-anchor neighborhood and `740` in the union of the two weak-anchor neighborhoods. The three anchors reproduced `86.21`, `85.27`, and `84.91`. There were no high/low neighborhood overlaps at distance one.

The audit predeclared a descriptor-family pass as: no high/low anchor collision, at least `80%` exact-signature purity, at least `80%` balanced nearest-anchor accuracy, and at most `50%` of candidates in singleton signatures.

| Descriptor family | Features | Signatures | Purity | Singleton rate | High / low / balanced accuracy | Retained exact high niche | Result |
| --- | ---: | ---: | ---: | ---: | --- | ---: | --- |
| Source family | 77 | 1,079 | 100.00% | 94.06% | 100.00% / 100.00% / 100.00% | 0 | Failed fragmentation criterion |
| Authored mechanics | 41 | 472 | 99.28% | 26.17% | 97.31% / 96.22% / 96.76% | 0 | Passed family-level criteria |
| Authored effect role | 88 | 1,112 | 100.00% | 100.00% | 99.73% / 99.73% / 99.73% | 0 | Failed fragmentation criterion |
| Centered scenario shape | 5 | 802 | 94.24% | 55.31% | 0.54% / 94.32% / 47.43% | 0 | Failed accuracy and fragmentation criteria |

Production Essence-level tag arrays are empty. The mechanic and effect-role descriptors therefore use resolved authored active/passive ability specifications. The strongest mechanic contrasts were `OnDamageDealt` (strong/weak mean `0.03`/`0.80`), `HasCondition` (`0.12`/`0.91`), `OnStatusExpired` (`0.83`/`0.04`), `EventDamageTypeIs` (`0.05`/`0.83`), and `OnMeleeAttack` (`0.83`/`0.43`). These are authored behavior signals rather than non-authoritative fitness estimates.

Conclusion: the study rejects source identity, the full effect-role vector, and centered scenario shape as direct niche keys. Authored mechanics contain real basin-separating information, but the complete 41-feature count signature still creates 472 niches and none of the 17 retained baseline candidates exactly matches the strong anchor's full signature. This is evidence for a bounded, coarsened mechanic descriptor study; it is not evidence for increasing the v15 island budget or using the full mechanic signature directly.

### 11. Audit-only coarse mechanic-map validation

Algorithm v17 added one prospective descriptor to the unchanged v16 audit. The descriptor is independent of all anchor Essence identities and reduces resolved authored behavior to eight binary axes: attack action, outgoing result, incoming reaction, health/recovery, status lifecycle, condition dependency, event filtering, and timeline/summon/terminal behavior. Its theoretical niche count is therefore hard-bounded at `2^8 = 256` for every legal candidate. It contains no benchmark score, source-family identity, or hand-coded high-basin genome feature.

The seed-`8471` diagnostic repeated the same `1,112` authoritative E5 audit evaluations and retained the unchanged `60,903` certification candidates. The coarse map produced:

- `55` observed neighborhood signatures under the hard `256` ceiling;
- `97.57%` exact-signature purity;
- `0.63%` singleton-candidate rate;
- `81.45%` high-neighborhood accuracy;
- `95.41%` low-neighborhood accuracy;
- `88.43%` balanced nearest-anchor accuracy;
- `71` distance ties treated as ambiguous rather than correct;
- no high/low anchor collision.

The strongest coarse contrasts were status-lifecycle behavior (`0.81` high vs `0.07` low), outgoing-result behavior (`0.09` vs `0.81`), condition dependency (`0.12` vs `0.82`), and health/recovery behavior (`0.84` vs `0.23`). The descriptor passed all predeclared v16 separation criteria and its explicit hard niche ceiling, making `mechanic-archetype` the only map-ready descriptor in the artifact.

The 17 optimizer-retained candidates still contain no exact match for the strong anchor's coarse signature. That does not invalidate the map: it demonstrates that ordinary retained populations omit a behavior niche associated with the stronger neighborhood. The audit remains topology evidence only and did not inject that signature, anchor, or any audit candidate into search.

Conclusion: a small restart-local mechanic-archetype island is now justified. It must pre-fill only from each restart's own complete baseline, use the generic eight-axis descriptor exactly as audited, benchmark every descendant authoritatively, preserve the fully refined baseline ceiling, and remain disabled by default. The first seed-`8471` trial should use the existing `256` candidate cap so the candidate budget does not exceed the descriptor's theoretical niche ceiling; seed `1337` remains gated behind a seed-`8471` pass.

### 12. Isolated mechanic-archetype island

Algorithm v18 implemented the bounded follow-up behind `--elite-mechanic-island`. It reuses the audited eight-axis descriptor without changing it, pre-fills only from the complete authoritative baseline of the same restart, evaluates 32 deterministic fresh legal candidates, and spends the remaining 224 evaluations on one-substitution descendants of restart-local niche champions. Audit candidates, audit anchors, and another restart's candidates never enter the archive or become parents. The known high E5 signature is consulted only after evaluation for collision telemetry; it does not affect generation, selection, scoring, replacement, refinement, or the final result.

The seed-`8471` run benchmarked `2,304` island candidates, retained `63,023` unique certification candidates, and completed in approximately `303` seconds. Islands occupied 133–153 of the descriptor's 256 possible niches and recorded 28–44 champion replacements. No island beat any fully refined baseline:

| Profile | Baseline/final restart scores | Best island scores | Occupied niches | Spread | Interpretation |
| --- | --- | --- | --- | ---: | --- |
| E4 | 77.98, 78.61, 78.32 | 76.55, 76.55, 77.19 | 153, 145, 149 | 0.63 | Ceiling preserved; no restart improved |
| E5 | 86.21, 84.91, 85.27 | 82.98, 82.09, 85.12 | 147, 148, 141 | 1.30 | Strong basin remained isolated; no restart improved |
| E6 | 87.46, 87.46, 87.12 | 86.15, 84.76, 86.19 | 136, 133, 142 | 0.34 | Baseline already passed; no restart improved |

The failure was not caused by missing the coarse high-archetype niche. That niche was already represented in every E5 restart's complete baseline, with best scores `86.21`, `74.34`, and `74.71`. The islands independently generated 3, 2, and 2 additional candidates in the same niche, whose best scores were only `79.52`, `76.35`, and `73.06`. The same eight-bit signature therefore contains both the known `86.21` basin and much weaker candidates. Its v17 neighborhood-level separability was real, but it is too lossy to act as a fitness-basin key for search.

The run remained `SearchUnstable`; seed `1337` was not run. The option remains disabled and the budget is not promoted. A deterministic replay produced identical baseline, island, and final search evidence. Increasing this island budget would mostly spend more evaluations inside descriptor collisions and is not justified by the result.

### 13. Audit-only coarse-niche collision study

Algorithm v19 extended `--elite-descriptor-audit` without adding search candidates or changing certification evidence. The prospectively frozen `mechanic-intensity-residual` uses four authored-mechanic intensity axes—outgoing result, health/recovery, status lifecycle, and condition dependency—each capped to `0`, `1`, or `2+`. The residual therefore has a hard maximum of `3^4 = 81` signatures. Essence identities, source families, benchmark scores, and anchor-genome membership are not descriptor features.

The collision audit selects every unique independently generated E5 candidate after restart and finalist refinement that occupies the known high mechanic-archetype niche. Audit-neighborhood candidates are excluded from this population. Authoritative scores define outcomes only: high means within the frozen `0.50` tolerance of the `86.21` anchor (`>= 85.71`), low means at or below the known `85.27` weak ceiling, and candidates in the gap are excluded. Exact-signature leave-one-out classification uses only other candidates sharing the residual signature; absent peers and label ties are ambiguous.

The first v19 execution applied the residual only to the old anchor-neighborhood population. All 150 candidates in its target coarse niche were high-neighborhood labeled and none were low-neighborhood labeled, so that artifact could not test the v18 collision and is superseded. The second execution used pre-refinement canonical search candidates: it found 35 low candidates but omitted the refined `86.21` winner, so it is also superseded. The corrected execution consumes the finalized `ProfileState.Candidates` collection while remaining read-only and audit-isolated.

Corrected seed-`8471` evidence:

| Metric | Result |
| --- | ---: |
| Finalized E5 candidates in parent coarse niche | 343 |
| High / low / gap-excluded candidates | 1 / 342 / 0 |
| Residual signatures | 3 of at most 81 |
| Exact-signature purity | 99.71% |
| Singleton-candidate rate | 0.29% |
| Leave-one-out high accuracy | 0.00% |
| Leave-one-out low accuracy | 99.71% |
| Leave-one-out balanced accuracy | 49.85% |
| Ambiguous candidates | 1 |
| High-anchor residual collides with low candidate | Yes |
| Retained exact high residual niche | 0 |

The high purity is a class-imbalance artifact: predicting the overwhelming low class produces nearly the same number. The sole high candidate shares its residual signature with low candidates. Health/recovery intensity is exactly `2.00` for both classes; status-lifecycle intensity is `1.00` high versus `1.04` low; outgoing-result and condition-dependency intensity are both zero. The descriptor passes its hard ceiling but fails anchor collision, high accuracy, balanced accuracy, separability, and map readiness.

Conclusion: authored mechanic presence and capped intensity do not explain the isolated `86.21` result inside the coarse niche. The residual is rejected, seed `1337` was not run, and no descriptor or search budget is promoted. Further descriptor-island work is not justified without evidence that quantitative effect payloads or higher-order Essence interactions can rank the high candidate prospectively.

## What the tests establish about E5

The evidence supports two simultaneous conclusions:

1. E5 has a genuine local fitness valley. The exhaustive shortest bridge requires a `1.30` regression, greater than the material-improvement and restart-tolerance scales.
2. The operational certification failure is global-search coverage. Some restarts discover `86.21`; others do not enter that basin, and crossover, bounded valley search, random large jumps, persistent undirected exploration, and simple stratified coverage have not made discovery reliable.

The bridge result rules out an overlooked monotone shortest path. The later experiments show that merely adding diversity does not solve basin discovery. The search needs diversity with structure and retention guarantees.

## Why E4 and E6 behave better

E4 and E6 do not show the same operational instability:

- multiple E4 restarts can reach `78.61`, and the corrected portfolio moved another restart to that ceiling;
- E6 commonly reaches `87.46` from two restarts, with the remaining restart close enough to stay within `0.50` in the baseline and corrected portfolio runs;
- their strong basins appear larger or more accessible to the current mutation/refinement operators;
- E4 can still produce misleading agreement if a prefilter or experimental stream removes the best ceiling, so ceiling retention remains mandatory;
- passing generic restart agreement does not certify E4 or E6 because curated player evidence is still missing.

## Approaches rejected by current evidence

The following should not be promoted without materially new evidence:

- increasing only the number of restart-refinement seeds;
- 35% elite-parent crossover;
- the width-16/depth-3 exhaustive valley budget;
- the 256-per-depth metadata valley prefilter;
- a 20% direct three/four-gene mutation rate;
- a 12-candidate persistent explorer archive at that rate;
- a 256-candidate arithmetic stratified portfolio;
- a 256-candidate strongest/weakest-scenario quality-diversity island;
- a 256-candidate eight-axis mechanic-archetype island;
- a four-axis capped mechanic-intensity residual inside the high coarse niche;
- relaxing the `0.50` spread tolerance;
- accepting low-spread results that lose a known ceiling;
- increasing a failed budget solely because one seed or one profile improved.

All experimental CLI options remain disabled in the approved developer and release defaults.

## Algorithm v20 benchmark-confidence and simulator-coverage audit

Algorithm v20 added an audit-only common-random-number benchmark study behind `--elite-benchmark-confidence-audit`. It forces the known E5 anchors and certification finalists into a deterministic score-stratified cohort, repeats all five production PvE scenarios on common seeds, and reports score uncertainty, rank correlations, top-k overlap, and paired anchor differences. Audit executions, rows, and warnings are serialized separately and cannot affect search candidates, percentiles, ceilings, challenges, verdicts, or `TotalUniqueCandidatesEvaluated`.

The seed-`8471` pilot used the unchanged `96`/`24`-`40`/`12` search, 512 of 21,761 finalized E5 candidates, 16 common seeds, and five scenarios: 40,960 audit-only combat executions. The single-seed baseline-to-mean Spearman correlation was `0.9127`, below the predeclared `0.95` boundary. Replicate-to-mean Spearman ranged from `0.9267` upward, but baseline top-20 overlap fell to `30%` and averaged only `40.63%`. The median approximate 95% score half-width was `0.65`, the maximum was `1.32`, and the most variable build would require 446 seeds to reach the requested `0.25` half-width under the observed variance. Both known high-versus-low comparisons reversed under the common-seed mean, so the known ordering gate also failed. Sixteen seeds are therefore insufficient for the requested score precision and the original one-seed ranking is not a reliable global ordering.

The same pilot replaced the random singleton sampler with the balanced 80-Essence round robin at 16 rounds per matchup: 3,160 unordered matchups and 50,560 battles, exactly 1,264 appearances per Essence. Coverage passed the simulator's 1,000-battle classification floor, but every Essence scored exactly `0.5000`. Algorithm v2 of the meta analyzer now detects this zero-range result, emits `SimulatorNoDiscrimination`, and reports `NoDiscrimination` instead of 80 misleading `Healthy` classifications. A large sample cannot compensate for an outcome that contains no Essence signal.

The planned pair-interaction study is deliberately blocked. Pair coefficients or tuning conclusions must not be derived from a singleton simulator with zero discrimination or from the sparse 240-build optimizer population. The next bounded engineering step is to establish a discriminatory neutral baseline—preferably fixed-hostile common-seed PvE singleton trials, or a side-bias-controlled duel score with a predeclared damage/survival endpoint—then rerun the singleton gate before sizing any factorial pair audit from its measured residual variance.

## Recommended next bounded investigation

Do not increase either island budget and do not run seed `1337`. First replace or repair the zero-discrimination singleton measurement and rerun its coverage/discrimination gate. Then size a balanced pair-interaction audit from measured neutral-baseline variance, using held-out restarts and keeping every model outside search and certification evidence. Another search experiment is justified only if an identity-independent payload model or restart-local interaction model ranks the strong candidate reliably without importing another restart's winner.

## Verification history

- Focused descriptor-audit test: `1/1` passed after the finalized-population correction.
- Complete `BalanceRunnerTests`: `60/60` passed, including audit determinism/isolation, collision-population accounting, hard-ceiling checks, island behavior, CLI parsing, disabled defaults, and invalid experiment combinations.
- Required full backend suite through `build/run-tests.ps1`: `1,662/1,662` passed against the final algorithm-v19 implementation; the Release build completed with zero warnings and zero errors.
- Bridge-audit isolation has an explicit deterministic test proving that enabling the audit changes only the audit option/section/count and does not change certification evidence or verdicts.
- Portfolio tests cover CLI parsing, disabled defaults, invalid experiment combinations, baseline build/genome serialization, direct portfolio accounting, and final-score dominance over the fully refined baseline.
- No database migration, production configuration, deployment, or production-content mutation is involved.

Algorithm v20 added focused coverage for common-seed confidence-audit isolation and accounting, balanced round-robin CLI parsing, even side assignment, and high-coverage zero-discrimination rejection. The repository-required Release verification through `build/run-tests.ps1` passed `1,666/1,666`; the final balance-runner Release build completed with zero warnings and zero errors. Two unrelated pre-existing xUnit analyzer warnings remain visible only when the complete test project is built.

## Artifact inventory

These paths are local diagnostic evidence and are not deployment inputs:

| Experiment | Latest certification artifact |
| --- | --- |
| Baseline | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-search-stabilization-96x24-seed8471\latest\elite-build-certification.json` |
| Eight refinement seeds | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-search-stabilization-96x24-seeds8-seed8471\latest\elite-build-certification.json` |
| Initial crossover | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-search-crossover-96x24-seed8471\latest\elite-build-certification.json` |
| Corrected crossover | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-search-crossover-v2-96x24-seed8471\latest\elite-build-certification.json` |
| Exhaustive valley | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-valley-16x3x5000-seed8471\latest\elite-build-certification.json` |
| Prefiltered valley | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-valley-prefilter256-budget768-seed8471\latest\elite-build-certification.json` |
| Bridge audit | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-bridge-audit-seed8471\latest\elite-build-certification.json` |
| Direct basin jump | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-basin-jump20-seed8471\latest\elite-build-certification.json` |
| Explorer archive | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-explorer12-jump20-seed8471\latest\elite-build-certification.json` |
| Superseded preliminary portfolio | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-stratified256-seed8471\latest\elite-build-certification.json` |
| Corrected isolated portfolio | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-stratified256-isolated-seed8471\latest\elite-build-certification.json` |
| Superseded fragmented quality island | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-quality-island256-seed8471\latest\elite-build-certification.json` |
| Corrected scenario-niche quality island | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-quality-island256-v2-seed8471\latest\elite-build-certification.json` |
| E5 descriptor-separability audit | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-descriptor-audit-seed8471\latest\elite-build-certification.json` |
| Coarse mechanic-map audit | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-coarse-mechanic-audit-seed8471\latest\elite-build-certification.json` |
| Superseded mechanic island without collision telemetry | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-mechanic-island256-seed8471\latest\elite-build-certification.json` |
| Mechanic-archetype island with collision telemetry | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-mechanic-island256-v2-seed8471\latest\elite-build-certification.json` |
| Superseded v19 anchor-neighborhood collision audit | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-residual-mechanic-audit-seed8471\latest\elite-build-certification.json` |
| Superseded v19 pre-refinement collision audit | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-residual-mechanic-audit-v2-seed8471\latest\elite-build-certification.json` |
| Corrected v19 finalized-population collision audit | `C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-residual-mechanic-audit-v3-seed8471\latest\elite-build-certification.json` |

The corrected v19 immutable collision-audit artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-residual-mechanic-audit-v3-seed8471\history\20260828T212956625Z-af30e8a0\elite-build-certification.json
```

The v18 immutable mechanic-island artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-mechanic-island256-v2-seed8471\history\20260828T204946703Z-15d02230\elite-build-certification.json
```

The v17 immutable coarse-mechanic artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-coarse-mechanic-audit-seed8471\history\20260828T201340949Z-0b7c01a2\elite-build-certification.json
```

The v16 immutable descriptor-audit artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-descriptor-audit-seed8471\history\20260828T191748257Z-6c3dfabe\elite-build-certification.json
```

The corrected v14 immutable artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-stratified256-isolated-seed8471\history\20260828T151913988Z-62ac6b8c\elite-build-certification.json
```

The corrected v15 immutable artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-quality-island256-v2-seed8471\history\20260828T170828680Z-4c53199f\elite-build-certification.json
```

The v11 immutable bridge-audit artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-bridge-audit-seed8471\history\20260828T131113960Z-0ae38979\elite-build-certification.json
```

## Related documents

- [Elite Build Certification Gate](elite-build-certification-gate.md)
- [Elite Build Certification Policy v1](elite-build-certification-policy-v1.md)
- [Automated Balance System Implementation Plan](../legendslegacy-automated-balance-system-implementation-plan.md)
- [Milestone 1 Balance Runner](../milestone-1-balance-runner.md)
