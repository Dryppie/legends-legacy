# Elite Build Certification Policy v1

**Status:** Implemented as the schema-15 runtime policy; release thresholds remain subject to explicit game-design approval  
**Policy ID:** `WorldTowerEliteCertificationV1`  
**Scope:** World Tower Region 1 and the pre-Milestone-14 elite certification gate

This policy turns the [Elite Build Certification Gate](elite-build-certification-gate.md) into explicit, versioned decisions that can be implemented by the one-click balance runner. It defines percentile semantics, convergence tolerances, encounter expectations, search budgets, curated-player evidence, and the evidence required for a certification verdict.

This policy does not change production content. Generic P75 builds remain the normal progression and calibration target. Elite populations are a separate stress boundary used to detect theoretical ceilings, encounter trivialization, hard counters, and mechanic bypasses.

## 1. Population semantics

The balance report must keep these populations independent:

| Population | Meaning | Use |
| --- | --- | --- |
| Generic P75 | Competently constructed build from the ordinary representative library | Normal progression, recommended Combat Rating, and encounter calibration |
| Certified P95 | Highly optimized generic build from the certification search | High-end difficulty and clear-speed pressure |
| Certified P99 | Extreme generic build from the certification search frontier | Theoretical-ceiling and exploit stress testing |
| Encounter-specialized elite | Party optimized directly against one real encounter | Hard-counter, mechanic-bypass, burst, sustain, and cheese detection |
| Curated player builds | Reviewed builds or parties supplied by developers or trusted testers | External comparison against automated search |

P95 and P99 are percentiles of the deduplicated certification-search population for a specific content fingerprint and certification policy. They are not percentiles of live players and must never be presented as such.

The percentile population combines all unique legal candidates evaluated by every certification restart and search strategy. Duplicate genomes count once. The report must retain the population size, strategy contribution, score distribution, and content fingerprint used to derive each percentile.

Encounter holdouts use deterministic cohorts centered on the P95 and P99 target scores, rather than taking the strongest builds above each threshold. This keeps the percentile populations distinct and prevents both holdouts from collapsing into the same upper-tail party. The selected cohort build IDs must be serialized in the certification artifact.

## 2. Certification tolerances

The following values are the proposed v1 engineering tolerances:

| Check | Requirement |
| --- | --- |
| Independent restart agreement | Best relevant-score spread is no more than `0.50` points |
| Cross-strategy agreement | Best strategies finish within `1.00` point and share at least one equivalent elite archetype |
| Release search plateau | No best-score improvement greater than `0.25` during the final 10 configured generations |
| Generic local challenge | No legal neighbor improves aggregate or relevant scenario fitness by more than `1.00` point |
| Encounter local challenge | No legal neighbor gains more than 3 clear-rate points or reduces median successful kill time by more than 5% without reducing clear rate |
| Holdout precision | The two-sided 95% Wilson clear-rate interval is no wider than 5 percentage points |
| Human-build comparison | No curated build exceeds the automated ceiling by more than 2 benchmark points, 3 clear-rate points, or a 5% kill-time advantage |
| Diversity | Multiple materially distinct elite archetypes are retained unless a mandatory-Essence warning explicitly explains convergence |
| Legality | Every build and party passes production source-family, slot, content-resolution, equipment, level, and progression rules |

A search-budget limit, runtime interruption, or incomplete neighborhood traversal must never be treated as passing evidence. The result must use a blocking incomplete-evidence warning or a non-certified verdict.

Aggregate local improvement is evaluated against the aggregate finalist. Scenario improvement is evaluated against the finalist leading that scenario and is blocking only when every other required benchmark component remains within the same `1.00`-point tolerance. This preserves intended Pareto tradeoffs while still rejecting a locally dominated specialist.

Party genomes are unordered multisets because multiple party members may use the same build. Party optimization is complete when it reaches the approved deterministic budget or exhausts the legal unique search space sooner. Reports must retain both the evaluated count and the computed legal party-search-space size; failing to reach the smaller of those two counts is incomplete evidence.

### Relevant fitness

Relevant fitness depends on the candidate being certified:

- generic finalists use aggregate PvE score and every required benchmark component;
- scenario specialists use their named benchmark component and survival consistency;
- encounter specialists use clear rate first, then survival, deaths, and successful kill time;
- party finalists use party-level clear rate, survival consistency, mechanic execution, and successful kill time.

An aggregate-score tie cannot hide a material improvement or regression on a required Pareto axis.

## 3. World Tower elite expectations

Elite clear rate confirms that the search found genuinely strong builds. Clear rate alone does not establish trivialization because elite players are expected to clear progression content reliably. Kill time and mechanic behavior provide the separate trivialization boundary.

| Population | Expected clear-rate evidence | Initial trivialization threshold |
| --- | --- | --- |
| Generic P75 | Existing 55%-75% Region 1 target | Existing Region 1 validation policy |
| Certified P95 | 95% interval lower bound is at least 80% | Median successful kill time is below 70% of the successful P75 median |
| Certified P99 | 95% interval lower bound is at least 90% | Median successful kill time is below 55% of the successful P75 median |
| Encounter-specialized party | Must materially outperform its generic comparison; no independent minimum | Median successful kill time is below 45% of the successful P75 median |
| Any elite population | Not applicable | A clear below 35% of the P75 median or bypass of a mandatory mechanic is a blocking mechanic-bypass warning |

The 70%, 55%, and 45% kill-time ratios begin as warnings during the first certification cycle. After reviewing the first complete distributions, game design should explicitly decide which ratios become blocking rules. Mandatory-mechanic bypass is blocking from the first version.

## 4. Execution profiles and search budgets

Evaluation-count budgets are deterministic and reproducible. Wall-clock targets are used only to choose or revise those counts.

| Setting | Developer profile | Release profile |
| --- | ---: | ---: |
| May emit `CertifiedElite` | No | Yes |
| Independent restarts | 3 | 8 |
| Population per restart and slot profile | 64 | 256 |
| Minimum generations | 12 | 60 |
| Maximum adaptive generations | 24 | 100 |
| Elites per generation | 8 | 32 |
| Required final plateau | 4 generations | 10 generations |
| Approximate genetic candidates per slot profile | 2,500-4,800 | 123,000-205,000 |
| Restart-winner refinement | Up to 6 complete one-swap passes per restart | Up to 12 complete one-swap passes per restart |
| Pareto-finalist refinement | Up to 3 reselection rounds | Up to 5 reselection rounds |
| Finalists per slot profile | 3-6 | 6-12 Pareto-diverse finalists |
| One-Essence challenge | Complete for every finalist | Complete for every finalist |
| Two-Essence challenge | Deterministic synergy-guided bounded search | Complete for every declared finalist |
| Holdout simulations per encounter and cohort | 4 seeds x 25 = 100 | 8 seeds x 200 = 1,600 |
| Initial party-genome budget per floor | 2,000 | 25,000 |
| Target runtime on the reference developer machine | No more than 15 minutes | No more than 8 hours |

The release sample of 1,600 trials is intended to satisfy the five-percentage-point confidence-width requirement even near the worst-case clear-rate distribution. Sample sizes remain configuration-owned and must be serialized into every report.

Each genetic restart runs for at least the minimum generation count. It then continues until the configured plateau requirement is observed or the serialized maximum generation count is reached. Reaching the maximum without the required quiet window is blocking `SearchUnstable` evidence.

Before restart agreement is measured, each raw restart winner receives bounded deterministic one-swap hill-climbing using the `0.25`-point material-improvement threshold. Convergence compares these independently refined optima. Pareto finalists then absorb material one- and two-swap discoveries and are reselected for a bounded number of rounds before the final `1.00`-point certification challenge. The report retains raw and refined restart scores, actual generations, refinement passes, candidate counts, and finalist-refinement rounds.

### Runtime measurement before final budget approval

Before marking release budgets approved:

1. Benchmark 1,000 generic candidate evaluations.
2. Benchmark one complete one-Essence neighborhood.
3. Benchmark one complete E6 two-Essence neighborhood.
4. Benchmark 1,000 party-genome evaluations against a production Guardian.
5. Project the complete developer and release runtimes.
6. Adjust deterministic counts or improve execution performance while preserving required evidence.

If the release search cannot complete within the intended runtime, the runner must preserve partial evidence and emit a non-certified verdict. It must not silently skip required checks.

## 5. Search and neighborhood requirements

Release certification requires at least two complementary deterministic strategies:

1. independent restarts of the diversity-aware genetic or beam search;
2. deterministic local or beam refinement seeded from top builds, measured Essence usage, pair synergy, and Pareto-frontier candidates.

Every declared finalist must receive:

- every legal one-Essence substitution;
- the configured two-Essence substitution search;
- high-synergy pair insertions;
- replacement challenges for repeatedly mandatory Essences;
- scenario-specific reranking;
- a production legality and content-resolution check.

Release mode requires a complete two-Essence challenge for each declared finalist. The finalist cohort should therefore remain small, Pareto-diverse, and explicitly recorded. If the implementation later permits a bounded release challenge, that is a policy-version change and cannot be introduced silently.

## 6. Pareto retention and party optimization

The certification library must preserve a Pareto frontier across:

- short single-target performance;
- sustained single-target performance;
- high incoming-damage survival;
- multi-target performance;
- attrition and sustain;
- real-encounter clear rate;
- survival consistency;
- successful kill time.

The system must optimize complete parties against real encounters. Party genomes must represent complementary kits, role coverage, duplicated versus diversified builds, buffs, debuffs, summons, statuses, burst timing, and sustain loops.

Strong individual candidates without completed party optimization cannot produce `CertifiedElite`; they produce `PartyOptimizationRequired` or another applicable non-certified verdict.

## 7. Curated top-player fixtures

The default repository-owned fixture should live outside production gameplay content at:

```text
LL/tools/LegendsLegacy.Balance/Fixtures/top-player-builds.json
```

The CLI should additionally support:

```text
--top-player-builds <path>
```

This allows private or pre-release fixture collections to participate without committing personal or environment-specific data.

Each fixture entry must contain:

```text
Stable build or party ID
Source category
Review date
Content-version fingerprint
Gear package or exact equipment
Character level and progression state
Essence IDs and evolution state
Intended role or encounter
Optional observed result
Reviewer note
```

Do not store player names, account IDs, or other personal identifiers. A fingerprint mismatch, missing production definition, illegal progression state, or invalid loadout must fail explicitly rather than evaluating stale or impossible content.

### Minimum player evidence

The proposed minimum evidence for `CertifiedElite` is:

- three independently authored and reviewed curated builds for each of E4, E5, and E6;
- at least one curated party for every encounter being certified;
- successful production materialization for every fixture;
- comparison of every fixture against the automated ceiling on common holdout seeds.

Do not invent player fixtures to satisfy the count. When automated evidence passes but credible player evidence is absent, the report may expose an `AutomatedEvidenceStable` sub-result, but the overall verdict remains `InsufficientPlayerEvidence`. Continuing toward Region 1 remediation in that state requires an explicit developer exception; it must not weaken the meaning of `CertifiedElite`.

## 8. Configuration and report ownership

The runtime configuration should be stored as a versioned machine-readable file, for example:

```text
LL/tools/LegendsLegacy.Balance/Configuration/elite-certification-policy.v1.json
```

It must own:

- policy ID and version;
- content type;
- developer and release budgets;
- convergence and plateau tolerances;
- local-challenge tolerances;
- P95 and P99 expectations;
- kill-time and mechanic-bypass rules;
- holdout precision and seed requirements;
- curated-fixture minimums;
- every prerequisite for each verdict.

The generated `elite-build-certification.json`, `summary.json`, and `summary.md` must include the resolved policy, policy fingerprint, content fingerprint, execution profile, and actual evaluation counts. Historical reports must remain interpretable after later policy revisions.

This policy is implemented in balance report schema version 15.

## 9. Verdict prerequisites

`CertifiedElite` is permitted only in the release profile and only when all of these pass:

- every required slot profile and encounter population was evaluated;
- independent restart and cross-strategy agreement passed;
- the required plateau completed;
- all declared finalists passed their one- and two-Essence challenges;
- Pareto scenario coverage passed;
- complete parties were optimized and validated;
- holdout confidence requirements passed;
- every curated fixture was valid and compared;
- minimum curated-player evidence was present;
- no human build materially exceeded the automated ceiling;
- no blocking legality, stale-content, mechanic-bypass, or incomplete-evidence warning remained.

Partial results remain valuable, but they must use a non-certified verdict such as:

```text
SearchUnstable
LocalImprovementFound
ScenarioCoverageFailure
PartyOptimizationRequired
HumanBuildOutperformed
InsufficientPlayerEvidence
```

## 10. Approval and implementation sequence

Proceed in this order:

1. Review and approve the percentile semantics and World Tower expectations in this policy.
2. Approve the proposed convergence and local-improvement tolerances.
3. Add genuine curated fixtures or explicitly accept that the first result will be `InsufficientPlayerEvidence`.
4. Implement timing-only benchmarks and measure the reference machine.
5. Finalize the deterministic developer and release budgets from those measurements.
6. Add policy configuration, CLI parsing, schema snapshots, and non-certifying report output.
7. Implement independent restarts and the second search strategy.
8. Add Pareto retention and deterministic neighborhood challenges.
9. Add direct party optimization and release holdout validation.
10. Enable `CertifiedElite` only after every required evidence source is implemented and tested.
11. Freeze the certified cohorts against the content fingerprint.
12. Recalibrate unstable Region 1 Floors 2-5.
13. Review mechanic and build-profile interactions on Floors 1, 6, 7, 9, and 10.
14. Stress every recommendation against certified P95/P99, specialized, and curated parties.
15. Rerun the existing [Region 1 Scaling Validation Gate](../region-1-scaling-validation.md) unchanged.
16. Begin Milestone 14 only after both gates pass or explicit exceptions are documented.

## 11. Approval checklist

Change this document's status to `Approved` only after each item has an explicit decision:

- [ ] P95/P99 search-population semantics accepted.
- [ ] P75 remains the normal progression target.
- [ ] Restart, cross-strategy, plateau, and local-challenge tolerances accepted.
- [ ] P95/P99 clear-rate expectations accepted.
- [ ] Kill-time warning ratios and mechanic-bypass rule accepted.
- [ ] Developer profile budget accepted.
- [ ] Release profile budget accepted after runtime measurement.
- [ ] Curated-fixture schema and storage location accepted.
- [ ] Minimum curated-player evidence accepted or revised.
- [ ] Missing player evidence confirmed to block `CertifiedElite`.
- [ ] Release-only certification behavior accepted.
- [ ] No certification stage may mutate production content automatically.
