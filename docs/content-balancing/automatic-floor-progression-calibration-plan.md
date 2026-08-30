# Automatic Floor-to-Progression Calibration Plan

Status: **Slices 1–4 implemented for policy-enabled Floors 1–5 and 7; full Region 1 policy coverage and optional apply workflow remain**
Target service: `LL/tools/LegendsLegacy.Balance`  
Current foundation: **balance schema 54**
Initial scope: **World Tower Region 1 Floors 1–10**

## 1. Goal

Add an opt-in constrained calibration mode that automatically searches temporary floor variants until each encounter fits an explicitly authored player-progression policy.

The system should answer:

> Which smallest allowed content changes make this floor fit its intended player cohort, difficulty window, encounter identity, and position in the Region 1 progression curve?

The search may automatically evaluate and refine candidates. It must not silently rewrite production content. Its final output is a proposed content patch plus before/after and holdout evidence for human approval.

## 2. Why target power alone is insufficient

A single benchmark-power or Combat Rating target cannot describe the player population that should clear a floor. Builds with similar aggregate power can have materially different burst, sustained damage, multi-target capability, survivability, sustain, and mechanic coverage.

Every calibrated floor therefore needs an authored policy that identifies:

- the expected character level, Essence slots, and gear package;
- the generated cohort and percentile profile used for the primary target;
- weaker and stronger progression guardrail cohorts;
- target clear-rate, duration, death, and remaining-health windows;
- acceptable and unacceptable failure modes;
- intended party-family advantages or disadvantages;
- the parameters the calibrator may change; and
- hard parameter bounds that preserve encounter identity.

Generated P50/P75/P90 cohorts remain generated-population percentiles. They must never be labeled as live-player percentiles.

## 3. Implemented floor policy

The current author-editable policy is
`LL/tools/LegendsLegacy.Balance/Configuration/floor-progression-policy.v5.json`.
The CLI loads v5; v1–v4 remain packaged as historical policy revisions and are not selected by default. The validated canonical JSON and its SHA-256 fingerprint are included in report provenance.

Every enabled floor currently uses a 55–70% primary clear-rate target, 60–90 second median-duration target, at most one median friendly death, and at least 10% median remaining Health. The generated P50 clear-rate ceiling is 35%, the P90 floor is 70%, and the certified-P95 floor is 80%. These are authored policy thresholds, not live-player percentile claims.

| Floor | Frozen primary | Intended identity | Required family response | Approved knobs | Explicit review boundary |
| ---: | --- | --- | --- | --- | --- |
| 1 | E4 P75, level 30, four Essences, Rare gear | Party attrition | IntendedBalanced should succeed | Health, offense, exact `Slam the Gates` distributed damage | Distributed tuning uses the named reviewed exception and makes no relative-family claim |
| 2 | E4 P75, level 30, four Essences, Rare gear | Weak-target collapse, party attrition, Bleed-fed boss sustain | IntendedBalanced should succeed | Health, offense | Boss-sustain dominance returns `Review`; no Velka-specific recovery adapter exists |
| 3 | E4 P75, level 30, four Essences, Rare gear | Add pressure | MultiTargetSpecialist advantaged | Health, offense, authored Broodling health/power | Summon tuning requires `AddPressureMultiTargetResetV1`; count and cadence are immutable |
| 4 | E4 P75, level 30, four Essences, Rare gear | Primary-target collapse, party attrition | IntendedBalanced should succeed | Health, offense | Reflection strength/window and Physical/Magical identity are immutable |
| 5 | E4 P75, level 30, four Essences, Rare gear | Collapse, attrition, add pressure, unmet priority objective | MultiTargetSpecialist advantaged | Offense only | Health would retune inherited pillar Health and barrier magnitude; objective-dominated and duration-only cases return `Review` |
| 7 | E6 P75, level 50, six Essences, Epic gear | Boss-sustain dominance | SingleTargetSpecialist advantaged | Health, exact Guardian ability healing | Primary-target-collapse dominance is prohibited |

The evaluator requires the run-resolved primary profile, level, Essence slots, and gear package to match the authored row. Population drift therefore produces a policy mismatch and `Review`; it never silently remaps a floor to another cohort.

## 4. Calibration flow

```text
Authored progression and floor policy
  -> resolve frozen player cohorts
  -> simulate the unchanged authored floor
  -> diagnose the dominant target and identity violations
  -> select one permitted mechanic-specific tuning dimension
  -> search bounded temporary floor variants with common seeds
  -> retain the smallest valid change
  -> revalidate on independent holdout seeds
  -> verify neighboring-floor and Region-wide progression
  -> emit proposed patch and evidence
```

Each iteration must preserve the same build population, party rosters, and common combat seeds. Candidate selection and holdout evaluation must use independently derived seeds.

## 5. Optimization objective

The calibrator should use constraints before preference scoring.

### 5.1 Hard constraints

A candidate is invalid when any applicable rule fails:

- the primary cohort is outside its clear-rate or duration window;
- an undergeared cohort clears above its authored ceiling;
- a stronger cohort performs worse than a weaker cohort beyond the allowed tolerance;
- death or remaining-health limits fail;
- the dominant observed failure contradicts the encounter identity;
- an intended specialist response fails;
- a prohibited family becomes dominant;
- a required elite or party-family guardrail is unavailable;
- an allowed knob leaves its configured bounds;
- more than the permitted parameter groups change;
- authored `RequiredSlots`, party rules, or ability identity changes; or
- production content is mutated during evaluation.

### 5.2 Candidate preference

Among candidates satisfying every hard constraint, prefer in order:

1. the fewest changed parameter groups;
2. the smallest normalized distance from authored values;
3. the candidate nearest the center of the primary clear-rate and duration windows;
4. the strongest holdout stability;
5. the lowest additional runtime cost.

The system must return `Review` when no candidate satisfies all hard constraints. It must not select the least-bad invalid candidate or silently widen bounds.

## 6. Mechanic-specific knob registry

Every tunable parameter needs a typed adapter with applicability, units, bounds, clone/apply behavior, and identity rules.

| Parameter group | Initial status | Intended use |
| --- | --- | --- |
| Guardian health | Pilot | Duration and sustained-damage pressure |
| Guardian offense | Pilot | Primary collapse and general lethal pressure |
| Guardian ability healing/regeneration | Pilot | Boss-sustain pressure |
| Defense/resistance | Later | Damage-type-specific durability when authored |
| Add health/power | Second slice | Add-pressure tuning without weakening the boss |
| Add count/cadence | Later, discrete search | Encounter-specific wave pressure |
| Ability-specific distributed damage | Second slice | PartyAttrition tuning without changing basic attacks |
| Harmful-status potency/cadence | Blocked | Requires real cleanse-capable player content and valid specialist rosters |

The shared health/offense factor remains a diagnostic distance-from-target probe. It must not become the default production patch when a mechanic-specific knob explains the failure.

## 7. Search strategy

The first implementation should avoid a general-purpose high-dimensional optimizer.

1. Run the authored baseline.
2. Use physical failure evidence to identify applicable allowed knobs.
3. Evaluate bounded sensitivity points for one parameter group at a time.
4. Bracket a feasible region when the response is coherent.
5. Refine with a bounded binary or ordered-grid search.
6. Evaluate a second parameter group only when the policy explicitly permits it and no one-group candidate succeeds.
7. Stop at the smallest valid candidate.
8. Revalidate the selected candidate on holdout seeds and all progression guardrails.

Discrete parameters such as add count or cadence require enumerated candidate sets rather than continuous interpolation.

## 8. Region-wide coordination

After individual floor calibration works reliably, a Region coordinator should:

1. freeze all intended player cohorts and policies;
2. calibrate floors independently into proposed candidates;
3. evaluate the complete proposed Floor 1–10 sequence;
4. reject cross-floor clear-rate, duration, power, or recommended-CR inversions outside policy tolerances;
5. rerun affected neighboring floors when a shared progression assumption changes; and
6. emit one atomic Region proposal rather than unrelated floor patches.

The coordinator must retain the current smooth-step progression unless a separately approved progression policy replaces it. Automatic floor tuning must not silently introduce the rejected fixed E4/E5/E6 floor mapping.

## 9. Outputs

The automatic calibration artifact should include:

- policy and algorithm versions;
- complete upstream population-protocol provenance;
- baseline floor values and outcomes;
- every evaluated candidate and rejection reason;
- selected parameter changes and normalized change distance;
- primary, guardrail, family, elite, and holdout results;
- cross-floor progression checks;
- combat count, simulated ticks, wall time, allocations, and cache usage;
- `productionContentModified: false` for analysis runs;
- a machine-readable proposed patch;
- a human-readable before/after table; and
- an explicit `Proposed`, `Review`, or `NoChangeRequired` verdict.

Immutable history output must retain enough information to reproduce the approval decision.

## 10. Safety and approval boundary

- All search candidates use detached in-memory floor clones.
- The default command only writes reports and proposed patches under balance output.
- No database or player state is read or changed.
- No migration or deployment is part of calibration.
- Applying a proposed patch is a separate explicit developer action.
- The tool must verify that the patch changes only approved content fields.
- A post-application balance run must reproduce the proposal before the change can be considered approved.
- Release certification remains separate from finding a numerically feasible candidate.

## 11. Implementation slices

### Slice 1 — Policy and evaluation contract — implemented in schema 47

- Add typed floor policies and validation.
- Resolve primary, undergeared, strong, and elite cohorts without changing existing population selection.
- Evaluate an authored floor against all targets and guardrails.
- Emit violations without performing a search.
- Add deterministic policy and report tests.

### Slice 2 — Two-floor continuous-knob pilot — implemented in schema 48

- Select two representative Region 1 floors with different identities.
- Support separate Guardian health, offense, and ability-healing knobs.
- Run bounded one-parameter sensitivity and refinement.
- Select the smallest valid candidate.
- Perform independent holdout validation.
- Emit a proposed patch without applying it.

The pilot should include deliberately mis-tuned fixtures proving that the calibrator selects the injected parameter group rather than merely finding a clear-rate multiplier.

### Slice 3 — Add and distributed-pressure adapters — implemented in schema 49

- Add typed add-health/power and ability-specific distributed-damage adapters.
- Require the confirmed AddPressure response contract.
- Require an approved DistributedAttrition physical family contract or an explicit policy exception.
- Keep discrete add count/cadence out of scope until continuous adapters are proven.

### Slice 4 — Region 1 coordinator — implemented in schema 50

- Calibrate all policy-enabled Region 1 floors.
- Check neighboring-floor and Region-wide progression ordering.
- Produce one atomic Region proposal and report.
- Run independent Region holdouts plus elite and party-family gates.

### Slice 5 — Optional reviewed apply workflow

- Validate a selected proposal against the current content fingerprint.
- Generate or apply only the approved JSON field changes.
- Require an explicit invocation and preserve a recoverable before-state.
- Rerun the complete affected Region before reporting success.

This slice is optional. Automatic search and patch generation provide most of the value without granting the balance tool general production-write authority.

## 12. Verification plan

Tests must cover:

- invalid or incomplete floor policies;
- deterministic candidate ordering;
- unchanged production content during search;
- permitted-field and bound enforcement;
- common-seed candidate comparison and independent holdouts;
- recovery of deliberately injected health, offense, and regeneration faults;
- rejection when the correct knob is not allowed;
- rejection when clear rate improves but identity or family constraints fail;
- undergeared, strong, and elite progression guardrails;
- cross-floor inversion detection;
- exhausted searches returning `Review`;
- proposed-patch serialization and content-fingerprint validation; and
- immutable artifact persistence.

Repository verification continues to run through:

```powershell
.\build\run-tests.ps1
```

## 13. Performance policy

The calibrator should reuse existing build, capability, roster, and authored-baseline caches. It should use a small sensitivity panel before spending holdout or elite budgets.

Every run must report marginal cost per floor and per accepted candidate. Default developer mode should favor fast diagnosis. Release mode may increase roster and seed coverage, but only after a candidate passes the inexpensive hard constraints.

Do not multiply every simulation count to address population uncertainty. Release conclusions require distinct valid rosters and multiple protocol-compatible populations, not only additional combat seeds for the same parties.

## 14. Completion criteria

The Region 1 automatic floor calibrator is ready for production review when:

- every enabled floor has a validated authored policy;
- the two-floor pilot recovers known injected faults with the correct parameter group;
- no candidate mutates production content during search;
- selected candidates pass independent primary, progression, party-family, and elite holdouts;
- exhausted or contradictory searches return `Review`;
- the full Floor 1–10 proposal preserves the approved progression ordering;
- proposed patches contain only explicitly allowed fields;
- the complete backend suite passes; and
- a human approves and manually applies the first Region proposal.

## 15. Non-goals

- Automatically inventing encounter identity or player-progression intent.
- Treating generated percentiles as live-player distributions.
- Replacing physical mechanic evidence with one aggregate score.
- Adding new universal capability dimensions.
- Optimizing Region 2+ before the Region 1 expansion gate is resolved.
- Automatically deploying or applying database changes.
- Guaranteeing that a numerically fitted floor is release-certified.

## 16. Recommended next action

Run and review the opt-in atomic Region coordinator across protocol-compatible developer populations, then author Floors 6 and 8–10 one at a time with reviewed cohort, identity, specialist, and knob contracts. Do not enable a floor merely to obtain ten-floor coverage. Consider Slice 5 only after the complete policy-enabled Region proposal is stable and manually reviewed.

### Implementation history

Schema 48 retains the schema-47 policy contract and adds an opt-in detached one-parameter search for the Floor 1 and Floor 7 pilots. It supports Guardian health, offense, and ability-healing adapters; uses common candidate seeds; checks candidate-specific P75/P50/P90, certified-P95, party-family, progression, and identity constraints; refines toward the authored value; rechecks on an independently derived holdout seed; and emits a fingerprinted, unapplied proposed patch. It never modifies production content.

Schema 49 adds continuous authored-effect adapters for Morrowmaw's summon health/power payload and Garran's exact `Slam the Gates` damage coefficient. Both use detached temporary ability-spec clones and preserve ability ID, effect ID, targets, summon count, and cadence. Add-pressure tuning is policy-valid only with the confirmed `AddPressureMultiTargetResetV1` specialist contract and candidate MultiTarget reset evidence. Distributed tuning requires direct attributed damage reaching at least two targets plus either an approved family contract or a named, reasoned policy exception. The Floor 1 pilot records the latter exception explicitly and makes no unsupported relative-family claim. Discrete add count and cadence remain outside automatic calibration.

Schema 50 adds automatic calibrator algorithm version 3 and the Region 1 coordinator. After per-floor search and holdout, every policy-enabled final factor is re-evaluated with one independently derived Region seed through primary, P50/P90, certified-P95, family, identity, and mechanic gates. The coordinator checks all authored Region 1 floors for nondecreasing recommended CR and target benchmark power, checks policy-enabled primary level/slot progression, and rejects later-floor clear-rate or duration inversions beyond author-owned tolerances. It emits one fingerprinted atomic Region patch only when every floor, holdout, and ordering constraint passes. Individual floor patches remain evidence; production content is unchanged.

Schema 51 introduces floor-progression policy v2 and enables Floor 3 alongside the existing Floor 1 and Floor 7 pilots. Floor 3 is bound to the run's frozen E4 P75/P50/P90 progression cohorts, declares AddPressure as its intended failure identity, prohibits PrimaryTargetCollapse as dominant, and requires the authored advantaged MultiTargetSpecialist response. Its summon-health/power knob is valid only under the confirmed `AddPressureMultiTargetResetV1` contract; the existing health and offense bounds remain available for physically diagnosed duration or collapse pressure.

Schema 52 introduces floor-progression policy v3 and enables Floor 2. Velka uses the frozen E4 cohorts and the default IntendedBalanced response. Her authored weak-target dive, all-party Bleed pressure, and Bleed-fed recovery make PrimaryTargetCollapse, PartyAttrition, and BossSustainDominance valid encounter outcomes; AddPressure is prohibited because the encounter has no summons. Only health and offense are approved. A BossSustain-dominated run therefore returns `Review` until a separately typed and validated Velka recovery adapter exists.

Schema 53 introduces floor-progression policy v4 and enables Floor 4. Vaelor uses the frozen E4 cohorts and default IntendedBalanced response. His highest/lowest-health lance, all-party shards, and reflected direct damage admit PrimaryTargetCollapse and PartyAttrition while prohibiting BossSustainDominance and AddPressure. Health and offense are the only approved controls; reflection strength/window and Physical/Magical damage identity are explicit immutable boundaries. A reflection-specific imbalance must therefore exhaust or return `Review`, not reinterpret offense as a reflection adapter.

Schema 54 introduces floor-progression policy v5 and enables Floor 5. Kharad uses the frozen E4 cohorts and the authored advantaged MultiTargetSpecialist response. Collapse, attrition, add pressure, and unmet priority objectives are valid identity outcomes; boss sustain is prohibited. Only offense is tunable. Guardian health is explicitly forbidden because both pillars inherit 10% of Kharad's MaxHealth and his Seal barrier also scales from MaxHealth, so a generic health change would silently retune the objective. Pillar identity, count, cadence, Resonance, and barrier behavior are immutable. Add/objective-dominated or duration-only violations therefore return `Review`. Floors 6 and 8–10 remain outside automatic search pending the same review standard.
