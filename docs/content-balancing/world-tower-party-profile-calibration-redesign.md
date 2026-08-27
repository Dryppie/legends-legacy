# World Tower party-profile calibration redesign

## Purpose

This document defines a more accurate and substantially faster Character Profile and recommended-power calibration model for World Tower.

The central finding is that World Tower roster capacity and party membership are different concepts:

- A five-slot expedition contains one party of five.
- A ten-slot expedition contains two parties of five.
- A fifteen-slot expedition contains three parties of five.

All parties fight the same guardian simultaneously, but allied effects remain restricted by party membership. The appropriate reusable unit for Essence discovery is therefore a five-character party, while the appropriate unit for final outcome certification remains the complete five-, ten-, or fifteen-character expedition.

This redesign supplements:

- [`combat-system-analysis.md`](combat-system-analysis.md)
- [`character-profile-generator-design.md`](character-profile-generator-design.md)
- [`combat-calibration-current-state-and-gaps.md`](combat-calibration-current-state-and-gaps.md)

## Implementation status — 2026-08-26

The first implementation slice is complete:

- World Tower campaign planning now creates one five-character discovery audit for each required Essence-slot count. The currently authored floor curve resolves to five audits covering five through nine Essences, instead of thirteen roster-and-equipment audits.
- Discovery equipment is an explicit campaign setting and is independent from the target floor equipment.
- Profile generation accepts a target roster size, tier, and rarity. It rebuilds all equipment through the canonical production materializer for the exact target scenario.
- Generated five-, ten-, and fifteen-character teams now contain one, two, and three explicit party records. Every character retains `PartyNumber`, party-local slot index, and reusable source-party identity through catalog validation and Tower runtime creation.
- World Tower generation rejects source audits that do not contain exactly five characters, and legacy campaign schema cannot be retried accidentally.
- Campaign defaults are 500 candidates, 10,000 screening battles per seed, 24 finalists, 34 finalist battles per pairing and seed, and 100 replacement-validation battles. With three seeds, every finalist matchup therefore contains 102 direct battles and satisfies the separate 100-matchup evidence gate.
- The previous thirteen-audit campaign was cancelled after four completed audits. Its artifacts remain preserved as legacy diagnostic evidence, and its automatic monitor is paused.

The mixed-expedition implementation is also complete. Profile schema and generator version 7 record audited evidence independently on every constituent party and mark multi-party teams as composed expeditions, preventing an assembled expedition from falsely claiming it was directly observed during discovery. Direct matchup evidence has a separate minimum sample requirement and a recorded Wilson 95% confidence interval. Every Expanded World Tower scenario generates exactly ten bounded profiles: Meta, Typical, Meta/Typical mix, role-specialist mix, Weak-but-Legal, Budget, Counter, Countered, Equal-Power Adversarial, and No-Essence diagnostic. Population weighting recognizes the two mixed families, and catalog validation rebuilds every character while independently validating each party's source, confidence, seed stability, diversity, adversary evidence, roles, ordered membership, and production Tower qualification.

The accuracy follow-up is now implemented as well:

- All five discovery contexts use the canonical Guardian, Restorer, Striker, Striker, and Controller roles with role-specific discovery-equipment attributes instead of five identical balanced participants.
- Each eligible finalist is qualified with ten deterministic production battles on every exact floor served by the scenario before profile families are selected.
- Qualification materializes the exact target roster and equipment, preserves one/two/three party assignments, and uses the production Tower guardian, scaling, stagger, cooldown, runtime, and playback paths.
- Selected parties persist exact floor outcomes and seed-manifest provenance; the catalog rejects incomplete, mathematically inconsistent, non-production, or malformed qualification evidence.
- Fingerprint contract 3 invalidates reuse when discovery roles/builds, target equipment, Tower definitions, guardians, creature abilities, guardian Essence loot, region scaling, or the qualification contract changes.

The first five-audit campaign completed all 281,400 discovery battles but correctly failed catalog generation because its old configuration produced only 30 direct battles per matchup while requiring 100. Diagnostic replay then exposed that the eight-slot finalists contained only one Budget candidate, already consumed by the old greedy family order, and the nine-slot finalists contained none. The repaired planner now rejects impossible evidence settings before launch, finalist selection reserves Common-only candidates, and constrained profile families are selected before generic score bands.

Replacement schema-3 campaign `e283b6e9-0463-497e-8ff3-3536ba1fd1b7` completed all five audits and 380,760 battles. Its first catalog pass found a numerical boundary defect at perfect 102–0 matchups: Wilson interval rounding produced an upper endpoint just below 1.0 and a reversed lower endpoint just above 0.0. Interval construction now guarantees that the observed score lies inside its interval. Retrying generated a structurally valid version-6 catalog with 13 profile sets and 130 teams. This remains historical evidence.

The first schema-4 campaign, `6339d840-f00a-4630-a869-d5ad862a3bd1`, subsequently completed candidate smoke and 100-sample certification. Promotion was correctly blocked by 29 confidence and profile-spread findings. Investigation showed that the then-current five-person discovery still used identical balanced participant attributes and that finalist selection had no production-floor evidence. Those discrepancies are corrected by version 7 and fingerprint contract 3. No current-contract campaign has run yet, and the source-controlled approved catalog remains empty.

## Confirmed production behavior

[`WorldTowerPartyRules.cs`](../../LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs) defines a maximum party size of five and derives party membership from the assigned rally slot.

When a Tower battle is prepared, [`WorldTowerService.cs`](../../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs) assigns every participant a `PartyNumber`. [`WorldTowerCombatRuntimeFactory.cs`](../../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerCombatRuntimeFactory.cs) then prepares all participants and the guardian in one `CombatEncounterRuntime`.

The resulting mechanics are:

| Concern | Production behavior |
| --- | --- |
| Rally organization | Slots are divided into parties of at most five |
| Engine execution | All parties participate in one encounter at the same time |
| Friendly combat side | All expedition participants belong to the same friendly combat team |
| Ally targeting | Heals, barriers, buffs, and other allied effects are limited to the source's `PartyNumber` |
| Guardian targeting | The guardian can target characters from any party |
| Threat | The encounter shares the guardian and its targeting/threat state |
| Guardian conditions | Debuffs and other effects from every party affect the same guardian state |
| Stagger | Every participant contributes to the same guardian stagger mechanic |
| Scaling | Guardian scaling uses the expedition's required slot count |
| Outcome | The complete expedition receives one victory or defeat result |

The correct mental model is therefore **multiple five-character parties cooperating in one encounter**, not one unrestricted fifteen-character party and not three independent five-versus-guardian battles.

## Historical problem in the full-roster Essence audit model

The retired thirteen-context campaign created separate Essence audits for full roster sizes of five, ten, and fifteen. At that time, [`AbilityBalanceSimulator.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityBalanceSimulator.cs) created every simulated participant without a production Tower party number.

When `PartyNumber` is absent, the combat engine treats every same-side participant as an eligible ally. In a ten- or fifteen-character audit, allied targeting can therefore reach the entire simulated team.

This differs from production Tower behavior, where [`FastCombatEngine.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) restricts ability allies to the source's party when `PartyNumber` is present.

Consequences of the current full-roster audits include:

- `AllAllies` effects can cover ten or fifteen characters instead of five.
- Healing, barriers, cleansing, and ally selection can cross boundaries that production forbids.
- Support Essences can appear stronger than they are in a real multi-party expedition.
- Full-roster audit rankings can reward compositions that cannot reproduce the same synergy in production.
- The expensive 10v10 and 15v15 mirror simulations do not represent the actual Tower encounter structure.

This was a calibration discrepancy, not a production-combat defect. Production World Tower correctly recorded and enforced party membership. Current campaign discovery now remains at exactly five characters, so it never models a ten- or fifteen-person unrestricted ally group.

## Why the retired thirteen-context campaign was expensive

Each retired full-roster audit was configured with:

- 1,000 generated candidate teams.
- 25,000 screening battles for each of three seeds.
- 100 finalists.
- Ten battles for every finalist pairing for each of three seeds.
- Up to 36,000 Essence replacement-validation battles.

The approximate cost per completed audit is:

| Stage | Battles |
| --- | ---: |
| Screening | 75,000 |
| 100-team finalist round robin | 148,500 |
| Replacement validation | Up to 36,000 |
| Total | 259,500 |

The finalist stage is quadratic. One hundred finalists produce 4,950 distinct pairings before seeds and repeated battles are applied.

The retired thirteen-context campaign therefore approached 3.37 million discovery battles. The current flow performs five discovery audits, then approximately 3,600 bounded production qualification battles with the default 24 finalists, 15 floors, and ten qualification samples.

## Evidence for sharing Essence discovery

Two completed campaign audits use the same ten-character/seven-Essence structure at different equipment rungs:

- Tier 1 Rare.
- Tier 2 Epic.

Because the structural inputs and seeds are the same, their candidate pools can be compared directly. Their finalist overlap was:

| Comparison | Shared teams |
| --- | ---: |
| Top 10 | 8 of 10 |
| Top 20 | 17 of 20 |
| Top 50 | 42 of 50 |

Equipment scaling changes some ordering, so it should not be ignored. However, this degree of overlap supports discovering a reusable Essence portfolio once and applying cheaper equipment-sensitivity and production-content validation afterward.

## Implemented calibration model

### Stage 1: Discover five-character party profiles

Run Essence discovery only for the five-character party structures used by World Tower:

| Discovery context | Purpose |
| --- | --- |
| Five characters × five Essences | Early Tower loadouts |
| Five characters × six Essences | Early/middle progression |
| Five characters × seven Essences | Middle progression |
| Five characters × eight Essences | Later progression |
| Five characters × nine Essences | Highest currently authored progression |

Team size should no longer vary in the World Tower discovery layer. Essence-slot count must remain a distinct dimension because legal combinations and opportunity cost change when slots are added.

Discovery uses a declared reference equipment context. The source audit retains that context as provenance, while target tier, rarity, and quality are supplied independently during materialization.

### Stage 2: Qualify and select a diversified party library

For each Essence-slot count, the current flow qualifies every evidence-eligible finalist against all exact target floors with ten common deterministic samples, then selects ten bounded five-character party/expedition families. The library represents more than maximum observed PvP win rate.

Recommended party families include:

- Meta.
- Typical.
- A second Typical variant.
- Offensive or stagger specialist.
- Defensive or sustain specialist.
- Budget or Weak-but-Legal.
- Counter.
- Countered.
- Equal-power adversarial.
- No-Essence diagnostic control, excluded from population weighting.

The exact count should be configuration-driven. A small, diverse portfolio is more useful for recommended-power calibration than a large collection of near-identical top performers.

Each party should use the canonical five-role cell:

1. Guardian.
2. Restorer.
3. Striker.
4. Striker.
5. Controller.

The roles describe deterministic calibration behavior and equipment profiles; they do not impose player classes on live characters.

### Stage 3: Materialize exact floor equipment

For every target floor, materialize each selected party using the floor's exact progression requirement:

- Equipment tier.
- Rarity.
- Quality.
- Role-specific equipment profile.
- Canonical equipment curve.
- Recipes, bases, weapons, blueprints, sets, and deterministic rolls.
- Exact Essence definitions and progression state.
- Content-specific preparation and Power Rating.

This produces complete, immutable snapshots through the existing profile materializer and `CombatPreparationPipeline`.

Discovery equipment and target equipment are separate contracts. One five-person discovery audit can therefore serve multiple floor equipment rungs without pretending that its reference gear is the final combat gear.

### Stage 4: Assemble complete expedition profiles

Floor capacity determines how many party profiles are combined:

| Required slots | Expedition composition |
| ---: | --- |
| 5 | One five-character party |
| 10 | Two five-character parties |
| 15 | Three five-character parties |

The system should create a bounded, deterministic set of complete expeditions rather than every mathematical party combination.

Recommended expedition families include:

- All Meta.
- All Typical.
- Mixed Meta and Typical.
- Realistic mixed population.
- Role-specialist mixture.
- Budget or Weak-but-Legal mixture.
- Favorable counter composition.
- Unfavorable counter composition.
- Equal-power high-variance composition.

For ten- and fifteen-slot floors, individual parties may use different party profiles. This captures realistic expedition diversity without requiring a new full-roster Essence search.

### Stage 5: Certify the complete production battle

Every assembled expedition must still run as a complete five-, ten-, or fifteen-character roster through the production World Tower path.

This validation is required because parties are not independent. Complete expedition execution captures:

- Shared guardian health.
- Shared debuffs and condition caps.
- Redundant or complementary party effects.
- Guardian threat and target selection across parties.
- Expedition-wide stagger contribution and breaks.
- Guardian scaling by required slots.
- Timeouts and encounter duration.
- The final content outcome.

Pull-request certification should run at least 100 common deterministic seeds per expedition. Release certification should use 500–1,000 samples.

Certification should continue to require:

- Exact floor and roster coverage.
- Exact target equipment scenario.
- Wilson 95% confidence intervals.
- Below/recommended/stronger monotonicity.
- Bounded equal-rating expedition spread.
- Bounded timeout rate.
- Complete content, rules, rating, preparation, generator, and seed provenance.

### Stage 6: Probe equipment sensitivity cheaply

Equipment can still change Essence performance through flat-versus-percentage scaling, survivability thresholds, healing breakpoints, speed, penetration, and fight duration.

Instead of generating a new 1,000-team pool for every equipment rung:

1. Retain the selected discovery finalists.
2. Re-run only those finalists at representative low, middle, and high equipment anchors.
3. Measure finalist overlap, rank movement, score spread, and classification changes.
4. Trigger a context-specific rediscovery only when an explicit stability threshold fails.

This makes equipment sensitivity an evidence-based exception rather than a default multiplication of every expensive discovery stage.

## Current discovery parameters

Initial values should be benchmarked, but a practical starting point is:

| Parameter | Retired full-roster flow | Current default |
| --- | ---: | ---: |
| Candidate pool | 1,000 | 500 |
| Screening battles per seed | 25,000 | 10,000 |
| Seeds | 3 | 3 |
| Finalists | 100 | 24 |
| Finalist battles per pairing and seed | 10 | 34 |
| Direct samples per finalist pair | 30 | 102 |
| Production qualification samples | None | 10 per finalist/floor |
| Generated complete profiles | Variable | 10 per scenario |

With 24 finalists, one seed has 276 pairings instead of 4,950. Across three seeds and ten battles per pairing, the finalist stage falls from 148,500 to 8,280 battles.

Five smaller structural discovery audits plus bounded qualification reduce discovery from the retired multi-million-battle design to 380,760 discovery battles plus approximately 3,600 default qualification battles. Smoke and certification then execute only the ten selected complete profiles per floor and the canonical cohorts.

## Implemented contract and architecture changes

### Separate source discovery from target progression

The source discovery context contains:

- Party size.
- Essence count.
- Reference equipment context.
- Audit content hash.
- Seeds and statistical thresholds.

The separate target profile scenario contains:

- Content type.
- Floor or scenario identity.
- Required expedition slots.
- Party count.
- Equipment tier, rarity, and quality.
- Target equipment curve/profile.
- Essence count.

Profile provenance records both contexts without implying that the discovery equipment is the final equipment.

### Represent parties and expeditions separately

The catalog distinguishes:

- `PartyProfile`: exactly five ordered character profiles and their selection family.
- `ExpeditionProfile`: one to three party-profile references materialized for one exact floor scenario.

Expedition identity should include the ordered party-profile IDs and the exact target scenario. Party profile identity should remain stable across floors when its Essence composition is unchanged.

### Preserve party number during validation

The profile materializer assigns and preserves the production party number for every expedition slot:

- Slots 1–5 to party 1.
- Slots 6–10 to party 2.
- Slots 11–15 to party 3.

Catalog validation reconstructs and compares these assignments.

### Update campaign planning

The audit campaign planner groups discovery work by:

- Party size, fixed at five for World Tower.
- Essence count.
- Reference equipment profile when genuinely different.

It does not group expensive discovery by target floor, expedition size, equipment tier, rarity, or quality.

After discovery, the campaign:

1. Generate the party-profile libraries.
2. Materialize exact floor variants.
3. Compose deterministic expedition profiles.
4. Validate the catalog.
5. Runs candidate smoke and certification when verification is enabled.

### Update certification selection

The shadow and certification runners select exact floor expedition profiles composed from party-profile evidence and materialized for the exact roster size.

Exact-size fail-closed behavior must remain. The redesign changes how a valid full expedition is authored; it must not reintroduce cloning or silent extrapolation inside the certification runner.

## Testing requirements

### Party-boundary tests

- `AllAllies` from party 1 never affects parties 2 or 3.
- Lowest-health and random-ally selectors remain inside the source party.
- Guardian enemy targeting can select participants from any party.
- Debuffs applied by separate parties affect the same guardian.
- Stagger contribution aggregates across every party.
- Complete encounter victory and timeout remain expedition-wide.

### Discovery tests

- World Tower discovery always uses exactly five participants.
- Every required Essence count produces a distinct discovery context.
- Tier, rarity, quality, and target floor do not duplicate discovery work.
- Identical seeds and inputs produce identical party libraries.
- Reduced finalist selection retains diversity and evidence thresholds.

### Materialization tests

- One discovery party can materialize at multiple legal target rungs.
- Exact equipment, Power Rating, prepared attributes, abilities, and tags match the target floor context.
- Source discovery provenance remains unchanged across target materializations.
- Multi-party expeditions assign correct party numbers.

### Certification tests

- Five-slot floors use one party.
- Ten-slot floors use exactly two parties.
- Fifteen-slot floors use exactly three parties.
- Missing party or expedition coverage fails closed.
- Certification runs the complete expedition through `WorldTowerCombatRuntimeFactory`.
- Common seed manifests are applied to every compared expedition and canonical cohort.

### Performance tests

- Report discovery battles separately from production certification battles.
- Benchmark each Essence-count discovery context.
- Assert or monitor the configured maximum finalist pairing count.
- Report total campaign wall time and resumable stage progress.

## Treatment of historical campaigns

The retired thirteen-context campaign's completed artifacts remain useful diagnostic and equipment-sensitivity evidence. In particular, comparable ten-character/seven-Essence audits provide historical evidence about ranking stability across equipment rungs.

They should not become the final approved World Tower profile source because the 10/15-person audit model does not reproduce production party-local allied targeting.

That legacy campaign was cancelled after four completed audits; its artifacts were retained and its monitor was paused. The later schema-3 and pre-role-aware schema-4 campaigns are also historical. The required operational action now is a fresh fingerprint-contract-3 campaign, followed by review and source-controlled promotion only if certification passes.

## Acceptance criteria and current status

The architecture satisfies criteria 1–7 below. Criteria 8–11 still require a fresh current-contract evidence run:

1. No World Tower Essence discovery audit contains more than five characters.
2. Every currently required Essence count has one reusable, evidence-backed party library.
3. Source discovery equipment is independent from target floor equipment.
4. Five-, ten-, and fifteen-slot expedition profiles contain one, two, and three explicit parties respectively.
5. Production party numbers are preserved through materialization, validation, and execution.
6. Allied effects cannot cross party boundaries in profile certification.
7. All parties still interact correctly with the shared guardian, stagger, threat, and outcome.
8. **Pending evidence:** every floor has five to ten diversified complete expedition profiles.
9. **Pending evidence:** the complete catalog passes stale-content, legality, identity, preparation, Power Rating, context-qualification, and coverage validation.
10. **Pending evidence:** the 100-sample certification passes before approval, and release evidence uses 500–1,000 samples.
11. **Pending measurement:** a full role-aware discovery, qualification, and pull-request certification round completes within an operationally acceptable time budget.

## Conclusion

The five-character party is the correct reusable unit for Essence discovery. The complete expedition remains the correct unit for World Tower outcome validation.

Separating those two responsibilities removes inaccurate 10/15-person ally behavior from the discovery stage, avoids millions of redundant mirror battles, preserves exact per-floor equipment and Power Rating, and still verifies every real multi-party interaction through the production Tower runtime.
