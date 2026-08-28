# Elite Build Search Investigation Log

**Status:** Active investigation; no search configuration is approved for certification.

**Evidence date:** 2026-08-28

**Current certification analyzer:** Algorithm v14

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
- relaxing the `0.50` spread tolerance;
- accepting low-spread results that lose a known ceiling;
- increasing a failed budget solely because one seed or one profile improved.

All experimental CLI options remain disabled in the approved developer and release defaults.

## Recommended next bounded investigation

If another experiment is authorized, the best-supported next step is an isolated quality-diversity island rather than another undirected diversity knob.

Suggested boundaries:

1. Run the complete baseline optimizer and baseline refinement first and serialize their final evidence.
2. Create a separate restart-local island with a fixed candidate/evaluation budget and independent deterministic RNG.
3. Define stable niches from gameplay-relevant descriptors, such as source-family composition, effect roles, scenario-score strengths, or mechanic coverage.
4. Retain the best authoritative candidate per niche even when it is below the aggregate elite, allowing weaker stepping stones to persist without displacing baseline elites.
5. Do not import another restart's winner, bridge nodes, or experimental descendants.
6. Benchmark every admitted candidate through the production boundary.
7. Merge only the final experimental evidence after the baseline result is complete; final restart evidence must dominate its baseline.
8. Report direct island candidates, descendant/local evaluations, niche occupancy, ceiling deltas, spread, runtime, and deterministic replay evidence separately.
9. Gate seed `1337` behind a complete seed-`8471` pass.
10. Require both seeds to retain known ceilings, satisfy spread `<= 0.50`, pass cross-strategy and local challenges, and meet the developer runtime criterion before considering any default or budget change.

This recommendation is intentionally a design direction, not an approved implementation or search budget.

## Verification completed with algorithm v14

- Focused `BalanceRunnerTests`: `53/53` passed.
- Required full backend suite through `build/run-tests.ps1`: `1,655/1,655` passed.
- Bridge-audit isolation has an explicit deterministic test proving that enabling the audit changes only the audit option/section/count and does not change certification evidence or verdicts.
- Portfolio tests cover CLI parsing, disabled defaults, invalid experiment combinations, baseline build/genome serialization, direct portfolio accounting, and final-score dominance over the fully refined baseline.
- No database migration, production configuration, deployment, or production-content mutation is involved.

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

The corrected v14 immutable artifact is:

```text
C:\Users\HrHoe\AppData\Local\Temp\legends-legacy-elite-stratified256-isolated-seed8471\history\20260828T151913988Z-62ac6b8c\elite-build-certification.json
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
